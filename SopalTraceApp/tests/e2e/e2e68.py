"""
Documents externes déposés par les services et retrouvés pour chaque OF.
- Chaque service dépose ses types (qualité : rapports de contrôle, certificats, rapport de contrôle rapide du produit
  fini ; achats : bons de commande fournisseur ; commercial : BL, bons de commande client ; développement industriel :
  plan de définition du produit fini).
- Numéros vérifiés dans les copies Sage, nom normalisé formé par la base, PDF seulement ; déjà déposé : refus nommant
  le document, remplacement avec motif (l'ancien reste) ; ni modification ni suppression en base.
- Traçabilité par OF (responsable qualité seulement) : lot scanné → rapport → commande fournisseur → bon de commande et
  certificat ; BL de l'expédition → commande Sage (ouverte) → référence client → bon de commande client ; OF de produit
  fini → plan de définition de son article et rapport de contrôle rapide.
"""
import base64, hashlib, hmac, json, time, urllib.request, urllib.error, uuid
from configuration import BASE, SECRET, sql


def b64(d): return base64.urlsafe_b64encode(d).rstrip(b'=').decode()
def jwt(mat, role):
    h = b64(json.dumps({'alg': 'HS256', 'typ': 'JWT'}).encode()); now = int(time.time())
    p = b64(json.dumps({'sub': '00000000-0000-0000-0000-0000000' + mat, 'stamp': '11111111-2222-3333-4444-555555555555', 'matricule': mat,
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
        'iss': 'SopalTraceApi', 'aud': 'SopalTraceVueJs', 'iat': now, 'nbf': now, 'exp': now + 3600}).encode())
    return f'{h}.{p}.' + b64(hmac.new(SECRET, f'{h}.{p}'.encode(), hashlib.sha256).digest())
def requete(tok, method, path, corps=None, type_contenu='application/json'):
    req = urllib.request.Request(BASE + path, method=method, data=corps, headers={'Authorization': 'Bearer ' + tok, 'Content-Type': type_contenu})
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            return r.status, r.headers.get('Content-Type', ''), r.read()
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get('Content-Type', ''), e.read()
def lire(t):
    t = t.decode(errors='replace')
    return json.loads(t) if t.strip().startswith(('{', '[')) else t[:300]
def call(tok, method, path, body=None):
    st, _, t = requete(tok, method, path, json.dumps(body).encode() if body is not None else None)
    return st, lire(t)
def data(r): return r.get('data', r) if isinstance(r, dict) else r
def message(r): return r.get('message', '') if isinstance(r, dict) else str(r)
def deposer(tok, champs, contenu, nom='scan.pdf'):
    limite = 'e2e' + uuid.uuid4().hex
    corps = b''
    for cle, valeur in champs.items():
        corps += f'--{limite}\r\nContent-Disposition: form-data; name="{cle}"\r\n\r\n{valeur}\r\n'.encode()
    if contenu is not None:
        corps += (f'--{limite}\r\nContent-Disposition: form-data; name="fichier"; filename="{nom}"\r\n'
                  f'Content-Type: application/pdf\r\n\r\n').encode() + contenu + b'\r\n'
    corps += f'--{limite}--\r\n'.encode()
    st, _, t = requete(tok, 'POST', '/documents-externes', corps, f'multipart/form-data; boundary={limite}')
    return st, lire(t)

def deposer_vers(tok, chemin):
    limite = 'e2e' + uuid.uuid4().hex
    corps = (f'--{limite}\r\nContent-Disposition: form-data; name="file"; filename="f.xlsx"\r\n'
             f'Content-Type: application/octet-stream\r\n\r\n').encode() + b'PK' + f'\r\n--{limite}--\r\n'.encode()
    return requete(tok, 'POST', chemin, corps, f'multipart/form-data; boundary={limite}')[0]

QUALITE = jwt('88883', 'SERVICE_QUALITE'); ACHATS = jwt('88881', 'SERVICE_ACHATS'); COMMERCIAL = jwt('88882', 'SERVICE_COMMERCIAL')
DI = jwt('88884', 'SERVICE_DI')
RQ = jwt('22222', 'RESPONSABLE_QUALITE'); SUP = jwt('44444', 'SUPERVISEUR_QUALITE'); ADMIN = jwt('66666', 'ADMIN'); OP = jwt('55555', 'OPERATEUR')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])
def nb(requete_sql): return int(sql(requete_sql) or 0)
def pdf(texte): return b'%PDF-1.4\n%' + texte.encode() + b'\n%%EOF\n'

