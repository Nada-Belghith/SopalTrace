# Import Excel des plans (fabrication, produit fini, assemblage) sur les fichiers réels de tests/fixtures/excel :
# aperçu sans écriture en base, refus explicites, puis enregistrement qui complète les référentiels.
import base64, hashlib, hmac, io, json, os, time, uuid, urllib.request, urllib.error, zipfile
from configuration import BASE, SECRET, sql

FIXTURES = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'fixtures', 'excel')

def b64(d): return base64.urlsafe_b64encode(d).rstrip(b'=').decode()
def jwt(mat, role):
    h = b64(json.dumps({'alg': 'HS256', 'typ': 'JWT'}).encode()); now = int(time.time())
    p = b64(json.dumps({'sub': '00000000-0000-0000-0000-0000000' + mat, 'stamp': '11111111-2222-3333-4444-555555555555', 'matricule': mat,
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
        'iss': 'SopalTraceApi', 'aud': 'SopalTraceVueJs', 'iat': now, 'nbf': now, 'exp': now + 3600}).encode())
    return f'{h}.{p}.' + b64(hmac.new(SECRET, f'{h}.{p}'.encode(), hashlib.sha256).digest())

def envoyer(req):
    try:
        with urllib.request.urlopen(req) as r:
            t = r.read().decode(); return r.status, (json.loads(t) if t else None)
    except urllib.error.HTTPError as e:
        t = e.read().decode(); return e.code, (json.loads(t) if t.strip().startswith(('{', '[')) else t[:300])

def call(tok, method, path, body=None):
    return envoyer(urllib.request.Request(BASE + path, method=method, data=json.dumps(body).encode() if body is not None else None,
        headers={'Authorization': 'Bearer ' + tok, 'Content-Type': 'application/json'}))

def importer(tok, nom, contenu):
    limite = uuid.uuid4().hex
    corps = (f'--{limite}\r\nContent-Disposition: form-data; name="file"; filename="{nom}"\r\n'
             f'Content-Type: application/octet-stream\r\n\r\n').encode() + contenu + f'\r\n--{limite}--\r\n'.encode()
    return envoyer(urllib.request.Request(BASE + '/ExcelImport/plan', method='POST', data=corps,
        headers={'Authorization': 'Bearer ' + tok, 'Content-Type': f'multipart/form-data; boundary={limite}'}))

def fixture(nom):
    with open(os.path.join(FIXTURES, nom), 'rb') as f: return f.read()
def msg(r): return r.get('message', '') if isinstance(r, dict) else str(r)

SUP = jwt('44444', 'SUPERVISEUR_QUALITE'); ADMIN = jwt('66666', 'ADMIN'); OP = jwt('55555', 'OPERATEUR')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])

if not os.path.isdir(FIXTURES):
    print('Fichiers Excel d\'exemple absents (tests/fixtures/excel) : suite ignorée.')
    print(f'\n{ok} OK / {ko} KO'); raise SystemExit(0)

def compter():
    return sql("SELECT CONCAT((SELECT COUNT(*) FROM Instrument),'|',(SELECT COUNT(*) FROM TypeCaracteristique),'|',"
               "(SELECT COUNT(*) FROM MoyenControle),'|',(SELECT COUNT(*) FROM TypeControle),'|',(SELECT COUNT(*) FROM Periodicite))")

def sections_document(resultat):
    """Payload d'enregistrement construit comme le fait l'éditeur à partir de l'aperçu."""
    return [{'ordreAffiche': i + 1, 'libelleSection': s.get('libelleSection') or s.get('nom') or f'Section {i + 1}',
             'typeSectionId': s.get('typeSectionId'), 'periodiciteId': s.get('periodiciteId'),
             'lignes': [{'ordreAffiche': j + 1, 'libelleAffiche': l['libelleAffiche'], 'typeCaracteristiqueId': l.get('typeCaracteristiqueId'),
                         'typeControleId': l.get('typeControleId'), 'moyenControleId': l.get('moyenControleId'),
                         'moyenTexteLibre': l.get('moyenTexteLibre'), 'instrumentCode': l.get('instrumentCode') or None,
                         'limiteSpecTexte': l.get('limiteSpecTexte') or None, 'observations': l.get('observations') or None,
                         'imageBase64': l.get('imageBase64')} for j, l in enumerate(s['lignes'])]}
            for i, s in enumerate(resultat['sections'])]

# ── Aperçu : fichiers réels lus complètement, rien n'est écrit ──
avant = compter()
attendus = {'Fab_en_cours_Manu.xlsx': [8, 4, 9, 4], 'PF_manu.xlsx': [20, 7, 5], 'PF_soupape.xlsx': [22, 6, 4],
            'pf_en_cours_auto.xlsx': [5, 5, 6], 'pf_en_cours_manu.xlsx': [8, 4, 9, 4]}
