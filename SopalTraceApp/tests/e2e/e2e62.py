# Magasin à la douchette : préparation des OF scan par scan et expédition des BL.
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
def msg(r): return r.get('message', '') if isinstance(r, dict) else str(r)

MAG = jwt('33333', 'MAGASINIER'); OP = jwt('55555', 'OPERATEUR')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])

# OF de test : 2 composants ; OF26MAG03 terminé en production (pour l'expédition).
sql("""INSERT INTO MFGHEAD_OrdreFabrication (NumeroOF, CodeArticle, QuantitePrevue, QuantiteLancee, QuantiteReelle, StatutOF)
       VALUES ('OF26MAG01','2511A01-1',100,100,0,'EN_COURS'), ('OF26MAG02','2511A01-1',50,50,0,'EN_COURS'), ('OF26MAG03','2511A01-1',80,80,80,'EN_COURS');
       INSERT INTO MFGMAT_BesoinOF (NumeroOF, CodeArticle, QuantiteRequise, QuantiteSortie)
       VALUES ('OF26MAG01','M1180RP',100,0), ('OF26MAG01','2903201-1',100,0), ('OF26MAG02','M1180RP',50,0);
       INSERT INTO Exec_ControleOF (Id, NumeroOF, OperationCode, TypeOf, Statut, DateDebut, DateFin)
       VALUES (NEWID(),'OF26MAG03','ASS','ASS','CLOTURE',SYSDATETIME(),SYSDATETIME());""")

def ofs_operateur():
    st, r = call(OP, 'GET', '/Operateur/ofs/operations')
    return {o['numeroOf'] for o in r} if st == 200 else set()

check("l'opérateur n'accède pas au magasin (403)", call(OP, 'POST', '/magasinier/preparations', {'numeroOf': 'OF26MAG01'})[0] == 403)
check("OF non préparé : invisible pour les opérateurs", 'OF26MAG01' not in ofs_operateur())

# ── Préparation ──
st, r = call(MAG, 'POST', '/magasinier/preparations', {'numeroOf': 'of:OF26MAG01'})
prep = r.get('id') if st == 200 else None
check('scan de l\'OF : préparation ouverte (2 composants)', st == 200 and r['statut'] == 'EN_COURS' and len(r['composants']) == 2, (st, r))
check('magasinier lu dans le jeton', sql(f"SELECT MatriculeMagasinier FROM Mag_PreparationOF WHERE Id='{prep}'") == '33333')
st, r = call(MAG, 'POST', '/magasinier/preparations', {'numeroOf': 'OF26MAG01'})
check('rescanner l\'OF reprend la même préparation', st == 200 and r['id'] == prep, (st, r))

st, r = call(MAG, 'POST', f'/magasinier/preparations/{prep}/scans', {'contenu': 'lot:L-001/cd:M1180RP/Rpt:QC-77/qty:60'})
check('scan d\'un lot : enregistré aussitôt', st == 200 and r['preparation']['composantsServis'] == 1 and r['lot']['quantite'] == 60, (st, r))
check('lot, rapport QC et contenu brut en base', sql(
    f"SELECT NumeroLotScanne+'|'+QualityControlReport+'|'+ScannedContent FROM Mag_PreparationOF_Lot WHERE PreparationOFId='{prep}'") == 'L-001|QC-77|lot:L-001/cd:M1180RP/Rpt:QC-77/qty:60')
st, r = call(MAG, 'POST', f'/magasinier/preparations/{prep}/scans', {'contenu': 'M1180RP;L-001'})
check('même lot rescanné : 409 (ignoré)', st == 409, (st, r))
st, r = call(MAG, 'POST', f'/magasinier/preparations/{prep}/scans', {'contenu': 'VIS-9;L-5'})
check('article hors nomenclature : 400', st == 400 and 'nomenclature' in msg(r), (st, r))
st, r = call(MAG, 'POST', f'/magasinier/preparations/{prep}/scans', {'contenu': 'éçàà'})
check('douchette en QWERTY détectée : 400 explicite', st == 400 and 'QWERTY' in msg(r), (st, r))
st, r = call(MAG, 'POST', f'/magasinier/preparations/{prep}/scans', {'contenu': 'n\'importe quoi'})
check('format inconnu : 400 avec les formats attendus', st == 400 and 'Formats attendus' in msg(r), (st, r))

