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
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, info)

ARTICLE, OF = '2623201-1-7', 'OF26FAB001'
sql(f"IF NOT EXISTS (SELECT 1 FROM MFGHEAD_OrdreFabrication WHERE NumeroOF='{OF}') "
    f"INSERT INTO MFGHEAD_OrdreFabrication (NumeroOF,CodeArticle,QuantitePrevue,QuantiteLancee,QuantiteReelle,StatutOF) VALUES ('{OF}','{ARTICLE}',100,100,0,'EN_COURS')")
reglage = sql("SELECT Id FROM TypeSection WHERE TypeNormalise='REGLAGE'")
section = lambda: [{'ordreAffiche': 1, 'libelleSection': 'Contrôle aux réglages', 'typeSectionId': reglage,
    'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Longueur = 100', 'moyenTexteLibre': 'Réglet', 'estCritique': True}]}]

# --- Modèle de fabrication (générique) ---
st, r = call(ADMIN, 'POST', '/ModeleFabrication', {'code': 'MODCORPS', 'libelle': 'Modèle corps', 'typeRobinetCode': None,
    'natureComposantCode': 'CORPS', 'operationCode': 'TRONC', 'sections': section()})
check('création modèle', st == 200, (st, r)); modele_id = (r or {}).get('id') if isinstance(r, dict) else None
st, r = call(ADMIN, 'GET', f'/ModeleFabrication/{modele_id}')
check('relecture modèle (code, libellé suffixé, 1 section, 1 ligne)', st == 200 and r['code'] == 'MODCORPS'
      and r['libelle'].startswith('Modèle corps V') and len(r['sections']) == 1 and len(r['sections'][0]['lignes']) == 1, (st, str(r)[:300]))
st, r = call(ADMIN, 'GET', '/ModeleFabrication?natureComposantCode=CORPS&operationCode=TRONC')
check('filtre modèles', st == 200 and [m['id'] for m in r] == [modele_id], (st, str(r)[:200]))
check('modèle stocké en MODELE_FAB / GENERIQUE', sql(f"SELECT TypeDocumentCode+'|'+Portee FROM Document_Entete WHERE Id='{modele_id}'") == 'MODELE_FAB|GENERIQUE')

# --- Plan de fabrication (spécifique au code article) ---
st, r = call(ADMIN, 'POST', '/PlanFabrication', {'nom': ARTICLE, 'codeArticleSageVersionne': ARTICLE, 'operationCode': 'TRONC',
    'statut': 'ACTIF', 'modeleSourceId': modele_id, 'sections': section()})
check('création plan', st == 200, (st, r)); plan_id = (r or {}).get('id') if isinstance(r, dict) else None
st, r = call(ADMIN, 'GET', f'/PlanFabrication/{plan_id}')
check('relecture plan (modèle source, article, sections)', st == 200 and r['modeleSourceId'] == modele_id
      and r['codeArticleSageVersionne'] == ARTICLE and len(r['sections'][0]['lignes']) == 1, (st, str(r)[:300]))
check('plan stocké en PLAN_FAB / SPECIFIQUE avec source', sql(
    f"SELECT TypeDocumentCode+'|'+Portee+'|'+CAST(DocumentSourceId AS VARCHAR(36)) FROM Document_Entete WHERE Id='{plan_id}'").lower()
      == f'plan_fab|specifique|{modele_id}'.lower())
check('lignes rattachées à l\'entête du plan', sql(f"SELECT COUNT(*) FROM Document_Ligne WHERE EnteteId='{plan_id}'") == '1')
check('auteur = matricule du JWT', sql(f"SELECT CreePar FROM Document_Entete WHERE Id='{plan_id}'") == '66666')

# --- Hub : chaque document listé une seule fois, dans sa catégorie ---
st, r = call(ADMIN, 'GET', '/hub/modeles'); r = r['data']
check('hub modèles : modèle listé une fois en FAB', st == 200 and [m['category'] for m in r if m['id'] == modele_id] == ['FAB'], (st, [m for m in r if m['id'] == modele_id]))
check('hub modèles : plan absent', st == 200 and not any(m['id'] == plan_id for m in r))
st, r = call(ADMIN, 'GET', '/hub/plans'); r = r['data']
check('hub plans : plan listé une fois', st == 200 and sum(1 for p in r if p['id'] == plan_id) == 1, (st, str(r)[:200]))

# --- Exécution : démarrage d'un OF de fabrication sur ce plan ---
st, r = call(OP, 'POST', '/operateur/of/start', {'numeroOf': OF, 'operationCode': 'TRONC', 'machineCode': 'TRN26', 'matriculeOperateur': '55555'})
check('démarrage OF FAB', st == 200, (st, r)); exec_id = (r or {}).get('id') if isinstance(r, dict) else None
check('exécution liée au plan (FK Document_Entete) et en réglage', sql(
    f"SELECT LOWER(CAST(PlanSourceId AS VARCHAR(36)))+'|'+Statut FROM Exec_ControleOF WHERE Id='{exec_id}'") == f'{plan_id}|REGLAGE'.lower()
      .replace('reglage', 'REGLAGE'))
st, r = call(OP, 'POST', f'/operateur/of/{exec_id}/close'); check('clôture OF', st == 204, (st, r))

# --- Versionnement ---
st, r = call(ADMIN, 'POST', '/PlanFabrication/nouvelle-version', {'ancienId': plan_id, 'codeArticleSageVersionne': ARTICLE, 'operationCode': 'TRONC'})
check('nouvelle version du plan', st == 200, (st, r)); v2 = (r or {}).get('id') if isinstance(r, dict) else None
check('ancienne version archivée, nouvelle active', sql(f"SELECT Statut FROM Document_Entete WHERE Id='{plan_id}'") == 'ARCHIVE'
      and sql(f"SELECT Statut FROM Document_Entete WHERE Id='{v2}'") == 'ACTIF')

# --- Erreurs : plus de détails techniques renvoyés au client ---
st, r = call(ADMIN, 'POST', '/PlanFabrication/nouvelle-version', {'ancienId': '00000000-0000-0000-0000-000000000099'})
check('plan inconnu -> 404 sans trace technique', st == 404 and 'details' not in json.dumps(r) and ' at ' not in json.dumps(r), (st, r))
st, r = call(OP, 'GET', '/PlanFabrication'); check('opérateur interdit sur les plans', st == 403, st)

print(f'\n{ok} OK / {ko} KO')