OF = 'OF2607ASS01'
# Expédition de l'OF sur le BL2607001 (le magasinier a scanné l'OF sur ce BL).
sql("DECLARE @e UNIQUEIDENTIFIER = NEWID(); "
    "INSERT INTO Mag_ExpeditionBL (Id, NumeroBL, MatriculeMagasinier, Statut, DateFin) VALUES (@e, 'BL2607001', '33333', 'TERMINE', SYSDATETIME()); "
    f"INSERT INTO Mag_ExpeditionBL_ScanOF (ExpeditionBLId, NumeroOFScanne) VALUES (@e, '{OF}');")

# ── A. Types par service ──────────────────────────────────────────────
types = {nom: [t['code'] for t in data(call(tok, 'GET', '/documents-externes/types')[1])]
         for nom, tok in (('qualite', QUALITE), ('achats', ACHATS), ('commercial', COMMERCIAL), ('di', DI))}
check('A1 chaque service ne voit que ses types de documents',
      types == {'qualite': ['CONTROL_REPORT', 'CONFORMITY_CERTIFICATE', 'FINISHED_PRODUCT_CONTROL_REPORT'],
                'achats': ['SUPPLIER_ORDER'], 'commercial': ['DELIVERY_NOTE', 'CUSTOMER_ORDER'],
                'di': ['FINISHED_PRODUCT_DEFINITION_PLAN']}, types)
rapport_type = next(t for t in data(call(QUALITE, 'GET', '/documents-externes/types')[1]) if t['code'] == 'CONTROL_REPORT')
check('A2 numéros à saisir dans l\'ordre du nom (rapport : n° du rapport puis commande fournisseur)',
      [c['code'] for c in rapport_type['champs']] == ['numeroRapport', 'numeroCommandeFournisseur'], rapport_type)
check('A3 autres rôles : pas de dépôt (responsable qualité, opérateur)',
      call(RQ, 'GET', '/documents-externes/types')[0] == 403 and deposer(OP, {'typeCode': 'SUPPLIER_ORDER'}, pdf('x'))[0] == 403)

# ── B. Dépôts et refus motivés ────────────────────────────────────────
st, r = deposer(ACHATS, {'typeCode': 'CONTROL_REPORT', 'numeroRapport': 'RC2607001', 'numeroCommandeFournisseur': 'CF2607001'}, pdf('a'))
check('B1 type d\'un autre service refusé', st == 400 and 'autre service' in message(r), (st, r))
st, r = deposer(QUALITE, {'typeCode': 'CONTROL_REPORT', 'numeroRapport': 'RC2607001', 'numeroCommandeFournisseur': 'CF2607001'}, b'PK\x03\x04 pas un pdf')
check('B2 fichier qui n\'est pas un PDF refusé', st == 400 and 'PDF' in message(r), (st, r))
st, r = deposer(QUALITE, {'typeCode': 'CONTROL_REPORT', 'numeroRapport': 'RC2607001', 'numeroCommandeFournisseur': 'CF9999999'}, pdf('a'))
check('B3 commande fournisseur inconnue de Sage refusée', st == 400 and 'inconnue de Sage' in message(r), (st, r))
st, r = deposer(QUALITE, {'typeCode': 'CONTROL_REPORT', 'numeroRapport': 'RC 2607/1', 'numeroCommandeFournisseur': 'CF2607001'}, pdf('a'))
check('B4 numéro avec caractères interdits refusé', st == 400 and 'lettres, chiffres' in message(r), (st, r))
st, r = deposer(QUALITE, {'typeCode': 'CONTROL_REPORT', 'numeroRapport': 'RC2607001'}, pdf('a'))
check('B5 numéro exigé manquant refusé', st == 400 and 'Saisissez' in message(r), (st, r))

# Premier dépôt avec la mauvaise commande (corrigé ensuite par un remplacement).
contenu_faux = pdf('rapport RC2607001 commande erronée')
st, r = deposer(QUALITE, {'typeCode': 'CONTROL_REPORT', 'numeroRapport': ' rc2607001 ', 'numeroCommandeFournisseur': 'cf2607002'}, contenu_faux, 'Scan 001.pdf')
premier = data(r) if isinstance(r, dict) else {}
check('B6 rapport déposé : numéros normalisés, nom formé par la base, empreinte du fichier',
      st == 200 and premier.get('nomFichier') == 'RC2607001-CF2607002.pdf' and premier.get('nomFichierOrigine') == 'Scan 001.pdf'
      and premier.get('empreinteSha256', '').lower() == hashlib.sha256(contenu_faux).hexdigest()
      and sql(f"SELECT FileName FROM ExternalDocument WHERE Id = '{premier.get('id')}'") == 'RC2607001-CF2607002.pdf', (st, r))
