"""
Assistant de l'opérateur d'assemblage : avancement calculé par le serveur.
- plan d'assemblage choisi par la famille du produit fini puis le poste (jamais le plan d'une autre famille) ;
- démarrage : vérification machine + première pièce (LOT_POSTE à chaque équipe, LOT_OF pour la première seulement) ;
- production : contrôles ECHANTILLONNAGE_2859 ; vérification « après pause » seulement après la pause déjeuner ;
- fin de poste / fin d'OF : refusée tant qu'une tâche de fin manque, sur tous les postes.
"""
import base64, hashlib, hmac, json, time, urllib.request, urllib.error
from configuration import BASE, SECRET, sql


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

SUP = jwt('44444', 'SUPERVISEUR_QUALITE'); ADMIN = jwt('66666', 'ADMIN'); OP = jwt('55555', 'OPERATEUR')
ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])

TYPE = {t: sql(f"SELECT Id FROM TypeSection WHERE TypeNormalise='{t}'") for t in ('LOT_POSTE', 'LOT_OF', 'ECHANTILLONNAGE_2859', 'REGLAGE', 'TOUS_PIECES')}

def plan(famille, poste, types):
    sections = [{'ordreAffiche': i + 1, 'libelleSection': t, 'typeSectionId': TYPE[t],
                 'lignes': [{'ordreAffiche': 1, 'libelleAffiche': f'Étanchéité {t}'}]} for i, t in enumerate(types)]
    st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'PLAN_ASS', 'nom': f'Plan {famille} {poste or "famille"}', 'familleProduitFiniCode': famille,
                                            'posteCode': poste, 'refFormulaireCodeReference': f'FE-ASS-{famille}-{poste or "FAMILLE"}', 'sections': sections})
    doc = data(r).get('id') if isinstance(data(r), dict) else None
    statut = sql(f"SELECT Statut FROM Document_Entete WHERE Id='{doc}'")
    check(f'plan d\'assemblage {famille} / {poste} créé directement actif (document générique)', st in (200, 201) and statut == 'ACTIF', (st, statut, r))

def avancement(exec_id, poste):
    st, r = call(OP, 'GET', f'/operateur/of/{exec_id}/assemblage/avancement?posteCode={poste}')
    return r if st == 200 else {'erreur': (st, r)}
def tache(av, code, periodicite=None):
    return next((t for t in av.get('taches', []) if t['code'] == code and (periodicite is None or t.get('periodicite') == periodicite)), {})
def sections(exec_id, poste):
    st, r = call(OP, 'GET', f'/ExecPlanAssemblage/{exec_id}?posteCode={poste}')
    return {s['typeSection']: s for s in r['sections']} if st == 200 else {}
def saisir(exec_id, poste, section, moment, resultat='C'):
    return call(OP, 'POST', f'/ExecPlanAssemblage/{exec_id}/resultat', {
        'posteCode': poste, 'sectionId': section['id'], 'moment': moment,
        'lignesControle': [{'ligneId': l['id'], 'resultat': resultat} for l in section['lignesPlan']]})
def machines(exec_id, poste, periodicite):
    st, r = call(OP, 'GET', f'/operateur/of/{exec_id}/poste/{poste}/verif-machines?periodicite={periodicite}')
    return r if st == 200 else {'erreur': (st, r), 'machines': []}
def machine(liste, code):
    return next((m for m in liste.get('machines', []) if m['machineCode'] == code), {})
def points(code, periodicite):
    """Points (échéances) du plan actif de la machine pour ce moment."""
    return sql(f"SELECT g.Id FROM Document_VerifMachine_Echeance g JOIN Document_Ligne l ON l.Id=g.LigneId "
               f"JOIN Document_Entete e ON e.Id=l.EnteteId JOIN PeriodiciteMachine p ON p.Id=g.PeriodiciteMachineId "
               f"WHERE e.TypeDocumentCode='VERIF_MACHINE' AND e.Statut='ACTIF' AND e.MachineCode='{code}' "
               f"AND p.ControlMoment = '{periodicite}'").split()
def valider(statut, code, periodicite, conforme=True, observation=None, sauf=0):
    reponses = [{'documentVerifMachineEcheanceId': g, 'conforme': conforme, 'observation': observation}
                for g in points(code, periodicite)[sauf:]]
    return call(OP, 'POST', f'/operateur/verif-machine/{statut}/valider', {'periodicite': periodicite, 'reponses': reponses})
