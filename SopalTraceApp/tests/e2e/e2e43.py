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

ADMIN = jwt('66666', 'ADMIN'); OP = jwt('55555', 'OPERATEUR')
# Clôture de nettoyage : seul un administrateur peut forcer la fin d'un OF d'assemblage (contrôles de fin : e2e64).
ADMIN_SUPERVISION = jwt('66666', 'ADMIN')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])
actifs = lambda: sql("SELECT COUNT(*) FROM Document_Entete WHERE TypeDocumentCode='PLAN_ECHANTILLONNAGE' AND Statut='ACTIF'")

P = '/documents-echantillonnage'
donnees = {'niveauControle': 'II', 'typePlan': 'SIMPLE', 'modeControle': 'NORMAL', 'valeurNqa': 1.5,
           'critereAcceptationAc': 1, 'critereRejetRe': 2, 'remarques': 'Plan de démo', 'legendeMoyens': '', 'refFormulaireCodeReference': 'FE-ECHAN-01'}

st, r = call(ADMIN, 'GET', P + '/actif'); check('aucun plan actif au départ', st == 200 and r['data'] is None, (st, r))
st, r = call(ADMIN, 'POST', P, dict(donnees, niveauControle='NIVEAU II'))
check('niveau hors référentiel refusé (400)', st == 400, (st, r))

st, r = call(ADMIN, 'POST', P, donnees); v0 = r.get('data') if isinstance(r, dict) else None
check('création plan', st == 200 and v0, (st, r))
check('stocké en PLAN_ECHANTILLONNAGE + SamplingPlan', sql(
    f"SELECT CASE WHEN e.Nom LIKE 'Plan d''_chantillonnage - V0' THEN 'NOM' ELSE e.Nom END+'|'+e.CreePar+'|'+s.InspectionLevelCode+'|'+CAST(s.AcceptanceNumber AS VARCHAR)+'/'+CAST(s.RejectionNumber AS VARCHAR) "
    f"FROM Document_Entete e JOIN SamplingPlan s ON s.DocumentId=e.Id WHERE e.Id='{v0}' AND e.TypeDocumentCode='PLAN_ECHANTILLONNAGE'")
      == "NOM|66666|II|1/2")
st, r = call(ADMIN, 'GET', P + '/actif'); d = r['data'] if st == 200 else {}
check('plan actif relu avec valeur NQA (plus 0)', d and d['id'] == v0 and d['valeurNqa'] == 1.5, (st, r))

version_v0 = call(ADMIN, 'GET', f'{P}/{v0}')[1]['data']['rowVersion']
st, r = call(ADMIN, 'PUT', f'{P}/{v0}', dict(donnees, modeControle='RENFORCE', rowVersion=version_v0))
check('correction en place', st == 200 and sql(f"SELECT InspectionSeverity FROM SamplingPlan WHERE DocumentId='{v0}'") == 'RENFORCE', (st, r))

st, r = call(ADMIN, 'POST', P + '/nouvelle-version', {'ancienId': v0, 'motifModification': 'Passage en NQA 2.5', 'donnees': dict(donnees, valeurNqa=2.5)})
v1 = r.get('data') if isinstance(r, dict) else None
check('nouvelle version', st == 200 and v1 and v1 != v0, (st, r))
check('ancienne archivée, un seul plan ACTIF', sql(f"SELECT Statut FROM Document_Entete WHERE Id='{v0}'") == 'ARCHIVE' and actifs() == '1')
check('motif de version conservé', sql(f"SELECT CommentaireVersion FROM Document_Entete WHERE Id='{v1}'") == 'Passage en NQA 2.5')

st, r = call(ADMIN, 'POST', P, donnees); v2 = r.get('data') if isinstance(r, dict) else None
check('création alors qu\'un plan est actif : remplacement', st == 200 and actifs() == '1'
      and sql(f"SELECT Statut FROM Document_Entete WHERE Id='{v2}'") == 'ACTIF', (st, r, actifs()))

st, r = call(ADMIN, 'PUT', f'/hub/plans/ECH/{v0}/statut?statut=ACTIF')
check('réactiver un plan archivé alors qu\'un autre est actif -> 409', st == 409 and actifs() == '1', (st, r))

st, r = call(ADMIN, 'GET', '/hub/modeles'); r = r['data'] if st == 200 else []
check('hub : plans listés une fois en ECH', [m['category'] for m in r if m['id'] in (v0, v1, v2)] == ['ECH'] * 3, [m for m in r if m['id'] in (v0, v1, v2)])

# --- Exécution opérateur ---
st, r = call(OP, 'POST', '/operateur/of/start-assemblage', {'numeroOf': 'OF2607ASS01', 'posteCodes': ['PAS71'], 'equipe': 'E1'})
check('démarrage assemblage', st == 200, (st, r)); exec_id = (r or {}).get('id') if isinstance(r, dict) else None
st, r = call(OP, 'GET', f'/ExecEchantillonnage/check-source/{exec_id}'); check('plan source disponible', st == 200, (st, r))
st, r = call(OP, 'POST', f'/ExecEchantillonnage/init/{exec_id}', {'posteCode': 'PAS71', 'nbPostes': 1, 'instrumentCodes': [], 'equipe': 'E1'})
check('initialisation échantillonnage', st == 200, (st, r))
st, r = call(OP, 'GET', f'/ExecEchantillonnage/{exec_id}?posteCode=PAS71')
check('échantillon calculé avec les paramètres du plan actif', st == 200 and r['niveauControle'] == 'II' and r['nqaValeur'] == 1.5
      and (r['critereAcceptationAc'], r['critereRejetRe']) == (1, 2) and r['effectifEchantillonA'] > 0, (st, r))
check('statut d\'exécution lié au plan actif', sql(
    f"SELECT LOWER(CAST(DocId AS VARCHAR(36))) FROM Exec_ControleDocumentStatut WHERE ExecControleOfId='{exec_id}' AND TypeDocument='ECHANTILLONNAGE'") == str(v2).lower())
st, r = call(ADMIN_SUPERVISION, 'POST', f'/operateur/of/{exec_id}/close'); check('clôture OF', st == 204, (st, r))

st, r = call(OP, 'POST', P, donnees); check('opérateur ne crée pas de plan (403)', st == 403, st)
print(f'\n{ok} OK / {ko} KO')
