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
def data(r): return r['data'] if isinstance(r, dict) and 'data' in r else r

ADMIN = jwt('66666', 'ADMIN'); OP = jwt('55555', 'OPERATEUR')
# Clôture de nettoyage : seul un administrateur peut forcer la fin d'un OF d'assemblage (contrôles de fin : e2e64).
ADMIN_SUPERVISION = jwt('66666', 'ADMIN')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])

# --- Lecture des plans de démonstration migrés ---
st, r = call(ADMIN, 'GET', '/DocumentVerifMachine?machineCode=MAS19')
check('plan MAS19 relu', st == 200 and len(r) == 1, (st, r))
mas19 = r[0] if st == 200 and r else {}
lignes = mas19.get('lignes', [])
check('lignes, échéances et matrice présentes', lignes and lignes[0]['libelleRisque'] and lignes[0]['echeances']
      and lignes[0]['echeances'][0]['matricePieces'], lignes[:1])
st, r = call(ADMIN, 'GET', '/DocumentVerifMachine')
check('liste complète = 6 plans de démo', st == 200 and len(r) == 6, (st, len(r) if isinstance(r, list) else r))

# --- Création / mise à jour d'un plan (machine MAS20 sans plan) ---
fam = sql("SELECT TOP 1 Id FROM Ref_FamilleCorps"); per = sql("SELECT TOP 1 Id FROM PeriodiciteMachine")
piece = sql("SELECT TOP 1 Id FROM PieceReference"); fam_dto = '11111111-1111-1111-1111-111111111111'
demande = {'nom': 'Vérification MAS20', 'machineCode': 'MAS20', 'refFormulaireCodeReference': 'FE-VM-MAS20',
    'familles': [{'id': fam_dto, 'refFamilleCorpsId': fam, 'ordreAffiche': 1}],
    'lignesConformite': [{'ordreAffiche': 1, 'typeLigne': 'CONFORMITE', 'libelleRisque': 'Pièce conforme', 'libelleMethode': 'Stations 2-4', 'echeances': []}],
    'lignesRisques': [{'ordreAffiche': 2, 'typeLigne': 'RISQUE', 'libelleRisque': 'Fuite', 'libelleMethode': 'Test 26 bar',
        'echeances': [{'ordreAffiche': 1, 'periodiciteMachineId': per, 'matricePieces': [{'familleId': fam_dto, 'roleVerif': 'PRC', 'pieceRefId': piece}]}]}]}
st, r = call(ADMIN, 'POST', '/DocumentVerifMachine', demande)
vm_id = (r or {}).get('id') if isinstance(r, dict) else None
check('création plan MAS20', st in (200, 201) and vm_id, (st, r))
check('stocké en VERIF_MACHINE, lignes sans section', sql(
    f"SELECT e.TypeDocumentCode+'|'+e.MachineCode+'|'+CAST(COUNT(l.Id) AS VARCHAR) FROM Document_Entete e JOIN Document_Ligne l ON l.EnteteId=e.Id AND l.SectionId IS NULL WHERE e.Id='{vm_id}' GROUP BY e.TypeDocumentCode, e.MachineCode")
      == 'VERIF_MACHINE|MAS20|2')
check('matrice reliée à la famille du plan', sql(
    f"SELECT COUNT(*) FROM Document_VerifMachine_MatricePiece m JOIN Document_VerifMachine_Famille f ON f.Id=m.FamilleId WHERE f.EnteteId='{vm_id}'") == '1')
st, r = call(ADMIN, 'GET', f'/DocumentVerifMachine/{vm_id}')
risque = [l for l in r.get('lignes', []) if l['typeLigne'] == 'RISQUE'] if st == 200 else []
check('relecture : libellé et méthode du risque', risque and (risque[0]['libelleRisque'], risque[0]['libelleMethode']) == ('Fuite', 'Test 26 bar'), (st, r))
demande['lignesRisques'][0]['libelleRisque'] = 'Fuite modifiée'
demande['rowVersion'] = call(ADMIN, 'GET', f'/DocumentVerifMachine/{vm_id}')[1]['rowVersion']
st, r = call(ADMIN, 'PUT', f'/DocumentVerifMachine/{vm_id}', demande)
check('mise à jour du plan', st in (200, 204), (st, r))
check('lignes remplacées', sql(f"SELECT COUNT(*) FROM Document_Ligne WHERE EnteteId='{vm_id}' AND LibelleAffiche='Fuite modifiée'") == '1')

