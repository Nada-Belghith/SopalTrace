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
def data(r): return r.get('data', r) if isinstance(r, dict) else r
def texte(r): return json.dumps(r, ensure_ascii=False)

# Deux personnes ouvrent le même document : même rôle, matricules différents.
ALICE = jwt('44444', 'SUPERVISEUR_QUALITE'); BOB = jwt('44445', 'SUPERVISEUR_QUALITE')
ADMIN_A = jwt('66666', 'ADMIN'); ADMIN_B = jwt('66667', 'ADMIN')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])
def conflit(st, r): return st == 409 and 'modifié par un autre utilisateur' in texte(r)

# ── Document générique (plan d'assemblage) ──
doc_req = {'typeDocumentCode': 'PLAN_ASS', 'nom': 'Plan concurrence', 'typeRobinetCode': 'AUTO', 'posteCode': 'PAS71',
           'sections': [{'ordreAffiche': 1, 'libelleSection': 'Contrôle', 'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Serrage'}]}]}
st, r = call(ALICE, 'POST', '/Document', doc_req); doc = data(r).get('id') if isinstance(data(r), dict) else None
st, r = call(ALICE, 'GET', f'/Document/{doc}'); d = data(r); v1 = d.get('rowVersion')
check('document : version transmise à l\'ouverture', st == 200 and v1, (st, str(r)[:200]))
sec = d['sections'][0]
def correction(version, libelle='Serrage'):
    return {'nom': 'Plan concurrence', 'typeRobinetCode': 'AUTO', 'rowVersion': version,
            'sections': [{'id': sec['id'], 'ordreAffiche': 1, 'libelleSection': 'Contrôle',
                          'lignes': [{'id': sec['lignes'][0]['id'], 'ordreAffiche': 1, 'libelleAffiche': libelle}]}]}
st, r = call(ALICE, 'PUT', f'/Document/{doc}', correction(v1)); v2 = (r or {}).get('rowVersion') if isinstance(r, dict) else None
check('Alice enregistre : nouvelle version renvoyée', st == 200 and v2 and v2 != v1, (st, r))
st, r = call(BOB, 'PUT', f'/Document/{doc}', correction(v1, 'Écrasement'))
check('Bob, ouvert avant l\'enregistrement d\'Alice : refusé (409, message clair)', conflit(st, r), (st, r))
check('le travail d\'Alice n\'est pas écrasé', data(call(ALICE, 'GET', f'/Document/{doc}')[1])['sections'][0]['lignes'][0]['libelleAffiche'] == 'Serrage')
st, r = call(ALICE, 'PUT', f'/Document/{doc}', correction(None))
check('correction sans version : refusée (400)', st == 400 and 'Version du document manquante' in texte(r), (st, r))
st, r = call(ALICE, 'PUT', f'/Document/{doc}', correction('n\'importe quoi'))
check('version illisible : refusée (400)', st == 400, (st, r))
st, r = call(ALICE, 'PUT', f'/Document/{doc}', correction(v2, 'Serrage au couple')); v3 = (r or {}).get('rowVersion') if isinstance(r, dict) else None
check('modification d\'une ligne seulement : la version évolue quand même', st == 200 and v3 and v3 != v2, (st, r))
nv = dict(doc_req, ancienId=doc, rowVersion=v2, motifModification='test')
st, r = call(BOB, 'POST', f'/Document/{doc}/version', nv)
check('nouvelle version depuis une version dépassée : refusée (409)', conflit(st, r), (st, r))
st, r = call(ALICE, 'POST', f'/Document/{doc}/version', dict(nv, rowVersion=v3))
check('nouvelle version depuis la version à jour : acceptée', st in (200, 201), (st, r))