def statut_machine(exec_id, poste, code):
    """Document de la machine pour l'équipe (créé à la première ouverture, comme le bouton « Remplir »)."""
    m = machine(machines(exec_id, poste, 'DEMARRAGE'), code)
    if m.get('statutId'): return m['statutId']
    st, r = call(OP, 'POST', f'/operateur/of/{exec_id}/poste/{poste}/verif-machines', {'machineCode': code})
    return r.get('id') if st == 200 else None

# ───────────── Référentiel : plan d'échantillonnage et plans d'assemblage ─────────────
st, r = call(ADMIN, 'POST', '/documents-echantillonnage', {'niveauControle': 'II', 'typePlan': 'SIMPLE', 'modeControle': 'NORMAL', 'valeurNqa': 1.5,
    'critereAcceptationAc': 1, 'critereRejetRe': 2, 'remarques': '', 'legendeMoyens': '', 'refFormulaireCodeReference': 'FE-ECHAN-01'})
check('plan d\'échantillonnage actif', st == 200, (st, r))
plan('RBGFM', 'PAS71', ['LOT_POSTE', 'LOT_OF', 'ECHANTILLONNAGE_2859'])
plan('RBGFA_SOUPAPE', 'PAS72', ['LOT_POSTE', 'LOT_OF', 'ECHANTILLONNAGE_2859', 'REGLAGE', 'TOUS_PIECES'])

st, r = call(OP, 'GET', '/referentiels/verif-machine')
moments = {p.get('code'): p.get('controlMoment') for p in (data(r) or {}).get('periodicitesMachine', [])} if st == 200 else {}
check('périodicités machine : moment de contrôle exposé par code (DEM, PAU, FIN)',
      moments == {'DEM': 'DEMARRAGE', 'PAU': 'APRES_PAUSE', 'FIN': 'FIN_POSTE'}, (st, moments))

# ───────────── A. OF manuel (OF2607ASS01) sur PAS71 + PAS72 ─────────────
st, r = call(OP, 'POST', '/operateur/of/start-assemblage', {'numeroOf': 'OF2607ASS01', 'posteCodes': ['PAS71', 'PAS72'], 'equipe': 'E1'})
check('A : OF manuel démarré sur deux postes', st == 200, (st, r)); ofa = data(r)['id'] if st == 200 else None

av = avancement(ofa, 'PAS71')
check('A : étape 1 = échantillonnage (bloquante)', av.get('phase') == 'ECHANTILLONNAGE' and tache(av, 'ECHANTILLONNAGE').get('etat') == 'A_FAIRE', av)
check('A : famille et type de robinet déduits de l\'article', (av.get('familleProduitFiniCode'), av.get('typeRobinetCode')) == ('RBGFM', 'MAN'), av)
st, r = call(OP, 'POST', f'/ExecEchantillonnage/init/{ofa}', {'posteCode': 'PAS71', 'nbPostes': 2, 'instrumentCodes': [], 'equipe': 'E1'})
check('A : fiche d\'échantillonnage remplie', st == 200, (st, r))

av72 = avancement(ofa, 'PAS72')
check('A : PAS72 en démarrage', av72.get('phase') == 'DEMARRAGE', av72)
check('A : article manuel sur PAS72 → plan soupape jamais repris (document manquant)',
      tache(av72, 'PLAN_PREMIERE_PIECE').get('etat') == 'MANQUANT', tache(av72, 'PLAN_PREMIERE_PIECE'))
st, r = call(OP, 'GET', f'/ExecPlanAssemblage/{ofa}?posteCode=PAS72')
check('A : lecture du plan PAS72 → 404 explicite (famille)', st == 404 and 'famille' in json.dumps(r, ensure_ascii=False), (st, r))

av71 = avancement(ofa, 'PAS71')
check('A : PAS71 démarrage → vérification machine à faire', tache(av71, 'VERIF_MACHINE', 'DEMARRAGE').get('etat') == 'A_FAIRE', av71)
check('A : compteur par poste', {p['posteCode']: p['tachesRestantes'] for p in av71.get('postes', [])} == {'PAS71': 2, 'PAS72': 2}, av71.get('postes'))
st, _ = call(OP, 'POST', f'/Operateur/of/{ofa}/verif-machine/cloturer-tous?posteCode=PAS71')
check('A : vérifications machine du poste clôturées', st in (200, 204), st)

