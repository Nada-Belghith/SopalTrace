import base64, hashlib, hmac, json, time, subprocess, urllib.parse, urllib.request, urllib.error
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
    st, r = call(SUP, 'POST', '/Document', document('FE-TEST-44', extras, image))
    check(f'{libelle} (400, message explicite)', st == 400 and mot in texte(r), (st, r))
check('documents refusés sans aucun effet : le document et son formulaire restent actifs, aucune version créée',
      sql(f"SELECT Statut FROM Document_Entete WHERE Id='{doc}'") == 'ACTIF'
      and sql("SELECT COUNT(*) FROM Ref_Formulaire WHERE CodeReference='FE-TEST-44'") == '1'
      and sql("SELECT Statut FROM Ref_Formulaire WHERE CodeReference='FE-TEST-44'") == 'ACTIF')

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
check("changer le type d'une colonne en correction mineure refusé (400) : nouvelle version obligatoire",
      st == 400 and 'nouvelle version' in texte(r), (st, r))

st, r = call(ADMIN, 'POST', '/DocumentVerifMachine', {'nom': 'VM', 'machineCode': 'MAS20', 'familles': [], 'lignesConformite': [],
    'lignesRisques': [{'ordreAffiche': 1, 'typeLigne': 'RISQUE', 'libelleRisque': 'x' * 251, 'libelleMethode': 'm', 'echeances': []}]})
check('vérification machine : validateur actif (libellé > 250 refusé)', st == 400 and '250' in texte(r), (st, r))

# Changement de code : FE-TEST-44.0 devient FE-TEST-44-B.0. L'ancien formulaire et son document sont archivés tels quels
# (traçabilité), le document est recréé à l'identique sur la nouvelle ligne (même version).
formulaire = sql("SELECT LOWER(CAST(Id AS VARCHAR(36))) FROM Ref_Formulaire WHERE CodeReference='FE-TEST-44'")
st, r = call(ADMIN, 'PUT', f'/referentiels/formulaires/{formulaire}/code', {'code': 'FE-TEST-44-B'})
check('changer le code : réservé au superviseur qualité (403)', st == 403, (st, r))
st, r = call(SUP, 'PUT', f'/referentiels/formulaires/{formulaire}/code', {'code': 'FE-RC-PAS72'})
check("changer le code : code d'un autre formulaire refusé (400)", st == 400 and 'FE-RC-PAS72' in texte(r), (st, r))
st, r = call(SUP, 'GET', f'/referentiels/formulaires/{formulaire}/documents-actifs')
check('documents recréés présentés au superviseur avant de confirmer',
      st == 200 and [str(d.get('id')).lower() for d in data(r)] == [str(doc).lower()], (st, r))
LIGNES = "SELECT CONCAT(COUNT(*), '/', SUM(CASE WHEN i.LineId IS NULL THEN 0 ELSE 1 END)) FROM Document_Ligne l LEFT JOIN DocumentLineImage i ON i.LineId=l.Id WHERE l.EnteteId='{}'"
VALEURS = ("SELECT STRING_AGG(CONCAT(l.OrdreAffiche, v.CleColonne, '=', v.ValeurColonne), ',') WITHIN GROUP (ORDER BY l.OrdreAffiche, v.CleColonne) "
           "FROM Document_Ligne_ExtraColonne v JOIN Document_Ligne l ON l.Id=v.LigneId WHERE l.EnteteId='{}'")
lignes_avant, valeurs_avant = sql(LIGNES.format(doc)), sql(VALEURS.format(doc))

st, r = call(SUP, 'PUT', f'/referentiels/formulaires/{formulaire}/code', {'code': 'fe-test-44-b'})
nouveau_form = str((data(r) or {}).get('formulaireId', '')).lower() if st == 200 else ''
check('changer le code : nouvelle ligne FE-TEST-44-B même version, même lignée ; FE-TEST-44.0 archivé',
      st == 200 and (data(r) or {}).get('documentsRecrees') == 1 and nouveau_form != formulaire
      and sql(f"SELECT CONCAT(CodeReference, '.', Version, ' ', Statut) FROM Ref_Formulaire WHERE Id='{nouveau_form}'") == 'FE-TEST-44-B.0 ACTIF'
      and sql(f"SELECT CONCAT(CodeReference, '.', Version, ' ', Statut) FROM Ref_Formulaire WHERE Id='{formulaire}'") == 'FE-TEST-44.0 ARCHIVE'
      and sql(f"SELECT COUNT(DISTINCT VersionGroupId) FROM Ref_Formulaire WHERE Id IN ('{formulaire}','{nouveau_form}')") == '1', (st, r))