apercus = {}
for nom, lignes in attendus.items():
    st, r = importer(SUP, nom, fixture(nom))
    apercus[nom] = r
    check(f'{nom} : {sum(lignes)} caractéristiques dans {len(lignes)} sections', st == 200 and r['succes']
          and [len(s['lignes']) for s in r['sections']] == lignes and r['lignesLues'] == sum(lignes), (st, str(r)[:300]))
check('aperçu : aucun référentiel créé', compter().split('|')[:4] == avant.split('|')[:4], (avant, compter()))

manu = apercus['pf_en_cours_manu.xlsx']
lignes = [l for s in manu['sections'] for l in s['lignes']]
check('croquis importés (7)', sum(1 for l in lignes if l.get('imageBase64')) == 7)
passage = next(l for l in lignes if l['libelleAffiche'].startswith("Existence d'un passage"))
check('moyen hors catalogue en texte libre, code instrument normalisé (SER 05 → SER05)',
      passage['moyenTexteLibre'] == 'Machine de marquage' and passage['moyenControleId'] is None and passage['instrumentCode'] == 'SER05', passage)
couple = next(l for l in lignes if l['libelleAffiche'].startswith('Vérification de la couple'))
check('moyen du catalogue rattaché (Clé dynamométrique)', couple['moyenControleId'] and not couple.get('moyenTexteLibre'), couple)
check('instruments inconnus annoncés', any('MAS22' in w for w in manu['warnings']), manu['warnings'])
pf = apercus['PF_manu.xlsx']
marquage = next(l for s in pf['sections'] for l in s['lignes'] if l['libelleAffiche'].startswith('Vérification du marquage sur le corps'))
check('« Marquage lisible » (colonne code instrument) placé en observation', not marquage['instrumentCode']
      and 'Marquage lisible' in (marquage['observations'] or ''), marquage)
check('note finale reprise dans les remarques', pf['remarques'].startswith('NB'), pf['remarques'][:80])

# ── Refus explicites ──
st, r = importer(OP, 'PF_manu.xlsx', fixture('PF_manu.xlsx')); check('opérateur : 403', st == 403, st)
st, r = importer(SUP, 'plan.xls', b'\xd0\xcf\x11\xe0' + b'0' * 100); check('.xls : 400 et invitation à enregistrer en .xlsx', st == 400 and '.xlsx' in msg(r), (st, r))
st, r = importer(SUP, 'plan.xlsx', b'pas un classeur'); check('faux .xlsx : 400', st == 400 and 'valide' in msg(r), (st, r))
st, r = importer(SUP, 'plan.xlsx', b''); check('fichier vide : 400', st == 400, (st, r))
st, r = importer(SUP, 'plan.pdf', b'%PDF-1.4'); check('extension refusée : 400', st == 400 and 'non pris en charge' in msg(r), (st, r))
zip_faux = io.BytesIO()
with zipfile.ZipFile(zip_faux, 'w') as z: z.writestr('a.txt', 'rien')
st, r = importer(SUP, 'plan.xlsx', zip_faux.getvalue()); check('archive ZIP qui n\'est pas un classeur : 400, pas 500', st == 400, (st, r))
st, r = importer(SUP, 'liste.xlsx', fixture('PF_manu.xlsx')[:4000]); check('classeur tronqué : 400', st == 400, (st, r))

# ── Enregistrement d'un plan d'assemblage issu de l'import : les référentiels sont complétés ──
st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'PLAN_ASS', 'nom': 'Plan importé PAS71', 'typeRobinetCode': 'AUTO',
    'posteCode': 'PAS71', 'refFormulaireCodeReference': 'FE-ASS-IMPORT-63', 'remarques': manu['remarques'], 'sections': sections_document(manu)})
doc_id = (r or {}).get('id') or ((r or {}).get('data') or {}).get('id') if isinstance(r, dict) else None
check('enregistrement du document importé', st in (200, 201) and doc_id, (st, r))
check('25 lignes enregistrées', sql(f"SELECT COUNT(*) FROM Document_Ligne WHERE EnteteId='{doc_id}'") == '25')
check('7 croquis enregistrés', sql(f"SELECT COUNT(*) FROM DocumentLineImage i JOIN Document_Ligne l ON l.Id=i.LineId WHERE l.EnteteId='{doc_id}'") == '7')
check('instruments créés à l\'enregistrement', sql("SELECT COUNT(*) FROM Instrument WHERE CodeInstrument IN ('BEE47','MAS22','SER05','CLD18','CLD02')") == '5')
check('moyen libre conservé, sans moyen catalogue', sql(
    f"SELECT COUNT(*) FROM Document_Ligne WHERE EnteteId='{doc_id}' AND MoyenTexteLibre='Machine de marquage' AND MoyenControleId IS NULL") == '1')