sec = sections(ofa, 'PAS71')
check('A : seules les sections de lot sont saisissables', set(sec) == {'LOT_POSTE', 'LOT_OF'}, list(sec))
check('A : première équipe → début de poste ET début d\'OF attendus',
      sec.get('LOT_POSTE', {}).get('premierePiece', {}).get('requis') and sec.get('LOT_OF', {}).get('premierePiece', {}).get('requis'), sec)
st, r = saisir(ofa, 'PAS71', sec['LOT_POSTE'], 'DERNIERE_PIECE')
check('A : dernière pièce refusée avant la demande de fin', st == 400, (st, r))
st, r = saisir(ofa, 'PAS71', sec['LOT_POSTE'], 'PREMIERE_PIECE')
check('A : première pièce début de poste enregistrée', st == 204, (st, r))
check('A : début d\'OF encore attendu → toujours en démarrage', avancement(ofa, 'PAS71').get('phase') == 'DEMARRAGE')
st, r = saisir(ofa, 'PAS71', sec['LOT_OF'], 'PREMIERE_PIECE')
check('A : première pièce début d\'OF enregistrée', st == 204, (st, r))
av71 = avancement(ofa, 'PAS71')
check('A : PAS71 passe en production', av71.get('phase') == 'PRODUCTION', av71)
check('A : contrôle de production listé, non bloquant', tache(av71, 'CONTROLES_PRODUCTION').get('bloquante') is False, av71)

st, _ = call(OP, 'POST', f'/operateur/of/{ofa}/assemblage/demande-cloture', {'mode': 'FIN_POSTE'})
av71 = avancement(ofa, 'PAS71')
check('A : fin de poste demandée → étape fin de poste (tenue par le serveur)', st == 204 and av71.get('phase') == 'FIN_POSTE' and av71.get('demandeCloture') == 'FIN_POSTE', av71)
check('A : plan absent sur PAS72 → signalé mais ne bloque pas la fin',
      not any(m.startswith('PAS72 : Plan') for m in av71.get('manquants', [])), av71.get('manquants'))
check('A : manquants listés poste par poste, PAS72 compris',
      any(m.startswith('PAS72 :') for m in av71.get('manquants', [])) and any(m.startswith('PAS71 :') for m in av71.get('manquants', [])) and not av71.get('peutTerminer'), av71.get('manquants'))
st, r = call(OP, 'POST', f'/operateur/of/{ofa}/assemblage/terminer')
check('A : terminer refusé tant que des tâches manquent (400 détaillé)', st == 400 and 'il reste' in json.dumps(r, ensure_ascii=False), (st, r))
st, _ = call(OP, 'DELETE', f'/operateur/of/{ofa}/assemblage/demande-cloture')
check('A : annulation → retour en production', st == 204 and avancement(ofa, 'PAS71').get('phase') == 'PRODUCTION')
st, r = call(OP, 'POST', f'/Operateur/of/{ofa}/fin-poste')
check("A : fin de poste directe refusée à l'opérateur tant que des tâches manquent", st == 400 and 'il reste' in json.dumps(r, ensure_ascii=False), (st, r))
st, r = call(OP, 'POST', f'/Operateur/of/{ofa}/close')
check("A : clôture directe refusée à l'opérateur", st == 400, (st, r))
st, r = call(ADMIN, 'POST', f'/Operateur/of/{ofa}/close')
check("A : l'administrateur peut forcer la clôture", st == 204, (st, r))

# Plan de famille sans poste : valable sur tous les postes (le plan du poste reste prioritaire).
plan('RBGFM', None, ['LOT_POSTE'])
st, r = call(OP, 'GET', f'/ExecPlanAssemblage/{ofa}?posteCode=PAS72')
check('A : plan de famille sans poste retenu pour PAS72', st == 200 and 'RBGFM' in (r.get('libellePlan') or ''), (st, r))
st, r = call(OP, 'GET', f'/ExecPlanAssemblage/{ofa}?posteCode=PAS71')
check('A : PAS71 garde son propre plan', st == 200 and 'PAS71' in (r.get('libellePlan') or ''), (st, r))

