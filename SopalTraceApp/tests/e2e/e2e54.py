import base64, hashlib, hmac, json, time, subprocess, urllib.request, urllib.error
from configuration import BASE, DB, SECRET, sql as _sql

def b64(d): return base64.urlsafe_b64encode(d).rstrip(b'=').decode()
def jwt(mat, role):
    h = b64(json.dumps({'alg': 'HS256', 'typ': 'JWT'}).encode()); now = int(time.time())
    p = b64(json.dumps({'sub': '00000000-0000-0000-0000-0000000' + mat, 'stamp': '11111111-2222-3333-4444-555555555555', 'matricule': mat,
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
        'iss': 'SopalTraceApi', 'aud': 'SopalTraceVueJs', 'iat': now, 'nbf': now, 'exp': now + 3600}).encode())
    return f'{h}.{p}.' + b64(hmac.new(SECRET, f'{h}.{p}'.encode(), hashlib.sha256).digest())
def call(tok, method, path, body=None):
    req = urllib.request.Request(BASE + path, method=method, data=json.dumps(body).encode() if body is not None else None,
        headers={'Authorization': 'Bearer ' + tok, 'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req) as r:
            t = r.read().decode(); return r.status, (json.loads(t) if t else None)
    except urllib.error.HTTPError as e:
        t = e.read().decode(); return e.code, (json.loads(t) if t.strip().startswith(('{', '[')) else t[:300])
def sql(q):
    return _sql(q)
def data(r): return r.get('data', r) if isinstance(r, dict) else r
def texte(r): return json.dumps(r, ensure_ascii=False)

SUP = jwt('44444', 'SUPERVISEUR_QUALITE'); OP = jwt('55555', 'OPERATEUR')
# Clôture de nettoyage : seul un administrateur peut forcer la fin d'un OF d'assemblage (contrôles de fin : e2e64).
ADMIN_SUPERVISION = jwt('66666', 'ADMIN')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])

st, r = call(OP, 'GET', '/exec/encf/by-of/OF2607ASS01/poste/PAS71')
check('ENCF refusé tant que l\'OF n\'est pas démarré (400, message clair)', st == 400 and 'Démarrez' in texte(r), (st, r))

st, r = call(OP, 'POST', '/operateur/of/start-assemblage', {'numeroOf': 'OF2607ASS01', 'posteCodes': ['PAS71'], 'equipe': 'E1'})
exec_id = (r or {}).get('id') if isinstance(r, dict) else None
check('démarrage OF assemblage', st == 200, (st, r))
# Fiche de réponse : aucun document « résultat CF » à créer ; son formulaire (famille, type de robinet, poste) est fixé à
# sa création et ses lignes viennent du plan d'assemblage du poste suivi par l'OF (ici, le plan de la famille de l'article).
famille = sql("SELECT p.FamilleProduitFiniCode FROM MFGHEAD_OrdreFabrication o JOIN ProduitFini p ON p.CodeArticle = o.CodeArticle WHERE o.NumeroOF='OF2607ASS01'")
st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'PLAN_ASS', 'nom': f'Plan {famille} E2E 54', 'familleProduitFiniCode': famille,
    'sections': [{'ordreAffiche': 1, 'libelleSection': 'Contrôles', 'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Aspect'}]}]})
plan_famille = str((data(r) or {}).get('id', '')).lower() if st in (200, 201) and isinstance(data(r), dict) else ''
check("plan d'assemblage de la famille de l'article créé (actif)", bool(plan_famille)
      and sql(f"SELECT Statut FROM Document_Entete WHERE Id='{plan_famille}'") == 'ACTIF', (famille, st, r))
st, r = call(OP, 'GET', '/exec/encf/by-of/OF2607ASS01/poste/PAS71')
form_encf = sql("SELECT LOWER(CAST(Id AS VARCHAR(36))) FROM Ref_Formulaire WHERE CodeReference='FE-RC-ENCF' AND Statut <> 'ARCHIVE'")
check('ENCF ouvert sur la vraie exécution de l\'OF, sans document « résultat CF » à créer', st == 200 and r['id'] == exec_id, (st, r))
check('aucune fausse exécution créée (une seule pour l\'OF)', sql("SELECT COUNT(*) FROM Exec_ControleOF WHERE NumeroOF='OF2607ASS01'") == '1')
check('fiche de réponse : son formulaire (FE-RC-ENCF) fixé à sa création, aucun document appliqué', sql(
    f"SELECT CONCAT(LOWER(CAST(FormId AS VARCHAR(36))), '|', ISNULL(CAST(DocId AS VARCHAR(36)), '-')) FROM Exec_ControleDocumentStatut "
    f"WHERE ExecControleOfId='{exec_id}' AND TypeDocument='RESULTAT_CONTROLE_CF'") == f'{form_encf}|-')
plan = (r or {}).get('planSourceId') if isinstance(r, dict) else None
check("lignes de la fiche : plan d'assemblage du poste suivi par l'OF (celui de la famille), lisible par le formulaire ENCF",
      str(plan or '').lower() == plan_famille and call(OP, 'GET', f'/Document/{plan}')[0] == 200, (plan, plan_famille))

enc = dict(r, statut='CLOTURE',
    piecesTypes=[{'resultat': 'C', 'remarque': 'Première pièce', 'matriculeOperateur': 'PIRATE'}],
    tranches=[{'trancheHoraire': '08h-09h', 'heureDebut': '2026-10-09T08:00:00', 'heureFin': '2026-10-09T09:00:00', 'resultatFinal': 'C', 'matriculeApprobateur': 'PIRATE'}])
st, r = call(OP, 'POST', '/exec/encf', enc)
check('enregistrement ENCF', st == 200 and len(r['piecesTypes']) == 1 and len(r['tranches']) == 1, (st, r))
check('pièce type et tranche au matricule du jeton (matricule du client ignoré)', sql(
    f"SELECT COUNT(*) FROM Exec_PieceType WHERE ExecControleOFId='{exec_id}' AND MatriculeOperateur='55555'") == '1' and sql(
    f"SELECT COUNT(*) FROM Exec_ControleTranche WHERE ExecControleOFId='{exec_id}' AND MatriculeApprobateur='55555'") == '1')
check('l\'ENCF ne clôture pas l\'OF', sql(f"SELECT Statut FROM Exec_ControleOF WHERE Id='{exec_id}'") != 'CLOTURE')

piece = r['piecesTypes'][0]
st, r = call(OP, 'POST', '/exec/encf', dict(r, piecesTypes=[dict(piece, resultat='NC', remarque='Bavure')], tranches=[]))
check('correction de la pièce type en place, tranche conservée (aucune preuve supprimée)', st == 200 and sql(
    f"SELECT Resultat FROM Exec_PieceType WHERE Id='{piece['id']}'") == 'NC' and len(r['tranches']) == 1, (st, r))
st, r = call(OP, 'POST', '/exec/encf', dict(r, piecesTypes=[{'resultat': 'X'}]))
check('résultat de pièce type invalide refusé (400)', st == 400, (st, r))

st, r = call(ADMIN_SUPERVISION, 'POST', f'/operateur/of/{exec_id}/close'); check('clôture OF', st == 204, (st, r))
st, r = call(OP, 'POST', '/exec/encf', dict(enc, id=exec_id, piecesTypes=[], tranches=[]))
check('ENCF non modifiable après clôture de l\'OF (409)', st == 409, (st, r))

print(f'\n{ok} OK / {ko} KO')
