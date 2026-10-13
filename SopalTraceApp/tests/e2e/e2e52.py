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
# Clôture de nettoyage : seul un administrateur peut forcer la fin d'un OF d'assemblage (contrôles de fin : e2e64).
ADMIN_SUPERVISION = jwt('66666', 'ADMIN')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])

# ── Contrôle au poste ──
st, r = call(OP, 'POST', '/operateur/of/start-assemblage', {'numeroOf': 'OF2607ASS01', 'posteCodes': ['PAS71'], 'equipe': 'E1'})
exec_id = (r or {}).get('id') if isinstance(r, dict) else None
check('démarrage OF assemblage', st == 200, (st, r))
st, r = call(OP, 'GET', f'/ExecRcPoste/{exec_id}?posteCode=PAS71&equipe=E1')
check('sans plan de contrôle poste actif : 404 (plus de faux document)', st == 404, (st, r))

st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'CTRL_POSTE', 'nom': 'Contrôle poste PAS71', 'posteCode': 'PAS71',
    'sections': [{'ordreAffiche': 1, 'libelleSection': 'Lignes de Contrôle', 'lignes': [
        {'ordreAffiche': 1, 'libelleAffiche': 'Fuite', 'machineCode': 'MAS19'}, {'ordreAffiche': 2, 'libelleAffiche': 'Rayure'}]}]})
check('plan de contrôle poste créé', st in (200, 201), (st, r))
st, r = call(OP, 'GET', f'/ExecRcPoste/{exec_id}?posteCode=PAS71&equipe=E1')
check('plan de contrôle poste ouvert (2 lignes)', st == 200 and len(r['lignes']) == 2, (st, r))
statut, l1, l2 = r['execControleDocumentStatutId'], r['lignes'][0]['docLigneId'], r['lignes'][1]['docLigneId']
check('machine de la ligne conservée', r['lignes'][0]['machineCode'] == 'MAS19', r['lignes'][0])

saisie = {'execControleDocumentStatutId': statut, 'nbPiecesRebuteesEquipe': 1,
    'lignes': [{'docLigneId': l1, 'heures': [{'trancheHoraire': '08-09', 'nbNcParHeure': 2}]},
               {'docLigneId': l2, 'heures': [{'trancheHoraire': '08-09', 'nbNcParHeure': 1}]}],
    'reponses': [{'trancheHoraire': '08-09', 'totalNcHeure': 999, 'totalRealiseHeure': 50}],
    'bilan': {'totalDefauts': 999, 'totalPiecesTestees': 999, 'nbPiecesRebutees': 999, 'nbPieceConforme': 999}}
st, r = call(OP, 'PUT', f'/ExecRcPoste/{statut}', saisie)
b = r.get('bilan', {}) if isinstance(r, dict) else {}
check('totaux recalculés par le serveur (totaux du client ignorés)', st == 200 and r['reponses'][0]['totalNcHeure'] == 3
      and (b['totalDefauts'], b['totalPiecesTestees'], b['nbPieceConforme'], b['tauxNc']) == (3, 53, 52, 5.66), (st, r))
check('comptages dans la table commune, au matricule du jeton', sql(
    f"SELECT COUNT(*) FROM Exec_ControleLigne_Reponse WHERE ExecControleDocumentStatutId='{statut}' AND ResultatType='COMPTAGE' AND MatriculeOperateur='55555'") == '2')
id_l1 = sql(f"SELECT Id FROM Exec_ControleLigne_Reponse WHERE ExecControleDocumentStatutId='{statut}' AND DocumentLigneId='{l1}'")

saisie['lignes'] = [{'docLigneId': l1, 'heures': [{'trancheHoraire': '08-09', 'nbNcParHeure': 4}]}, {'docLigneId': l2, 'heures': []}]
derniere = r.get('derniereSaisieEquipe') if isinstance(r, dict) else None
st, r = call(OP, 'PUT', f'/ExecRcPoste/{statut}', dict(saisie, derniereSaisieEquipe=None))
check('saisie concurrente (autre tablette de la même équipe) refusée (409)', st == 409, (st, r))
saisie['derniereSaisieEquipe'] = derniere
st, r = call(OP, 'PUT', f'/ExecRcPoste/{statut}', saisie)
check('correction : valeur mise à jour en place (même enregistrement)', st == 200 and sql(
    f"SELECT CAST(Quantite AS VARCHAR) FROM Exec_ControleLigne_Reponse WHERE Id='{id_l1}'") == '4', (st, r))