st, r = deposer(QUALITE, {'typeCode': 'CONTROL_REPORT', 'numeroRapport': 'RC2607001', 'numeroCommandeFournisseur': 'CF2607001'}, pdf('b'))
check('B7 même rapport déjà en vigueur : refus qui nomme le document existant', st == 409 and 'RC2607001-CF2607002.pdf' in message(r), (st, r))
st, r = deposer(QUALITE, {'typeCode': 'CONTROL_REPORT', 'numeroRapport': 'RC2607001', 'numeroCommandeFournisseur': 'CF2607001', 'remplacer': 'true'}, pdf('b'))
check('B8 remplacement sans motif refusé', st == 400 and 'motif' in message(r), (st, r))
contenu_rapport = pdf('rapport RC2607001 commande CF2607001')
st, r = deposer(QUALITE, {'typeCode': 'CONTROL_REPORT', 'numeroRapport': 'RC2607001', 'numeroCommandeFournisseur': 'CF2607001',
                          'remplacer': 'true', 'motifRemplacement': 'Mauvaise commande saisie'}, contenu_rapport)
rapport = data(r) if isinstance(r, dict) else {}
check('B9 remplacement avec motif : nouveau document en vigueur, l\'ancien marqué remplacé et conservé',
      st == 200 and rapport.get('nomFichier') == 'RC2607001-CF2607001.pdf' and rapport.get('remplaceDocumentId') == premier.get('id')
      and nb(f"SELECT COUNT(*) FROM ExternalDocument WHERE Id = '{premier.get('id')}' AND SupersededAt IS NOT NULL") == 1
      and nb("SELECT COUNT(*) FROM ExternalDocument WHERE ControlReportNumber = 'RC2607001' AND SupersededAt IS NULL") == 1, (st, r))

st, r = deposer(QUALITE, {'typeCode': 'CONFORMITY_CERTIFICATE', 'codeArticle': '2903201-1', 'numeroCommandeFournisseur': 'CF2607001'}, pdf('certificat'))
check('B10 certificat de conformité : CodeArticle-NumCdeFrs', st == 200 and data(r).get('nomFichier') == '2903201-1-CF2607001.pdf', (st, r))
st, r = deposer(QUALITE, {'typeCode': 'CONFORMITY_CERTIFICATE', 'codeArticle': 'ARTICLE-INCONNU', 'numeroCommandeFournisseur': 'CF2607001'}, pdf('c'))
check('B11 article inconnu de Sage refusé', st == 400 and 'inconnu de Sage' in message(r), (st, r))
st, r = deposer(ACHATS, {'typeCode': 'SUPPLIER_ORDER', 'numeroCommandeFournisseur': 'CF2607001'}, pdf('bc frs'))
check('B12 bon de commande fournisseur : NumCdeFrs', st == 200 and data(r).get('nomFichier') == 'CF2607001.pdf', (st, r))
# Le commercial saisit le n° du BL et celui de la commande ouverte Sage ; la référence client est reprise de la commande.
# BL2607001 et BL2607003 livrent la commande ouverte SOPCOV-2607001, de référence client 9400108469.
bl_type = next(t for t in data(call(COMMERCIAL, 'GET', '/documents-externes/types')[1]) if t['code'] == 'DELIVERY_NOTE')
st, r = call(COMMERCIAL, 'GET', '/documents-externes/commandes-clients/sopcov-2607001')
check('B12b BL : n° du BL et commande Sage saisis (référence reprise de Sage) ; aperçu de la référence de la commande',
      [(c['code'], c['dansLeNom']) for c in bl_type['champs']] == [('numeroBl', True), ('numeroCommande', False)]
      and bl_type['referenceClientDeLaCommande'] and st == 200 and data(r).get('referenceCommandeClient') == '9400108469', (bl_type, st, r))