# --- Hub ---
st, r = call(ADMIN, 'GET', '/hub/modeles'); r = data(r)
check('hub : plan VM listé une fois en VM', st == 200 and [m['category'] for m in r if m['id'] == vm_id] == ['VM'], [m for m in r if m['id'] == vm_id])
st, r = call(ADMIN, 'PUT', f"/hub/modeles/VM/{mas19.get('id')}/statut?statut=ACTIF")
check('hub : statut VM modifiable', st == 200, (st, r))

# --- Unicité : un seul plan ACTIF par machine (trigger) ---
st, r = call(ADMIN, 'PUT', f'/hub/modeles/VM/{vm_id}/statut?statut=ARCHIVE'); check('archivage plan MAS20', st == 200, (st, r))
sql(f"UPDATE Document_Entete SET MachineCode='MAS19' WHERE Id='{vm_id}'")
check('plan archivé déplacé sur MAS19', sql(f"SELECT MachineCode FROM Document_Entete WHERE Id='{vm_id}'") == 'MAS19')
st, r = call(ADMIN, 'PUT', f'/hub/modeles/VM/{vm_id}/statut?statut=ACTIF')
check('deuxième plan ACTIF sur MAS19 refusé (409)', st == 409, (st, r))
sql(f"UPDATE Document_Entete SET MachineCode='MAS20' WHERE Id='{vm_id}'")

# --- Exécution opérateur : démarrage OF, vérification machine sur le poste ---
st, r = call(OP, 'POST', '/operateur/of/start-assemblage', {'numeroOf': 'OF2607ASS01', 'posteCodes': ['PAS71'], 'equipe': 'E1'})
check('démarrage assemblage', st == 200, (st, r)); exec_id = (r or {}).get('id') if isinstance(r, dict) else None
st, machines = call(OP, 'GET', '/operateur/poste/PAS71/machines')
avec_plan = [m for m in (machines or []) if m.get('hasDemarragePlan')] if st == 200 else []
check('machines du poste avec plan de démarrage', st == 200 and avec_plan, (st, machines))
if exec_id and avec_plan:
    machine = avec_plan[0]['codeMachine']
    st, r = call(OP, 'POST', f'/operateur/of/{exec_id}/verif-machine/init', {'posteCode': 'PAS71', 'machineCode': machine})
    check(f'init vérification {machine}', st == 200, (st, r)); statut_id = (r or {}).get('id') if isinstance(r, dict) else None
    plan = call(ADMIN, 'GET', f'/DocumentVerifMachine?machineCode={machine}')[1][0]
    echeance = next(e for l in plan['lignes'] for e in l['echeances'])
    st, r = call(OP, 'POST', '/operateur/verif-machine/save', {'execControleDocumentStatutId': statut_id,
        'reponses': [{'documentVerifMachineEcheanceId': echeance['id'], 'dateExecution': time.strftime('%Y-%m-%dT%H:%M:%S'), 'conforme': True}]})
    check('réponse enregistrée sur une échéance', st == 204, (st, r))
    check('réponse attribuée au matricule du JWT', sql(
        f"SELECT MatriculeOperateur FROM Exec_VerifMachine_Reponse WHERE DocumentVerifMachineEcheanceId='{echeance['id']}'") == '55555')
    st, r = call(OP, 'GET', f'/operateur/verif-machine/statut/{statut_id}')
    check('relecture des réponses', st == 200 and len(r.get('reponses', [])) == 1, (st, r))
    st, r = call(ADMIN_SUPERVISION, 'POST', f'/operateur/of/{exec_id}/close'); check('clôture OF', st == 204, (st, r))

st, r = call(OP, 'POST', '/DocumentVerifMachine', demande); check('opérateur ne crée pas de plan (403)', st == 403, st)
print(f'\n{ok} OK / {ko} KO')
