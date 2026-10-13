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

SUP = jwt('44444', 'SUPERVISEUR_QUALITE'); ADMIN = jwt('66666', 'ADMIN')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])

PNG = 'data:image/png;base64,' + base64.b64encode(bytes([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3])).decode()
SVG = 'data:image/svg+xml;base64,' + base64.b64encode(b'<svg onload="alert(1)"/>').decode()
COLONNES = [{'key': 'col_couple', 'label': 'Couple', 'type': 'Nombre'},            # ancien libellé du front
            {'key': 'col_etat', 'label': 'État', 'type': 'LIST', 'options': ['Conforme', 'Rebut']},
            {'key': 'col_date', 'label': 'Étalonnage', 'type': 'DATE'}]

def document(code, extras, image=PNG):
    return {'typeDocumentCode': 'PLAN_ASS', 'nom': f'Plan {code}', 'typeRobinetCode': 'AUTO', 'posteCode': 'PAS71',
            'refFormulaireCodeReference': code, 'colonneDefs': COLONNES,
            'sections': [{'ordreAffiche': 1, 'libelleSection': 'Contrôle', 'lignes': [
                {'ordreAffiche': 1, 'libelleAffiche': 'Serrage', 'imageBase64': image, 'extraColonnes': extras}]}]}
valides = [{'cleColonne': 'col_couple', 'valeurColonne': '12.5'}, {'cleColonne': 'col_etat', 'valeurColonne': 'Conforme'},
           {'cleColonne': 'col_date', 'valeurColonne': '2026-10-09'}]

st, r = call(SUP, 'POST', '/Document', document('FE-TEST-44', valides)); doc = data(r).get('id') if isinstance(data(r), dict) else None
check('document avec colonnes typées et image créé', st in (200, 201) and doc, (st, r))
check('« Nombre » du front enregistré en NUMBER, liste en LIST', sql(
    "SELECT STRING_AGG(d.CleColonne+'='+d.TypeValeur, ',') WITHIN GROUP (ORDER BY d.CleColonne) FROM Ref_Formulaire_ColonneDef d "
    "JOIN Ref_Formulaire f ON f.Id=d.FormulaireId WHERE f.CodeReference='FE-TEST-44'") == 'col_couple=NUMBER,col_date=DATE,col_etat=LIST')
check('valeurs proposées de la liste enregistrées', sql(
    "SELECT STRING_AGG(v.Value, ',') WITHIN GROUP (ORDER BY v.DisplayOrder) FROM ColumnAllowedValue v JOIN Ref_Formulaire_ColonneDef d ON d.Id=v.ColumnDefinitionId "
    "JOIN Ref_Formulaire f ON f.Id=d.FormulaireId WHERE f.CodeReference='FE-TEST-44'") == 'Conforme,Rebut')
check('image stockée hors de la ligne, en PNG', sql(
    f"SELECT i.ContentType FROM DocumentLineImage i JOIN Document_Ligne l ON l.Id=i.LineId WHERE l.EnteteId='{doc}'") == 'image/png')

st, r = call(SUP, 'GET', f'/Document/{doc}'); d = data(r) if st == 200 else {}
ligne = d.get('sections', [{}])[0].get('lignes', [{}])[0] if d else {}
check('relecture : image identique', ligne.get('imageBase64') == PNG, ligne.get('imageBase64', '')[:60])
etat = [c for c in d.get('colonneDefs') or [] if c.get('cleColonne') == 'col_etat']
check('relecture : options de la liste transmises', etat and etat[0].get('options') == ['Conforme', 'Rebut'], d.get('colonneDefs'))

for libelle, extras, image, mot in [
        ('nombre invalide refusé', [{'cleColonne': 'col_couple', 'valeurColonne': 'douze'}], PNG, 'Couple'),
        ('valeur hors liste refusée', [{'cleColonne': 'col_etat', 'valeurColonne': 'Bleu'}], PNG, 'État'),
        ('date mal formée refusée', [{'cleColonne': 'col_date', 'valeurColonne': '09/10/2026'}], PNG, 'Étalonnage'),
        ('colonne inconnue refusée', [{'cleColonne': 'col_fantome', 'valeurColonne': 'x'}], PNG, 'col_fantome'),
        ('image SVG refusée', valides, SVG, 'PNG')]:
    st, r = call(SUP, 'POST', '/Document', document('FE-TEST-44-' + mot[:3].upper(), extras, image))
    check(f'{libelle} (400, message explicite)', st == 400 and mot in texte(r), (st, r))