st, r = deposer(COMMERCIAL, {'typeCode': 'DELIVERY_NOTE', 'numeroBl': 'BL2607001', 'numeroCommande': 'SOPCOV-2607002'}, pdf('bl'))
check('B13 BL avec une autre commande que celle que Sage lui donne refusé', st == 400 and 'livre la commande SOPCOV-2607001' in message(r), (st, r))
st, r = deposer(COMMERCIAL, {'typeCode': 'DELIVERY_NOTE', 'numeroBl': 'BL2607001', 'numeroCommande': 'SOPCOV-2607001'}, pdf('bl'))
st3, r3 = deposer(COMMERCIAL, {'typeCode': 'DELIVERY_NOTE', 'numeroBl': 'bl2607003', 'numeroCommande': 'sopcov-2607001'}, pdf('bl 3'))
check('B14 bons de livraison : NumBL-RefCdeClient (référence de la commande), plusieurs BL pour une même commande',
      st == 200 and data(r).get('nomFichier') == 'BL2607001-9400108469.pdf' and data(r).get('numeroCommande') == 'SOPCOV-2607001'
      and data(r).get('referenceCommandeClient') == '9400108469'
      and st3 == 200 and data(r3).get('nomFichier') == 'BL2607003-9400108469.pdf', (st, r, st3, r3))
st, r = deposer(COMMERCIAL, {'typeCode': 'DELIVERY_NOTE', 'numeroBl': 'BL2607004', 'numeroCommande': 'SOPCOV-2607004'}, pdf('bl 4'))
check('B15 BL dont la commande Sage n\'a pas de référence client refusé', st == 400 and 'pas de référence client' in message(r), (st, r))
st, r = deposer(COMMERCIAL, {'typeCode': 'CUSTOMER_ORDER', 'referenceCommandeClient': '  oa 044803 '}, pdf('bc client oa'))
check('B16 BC client : référence reprise telle qu\'écrite dans Sage, espace remplacé dans le nom',
      st == 200 and data(r).get('referenceCommandeClient') == 'OA 044803' and data(r).get('nomFichier') == 'OA_044803.pdf', (st, r))
st, r = deposer(COMMERCIAL, {'typeCode': 'CUSTOMER_ORDER', 'referenceCommandeClient': 'REF-INCONNUE'}, pdf('bc client x'))
check('B17 BC client : référence inconnue de Sage refusée', st == 400 and 'dans Sage' in message(r), (st, r))
st, r = deposer(DI, {'typeCode': 'CONTROL_REPORT', 'numeroRapport': 'RC2607009', 'numeroCommandeFournisseur': 'CF2607001'}, pdf('x'))
check('B18 le service DI ne dépose que ses plans de définition', st == 400 and 'autre service' in message(r), (st, r))

# ── C. Intégrité en base ──────────────────────────────────────────────
rid = rapport.get('id')
check('C1 un document externe ne se supprime pas',
      sql(f"BEGIN TRY DELETE FROM ExternalDocument WHERE Id = '{rid}' END TRY BEGIN CATCH SELECT ERROR_NUMBER() END CATCH") == '50002'
      and nb(f"SELECT COUNT(*) FROM ExternalDocument WHERE Id = '{rid}'") == 1)
check('C2 un document externe déposé ne se modifie pas',
      sql(f"BEGIN TRY UPDATE ExternalDocument SET SupplierOrderNumber = 'CF2607003' WHERE Id = '{rid}' END TRY BEGIN CATCH SELECT ERROR_NUMBER() END CATCH") == '50002'
      and sql(f"SELECT SupplierOrderNumber FROM ExternalDocument WHERE Id = '{rid}'") == 'CF2607001')

# ── D. Consultation et accès aux fichiers ─────────────────────────────
liste_achats = data(call(ACHATS, 'GET', '/documents-externes')[1])
liste_rq = data(call(RQ, 'GET', '/documents-externes')[1])
check('D1 un service liste ses documents ; le responsable qualité les voit tous (remplacé compris)',
      {d['typeCode'] for d in liste_achats} == {'SUPPLIER_ORDER'}
      and {'CONTROL_REPORT', 'CONFORMITY_CERTIFICATE', 'SUPPLIER_ORDER', 'DELIVERY_NOTE'} <= {d['typeCode'] for d in liste_rq}
      and any(d['id'] == premier.get('id') and d.get('remplaceLe') for d in liste_rq), (liste_achats, len(liste_rq)))
st, type_contenu, contenu = requete(RQ, 'GET', f'/documents-externes/{rid}/fichier')
check('D2 le responsable qualité ouvre le PDF déposé (fichier identique)', st == 200 and 'pdf' in type_contenu and contenu == contenu_rapport, (st, type_contenu))
check('D3 un service n\'ouvre pas les documents des autres services', requete(ACHATS, 'GET', f'/documents-externes/{rid}/fichier')[0] == 404)