st, r = call(MAG, 'POST', f'/magasinier/preparations/{prep}/terminer', {'motif': None})
check('terminer avec un composant manquant sans motif : 400', st == 400 and '2903201-1' in msg(r), (st, r))
st, r = call(MAG, 'POST', f'/magasinier/preparations/{prep}/scans', {'contenu': ']Q12903201-1;LOT-C9\r\n'})
check('préfixe de symbologie et fin de ligne nettoyés', st == 200 and r['codeArticle'] == '2903201-1', (st, r))
lot = r['lot']['id'] if st == 200 else None
st, r = call(MAG, 'DELETE', f'/magasinier/preparations/{prep}/scans/{lot}')
check('retirer un scan', st == 200 and r['composantsServis'] == 1, (st, r))
call(MAG, 'POST', f'/magasinier/preparations/{prep}/scans', {'contenu': '2903201-1;LOT-C9'})
st, r = call(MAG, 'POST', f'/magasinier/preparations/{prep}/terminer', {'motif': None})
check('préparation complète terminée', st == 200 and r['statut'] == 'TERMINE' and r['motifIncomplet'] is None, (st, r))
st, r = call(MAG, 'POST', f'/magasinier/preparations/{prep}/scans', {'contenu': 'M1180RP;L-002'})
check('préparation terminée : plus de scan (409)', st == 409, (st, r))
check('OF préparé : disponible pour les opérateurs', 'OF26MAG01' in ofs_operateur())

# Annulation puis nouvelle préparation ; préparation incomplète avec motif.
st, r = call(MAG, 'POST', '/magasinier/preparations', {'numeroOf': 'OF26MAG02'}); prep2 = r['id']
call(MAG, 'POST', f'/magasinier/preparations/{prep2}/annuler')
st, r = call(MAG, 'POST', '/magasinier/preparations', {'numeroOf': 'OF26MAG02'})
check('après annulation, une nouvelle préparation est ouverte', st == 200 and r['id'] != prep2 and r['statut'] == 'EN_COURS', (st, r))
st, r = call(MAG, 'POST', f"/magasinier/preparations/{r['id']}/terminer", {'motif': 'Matière en rupture'})
check('terminer incomplet avec motif', st == 200 and r['motifIncomplet'] == 'Matière en rupture', (st, r))

# ── Expédition ──
st, r = call(MAG, 'POST', '/magasinier/expeditions', {'numeroBl': 'BL2607001'})
exp = r.get('id') if st == 200 else None
check('scan du BL : expédition ouverte', st == 200 and r['statut'] == 'EN_COURS' and r['codeClient'] == 'CLI-STEG', (st, r))
st, r = call(MAG, 'POST', '/magasinier/expeditions', {'numeroBl': 'BL9999999'})
check('BL inconnu : 404', st == 404, (st, r))
st, r = call(MAG, 'POST', f'/magasinier/expeditions/{exp}/scans', {'contenu': 'OF26MAG01'})
check('OF non terminé en production : refusé', st == 400 and 'production' in msg(r), (st, r))
st, r = call(MAG, 'POST', f'/magasinier/expeditions/{exp}/scans', {'contenu': 'OF26MAG03'})
check('OF terminé : ajouté au BL', st == 200 and [o['numeroOf'] for o in r['ofs']] == ['OF26MAG03'], (st, r))
check('OF rescanné sur le même BL : 409', call(MAG, 'POST', f'/magasinier/expeditions/{exp}/scans', {'contenu': 'OF26MAG03'})[0] == 409)
st, r = call(MAG, 'POST', '/magasinier/expeditions', {'numeroBl': 'BL2607002'}); exp2 = r['id']
st, r = call(MAG, 'POST', f'/magasinier/expeditions/{exp2}/terminer')
check('valider un BL sans OF : 400', st == 400, (st, r))
st, r = call(MAG, 'POST', f'/magasinier/expeditions/{exp2}/scans', {'contenu': 'OF26MAG03'})
check('livraison partielle : le même OF sur un second BL est accepté', st == 200 and [o['numeroOf'] for o in r['ofs']] == ['OF26MAG03'], (st, r))
check('OF présent sur les deux BL', sql("SELECT COUNT(*) FROM Mag_ExpeditionBL_ScanOF WHERE NumeroOFScanne='OF26MAG03'") == '2')
st, r = call(MAG, 'POST', f'/magasinier/expeditions/{exp}/terminer')
check('expédition validée', st == 200 and r['statut'] == 'TERMINE', (st, r))
st, r = call(MAG, 'DELETE', f"/magasinier/expeditions/{exp}/scans/{r['ofs'][0]['id']}")
check('expédition validée : plus modifiable (409)', st == 409, (st, r))
st, r = call(MAG, 'POST', '/magasinier/expeditions', {'numeroBl': 'BL2607001'})
check('rescanner un BL expédié : consultation', st == 200 and r['id'] == exp and r['statut'] == 'TERMINE', (st, r))

print(f'\n{ok} OK / {ko} KO')