check('aucune ligne avec moyen catalogue ET texte libre', sql(
    f"SELECT COUNT(*) FROM Document_Ligne WHERE EnteteId='{doc_id}' AND MoyenControleId IS NOT NULL AND MoyenTexteLibre IS NOT NULL") == '0')
check('toutes les lignes ont un type de caractéristique', sql(
    f"SELECT COUNT(*) FROM Document_Ligne WHERE EnteteId='{doc_id}' AND TypeCaracteristiqueId IS NULL") == '0')
car = sql("SELECT COUNT(*) FROM TypeCaracteristique WHERE Libelle='Etanchéité externe à 30 bars'")
check('caractéristique répétée créée une seule fois', car == '1', car)
check('aucun moyen de contrôle créé', compter().split('|')[2] == avant.split('|')[2])

# Mise à jour : une ligne ajoutée avec un moyen du catalogue saisi en texte libre.
st, d = call(SUP, 'GET', f'/Document/{doc_id}'); d = d.get('data', d) if isinstance(d, dict) else {}
sections = d.get('sections') or []
if sections:
    sections[0]['lignes'].append({'ordreAffiche': 99, 'libelleAffiche': 'Contrôle ajouté après import', 'moyenTexteLibre': 'banc d’essai',
                                  'instrumentCode': 'BEE99'})
st, r = call(SUP, 'PUT', f'/Document/{doc_id}', {'rowVersion': d.get('rowVersion'), 'nom': d.get('nom'), 'sections': sections})
check('mise à jour du document', st == 200, (st, r))
check('mise à jour : moyen rattaché au catalogue, texte libre vidé', sql(
    f"SELECT CONCAT(CASE WHEN MoyenControleId IS NULL THEN 'null' ELSE 'id' END,'|',ISNULL(MoyenTexteLibre,'null')) FROM Document_Ligne "
    f"WHERE EnteteId='{doc_id}' AND LibelleAffiche='Contrôle ajouté après import'") == 'id|null')
check('mise à jour : instrument et caractéristique créés', sql("SELECT COUNT(*) FROM Instrument WHERE CodeInstrument='BEE99'") == '1'
      and sql("SELECT COUNT(*) FROM TypeCaracteristique WHERE Libelle='Contrôle ajouté après import'") == '1')

# ── Plan de fabrication issu de l'import ──
fab = apercus['Fab_en_cours_Manu.xlsx']
st, r = call(ADMIN, 'POST', '/PlanFabrication', {'nom': '2623201-1-7', 'codeArticleSageVersionne': '2623201-1-7', 'operationCode': 'TRONC',
    'statut': 'ACTIF', 'sections': sections_document(fab)})
plan_id = (r or {}).get('id') if isinstance(r, dict) else None
check('enregistrement du plan de fabrication importé', st == 200 and plan_id, (st, r))
check('plan : 25 lignes, instrument CLD03 créé', sql(f"SELECT COUNT(*) FROM Document_Ligne WHERE EnteteId='{plan_id}'") == '25'
      and sql("SELECT COUNT(*) FROM Instrument WHERE CodeInstrument='CLD03'") == '1')

# ── Tableau de bord superviseur : compteurs réels ──
st, r = call(SUP, 'GET', '/hub/statistiques')
attendu = {}
for ligne in sql("SELECT TypeDocumentCode+'|'+Portee+'|'+Statut+'|'+CAST(COUNT(*) AS VARCHAR) "
                 "FROM Document_Entete GROUP BY TypeDocumentCode, Portee, Statut").splitlines():
    t, p_, s_, n = ligne.split('|')
    attendu[(t, p_, s_)] = int(n)
spec_actifs = sum(n for (t, p_, s_), n in attendu.items() if p_ == 'SPECIFIQUE' and s_ == 'ACTIF')
gen_actifs = sum(n for (t, p_, s_), n in attendu.items() if p_ == 'GENERIQUE' and s_ == 'ACTIF')
check('statistiques : génériques et spécifiques actifs conformes à la base', st == 200
      and r['specifiques']['actifs'] == spec_actifs and r['generiques']['actifs'] == gen_actifs and spec_actifs >= 1, (st, r, attendu))
ass = next((t for t in r.get('parType', []) if t['typeDocumentCode'] == 'PLAN_ASS'), None) if st == 200 else None
check("statistiques : détail par type de document (plan d'assemblage présent)",
      ass is not None and ass['comptes']['actifs'] + ass['comptes']['brouillons'] >= 1, ass)
check("statistiques : refusées à l'opérateur (403)", call(OP, 'GET', '/hub/statistiques')[0] == 403)

print(f'\n{ok} OK / {ko} KO')