# ───────────── B. OF soupape (OF2607ASS02) sur PAS72 : pauses, fin de poste, équipe suivante ─────────────
st, r = call(OP, 'POST', '/operateur/of/start-assemblage', {'numeroOf': 'OF2607ASS02', 'posteCodes': ['PAS72'], 'equipe': 'E1'})
check('B : OF soupape démarré sur PAS72', st == 200, (st, r)); ofb = data(r)['id'] if st == 200 else None
call(OP, 'POST', f'/ExecEchantillonnage/init/{ofb}', {'posteCode': 'PAS72', 'nbPostes': 1, 'instrumentCodes': [], 'equipe': 'E1'})
vm = machines(ofb, 'PAS72', 'DEMARRAGE')
check('B : machines du poste au démarrage (BEE46, MAS26 à faire)',
      (machine(vm, 'BEE46').get('etat'), machine(vm, 'MAS26').get('etat'), vm.get('attendues'), vm.get('validees')) == ('A_FAIRE', 'A_FAIRE', 2, 0), vm)
bee46 = statut_machine(ofb, 'PAS72', 'BEE46'); mas26 = statut_machine(ofb, 'PAS72', 'MAS26')
st, r = valider(bee46, 'BEE46', 'DEMARRAGE', sauf=1)
check('B : validation refusée tant qu\'un point du démarrage manque', st == 400 and 'sans réponse' in json.dumps(r, ensure_ascii=False), (st, r))
st, r = valider(mas26, 'MAS26', 'DEMARRAGE', conforme=False)
check('B : non-conformité sans observation refusée', st == 400 and 'observation' in json.dumps(r, ensure_ascii=False), (st, r))
st, r = valider(bee46, 'BEE46', 'DEMARRAGE')
check('B : BEE46 validée au démarrage', st == 204 and machine(machines(ofb, 'PAS72', 'DEMARRAGE'), 'BEE46').get('etat') == 'VALIDEE', (st, r))
st, _ = call(OP, 'POST', f'/operateur/verif-machine/{bee46}/corriger?periodicite=DEMARRAGE')
check('B : corriger → la vérification repasse en cours (réponses gardées)',
      st == 204 and machine(machines(ofb, 'PAS72', 'DEMARRAGE'), 'BEE46').get('etat') == 'EN_COURS')
st, _ = call(OP, 'POST', f'/operateur/verif-machine/{bee46}/valider', {'periodicite': 'DEMARRAGE', 'reponses': []})
check('B : revalidation avec les réponses déjà enregistrées', st == 204)
st, r = call(OP, 'POST', f'/operateur/of/{ofb}/poste/PAS72/verif-machines', {'machineCode': 'BEE47'})
vm = machines(ofb, 'PAS72', 'DEMARRAGE')
check('B : machine ajoutée par l\'équipe gardée dans la liste du poste',
      st == 200 and machine(vm, 'BEE47').get('ajoutee') is True and machine(vm, 'BEE47').get('etat') == 'A_FAIRE', (st, vm))
bee47 = r.get('id') if isinstance(r, dict) else None
check('B : démarrage : MAS26 et BEE47 validées',
      valider(mas26, 'MAS26', 'DEMARRAGE', conforme=False, observation='Fuite légère')[0] == 204 and valider(bee47, 'BEE47', 'DEMARRAGE')[0] == 204)
vm = machines(ofb, 'PAS72', 'DEMARRAGE')
check('B : 3 / 3 machines validées au démarrage', (vm.get('attendues'), vm.get('validees')) == (3, 3), vm)
st, r = call(OP, 'GET', f'/operateur/of/{ofb}/poste/PAS72/verif-machines/historique?machineCode=BEE46&equipe=E1')
check('B : historique filtré par machine et équipe', st == 200 and len(r) == 1 and r[0]['demarrage'] and r[0]['sessionActive'], (st, r))
sec = sections(ofb, 'PAS72')
check('B : plan soupape du poste PAS72 retenu', set(sec) == {'LOT_POSTE', 'LOT_OF'}, list(sec))
saisir(ofb, 'PAS72', sec['LOT_POSTE'], 'PREMIERE_PIECE'); saisir(ofb, 'PAS72', sec['LOT_OF'], 'PREMIERE_PIECE', resultat='C')
av = avancement(ofb, 'PAS72')
check('B : production', av.get('phase') == 'PRODUCTION', av)
check('B : registre de traçabilité à remplir pendant la production (non bloquant)', tache(av, 'TRACABILITE').get('bloquante') is False, av.get('taches'))
check('B : contrôles à la demande proposés (réglage, toutes les pièces)',
      [c['code'] for c in av.get('controlesALaDemande', [])] == ['REGLAGE', 'TOUTES_PIECES'], av.get('controlesALaDemande'))
