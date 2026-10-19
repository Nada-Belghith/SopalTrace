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

SUP = jwt('44444', 'SUPERVISEUR_QUALITE'); ADMIN = jwt('66666', 'ADMIN'); OP = jwt('55555', 'OPERATEUR')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])

G0 = '00000000-0000-0000-0000-000000000001'

# ── Faille 1 : l'opérateur ne modifie plus les plans ──
check("plus de modification d'une ligne de plan par l'opérateur", call(OP, 'PUT', f'/operateur/plan/ligne/{G0}', {'libelleAffiche': 'x'})[0] in (404, 405))
check("plus de modification du plan d'échantillonnage depuis l'exécution", call(OP, 'PUT', f'/ExecEchantillonnage/{G0}', {})[0] in (404, 405))

# ── Faille 2 : validation serveur ──
st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'CTRL_POSTE', 'nom': 'X' * 300, 'posteCode': 'PAS71', 'sections': []})
check('nom trop long refusé (400) avant la base', st == 400, (st, r))

# ── Faille 3 : écritures du référentiel réservées à la qualité, sans détail d'exception ──
st, r = call(OP, 'POST', '/referentiels/periodicites', {'nbrPieces': 4, 'unite': 'HEURE'})
check("l'opérateur ne crée pas de périodicité (403)", st == 403, (st, r))

# ── Faille 4 : règles portées par les données ──
st, r = call(SUP, 'GET', '/referentiels/verif-machine'); machines = {m['code']: m for m in data(r).get('machines', [])} if st == 200 else {}
check('MAS22 : tableaux étalon séparés (donnée machine)', machines.get('MAS22', {}).get('tablesEtalonSeparees') is True, machines.get('MAS22'))
check('BEE47 : fuite en Pa, modèle BEE, sans conformité', (machines.get('BEE47', {}).get('fuiteEnPa'), machines.get('BEE47', {}).get('verifModele'), machines.get('BEE47', {}).get('sansConformite')) == (True, 'BEE', True), machines.get('BEE47'))
check('MAS26 : colonnes par tableau', machines.get('MAS26', {}).get('colonnesParTableau') is True, machines.get('MAS26'))
check('machine sans modèle : formulaire générique', machines.get('TRF14', {}).get('verifModele') is None, machines.get('TRF14'))
st, r = call(SUP, 'GET', '/referentiels/fabrication'); familles = {f['code']: f for f in data(r).get('famillesProduit', [])} if st == 200 else {}
check('famille soupape : plans par poste (donnée famille)', familles.get('RBGFA_SOUPAPE', {}).get('planParPoste') is True and familles.get('RBGFM', {}).get('planParPoste') is False, familles)
check('opérations : machine pour TRONC/ESTOMP/USINAG, cotes pour TRONC seulement',
      sql("SELECT STRING_AGG(Code+':'+CAST(AvecMachine AS CHAR(1))+CAST(AvecCotesTronconnage AS CHAR(1)), ',') WITHIN GROUP (ORDER BY Code) FROM Operation") == 'ASS:00,ESTOMP:10,TRONC:11,USINAG:10')

# Contrôle au poste : document de la famille du produit fini, sinon générique ; jamais celui d'une autre famille.
st, r = call(OP, 'POST', '/operateur/of/start-assemblage', {'numeroOf': 'OF2607ASS02', 'posteCodes': ['PAS71'], 'equipe': 'E1'})
exec_id = (r or {}).get('id') if isinstance(r, dict) else None
check('démarrage OF soupape', st == 200, (st, r))
def ctrl(nom, famille, code):
    st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'CTRL_POSTE', 'nom': nom, 'posteCode': 'PAS71', 'familleProduitFiniCode': famille,
        'refFormulaireCodeReference': code, 'sections': [{'ordreAffiche': 1, 'libelleSection': 'Lignes', 'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Fuite'}]}]})
    doc = data(r).get('id') if isinstance(data(r), dict) else None
    st2, _ = call(SUP, 'PUT', f'/hub/modeles/RC/{doc}/statut?statut=ACTIF')
    check(f'document {nom} créé et actif', st in (200, 201) and st2 == 200, (st, r, st2))
ctrl('CP manuel', 'RBGFM', 'FE-RC-PAS71-RBGFM')
st, r = call(OP, 'GET', f'/ExecRcPoste/{exec_id}?posteCode=PAS71&equipe=E1')
check("le document d'une autre famille n'est jamais pris (404)", st == 404, (st, r.get('planNom') if isinstance(r, dict) else r))
ctrl('CP générique', None, 'FE-RC-PAS71')
ctrl('CP soupape', 'RBGFA_SOUPAPE', None)       # formulaire de son champ d'application : FE-RC-PAS71_SOUPAPE
st, r = call(OP, 'GET', f'/ExecRcPoste/{exec_id}?posteCode=PAS71&equipe=E1')
check('document de la famille prioritaire sur le générique', st == 200 and (r.get('planNom') or '').startswith('CP soupape'), (st, r.get('planNom') if isinstance(r, dict) else r))
check('les trois documents restent actifs ensemble', sql("SELECT COUNT(*) FROM Document_Entete WHERE TypeDocumentCode='CTRL_POSTE' AND PosteCode='PAS71' AND Statut='ACTIF'") == '3')

# ── Faille 5 : l'ENCF est un document du poste, visible dans la liste des documents de l'OF ──
# Formulaire des résultats en cours de fabrication de la famille de l'article : il l'emporte sur le formulaire générique.
famille = sql("SELECT p.FamilleProduitFiniCode FROM MFGHEAD_OrdreFabrication o JOIN ProduitFini p ON p.CodeArticle = o.CodeArticle WHERE o.NumeroOF='OF2607ASS02'")
st, r = call(SUP, 'POST', '/referentiels/formulaires', {'role': 'RESULTAT_CONTROLE_CF', 'code': 'FE-RC-ENCF-60', 'familleProduitFiniCode': famille})
form_famille = str(data(r)).lower() if st == 200 else ''
st, r = call(OP, 'GET', '/exec/encf/by-of/OF2607ASS02/poste/PAS71')
check('ENCF ouvert par l\'opérateur depuis le poste', st == 200, (st, r))
check('formulaire de la famille de l\'article (type de robinet compris) préféré au formulaire générique', bool(form_famille) and sql(
    f"SELECT LOWER(CAST(FormId AS VARCHAR(36))) FROM Exec_ControleDocumentStatut WHERE ExecControleOfId='{exec_id}' AND TypeDocument='RESULTAT_CONTROLE_CF'") == form_famille,
    (famille, form_famille))
st, r = call(OP, 'GET', f'/Operateur/of/{exec_id}/assemblage-documents')
check("l'ENCF figure dans les documents de l'OF (catégorie de l'assistant)", st == 200 and any(d['typeDocument'] == 'RESULTAT_CONTROLE_CF' and d['posteCode'] == 'PAS71' for d in r), (st, r))

print(f'\n{ok} OK / {ko} KO')