check('ancien document archivé tel quel, sur FE-TEST-44.0',
      sql(f"SELECT CONCAT(Statut, ' ', LOWER(CAST(FormulaireId AS VARCHAR(36)))) FROM Document_Entete WHERE Id='{doc}'") == f'ARCHIVE {formulaire}')
recree = sql(f"SELECT LOWER(CAST(Id AS VARCHAR(36))) FROM Document_Entete WHERE FormulaireId='{nouveau_form}'")
check("document recréé à l'identique sur FE-TEST-44-B.0 : même nom, même version, lignes, croquis et valeurs, motif",
      bool(recree) and sql(f"SELECT CONCAT(Nom, '|', Version, '|', Statut) FROM Document_Entete WHERE Id='{recree}'")
          == sql(f"SELECT CONCAT(Nom, '|', Version, '|ACTIF') FROM Document_Entete WHERE Id='{doc}'")
      and sql(LIGNES.format(recree)) == lignes_avant and sql(VALEURS.format(recree)) == valeurs_avant
      and 'FE-TEST-44-B' in sql(f"SELECT CommentaireVersion FROM Document_Entete WHERE Id='{recree}'"), (recree, lignes_avant, valeurs_avant))
check('changement journalisé avec le matricule du jeton (ligne remplacée, ligne qui la remplace)',
      sql(f"SELECT CONCAT(LOWER(CAST(NewFormId AS VARCHAR(36))), ' ', ChangedBy) FROM FormCodeChange WHERE PreviousFormId='{formulaire}'") == f'{nouveau_form} 44444')
st, r = call(SUP, 'GET', '/rapports/documents?recherche=' + urllib.parse.quote('Plan FE-TEST-44'))
par_id = {str(d.get('documentEnteteId', '')).lower(): d.get('formulaire') for d in (data(r) or [])}
check('traçabilité : chaque document garde son code.version (FE-TEST-44 v0, FE-TEST-44-B v0)',
      str(par_id.get(str(doc).lower(), '')).startswith('FE-TEST-44 ') and str(par_id.get(recree, '')).startswith('FE-TEST-44-B '), par_id)

check('formulaire utilisé : son code ne se modifie plus en base (nouvelle ligne obligatoire)',
      'Changer de code' in sql(f"UPDATE Ref_Formulaire SET CodeReference='FE-TEST-44-X' WHERE Id='{formulaire}';")
      and sql(f"SELECT CodeReference FROM Ref_Formulaire WHERE Id='{formulaire}'") == 'FE-TEST-44')
check('document générique actif impossible sur un formulaire archivé',
      'formulaire en cours' in sql(f"UPDATE Document_Entete SET Statut='ACTIF' WHERE Id='{doc}';")
      and sql(f"SELECT Statut FROM Document_Entete WHERE Id='{doc}'") == 'ARCHIVE')
check('un seul document générique actif par formulaire',
      'UX_DocumentEntete_GenericActiveForm' in sql(
          f"DECLARE @n UNIQUEIDENTIFIER; EXEC dbo.CopyDocumentToForm '{recree}', '{nouveau_form}', 'ACTIF', '44444', N'doublon', @n OUTPUT;"))
st, r = call(SUP, 'PUT', f'/referentiels/formulaires/{nouveau_form}/code', {'code': 'FE-TEST-44'})
check('revenir à FE-TEST-44 version 0 refusé : cette identité existe déjà (archivée)', st == 400 and 'FE-TEST-44 version 0' in texte(r), (st, r))
check('journal des changements de code : ni modifiable ni supprimable',
      'historique des codes' in sql('DELETE FROM FormCodeChange;').lower() and sql('SELECT COUNT(*) FROM FormCodeChange') == '1')

# Formulaire jamais utilisé : son code n'est encore qu'un projet, il est corrigé sur place.
st, r = call(SUP, 'POST', '/referentiels/formulaires', {'role': 'EN_COURS_DE_ASSEMBLAGE', 'code': 'FE-PROJET-44', 'natureArticleCode': 'CORPS'})
projet = str(data(r)).lower() if st == 200 else ''
st, r = call(SUP, 'PUT', f'/referentiels/formulaires/{projet}/code', {'code': 'FE-PROJET-44-B'})
check('formulaire jamais utilisé : code corrigé sur place, sans nouvelle ligne ni journal',
      st == 200 and str((data(r) or {}).get('formulaireId', '')).lower() == projet
      and sql(f"SELECT CodeReference FROM Ref_Formulaire WHERE Id='{projet}'") == 'FE-PROJET-44-B'
      and sql(f"SELECT COUNT(*) FROM FormCodeChange WHERE PreviousFormId='{projet}'") == '0', (st, r))