st, r = call(OP, 'GET', f'/ExecPlanAssemblage/{ofb}?posteCode=PAS72'); demande = {x['contexte']: x for x in (r or {}).get('sectionsALaDemande', [])}
st, r = saisir(ofb, 'PAS72', demande['REGLAGE'], 'REGLAGE')
check("B : contrôle aux réglages saisi quand l'opérateur le décide", st == 204, (st, r))
st, r = saisir(ofb, 'PAS72', demande['100PCT'], '100PCT', resultat='C')
check('B : contrôle de toutes les pièces saisi', st == 204, (st, r))
st, r = saisir(ofb, 'PAS72', demande['REGLAGE'], '100PCT')
check('B : type de contrôle incohérent avec la section refusé', st == 400, (st, r))
reglage = next((c for c in avancement(ofb, 'PAS72').get('controlesALaDemande', []) if c['code'] == 'REGLAGE'), {})
check('B : dernier réglage affiché', reglage.get('etat') == 'FAIT' and reglage.get('compteur') == 1, reglage)

call(OP, 'POST', f'/Operateur/of/{ofb}/pause', {'raison': 'Réunion', 'typePause': 'AUTRE'})
check('B : en pause', avancement(ofb, 'PAS72').get('phase') == 'PAUSE')
call(OP, 'POST', f'/Operateur/of/{ofb}/reprendre')
check('B : pause « autre » → retour direct en production', avancement(ofb, 'PAS72').get('phase') == 'PRODUCTION')
call(OP, 'POST', f'/Operateur/of/{ofb}/pause', {'raison': 'Déjeuner', 'typePause': 'DEJEUNER'})
call(OP, 'POST', f'/Operateur/of/{ofb}/reprendre')
av = avancement(ofb, 'PAS72')
check('B : après le déjeuner → vérification machine « après pause » (BEE46)',
      av.get('phase') == 'REPRISE' and 'BEE46' in tache(av, 'VERIF_MACHINE', 'APRES_PAUSE').get('detail', ''), av)
vm = machines(ofb, 'PAS72', 'APRES_PAUSE')
check('B : après pause : MAS26 non prévue', machine(vm, 'MAS26').get('etat') == 'NON_PREVUE', vm)
check('B : vérifications « après pause » validées',
      valider(bee46, 'BEE46', 'APRES_PAUSE')[0] == 204 and valider(bee47, 'BEE47', 'APRES_PAUSE')[0] == 204)
check('B : vérification après pause faite → production', avancement(ofb, 'PAS72').get('phase') == 'PRODUCTION')

call(OP, 'POST', f'/operateur/of/{ofb}/assemblage/demande-cloture', {'mode': 'FIN_POSTE'})
st, r = saisir(ofb, 'PAS72', sections(ofb, 'PAS72')['LOT_OF'], 'DERNIERE_PIECE')
check('B : fin d\'OF non demandée → dernière pièce LOT_OF refusée', st == 400, (st, r))
saisir(ofb, 'PAS72', sections(ofb, 'PAS72')['LOT_POSTE'], 'DERNIERE_PIECE')
check('B : vérifications de fin de poste validées',
      valider(bee46, 'BEE46', 'FIN_POSTE')[0] == 204 and valider(bee47, 'BEE47', 'FIN_POSTE')[0] == 204)