# ── Plan de fabrication ──
st, r = call(ADMIN_A, 'POST', '/PlanFabrication', {'nom': '2623201-1-7', 'codeArticleSageVersionne': '2623201-1-7', 'operationCode': 'TRONC', 'statut': 'ACTIF', 'sections': []})
plan = (r or {}).get('id') if isinstance(r, dict) else None
pv = call(ADMIN_A, 'GET', f'/PlanFabrication/{plan}')[1].get('rowVersion')
maj_plan = lambda v: {'codeArticleSageVersionne': '2623201-1-7', 'remarques': 'r', 'sections': [], 'rowVersion': v}
st, r = call(ADMIN_A, 'PUT', f'/PlanFabrication/{plan}', maj_plan(pv))
check('plan FAB : enregistrement à jour accepté', st == 200 and r.get('rowVersion') and r['rowVersion'] != pv, (st, r))
st, r = call(ADMIN_B, 'PUT', f'/PlanFabrication/{plan}', maj_plan(pv))
check('plan FAB : version dépassée refusée (409)', conflit(st, r), (st, r))

# ── Modèle de fabrication (nouvelle version) ──
st, r = call(ADMIN_A, 'POST', '/ModeleFabrication', {'code': 'MODCONC', 'libelle': 'Modèle', 'typeRobinetCode': None,
    'natureComposantCode': 'CORPS', 'operationCode': 'TRONC', 'sections': []}); mod = (r or {}).get('id') if isinstance(r, dict) else None
mv = call(ADMIN_A, 'GET', f'/ModeleFabrication/{mod}')[1].get('rowVersion')
st, r = call(ADMIN_A, 'POST', '/ModeleFabrication/nouvelle-version', {'ancienId': mod, 'rowVersion': mv})
check('modèle : nouvelle version depuis la version à jour', st == 200, (st, r))
st, r = call(ADMIN_B, 'POST', '/ModeleFabrication/nouvelle-version', {'ancienId': mod, 'rowVersion': mv})
check('modèle : deuxième nouvelle version sur la même base refusée (409)', conflit(st, r), (st, r))

# ── Vérification machine ──
vm = call(ADMIN_A, 'GET', '/DocumentVerifMachine?machineCode=MAS19')[1][0]
corps_vm = {'nom': vm['nom'], 'machineCode': 'MAS19', 'familles': [], 'lignesConformite': [],
            'lignesRisques': [{'ordreAffiche': 1, 'typeLigne': 'RISQUE', 'libelleRisque': 'Fuite', 'libelleMethode': 'm', 'echeances': []}]}
st, r = call(ADMIN_A, 'PUT', f"/DocumentVerifMachine/{vm['id']}", dict(corps_vm, rowVersion=vm['rowVersion']))
check('vérification machine : enregistrement à jour accepté', st == 200 and r.get('rowVersion'), (st, r))
st, r = call(ADMIN_B, 'PUT', f"/DocumentVerifMachine/{vm['id']}", dict(corps_vm, rowVersion=vm['rowVersion']))
check('vérification machine : version dépassée refusée (409)', conflit(st, r), (st, r))

# ── Plan d'échantillonnage ──
P = '/documents-echantillonnage'
donnees = {'niveauControle': 'II', 'typePlan': 'SIMPLE', 'modeControle': 'NORMAL', 'valeurNqa': 1.5,
           'critereAcceptationAc': 1, 'critereRejetRe': 2, 'remarques': '', 'legendeMoyens': '', 'refFormulaireCodeReference': 'FE-ECHAN-01'}
ech = call(ADMIN_A, 'POST', P, donnees)[1]['data']
ev = call(ADMIN_A, 'GET', f'{P}/{ech}')[1]['data']['rowVersion']
st, r = call(ADMIN_A, 'PUT', f'{P}/{ech}', dict(donnees, rowVersion=ev))
check('échantillonnage : enregistrement à jour accepté', st == 200 and r.get('rowVersion'), (st, r))
st, r = call(ADMIN_B, 'PUT', f'{P}/{ech}', dict(donnees, rowVersion=ev))
check('échantillonnage : version dépassée refusée (409)', conflit(st, r), (st, r))
st, r = call(ADMIN_B, 'POST', P + '/nouvelle-version', {'ancienId': ech, 'rowVersion': ev, 'motifModification': 'm', 'donnees': donnees})
check('échantillonnage : nouvelle version sur base dépassée refusée (409)', conflit(st, r), (st, r))

print(f'\n{ok} OK / {ko} KO')