# Rôles des formulaires (table FormRole) et champ d'application conforme au rôle.
st, r = call(SUP, 'GET', '/referentiels/formulaires/roles')
roles = {x['code']: x for x in (data(r) or [])} if st == 200 else {}
check('rôles des formulaires lus en base : 9 rôles (registre de traçabilité compris), libellés et critères permis',
      st == 200 and len(roles) == 9 and roles['EN_COURS_DE_ASSEMBLAGE']['parNature'] and roles['EN_COURS_DE_ASSEMBLAGE']['parPoste']
      and roles['REGISTRE_TRACABILITE']['nature'] == 'RESPONSE_SHEET'
      and roles['RESULTAT_CONTROLE_CF']['parFamille'] and roles['RESULTAT_CONTROLE_CF']['parTypeRobinet'] and roles['RESULTAT_CONTROLE_CF']['parPoste']
      and not roles['PRODUIT_FINI']['parMachine'] and roles['VERIF_MACHINE']['parMachine'] and roles['PRODUIT_FINI']['libelle'], (st, r))
st, r = call(SUP, 'POST', '/referentiels/formulaires', {'role': 'PRODUIT_FINI', 'code': 'FE-REFUS-44', 'machineCode': 'MAS20'})
check('critère que le rôle ne prévoit pas (plan produit fini par machine) : refusé avec le critère en cause',
      st == 400 and 'machine' in texte(r) and sql("SELECT COUNT(*) FROM Ref_Formulaire WHERE CodeReference='FE-REFUS-44'") == '0', (st, r))
st, r = call(SUP, 'POST', '/referentiels/formulaires', {'role': 'EN_COURS_DE_ASSEMBLAGE', 'code': 'FE-REFUS-44', 'familleProduitFiniCode': 'RBGFA',
                                                     'natureArticleCode': 'PISTON'})
check('nature avec une famille : refusé (la nature ne désigne un formulaire que sans famille)', st == 400 and 'nature' in texte(r), (st, r))
st, r = call(SUP, 'POST', '/referentiels/formulaires', {'role': 'EN_COURS_DE_ASSEMBLAGE', 'code': 'FE-TYPE-44', 'familleProduitFiniCode': 'RBGFA',
                                                     'posteCode': 'PAS78', 'typeRobinetCode': 'MAN'})
check('type de robinet repris de la famille (RBGFA : AUTO), jamais saisi à part',
      st == 200 and sql("SELECT TypeRobinetCode FROM Ref_Formulaire WHERE CodeReference='FE-TYPE-44'") == 'AUTO', (st, r))
check('la base refuse un type de robinet différent de celui de la famille',
      'celui de sa famille' in sql("UPDATE Ref_Formulaire SET TypeRobinetCode='MAN' WHERE CodeReference='FE-TYPE-44'")
      and sql("SELECT TypeRobinetCode FROM Ref_Formulaire WHERE CodeReference='FE-TYPE-44'") == 'AUTO')
st, r = call(SUP, 'GET', '/referentiels/formulaires/champs-application')
listes = data(r) or {}
check("listes du champ d'application : types de robinet, et type de chaque famille",
      st == 200 and {'AUTO', 'MAN', 'SOUP'} <= {t['code'] for t in listes.get('typesRobinet', [])}
      and next((f['typeRobinet'] for f in listes.get('familles', []) if f['code'] == 'RBGFM'), None) == 'MAN', (st, r))
st, r = call(SUP, 'GET', '/referentiels/formulaires/liste/EN_COURS_DE_ASSEMBLAGE')
choisi = next((f for f in (data(r) or []) if f['codeReference'] == 'FE-TYPE-44'), {})
check("liste des formulaires : champ d'application complet (repris et verrouillé à la création des documents)",
      st == 200 and (choisi.get('familleProduitFiniCode'), choisi.get('posteCode'), choisi.get('typeRobinetCode')) == ('RBGFA', 'PAS78', 'AUTO')
      and 'machineCode' in choisi and 'natureArticleCode' in choisi, (st, choisi))