sql(f"INSERT INTO ExecRegistreTracabilite (ExecControleOfId, Version, Corps, Volant) VALUES ('{ofb}', 1, 'C-TEST', 'V-TEST')")
st, r = call(OP, 'POST', f'/Operateur/tracabilite/{ofb}/confirmer?posteCode=PAS72')
check("B : lots de l'équipe confirmés dans le registre", st == 200, (st, r))
call(OP, 'POST', f'/ExecRcPoste/{ofb}/cloturer-tous?posteCode=PAS72')
av = avancement(ofb, 'PAS72')
check('B : toutes les tâches de fin faites → fin possible', av.get('peutTerminer') is True and av.get('manquants') == [], av.get('manquants'))
st, r = call(OP, 'POST', f'/operateur/of/{ofb}/assemblage/terminer')
check('B : poste terminé, OF toujours ouvert', st == 204 and sql(f"SELECT Statut FROM Exec_ControleOF WHERE Id='{ofb}'") == 'EN_COURS', (st, r))
check('B : sans équipe active', avancement(ofb, 'PAS72').get('phase') == 'SANS_EQUIPE')

st, r = call(OP, 'POST', f'/Operateur/of/{ofb}/ajouter-postes', {'posteCodes': [], 'equipe': 'E2'})
check('B : l\'équipe E2 rejoint l\'OF', st in (200, 204), (st, r))
av = avancement(ofb, 'PAS72')
check('B : échantillonnage déjà fait par E1 → E2 commence au démarrage', av.get('phase') == 'DEMARRAGE' and not av.get('premiereEquipe'), av)
sec = sections(ofb, 'PAS72')
check('B : E2 → début de poste attendu, début d\'OF non attendu',
      sec['LOT_POSTE']['premierePiece']['requis'] and not sec['LOT_POSTE']['premierePiece']['fait']
      and not sec['LOT_OF']['premierePiece']['requis'], sec)
st, r = saisir(ofb, 'PAS72', sec['LOT_OF'], 'PREMIERE_PIECE')
check('B : E2 → contrôle de début d\'OF refusé (OF déjà démarré)', st == 400, (st, r))
st, r = call(OP, 'POST', f'/operateur/of/{ofb}/assemblage/demande-cloture', {'mode': 'FIN_OF'})
check('B : E2 demande la clôture de l\'OF', st == 204 and avancement(ofb, 'PAS72').get('phase') == 'FIN_OF', (st, r))
check('B : fin d\'OF → dernière pièce LOT_OF attendue', sections(ofb, 'PAS72')['LOT_OF']['dernierePiece']['requis'])
call(ADMIN, 'POST', f'/Operateur/of/{ofb}/close')

# ───────────── C. Plan sans section de lot : le démarrage ne bloque pas sur la première pièce ─────────────
plan('RBGFM', 'PAS78', ['ECHANTILLONNAGE_2859'])
sql("IF NOT EXISTS (SELECT 1 FROM Mag_PreparationOF WHERE NumeroOF='OF26041131') "
    "INSERT INTO Mag_PreparationOF (Id, NumeroOF, MatriculeMagasinier, Statut, DateDebut, DateFin) "
    "VALUES (NEWID(), 'OF26041131', '33333', 'TERMINE', SYSDATETIME(), SYSDATETIME())")
st, r = call(OP, 'POST', '/operateur/of/start-assemblage', {'numeroOf': 'OF26041131', 'posteCodes': ['PAS78'], 'equipe': 'E1'})
check('C : OF démarré sur PAS78', st == 200, (st, r)); ofc = data(r)['id'] if st == 200 else None
call(OP, 'POST', f'/ExecEchantillonnage/init/{ofc}', {'posteCode': 'PAS78', 'nbPostes': 1, 'instrumentCodes': [], 'equipe': 'E1'})
av = avancement(ofc, 'PAS78')
check('C : pas de tâche « première pièce » (plan sans LOT_POSTE / LOT_OF)',
      'PLAN_PREMIERE_PIECE' not in [t['code'] for t in av.get('taches', [])], av.get('taches'))
check('C : seules les vérifications machine peuvent retenir le démarrage',
      av.get('phase') == 'PRODUCTION' or all(t['code'] == 'VERIF_MACHINE' for t in av.get('taches', [])), av)
call(OP, 'POST', f'/Operateur/of/{ofc}/verif-machine/cloturer-tous?posteCode=PAS78')
av = avancement(ofc, 'PAS78')
check('C : vérifications machine faites → production directement', av.get('phase') == 'PRODUCTION', av)
call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/demande-cloture', {'mode': 'FIN_POSTE'})
av = avancement(ofc, 'PAS78')
check('C : fin de poste : pas de « dernière pièce » attendue',
      not any('dernière pièce' in m for m in av.get('manquants', [])) and 'PLAN_DERNIERE_PIECE' not in [t['code'] for t in av.get('taches', [])], av)