# ── E. Traçabilité par OF en cascade ──────────────────────────────────
check('E1 traçabilité par OF réservée au responsable qualité',
      call(SUP, 'GET', f'/rapports/tracabilite/{OF}')[0] == 403 and call(ADMIN, 'GET', f'/rapports/tracabilite/{OF}')[0] == 403
      and call(QUALITE, 'GET', f'/rapports/tracabilite/{OF}')[0] == 403)
st, r = call(RQ, 'GET', f'/rapports/tracabilite/{OF}')
externes = data(r).get('externes', {}) if st == 200 else {}
etats = lambda cartes: {c['typeCode']: c['etat'] for c in cartes}
lot = next((l for l in externes.get('fournisseurs', []) if l['numeroLot'] == 'LOT-CORPS-01'), {})
check('E2 fournisseurs : lot scanné → rapport → commande CF2607001 → bon de commande et certificat, tous disponibles',
      lot.get('numeroRapport') == 'RC2607001' and lot.get('numeroCommandeFournisseur') == 'CF2607001'
      and etats(lot.get('documents', [])) == {'CONTROL_REPORT': 'DISPONIBLE', 'SUPPLIER_ORDER': 'DISPONIBLE', 'CONFORMITY_CERTIFICATE': 'DISPONIBLE'}
      and next(c for c in lot['documents'] if c['typeCode'] == 'CONTROL_REPORT').get('documentId') == rid, (st, lot))
bl = next((b for b in externes.get('clients', []) if b['numeroBl'] == 'BL2607001'), {})
bc = next((c for c in bl.get('documents', []) if c['typeCode'] == 'CUSTOMER_ORDER'), {})
check('E3 clients : BL → commande Sage SOPCOV-2607001 → référence 9400108469 ; BL disponible, BC client manquant (nom attendu, raison)',
      bl.get('numeroCommande') == 'SOPCOV-2607001' and bl.get('referenceCommandeClient') == '9400108469'
      and etats(bl.get('documents', [])).get('DELIVERY_NOTE') == 'DISPONIBLE'
      and bc.get('etat') == 'MANQUANT' and bc.get('nomFichier') == '9400108469.pdf' and '9400108469' in (bc.get('raisonManquant') or ''), bl)
article = sql(f"SELECT CodeArticle FROM MFGHEAD_OrdreFabrication WHERE NumeroOF = '{OF}'")
pf = externes.get('produitFini', [])
check('E4 produit fini : plan de définition de l\'article et rapport de contrôle rapide attendus, manquants',
      [c['typeCode'] for c in pf] == ['FINISHED_PRODUCT_DEFINITION_PLAN', 'FINISHED_PRODUCT_CONTROL_REPORT']
      and all(c['etat'] == 'MANQUANT' for c in pf) and pf[0]['nomFichier'] == f'{article}.pdf' and pf[1]['nomFichier'] == f'{OF}.pdf', pf)

st1, _ = deposer(COMMERCIAL, {'typeCode': 'CUSTOMER_ORDER', 'referenceCommandeClient': '9400108469'}, pdf('bc client'))
st2, r2 = deposer(QUALITE, {'typeCode': 'FINISHED_PRODUCT_CONTROL_REPORT', 'numeroOf': OF.lower()}, pdf('rapport pf'))
st4, r4 = deposer(DI, {'typeCode': 'FINISHED_PRODUCT_DEFINITION_PLAN', 'codeArticle': article}, pdf('dessin d\'ensemble'))
externes = data(call(RQ, 'GET', f'/rapports/tracabilite/{OF}')[1]).get('externes', {})
bl = next((b for b in externes.get('clients', []) if b['numeroBl'] == 'BL2607001'), {})
check('E5 après dépôt du BC client, du rapport du produit fini et du plan de définition (service DI) : disponibles',
      st1 == 200 and st2 == 200 and data(r2).get('nomFichier') == f'{OF}.pdf' and st4 == 200 and data(r4).get('nomFichier') == f'{article}.pdf'
      and etats(bl.get('documents', [])) == {'DELIVERY_NOTE': 'DISPONIBLE', 'CUSTOMER_ORDER': 'DISPONIBLE'}
      and etats(externes.get('produitFini', [])) == {'FINISHED_PRODUCT_DEFINITION_PLAN': 'DISPONIBLE', 'FINISHED_PRODUCT_CONTROL_REPORT': 'DISPONIBLE'},
      (st1, st2, st4, externes))