maj = {'nom': 'Plan FE-TEST-44', 'refFormulaireCodeReference': 'FE-TEST-44', 'typeRobinetCode': 'AUTO',
       'configurationColonnesJson': json.dumps(COLONNES),
       'sections': [{'ordreAffiche': 1, 'libelleSection': 'Contrôle', 'lignes': [
           {'ordreAffiche': 1, 'libelleAffiche': 'Serrage', 'imageBase64': None, 'extraColonnes': valides}]}]}
maj['rowVersion'] = data(call(SUP, 'GET', f'/Document/{doc}')[1])['rowVersion']
st, r = call(SUP, 'PUT', f'/Document/{doc}', maj)
check('mise à jour sans image : croquis retiré', st == 200 and sql(
    f"SELECT COUNT(*) FROM DocumentLineImage i JOIN Document_Ligne l ON l.Id=i.LineId WHERE l.EnteteId='{doc}'") == '0', (st, r))

# Comme le front : identifiants existants conservés, ajout d'une nouvelle ligne avec ses valeurs.
st, r = call(SUP, 'GET', f'/Document/{doc}'); sec = data(r)['sections'][0]
maj['sections'] = [{'id': sec['id'], 'ordreAffiche': 1, 'libelleSection': 'Contrôle', 'lignes': [
    {'id': sec['lignes'][0]['id'], 'ordreAffiche': 1, 'libelleAffiche': 'Serrage', 'imageBase64': PNG, 'extraColonnes': valides},
    {'ordreAffiche': 2, 'libelleAffiche': 'Étanchéité', 'extraColonnes': [{'cleColonne': 'col_etat', 'valeurColonne': 'Rebut'}]}]}]
maj['rowVersion'] = data(call(SUP, 'GET', f'/Document/{doc}')[1])['rowVersion']
st, r = call(SUP, 'PUT', f'/Document/{doc}', maj)
check('correction : nouvelle ligne ajoutée avec ses valeurs (plus de faux conflit)', st == 200 and sql(
    f"SELECT COUNT(*) FROM Document_Ligne WHERE EnteteId='{doc}'") == '2' and sql(
    f"SELECT COUNT(*) FROM Document_Ligne_ExtraColonne v JOIN Document_Ligne l ON l.Id=v.LigneId WHERE l.EnteteId='{doc}' AND v.ValeurColonne='Rebut'") == '1', (st, r))
check('correction : image remise sur la ligne existante', sql(
    f"SELECT COUNT(*) FROM DocumentLineImage i JOIN Document_Ligne l ON l.Id=i.LineId WHERE l.EnteteId='{doc}'") == '1')

maj['configurationColonnesJson'] = json.dumps([dict(COLONNES[0], type='TEXT')] + COLONNES[1:])
maj['rowVersion'] = data(call(SUP, 'GET', f'/Document/{doc}')[1])['rowVersion']
st, r = call(SUP, 'PUT', f'/Document/{doc}', maj)
check('changer le type d\'une colonne déjà renseignée refusé (400)', st == 400 and 'type' in texte(r), (st, r))

st, r = call(ADMIN, 'POST', '/DocumentVerifMachine', {'nom': 'VM', 'machineCode': 'MAS20', 'familles': [], 'lignesConformite': [],
    'lignesRisques': [{'ordreAffiche': 1, 'typeLigne': 'RISQUE', 'libelleRisque': 'x' * 251, 'libelleMethode': 'm', 'echeances': []}]})
check('vérification machine : validateur actif (libellé > 250 refusé)', st == 400 and '250' in texte(r), (st, r))

print(f'\n{ok} OK / {ko} KO')