check('valeur effacée retirée', sql(f"SELECT COUNT(*) FROM Exec_ControleLigne_Reponse WHERE ExecControleDocumentStatutId='{statut}'") == '1')
check('modification tracée dans le journal d\'audit', int(sql(
    f"SELECT COUNT(*) FROM AuditLog WHERE TableName='Exec_ControleLigne_Reponse' AND Action='UPDATE' AND EntityId='{id_l1.lower()}'") or 0) >= 1)
st, r = call(OP, 'PUT', f'/ExecRcPoste/{statut}', dict(saisie, lignes=[{'docLigneId': '00000000-0000-0000-0000-000000000001', 'heures': []}]))
check('ligne étrangère au plan refusée (400)', st == 400, (st, r))
st, r = call(ADMIN_SUPERVISION, 'POST', f'/operateur/of/{exec_id}/close'); check('clôture OF assemblage', st == 204, (st, r))

# ── Réponse à une occurrence d'OF de fabrication (cassée depuis la 4.1) ──
sql("IF NOT EXISTS (SELECT 1 FROM MFGHEAD_OrdreFabrication WHERE NumeroOF='OF26FAB052') INSERT INTO MFGHEAD_OrdreFabrication "
    "(NumeroOF,CodeArticle,QuantitePrevue,QuantiteLancee,QuantiteReelle,StatutOF) VALUES ('OF26FAB052','2903201-1',100,100,0,'EN_COURS')")
tous = sql("SELECT Id FROM TypeSection WHERE TypeNormalise='TOUS_PIECES'")
per = sql("SELECT TOP 1 Id FROM Periodicite WHERE Unite='HEURE' AND NbrPieces=4")
st, r = call(ADMIN, 'POST', '/PlanFabrication', {'nom': '2903201-1', 'codeArticleSageVersionne': '2903201-1', 'operationCode': 'TRONC', 'statut': 'ACTIF',
    'sections': [{'ordreAffiche': 1, 'libelleSection': 'Production', 'typeSectionId': tous, 'periodiciteId': per,
                  'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Longueur', 'moyenTexteLibre': 'Réglet'}]}]})
check('plan FAB avec section de production créé', st == 200, (st, r))
st, r = call(OP, 'POST', '/operateur/of/start', {'numeroOf': 'OF26FAB052', 'operationCode': 'TRONC', 'machineCode': 'TRN26'})
fab = (r or {}).get('id') if isinstance(r, dict) else None
check('démarrage OF de fabrication', st == 200, (st, r))
call(OP, 'GET', f'/operateur/of/{fab}/alertes-actives')
# Le serveur crée un contrôle quand il devient dû : on avance le temps d'un intervalle et demi.
sql(f"UPDATE Exec_ProductionPeriod SET StartedAt = DATEADD(ms, -CAST(1.5 * IntervalMinutes * 60000 AS INT), StartedAt) WHERE ExecControleOfId='{fab}' AND EndedAt IS NULL")
call(OP, 'GET', f'/operateur/of/{fab}/alertes-actives')
occ = sql(f"SELECT TOP 1 Id FROM Exec_Prelevement_Intermediaire WHERE ExecControleOFId='{fab}' ORDER BY HeureNotifPrevue")
ligne = sql(f"SELECT l.Id FROM Document_Ligne l JOIN Document_Entete e ON e.Id=l.EnteteId WHERE e.CodeArticleSageVersionne='2903201-1'")
check('occurrences générées pour la section de production', bool(occ), occ)
if occ:
    st, r = call(OP, 'POST', f'/operateur/occurrence/{occ}/repondre', {'resultat': 'C', 'lignes': [{'lignePlanId': ligne, 'resultat': 'C'}]})
    check('réponse à l\'occurrence enregistrée', st in (200, 204), (st, r))
    check('réponse rattachée à la ligne du plan FAB et au matricule', sql(
        f"SELECT COUNT(*) FROM Exec_ControleLigne_Reponse WHERE ExecControleOFId='{fab}' AND DocumentLigneId='{ligne}' AND MatriculeOperateur='55555'") == '1')

print(f'\n{ok} OK / {ko} KO')