# Page Formulaires : tous les formulaires en cours par rôle, avec l'état de leur modèle et les modèles du rôle.
st, r = call(SUP, 'GET', '/rapports/formulaires')
par_role = {g['role']: g for g in (data(r) or [])} if st == 200 else {}
vm = {f['codeReference']: f for f in par_role.get('VERIF_MACHINE', {}).get('formulaires', [])}
check('formulaires par rôle : tous les rôles, modèles du rôle, modèle de chaque formulaire',
      st == 200 and len(par_role) == 9
      and {'VERIF_MACHINE_MAS22', 'VERIF_MACHINE'} <= {m['code'] for m in par_role['VERIF_MACHINE']['modeles']}
      and (vm.get('FE-VM-MAS22') or {}).get('modeleActuel', {}).get('codeModele') == 'VERIF_MACHINE_MAS22'
      and any(f['codeReference'] == 'FE-TEST-44-B' for f in par_role['EN_COURS_DE_ASSEMBLAGE']['formulaires'])
      and not any(f['codeReference'] == 'FE-TEST-44' for f in par_role['EN_COURS_DE_ASSEMBLAGE']['formulaires']), (st, list(par_role)))
check("formulaires par rôle : superviseur qualité et administrateur seulement (qui rattache les modèles)",
      call(ADMIN, 'GET', '/rapports/formulaires')[0] == 200 and call(jwt('22222', 'RESPONSABLE_QUALITE'), 'GET', '/rapports/formulaires')[0] == 403)
st, r = call(SUP, 'GET', '/referentiels/formulaires/champs-application')
listes = data(r) if st == 200 else {}
check("listes du champ d'application : familles, postes, machines, natures actifs",
      st == 200 and any(e['code'] == 'RBGFA_SOUPAPE' for e in listes['familles']) and any(e['code'] == 'PAS71' for e in listes['postes'])
      and any(e['code'] == 'MAS22' for e in listes['machines']) and any(e['code'] == 'PISTON' for e in listes['natures']), (st, r))
st, r = call(SUP, 'GET', '/rapports/documents?recherche=' + urllib.parse.quote('Plan FE-TEST-44'))
check('document : son formulaire (lien « Rattacher un modèle »)',
      any(str(d.get('formulaireId', '')).lower() == nouveau_form for d in (data(r) or [])), (st, r))

# Nouvelle version avec une colonne retirée alors que les lignes portent encore sa valeur (éditeur rouvert) : la valeur
# de la colonne retirée est abandonnée, la nouvelle version est créée.
st, r = call(SUP, 'POST', '/referentiels/formulaires', {'role': 'EN_COURS_DE_ASSEMBLAGE', 'code': 'FE-TEST-44-COL', 'natureArticleCode': 'ECROU'})
plan_col = lambda cols, extras: {'typeDocumentCode': 'PLAN_ASS', 'nom': 'Plan colonnes 44', 'natureArticleCode': 'ECROU',
                                 'refFormulaireCodeReference': 'FE-TEST-44-COL', 'colonneDefs': cols,
                                 'sections': [{'ordreAffiche': 1, 'libelleSection': 'Contrôle', 'lignes': [
                                     {'ordreAffiche': 1, 'libelleAffiche': 'Serrage', 'extraColonnes': extras}]}]}
deux = [{'key': 'custom_1', 'label': 'test', 'type': 'TEXT'}, {'key': 'custom_2', 'label': 'test2', 'type': 'TEXT'}]
avec_valeurs = [{'cleColonne': 'custom_1', 'valeurColonne': 'a'}, {'cleColonne': 'custom_2', 'valeurColonne': 'b'}]
st, r = call(SUP, 'POST', '/Document', plan_col(deux, avec_valeurs)); doc_col = (data(r) or {}).get('id') if isinstance(data(r), dict) else None
st, r = call(SUP, 'POST', f'/Document/{doc_col}/version', dict(plan_col(deux[:1], avec_valeurs), ancienId=doc_col, motifModification='colonne retirée'))
v1_col = (data(r) or {}).get('id') if isinstance(data(r), dict) else None
check('nouvelle version, colonne retirée mais encore dans les lignes : créée, valeur de la colonne retirée abandonnée',
      st == 201 and sql(f"SELECT STRING_AGG(x.CleColonne + '=' + ISNULL(x.ValeurColonne, ''), ',') FROM Document_Ligne_ExtraColonne x "
                        f"JOIN Document_Ligne l ON l.Id = x.LigneId WHERE l.EnteteId = '{v1_col}'") == 'custom_1=a', (st, r))

print(f'\n{ok} OK / {ko} KO')