# ───────────── D. Deux équipes : E1 en pause, E2 prend la main, E1 reprend son étape ─────────────
def avancement_equipe(exec_id, poste, equipe):
    st, r = call(OP, 'GET', f'/operateur/of/{exec_id}/assemblage/avancement?posteCode={poste}&equipe={equipe}')
    return r if st == 200 else {'erreur': (st, r)}

call(OP, 'DELETE', f'/operateur/of/{ofc}/assemblage/demande-cloture')
call(OP, 'POST', f'/Operateur/of/{ofc}/pause', {'raison': 'Changement', 'typePause': 'AUTRE'})
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/ajouter-postes', {'posteCodes': [], 'equipe': 'E2'})
check("D : E2 rejoint l'OF pendant la pause de E1", st in (200, 204), (st, r))
e2 = avancement_equipe(ofc, 'PAS78', 'E2'); e1 = avancement_equipe(ofc, 'PAS78', 'E1')
check('D : E2 en production, E1 vue en pause avec E2 en production',
      e2.get('phase') == 'PRODUCTION' and e1.get('phase') == 'PAUSE' and e1.get('equipeEnProduction') == 'E2' and e2.get('equipesEnPause') == ['E1'], (e1, e2))
st, r = call(OP, 'GET', '/Operateur/ofs/assemblage-statut')
of_d = next((o for o in (r or []) if o.get('numeroOf') == 'OF26041131'), {})
check('D : liste des OF : E2 a la main, E1 en pause', (of_d.get('equipeActuelle'), of_d.get('equipesEnPause')) == ('E2', ['E1']), of_d)
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/reprendre?equipe=E1')
check('D : E1 ne peut pas reprendre pendant que E2 produit', st == 409 and 'E2' in json.dumps(r, ensure_ascii=False), (st, r))
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/ajouter-postes', {'posteCodes': [], 'equipe': 'E3'})
check('D : une troisième équipe ne prend pas la main pendant que E2 produit', st == 409, (st, r))
call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/demande-cloture', {'mode': 'FIN_OF'})
e2 = avancement_equipe(ofc, 'PAS78', 'E2')
check("D : clôture de l'OF : E1 (en pause) doit d'abord faire sa fin de poste",
      any(m.startswith('Équipe E1') for m in e2.get('manquants', [])) and not e2.get('peutTerminer'), e2.get('manquants'))
call(OP, 'DELETE', f'/operateur/of/{ofc}/assemblage/demande-cloture')
call(OP, 'POST', f'/Operateur/of/{ofc}/pause', {'raison': 'Relève', 'typePause': 'AUTRE'})
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/reprendre?equipe=E1')
e1 = avancement_equipe(ofc, 'PAS78', 'E1')
check("D : E1 reprend son étape telle qu'elle l'avait laissée", st == 204 and e1.get('phase') == 'PRODUCTION' and e1.get('equipesEnPause') == ['E2'], (st, r, e1))
call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/demande-cloture', {'mode': 'FIN_POSTE'})
sql(f"INSERT INTO ExecRegistreTracabilite (ExecControleOfId, Version, Corps, Volant) VALUES ('{ofc}', 1, 'C-TEST', 'V-TEST')")
call(OP, 'POST', f'/Operateur/tracabilite/{ofc}/confirmer?posteCode=PAS78')
call(OP, 'POST', f'/ExecRcPoste/{ofc}/cloturer-tous?posteCode=PAS78')
st, r = call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/terminer')
check("D : fin de poste de E1 → l'OF reste en pause (E2 en pause)",
      st == 204 and sql(f"SELECT Statut FROM Exec_ControleOF WHERE Id='{ofc}'") == 'EN_PAUSE', (st, r))
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/reprendre?equipe=E2')
call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/demande-cloture', {'mode': 'FIN_OF'})
e2 = avancement_equipe(ofc, 'PAS78', 'E2')
check("D : E1 a terminé son poste → plus de blocage d'équipe pour la clôture",
      st == 204 and not any(m.startswith('Équipe') for m in e2.get('manquants', [])), (st, e2.get('manquants')))
call(ADMIN, 'POST', f'/Operateur/of/{ofc}/close')

print(f'e2e64: {ok} OK / {ko} KO')