# Lot dont l'étiquette ne porte pas de n° de rapport : ses documents fournisseur manquent, avec la raison.
lots = data(call(RQ, 'GET', '/rapports/tracabilite/OF2607ASS02')[1]).get('externes', {}).get('fournisseurs', [])
sans_rapport = next((l for l in lots if l['numeroLot'] == 'LOT-ECROUS-01'), {})
check('E6 étiquette sans n° de rapport : rapport, bon de commande et certificat manquants, raison donnée',
      sans_rapport and all(c['etat'] == 'MANQUANT' for c in sans_rapport['documents'])
      and 'étiquette' in sans_rapport['documents'][0].get('raisonManquant', ''), sans_rapport)
check('E7 OF inconnu de Sage : 404', call(RQ, 'GET', '/rapports/tracabilite/OF-INEXISTANT')[0] == 404)
# Plan produit fini générique actif de la famille de l'article (géré par le superviseur qualité, non rempli par les opérateurs).
famille = sql(f"SELECT p.FamilleProduitFiniCode FROM ProduitFini p JOIN MFGHEAD_OrdreFabrication o ON o.CodeArticle = p.CodeArticle WHERE o.NumeroOF = '{OF}'")
erreur_plan = sql("BEGIN TRY INSERT INTO Document_Entete (TypeDocumentCode, Portee, Nom, Statut, FamilleProduitFiniCode, CreePar) "
                  f"VALUES ('PLAN_PF', 'GENERIQUE', 'Plan PF {famille} E2E', 'ACTIF', '{famille}', '44444') END TRY BEGIN CATCH SELECT ERROR_MESSAGE() END CATCH")
documents = data(call(RQ, 'GET', f'/rapports/tracabilite/{OF}')[1]).get('documents', [])
# Sans modèle Excel (pas encore fourni pour ce type), le plan est manquant comme tout document sans modèle.
check('E8 plan produit fini de la famille de l\'article (géré par le superviseur) : dans la rubrique produit fini, manquant sans modèle Excel',
      not erreur_plan and any(d['operation'] == 'PF' and d['typeCode'] == 'PLAN_PF' and d['nom'] == f'Plan PF {famille} E2E'
                              and d['etat'] == 'MANQUANT' and d.get('raisonManquant') for d in documents),
      (erreur_plan, [(d['operation'], d['typeCode'], d.get('etat')) for d in documents]))

# ── F. Responsable DI : la fabrication seulement ; les documents qualité appartiennent au superviseur qualité ──
RDI = jwt('88885', 'RESPONSABLE_DI')
inconnu = '00000000-0000-0000-0000-000000000001'
check('F1 responsable DI : ni création ni modification des plans de vérification machine et d\'échantillonnage',
      call(RDI, 'POST', '/DocumentVerifMachine', {})[0] == 403 and call(RDI, 'PUT', f'/DocumentVerifMachine/{inconnu}', {})[0] == 403
      and call(RDI, 'POST', '/documents-echantillonnage', {})[0] == 403 and call(RDI, 'PUT', f'/documents-echantillonnage/{inconnu}/activer')[0] == 403)
check('F2 responsable DI : archiver ou supprimer un document qualité refusé, un plan de fabrication permis',
      call(RDI, 'PUT', f'/hub/plans/VM/{inconnu}/statut?statut=ARCHIVE')[0] == 403 and call(RDI, 'DELETE', f'/hub/plans/ECH/{inconnu}')[0] == 403
      and call(RDI, 'PUT', f'/hub/plans/FAB/{inconnu}/statut?statut=ARCHIVE')[0] == 400)
check('F3 responsable DI : import Excel des documents qualité refusé',
      deposer_vers(RDI, '/ExcelImport/verif-machine') == 403 and deposer_vers(RDI, '/ExcelImport/controle-poste') == 403)
check('F4 responsable DI : consultation et tableau de bord permis',
      call(RDI, 'GET', '/documents-echantillonnage/actif')[0] == 200 and call(RDI, 'GET', '/hub/statistiques')[0] == 200
      and call(RDI, 'GET', '/hub/plans')[0] == 200)
check('F5 responsable qualité : consulte le hub sans modifier', call(RQ, 'PUT', f'/hub/plans/FAB/{inconnu}/statut?statut=ARCHIVE')[0] == 403
      and call(RQ, 'GET', '/hub/plans')[0] == 200)

print(f'\n{ok} OK / {ko} KO')
