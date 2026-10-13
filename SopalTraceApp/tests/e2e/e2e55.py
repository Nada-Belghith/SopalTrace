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
    else: ko += 1; print('KO ', label, str(info)[:400])

tous = sql("SELECT Id FROM TypeSection WHERE TypeNormalise='TOUS_PIECES'")
per = sql("SELECT TOP 1 Id FROM Periodicite WHERE Unite='HEURE' AND NbrPieces=4")
for of in ('OF26TRN001', 'OF26TRN002'):
    sql(f"IF NOT EXISTS (SELECT 1 FROM MFGHEAD_OrdreFabrication WHERE NumeroOF='{of}') INSERT INTO MFGHEAD_OrdreFabrication "
        f"(NumeroOF,CodeArticle,QuantitePrevue,QuantiteLancee,QuantiteReelle,StatutOF) VALUES ('{of}','2630201.1.9',100,100,0,'EN_COURS')")

# Plan sans cote : la première exécution la fixe.
st, r = call(ADMIN, 'POST', '/PlanFabrication', {'nom': '2630201.1.9', 'codeArticleSageVersionne': '2630201.1.9', 'operationCode': 'TRONC', 'statut': 'ACTIF',
    'sections': [{'ordreAffiche': 1, 'libelleSection': 'Production', 'typeSectionId': tous, 'periodiciteId': per,
                  'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Longueur', 'moyenTexteLibre': 'Réglet'},
                             {'ordreAffiche': 2, 'libelleAffiche': 'Diamètre', 'moyenTexteLibre': 'Pied à coulisse'}]}]})
check('plan de tronçonnage sans cote', st == 200, (st, r))
st, r = call(OP, 'GET', '/operateur/plan/existe?articleCode=2630201.1.9&operationCode=TRONC')
check('aucune cote connue avant le premier OF', st == 200 and r['existe'] and r['longueur'] is None and r['diametre'] is None, (st, r))

st, r = call(OP, 'POST', '/operateur/of/start', {'numeroOf': 'OF26TRN001', 'operationCode': 'TRONC', 'machineCode': 'TRN26', 'longueur': 123.5, 'diametre': 32})
exec1 = (r or {}).get('id') if isinstance(r, dict) else None
check('démarrage avec longueur et diamètre', st == 200, (st, r))
check('cotes enregistrées sur l\'exécution', sql(
    f"SELECT CAST(LongueurNominale AS VARCHAR)+'|'+CAST(DiametreNominal AS VARCHAR) FROM Exec_ControleOF WHERE Id='{exec1}'") == '123.500|32.000')
check("le plan qualité n'est pas modifié (aucune cote écrite dans ses lignes)", sql(
    "SELECT COUNT(*) FROM Document_Ligne l JOIN Document_Entete e ON e.Id=l.EnteteId "
    "WHERE e.CodeArticleSageVersionne='2630201.1.9' AND l.LibelleAffiche LIKE '%=%'") == '0')
call(OP, 'GET', f'/operateur/of/{exec1}/alertes-actives')
# Le serveur crée un contrôle quand il devient dû : on avance le temps d'un intervalle et demi.
sql(f"UPDATE Exec_ProductionPeriod SET StartedAt = DATEADD(ms, -CAST(1.5 * IntervalMinutes * 60000 AS INT), StartedAt) WHERE ExecControleOfId='{exec1}' AND EndedAt IS NULL")
st, r = call(OP, 'GET', f'/operateur/of/{exec1}/alertes-actives')
libelles = [c['libelle'] for t in (r or []) for o in t.get('occurrences', t.get('alertes', [])) for c in o.get('caracteristiques', [])] if st == 200 else []
check('pendant l\'OF, les lignes affichent les cotes saisies', 'Longueur = 123.5' in libelles and 'Diamètre = 32' in libelles, (st, libelles[:4], str(r)[:300]))
st, r = call(OP, 'POST', f'/operateur/of/{exec1}/close'); check('clôture du premier OF', st == 204, (st, r))

st, r = call(OP, 'GET', '/operateur/plan/existe?articleCode=2630201.1.9&operationCode=TRONC')
check('OF suivant : cotes pré-remplies depuis le dernier OF', st == 200 and (r['longueur'], r['diametre']) == (123.5, 32), (st, r))

# Une cote écrite dans le plan par la qualité reste prioritaire.
st, r = call(ADMIN, 'POST', '/PlanFabrication/nouvelle-version', {'ancienId': call(ADMIN, 'GET', '/PlanFabrication?codeArticleSageVersionne=2630201.1.9')[1][0]['id'],
    'codeArticleSageVersionne': '2630201.1.9', 'operationCode': 'TRONC', 'statut': 'ACTIF',
    'sections': [{'ordreAffiche': 1, 'libelleSection': 'Production', 'typeSectionId': tous, 'periodiciteId': per,
                  'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Longueur = 120', 'moyenTexteLibre': 'Réglet'}]}]})
st, r = call(OP, 'GET', '/operateur/plan/existe?articleCode=2630201.1.9&operationCode=TRONC')
check('cote du plan (qualité) prioritaire sur celle de l\'OF précédent', st == 200 and r['longueur'] == 120, (st, r))

print(f'\n{ok} OK / {ko} KO')
