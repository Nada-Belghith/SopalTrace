"""
Assistant de l'opérateur d'assemblage : avancement calculé par le serveur.
- plan d'assemblage choisi par la famille du produit fini puis le poste (jamais le plan d'une autre famille) ;
- démarrage : vérification machine + première pièce (LOT_POSTE à chaque équipe, LOT_OF pour la première seulement) ;
- production : contrôles ECHANTILLONNAGE_2859 ; vérification « après pause » seulement après la pause déjeuner ;
- fin de poste : poste par poste (les autres postes de l'équipe continuent), refusée tant qu'une tâche de fin du
  poste manque ; clôture de l'OF : tâches de fin de tous les postes ;
- plusieurs équipes en même temps sur l'OF, une seule à la fois par poste.
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

def code_formulaire(famille, poste):
    """Formulaire du champ d'application s'il existe (le serveur le trouve seul) ; sinon le code d'un nouveau formulaire."""
    existe = sql("SELECT COUNT(*) FROM Ref_Formulaire WHERE Role='EN_COURS_DE_ASSEMBLAGE' AND Statut IN ('BROUILLON','ACTIF') AND "
                 + (f"FamilleProduitFiniCode='{famille}'" if famille else 'FamilleProduitFiniCode IS NULL') + ' AND '
                 + (f"PosteCode='{poste}'" if poste else 'PosteCode IS NULL') + ' AND NatureArticleCode IS NULL AND MachineCode IS NULL')
    return None if existe == '1' else f'FE-ASS-{famille}-{poste or "FAMILLE"}'

def plan(famille, poste, types):
    sections = [{'ordreAffiche': i + 1, 'libelleSection': t, 'typeSectionId': TYPE[t],
                 'lignes': [{'ordreAffiche': 1, 'libelleAffiche': f'Étanchéité {t}'}]} for i, t in enumerate(types)]
    st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'PLAN_ASS', 'nom': f'Plan {famille} {poste or "famille"}', 'familleProduitFiniCode': famille,
                                            'posteCode': poste, 'refFormulaireCodeReference': code_formulaire(famille, poste), 'sections': sections})
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
check('A : fiche d\'échantillonnage de PAS71 remplie', st == 200, (st, r))
st, r = call(OP, 'POST', f'/ExecEchantillonnage/init/{ofa}', {'posteCode': 'PAS71', 'nbPostes': 1, 'instrumentCodes': [], 'equipe': 'E1'})
check('A : une seule fiche d\'échantillonnage par poste (seconde refusée)', st == 409 and 'PAS71' in json.dumps(r, ensure_ascii=False), (st, r))

av72 = avancement(ofa, 'PAS72')
check('A : PAS72 : sa propre fiche d\'échantillonnage à faire (chaque poste comme s\'il était seul)',
      av72.get('phase') == 'ECHANTILLONNAGE' and 'PAS72' in tache(av72, 'ECHANTILLONNAGE').get('libelle', ''), av72.get('phase'))
st, r = call(OP, 'POST', f'/ExecEchantillonnage/init/{ofa}', {'posteCode': 'PAS72', 'nbPostes': 1, 'instrumentCodes': [], 'equipe': 'E1'})
av72 = avancement(ofa, 'PAS72')
check('A : fiche de PAS72 remplie → PAS72 en démarrage', st == 200 and av72.get('phase') == 'DEMARRAGE', (st, av72.get('phase')))
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

st, r = call(OP, 'POST', f'/operateur/of/{ofa}/assemblage/demande-cloture', {'mode': 'FIN_POSTE'})
check('A : fin de poste sans poste précisé refusée (400)', st == 400, (st, r))
st, _ = call(OP, 'POST', f'/operateur/of/{ofa}/assemblage/demande-cloture', {'mode': 'FIN_POSTE', 'posteCode': 'PAS71'})
av71 = avancement(ofa, 'PAS71')
check('A : fin de poste demandée sur PAS71 → étape fin de poste (tenue par le serveur)', st == 204 and av71.get('phase') == 'FIN_POSTE' and av71.get('demandeCloture') == 'FIN_POSTE', av71)
check('A : fin de PAS71 seulement : PAS72 continue son propre parcours (démarrage)', avancement(ofa, 'PAS72').get('phase') == 'DEMARRAGE')
check('A : manquants du seul poste PAS71',
      av71.get('manquants') and all(m.startswith('PAS71 :') for m in av71.get('manquants', [])) and not av71.get('peutTerminer'), av71.get('manquants'))
check('A : badge des postes : PAS71 en fin de poste, PAS72 en démarrage, tenus par E1',
      {p['posteCode']: (p['phase'], p['equipeTitulaire']) for p in av71.get('postes', [])} == {'PAS71': ('FIN_POSTE', 'E1'), 'PAS72': ('DEMARRAGE', 'E1')}, av71.get('postes'))
st, r = call(OP, 'POST', f'/operateur/of/{ofa}/assemblage/terminer?posteCode=PAS71')
check('A : terminer refusé tant que des tâches manquent (400 détaillé)', st == 400 and 'il reste' in json.dumps(r, ensure_ascii=False), (st, r))
st, _ = call(OP, 'DELETE', f'/operateur/of/{ofa}/assemblage/demande-cloture?posteCode=PAS71')
check('A : annulation → retour en production', st == 204 and avancement(ofa, 'PAS71').get('phase') == 'PRODUCTION')
st, r = call(OP, 'POST', f'/Operateur/of/{ofa}/fin-poste')
check("A : fin de poste directe refusée à l'opérateur tant que des tâches manquent", st == 400 and 'il reste' in json.dumps(r, ensure_ascii=False), (st, r))
st, r = call(OP, 'POST', f'/Operateur/of/{ofa}/close')
check("A : clôture directe refusée à l'opérateur", st == 400, (st, r))
st, r = call(ADMIN, 'POST', f'/Operateur/of/{ofa}/close')
check("A : l'administrateur peut forcer la clôture", st == 204, (st, r))
st, r = call(OP, 'GET', '/Operateur/ofs/assemblage-statut')
of_a = next((o for o in (r or []) if o.get('numeroOf') == 'OF2607ASS01'), {})
check('A : OF clôturé encore au tableau de bord (consultation), avec sa date de clôture',
      of_a.get('statut') == 'CLOTURE' and of_a.get('dateCloture') and of_a.get('execControleOfId', '').lower() == ofa.lower(), of_a)
av_a = avancement(ofa, 'PAS71')
check('A : OF clôturé ouvert en consultation : étape clôturée, aucune tâche', av_a.get('phase') == 'CLOTURE' and av_a.get('taches') == [], av_a)
st, r = call(OP, 'GET', f'/Operateur/of/{ofa}/assemblage-documents')
types_a = sorted({d['typeDocument'] for d in (r or []) if d.get('posteCode') == 'PAS71'})
check("A : OF clôturé : ses documents restent consultables (toutes équipes)", st == 200 and 'ECHANTILLONNAGE' in types_a, (st, types_a))
check("A : OF clôturé : postes affichés « clôturé »", {p['phase'] for p in av_a.get('postes', [])} == {'CLOTURE'}, av_a.get('postes'))

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

# Contrôle au poste : la famille soupape a son propre formulaire pour PAS72 ; le document général du poste ne la
# remplace jamais, même tant que le document soupape n'est pas créé (champ d'application en base, pas de texte).
LIGNES_RC = [{'ordreAffiche': 1, 'libelleSection': 'Lignes de Contrôle', 'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Fuite'}]}]
st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'CTRL_POSTE', 'nom': 'Contrôle poste PAS72 général', 'posteCode': 'PAS72',
    'refFormulaireCodeReference': 'FE-RC-PAS72', 'sections': LIGNES_RC})
check('B : document général de contrôle au poste PAS72 actif', st in (200, 201), (st, r))
rc = tache(avancement(ofb, 'PAS72'), 'CONTROLE_POSTE')
check("B : OF soupape : le document général PAS72 n'est pas repris → document manquant (à signaler)",
      rc.get('etat') == 'MANQUANT' and 'PAS72' in rc.get('detail', ''), rc)
st, r = call(OP, 'GET', '/ExecRcPoste/' + str(ofb) + '?posteCode=PAS72&equipe=E1')
check("B : OF soupape : l'écran du contrôle au poste ne propose pas le document général", st == 404, (st, r))
st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'CTRL_POSTE', 'nom': 'Contrôle poste PAS72 soupape', 'posteCode': 'PAS72',
    'refFormulaireCodeReference': 'FE-RC-PAS72_SOUPAPE', 'sections': LIGNES_RC})
check('B : document PAS72 soupape actif', st in (200, 201), (st, r))
call(OP, 'GET', '/ExecRcPoste/' + str(ofb) + '?posteCode=PAS72&equipe=E1')
check("B : OF soupape : c'est le document PAS72 soupape qui s'applique",
      tache(avancement(ofb, 'PAS72'), 'CONTROLE_POSTE').get('etat') == 'A_FAIRE' and sql(
          f"SELECT DISTINCT ISNULL(d.FamilleProduitFiniCode, 'AUCUNE') FROM Exec_ControleDocumentStatut s JOIN Document_Entete d ON d.Id = s.DocId "
          f"WHERE s.ExecControleOfId = '{ofb}' AND s.TypeDocument = 'RESULTAT_CONTROLE_POSTE'") == 'RBGFA_SOUPAPE')

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

call(OP, 'POST', f'/operateur/of/{ofb}/assemblage/demande-cloture', {'mode': 'FIN_POSTE', 'posteCode': 'PAS72'})
st, r = saisir(ofb, 'PAS72', sections(ofb, 'PAS72')['LOT_OF'], 'DERNIERE_PIECE')
check('B : fin d\'OF non demandée → dernière pièce LOT_OF refusée', st == 400, (st, r))
saisir(ofb, 'PAS72', sections(ofb, 'PAS72')['LOT_POSTE'], 'DERNIERE_PIECE')
check('B : vérifications de fin de poste validées',
      valider(bee46, 'BEE46', 'FIN_POSTE')[0] == 204 and valider(bee47, 'BEE47', 'FIN_POSTE')[0] == 204)
sql(f"INSERT INTO ExecRegistreTracabilite (ExecControleOfId, Version, Corps, Volant, PosteCode) VALUES ('{ofb}', 1, 'C-TEST', 'V-TEST', 'PAS72')")
st, r = call(OP, 'GET', f'/Operateur/tracabilite/{ofb}?posteCode=PAS71')
st72, r72 = call(OP, 'GET', f'/Operateur/tracabilite/{ofb}?posteCode=PAS72')
check('B : chaque poste a son registre (la ligne de PAS72 n\'apparaît pas dans celui de PAS71)',
      st == 200 and data(r).get('lignesHistorique') == [] and len(data(r72).get('lignesHistorique', [])) == 1, (st, r, st72))
# Garanties portées par la base, même en écrivant directement en SQL (sans passer par l'application).
nb_registre = lambda: sql(f"SELECT COUNT(*) FROM ExecRegistreTracabilite WHERE ExecControleOfId='{ofb}'")
avant = nb_registre()
sortie = sql(f"INSERT INTO ExecRegistreTracabilite (ExecControleOfId, Version, Corps, Volant, PosteCode) VALUES ('{ofb}', 1, 'C', 'V', 'PAS78')")
check("B : base : registre d'un poste absent de l'OF refusé", 'FK_ExecRegistreTracabilite_OfPoste' in sortie and nb_registre() == avant, sortie)
sortie = sql(f"INSERT INTO ExecRegistreTracabilite (ExecControleOfId, Version, Corps, Volant) VALUES ('{ofb}', 1, 'C', 'V')")
check('B : base : ligne de registre sans poste refusée', 'PosteCode' in sortie and nb_registre() == avant, sortie)
autre_session = sql(f"SELECT TOP 1 CAST(Id AS VARCHAR(40)) FROM Exec_ControleOf_Session WHERE ExecControleOfId='{ofa}'")
sortie = sql(f"INSERT INTO ExecRegistreTracabilite (ExecControleOfId, Version, Corps, Volant, PosteCode, SessionId) "
             f"VALUES ('{ofb}', 1, 'C', 'V', 'PAS72', '{autre_session}')")
check("B : base : ligne d'une équipe qui n'a pas tenu ce poste de l'OF refusée", '50114' in sortie and nb_registre() == avant, sortie)
sortie = sql(f"INSERT INTO Exec_ControleDocumentStatut (ExecControleOfId, TypeDocument, EstTermine) VALUES ('{ofb}', 'REGISTRE_TRACA', 1)")
check('B : base : confirmation de lots sans poste refusée', 'CK_ExecControleDocumentStatut_RegistrePoste' in sortie, sortie)
st, r = call(OP, 'POST', f'/Operateur/tracabilite/{ofb}/confirmer?posteCode=PAS72')
check("B : lots de l'équipe confirmés dans le registre", st == 200, (st, r))
call(OP, 'POST', f'/ExecRcPoste/{ofb}/cloturer-tous?posteCode=PAS72')
av = avancement(ofb, 'PAS72')
check('B : toutes les tâches de fin faites → fin possible', av.get('peutTerminer') is True and av.get('manquants') == [], av.get('manquants'))
st, r = call(OP, 'POST', f'/operateur/of/{ofb}/assemblage/terminer?posteCode=PAS72')
check('B : poste terminé, OF toujours ouvert', st == 204 and sql(f"SELECT Statut FROM Exec_ControleOF WHERE Id='{ofb}'") == 'EN_COURS', (st, r))
check('B : dernier poste de E1 terminé → sa session aussi', sql(
    f"SELECT Statut FROM Exec_ControleOf_Session WHERE ExecControleOfId='{ofb}' AND Equipe='E1'") == 'TERMINE')
check('B : sans équipe active', avancement(ofb, 'PAS72').get('phase') == 'SANS_EQUIPE')

st, r = call(OP, 'POST', f'/Operateur/of/{ofb}/ajouter-postes', {'posteCodes': ['PAS72'], 'equipe': 'E2'})
check('B : l\'équipe E2 prend l\'OF après la fin de poste de E1 (poste PAS72)', st in (200, 204), (st, r))
av = avancement(ofb, 'PAS72')
check('B : échantillonnage déjà fait par E1 → E2 commence au démarrage du poste', av.get('phase') == 'DEMARRAGE' and av.get('posteDemarre') and not av.get('premierDemarragePoste'), av)
sec = sections(ofb, 'PAS72')
check('B : E2 → début de poste attendu, début d\'OF non attendu',
      sec['LOT_POSTE']['premierePiece']['requis'] and not sec['LOT_POSTE']['premierePiece']['fait']
      and not sec['LOT_OF']['premierePiece']['requis'], sec)
st, r = saisir(ofb, 'PAS72', sec['LOT_OF'], 'PREMIERE_PIECE')
check('B : E2 → contrôle de début d\'OF refusé (OF déjà démarré)', st == 400, (st, r))
st, r = call(OP, 'POST', f'/operateur/of/{ofb}/assemblage/demande-cloture', {'mode': 'FIN_OF', 'posteCode': 'PAS72'})
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
call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/demande-cloture', {'mode': 'FIN_POSTE', 'posteCode': 'PAS78'})
av = avancement(ofc, 'PAS78')
check('C : fin de poste : pas de « dernière pièce » attendue',
      not any('dernière pièce' in m for m in av.get('manquants', [])) and 'PLAN_DERNIERE_PIECE' not in [t['code'] for t in av.get('taches', [])], av)
# ───────────── D. Une équipe à la fois par poste ; plusieurs équipes sur l'OF ; clôture seule ─────────────
def avancement_equipe(exec_id, poste, equipe):
    st, r = call(OP, 'GET', f'/operateur/of/{exec_id}/assemblage/avancement?posteCode={poste}&equipe={equipe}')
    return r if st == 200 else {'erreur': (st, r)}
def liste_d():
    st, r = call(OP, 'GET', '/Operateur/ofs/assemblage-statut')
    of_d = next((o for o in (r or []) if o.get('numeroOf') == 'OF26041131'), {})
    return {e['equipe']: (e['enPause'], e['postes']) for e in of_d.get('equipes', [])}

call(OP, 'DELETE', f'/operateur/of/{ofc}/assemblage/demande-cloture?posteCode=PAS78')
call(OP, 'POST', f'/Operateur/of/{ofc}/pause', {'raison': 'Changement', 'typePause': 'AUTRE'})
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/ajouter-postes', {'posteCodes': ['PAS78'], 'equipe': 'E2'})
check("D : E2 ne prend pas PAS78 tant que E1 le tient (même en pause)", st == 409 and 'E1' in json.dumps(r, ensure_ascii=False), (st, r))
check('D : liste des OF : E1 en pause sur PAS78', liste_d() == {'E1': (True, ['PAS78'])}, liste_d())
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/reprendre?equipe=E2')
check("D : une équipe sans session ne reprend pas celle de E1", st == 409, (st, r))
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/reprendre?equipe=E1')
check("D : E1 reprend son étape telle qu'elle l'avait laissée", st == 204 and avancement_equipe(ofc, 'PAS78', 'E1').get('phase') == 'PRODUCTION', (st, r))
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/ajouter-postes', {'posteCodes': ['PAS71'], 'equipe': 'E1'})
check('D : E1 démarre aussi PAS71 (poste ajouté à l\'OF)', st in (200, 204) and avancement_equipe(ofc, 'PAS71', 'E1').get('posteDemarre'), (st, r))

# Fin de poste de PAS78 seulement : PAS71 continue ; le poste libéré peut être pris par une autre équipe.
st, _ = call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/demande-cloture', {'mode': 'FIN_POSTE', 'posteCode': 'PAS78', 'equipe': 'E1'})
check('D : fin de poste demandée sur PAS78 : PAS71 de E1 garde son étape (échantillonnage)',
      st == 204 and avancement_equipe(ofc, 'PAS78', 'E1').get('phase') == 'FIN_POSTE' and avancement_equipe(ofc, 'PAS71', 'E1').get('phase') == 'ECHANTILLONNAGE')
st, r = call(ADMIN, 'POST', f'/Operateur/of/{ofc}/fin-poste?equipe=E1&posteCode=PAS78')
check('D : PAS78 terminé par E1 : sa session reste ouverte pour PAS71', st == 204 and sql(
    f"SELECT Statut FROM Exec_ControleOf_Session WHERE ExecControleOfId='{ofc}' AND Equipe='E1'") == 'EN_COURS', (st, r))
check('D : E1 sur PAS78 → « poste terminé »', avancement_equipe(ofc, 'PAS78', 'E1').get('phase') == 'POSTE_TERMINE')
st, r = call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/demande-cloture', {'mode': 'FIN_POSTE', 'posteCode': 'PAS78', 'equipe': 'E1'})
check('D : un poste terminé n\'a plus de fin de poste à demander', st == 409, (st, r))
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/ajouter-postes', {'posteCodes': ['PAS78'], 'equipe': 'E2'})
check('D : E2 prend PAS78 pendant que E1 continue sur PAS71', st in (200, 204), (st, r))
check('D : liste des OF : E1 sur PAS71, E2 sur PAS78', liste_d() == {'E1': (False, ['PAS71']), 'E2': (False, ['PAS78'])}, liste_d())
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/ajouter-postes', {'posteCodes': ['PAS71'], 'equipe': 'E2'})
check('D : E2 ne prend pas PAS71 tant que E1 ne l\'a pas terminé', st == 409 and 'E1' in json.dumps(r, ensure_ascii=False), (st, r))
st, r = call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/postes/PAS71/demarrer?equipe=E2')
check('D : « Démarrer le poste » PAS71 refusé à E2 (tenu par E1)', st == 409, (st, r))
e2_71 = avancement_equipe(ofc, 'PAS71', 'E2')
check('D : E2 voit PAS71 occupé par E1', (e2_71.get('phase'), e2_71.get('equipeTitulaire'), e2_71.get('taches')) == ('POSTE_OCCUPE', 'E1', []), e2_71)
e2_78 = avancement_equipe(ofc, 'PAS78', 'E2')
check("D : E2 sur PAS78 : début de poste, pas de « début d'OF » (poste déjà démarré)",
      e2_78.get('posteDemarre') and e2_78.get('premierDemarragePoste') is False and e2_78.get('phase') in ('DEMARRAGE', 'PRODUCTION'), e2_78)
st, r = call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/demande-cloture', {'mode': 'FIN_OF', 'posteCode': 'PAS78', 'equipe': 'E2'})
check("D : clôture refusée tant qu'une autre équipe tient un poste", st == 409 and 'E1' in json.dumps(r, ensure_ascii=False) and 'PAS71' in json.dumps(r, ensure_ascii=False), (st, r))
call(OP, 'POST', f'/Operateur/of/{ofc}/pause', {'raison': 'Relève', 'typePause': 'AUTRE', 'equipe': 'E1'})
check('D : pause de E1 : E2 continue, l\'OF reste en cours',
      avancement_equipe(ofc, 'PAS71', 'E1').get('phase') == 'PAUSE' and avancement_equipe(ofc, 'PAS78', 'E2').get('phase') != 'PAUSE'
      and sql(f"SELECT Statut FROM Exec_ControleOF WHERE Id='{ofc}'") == 'EN_COURS')
etapes_e1 = {e['code']: e['etat'] for e in avancement_equipe(ofc, 'PAS71', 'E1').get('etapes', [])}
check("D : en pause, la frise reste sur l'étape où le poste a été laissé (échantillonnage)",
      etapes_e1 == {'ECHANTILLONNAGE': 'EN_COURS', 'DEMARRAGE': 'A_VENIR', 'PRODUCTION': 'A_VENIR', 'FIN': 'A_VENIR'}, etapes_e1)
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/pause', {'raison': 'Relève', 'typePause': 'AUTRE'})
check("D : pause sans préciser l'équipe refusée quand plusieurs équipes travaillent", st == 400, (st, r))
call(OP, 'POST', f'/Operateur/of/{ofc}/reprendre?equipe=E1')
check("D : deux équipes tiennent chacune un poste de l'OF", sql(
    f"SELECT COUNT(*) FROM Exec_SessionPost WHERE ExecControleOfId='{ofc}' AND EndedAt IS NULL") == '2')

st, r = call(ADMIN, 'POST', f'/Operateur/of/{ofc}/fin-poste?equipe=E1')
check("D : fin de poste de E1 (PAS71) → OF toujours en cours, PAS71 libre",
      st == 204 and sql(f"SELECT Statut FROM Exec_ControleOF WHERE Id='{ofc}'") == 'EN_COURS' and avancement(ofc, 'PAS71').get('phase') == 'SANS_EQUIPE', (st, r))
check("D : l'échantillonnage reste fait pour l'OF", sql(f"SELECT COUNT(*) FROM Exec_ControleDocumentStatut WHERE ExecControleOfId='{ofc}' AND TypeDocument='ECHANTILLONNAGE'") == '1')

# PAS71 libre : E2 voit « Démarrer le poste » ; sa fin de poste de PAS78 ne touche pas PAS71.
e2_71 = avancement_equipe(ofc, 'PAS71', 'E2')
check('D : PAS71 libre, non démarré par E2 → « Démarrer le poste »',
      e2_71.get('phase') == 'NON_DEMARRE' and [t['code'] for t in e2_71.get('taches', [])] == ['DEMARRER_POSTE'] and not e2_71.get('posteDemarre'), e2_71)
st, r = call(OP, 'POST', f'/operateur/of/{ofc}/poste/PAS71/verif-machines', {'machineCode': 'MAS26'})
check('D : vérification machine refusée sur un poste que personne ne tient', st == 409, (st, r))
call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/demande-cloture', {'mode': 'FIN_POSTE', 'posteCode': 'PAS78', 'equipe': 'E2'})
e2_71 = avancement_equipe(ofc, 'PAS71', 'E2')
check('D : fin de poste de PAS78 : PAS71 non concerné (toujours « Démarrer le poste »)',
      [t['code'] for t in e2_71.get('taches', [])] == ['DEMARRER_POSTE'] and not e2_71.get('manquants'), (e2_71.get('taches'), e2_71.get('manquants')))
call(OP, 'DELETE', f'/operateur/of/{ofc}/assemblage/demande-cloture?posteCode=PAS78&equipe=E2')
st, _ = call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/postes/PAS71/demarrer?equipe=E2')
e2_71 = avancement_equipe(ofc, 'PAS71', 'E2')
check("D : E2 démarre PAS71 → d'abord la fiche d'échantillonnage de PAS71 (pas encore remplie sur cet OF)",
      st == 204 and e2_71.get('posteDemarre') and e2_71.get('phase') == 'ECHANTILLONNAGE', (st, e2_71.get('phase')))
call(ADMIN, 'POST', f'/Operateur/of/{ofc}/fin-poste?equipe=E2')
check('D : fin de poste forcée de E2 : ses deux postes et sa session terminés', sql(
    f"SELECT COUNT(*) FROM Exec_SessionPost WHERE ExecControleOfId='{ofc}' AND EndedAt IS NULL") == '0' and sql(
    f"SELECT COUNT(*) FROM Exec_ControleOf_Session WHERE ExecControleOfId='{ofc}' AND Statut <> 'TERMINE'") == '0')

# E1 revient plus tard (nouvelle session de la même équipe), puis E3 vient seulement clôturer.
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/ajouter-postes', {'posteCodes': ['PAS78'], 'equipe': 'E1'})
check("D : E1 ouvre une nouvelle session quand elle veut", st in (200, 204) and sql(
    f"SELECT COUNT(*) FROM Exec_ControleOf_Session WHERE ExecControleOfId='{ofc}' AND Equipe='E1'") == '2', (st, r))
check("D : nouvelle session de E1 → PAS78 au démarrage (pas de « début d'OF » : poste déjà démarré)",
      avancement_equipe(ofc, 'PAS78', 'E1').get('premierDemarragePoste') is False)
call(ADMIN, 'POST', f'/Operateur/of/{ofc}/fin-poste?equipe=E1')
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/ajouter-postes', {'posteCodes': [], 'equipe': 'E3', 'pourCloturer': True})
e3 = avancement_equipe(ofc, 'PAS78', 'E3')
check('D : E3 vient seulement clôturer → étape clôture, aucun contrôle de démarrage ni de fin de poste',
      st in (200, 204) and e3.get('phase') == 'FIN_OF' and not e3.get('posteDemarre')
      and all(t['code'] not in ('VERIF_MACHINE', 'CONTROLE_POSTE', 'TRACABILITE') for t in e3.get('taches', [])), (st, e3.get('phase'), e3.get('taches')))
st, r = call(OP, 'POST', f'/Operateur/of/{ofc}/ajouter-postes', {'posteCodes': ['PAS78'], 'equipe': 'E2'})
check("D : pendant la clôture, aucune autre équipe ne commence", st == 409 and 'E3' in json.dumps(r, ensure_ascii=False), (st, r))
e3_71 = avancement_equipe(ofc, 'PAS71', 'E3')
check("D : E3 : sur PAS71 (démarré sur l'OF), seule la dernière pièce « fin d'OF »",
      [t['code'] for t in e3_71.get('taches', [])] == ['PLAN_DERNIERE_PIECE'] and "fin d'OF" in e3_71['taches'][0].get('detail', ''), e3_71.get('taches'))
st, r = call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/terminer?posteCode=PAS78&equipe=E3')
check("D : clôture refusée tant que la fin d'OF de PAS71 manque", st == 400 and 'PAS71' in json.dumps(r, ensure_ascii=False), (st, r))
sec71 = sections(ofc, 'PAS71')
check('D : E3 : dernière pièce de fin de poste non demandée (poste non démarré)', saisir(ofc, 'PAS71', sec71['LOT_POSTE'], 'DERNIERE_PIECE')[0] == 409)
st, r = saisir(ofc, 'PAS71', sec71['LOT_OF'], 'DERNIERE_PIECE')
check("D : E3 saisit la dernière pièce « fin d'OF » de PAS71", st == 204, (st, r))
st, r = call(OP, 'POST', f'/operateur/of/{ofc}/assemblage/terminer?posteCode=PAS78&equipe=E3')
check("D : clôture de l'OF par E3", st == 204 and sql(f"SELECT Statut FROM Exec_ControleOF WHERE Id='{ofc}'") == 'CLOTURE', (st, r))
check("D : toutes les sessions et tous les postes terminés avec l'OF", sql(
    f"SELECT COUNT(*) FROM Exec_SessionPost WHERE ExecControleOfId='{ofc}' AND EndedAt IS NULL") == '0' and sql(
    f"SELECT COUNT(*) FROM Exec_ControleOf_Session WHERE ExecControleOfId='{ofc}' AND Statut <> 'TERMINE'") == '0')

# ── R : documents d'exécution mis en forme à la demande ─────────────────────────
import io, zipfile
def binaire(tok, chemin):
    req = urllib.request.Request(BASE + chemin, headers={'Authorization': 'Bearer ' + tok})
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            return r.status, r.read()
    except urllib.error.HTTPError as e:
        return e.code, e.read()
RQ_R = jwt('22222', 'RESPONSABLE_QUALITE')
check("R aucun PDF produit en arrière-plan (OF clôturé) : seulement à la consultation", sql(
    "SELECT COUNT(*) FROM GeneratedDocument g JOIN Exec_ControleDocumentStatut s ON s.Id = g.ExecControleDocumentStatutId "
    "WHERE s.ExecControleOfId = '" + ofc + "'") == '0')
numero_of = sql("SELECT NumeroOF FROM Exec_ControleOF WHERE Id='" + ofc + "'")
st, r = call(SUP, 'GET', f'/rapports/tracabilite/{numero_of}')
cartes = (r.get('data') or {}).get('documents', []) if isinstance(r, dict) else []
check("R traçabilité de l'OF : chaque document est disponible (avec le modèle en vigueur à sa création) ou manquant avec sa raison",
      st == 200 and cartes and all((c.get('etat') == 'DISPONIBLE' and c.get('modele')) or (c.get('etat') == 'MANQUANT' and c.get('raisonManquant'))
                                   for c in cartes), (st, [(c['typeCode'], c.get('etat'), c.get('raisonManquant')) for c in cartes]))
check("R traçabilité de l'OF : seulement les fiches remplies, jamais les clôtures de poste ni un plan déjà représenté par ses fiches",
      not any(c['typeCode'].startswith('CLOTURE_') for c in cartes)
      and not any(c['categorie'] == 'QUALITE' and not c.get('contratDisponible') and c.get('etat') == 'DISPONIBLE' for c in cartes),
      [(c['categorie'], c['typeCode'], c.get('etat')) for c in cartes])
# Chaque fiche reçoit le modèle de son formulaire : résultats d'assemblage selon le formulaire de résultats fixé à sa
# création (poste 71 / auto : FE0816 ; postes 72 et 78 / manuel : FE0817), vérification machine selon la machine.
MODELE_DU_FORMULAIRE = {'FE-RC-ENCF-ASS-AUTO': 'PLAN_ASS_RESULTATS', 'FE-RC-ENCF-ASS-PAS71_SOUPAPE': 'PLAN_ASS_RESULTATS',
                        'FE-RC-ENCF-ASS-MANU': 'PLAN_ASS_RESULTATS_PAS72', 'FE-RC-ENCF-ASS-PAS72_SOUPAPE': 'PLAN_ASS_RESULTATS_PAS72',
                        'FE-RC-ENCF-ASS-PAS78_SOUPAPE': 'PLAN_ASS_RESULTATS_PAS72'}
data_ok = lambda reponse: (reponse[1].get('data') or []) if isinstance(reponse[1], dict) else []
MODELE_DE_LA_MACHINE = {'BEE22': 'VERIF_MACHINE', 'BEE46': 'VERIF_MACHINE', 'BEE47': 'VERIF_MACHINE', 'MAS19': 'VERIF_MACHINE_MAS',
                        'MAS22': 'VERIF_MACHINE_MAS22', 'MAS26': 'VERIF_MACHINE_MAS26', 'SER05': 'VERIF_MACHINE_SER05'}
fiches = [c for c in cartes if c['categorie'] == 'EXECUTION' and c['typeCode'] == 'PLAN_ASS']
check("R résultats d'assemblage : chaque fiche a son formulaire de résultats (FE-RC-ENCF-ASS-*) et le modèle rattaché à ce formulaire",
      fiches and all((c.get('formulaire') or '').split(' version')[0] in MODELE_DU_FORMULAIRE
                     and (c.get('modele') or {}).get('codeModele') == MODELE_DU_FORMULAIRE[c['formulaire'].split(' version')[0]] for c in fiches),
      [(c['poste'], c.get('formulaire'), (c.get('modele') or {}).get('codeModele')) for c in fiches])
verifs = data_ok(call(SUP, 'GET', '/rapports/documents?categorie=EXECUTION&type=VERIF_MACHINE'))
check('R vérifications machine : chaque fiche a le modèle rattaché au formulaire de sa machine (FE-VM-machine)',
      verifs and all((c.get('modele') or {}).get('codeModele') == MODELE_DE_LA_MACHINE.get(c['machine']) for c in verifs),
      [(c['machine'], c.get('formulaire'), (c.get('modele') or {}).get('codeModele')) for c in verifs])
for type_execution, libelle in (('ECHANTILLONNAGE', "fiche d'échantillonnage"), ('RESULTAT_CONTROLE_POSTE', 'résultat de contrôle au poste'),
                                ('VERIF_MACHINE', 'rapport de vérification machine'), ('PLAN_ASS', "résultats du contrôle d'assemblage du poste"),
                                ('REGISTRE_TRACA', 'registre de traçabilité du poste')):
    st, r = call(SUP, 'GET', f'/rapports/documents?categorie=EXECUTION&type={type_execution}')
    liste = (r.get('data') or []) if isinstance(r, dict) else []
    source = next((d for d in liste if d.get('modele')), None)
    check(f'R {libelle} : listé avec son modèle (version 1, en vigueur à sa création)',
          st == 200 and source and source['modele']['numeroVersion'] == 1 and source['modele']['regle'] == 'EN_VIGUEUR_A_LA_CREATION', (st, liste[:1]))
    if not source: continue
    statut = source['execControleDocumentStatutId']
    st, pdf = binaire(SUP, f'/rapports/documents/par-execution/{statut}/pdf')
    genere = sql(f"SELECT TOP 1 Id FROM GeneratedDocument WHERE ExecControleDocumentStatutId='{statut}'")
    check(f'R {libelle} : PDF produit au clic (signature %PDF), conservé avec son demandeur et son empreinte',
          st == 200 and pdf[:5] == b'%PDF-' and sql(f"SELECT RequestedBy FROM GeneratedDocument WHERE Id='{genere}'") == '44444'
          and sql(f"SELECT PdfSha256 FROM GeneratedDocument WHERE Id='{genere}'") == hashlib.sha256(pdf).hexdigest().upper(), st)
    st, pdf2 = binaire(RQ_R, f'/rapports/documents/par-execution/{statut}/pdf')
    check(f'R {libelle} : deuxième consultation (responsable qualité) : même PDF, rien de reproduit',
          st == 200 and pdf2 == pdf and sql(f"SELECT COUNT(*) FROM GeneratedDocument WHERE ExecControleDocumentStatutId='{statut}'") == '1', st)
    st, xlsx = binaire(SUP, f'/rapports/documents/par-execution/{statut}/excel')
    feuille = __import__('openpyxl').load_workbook(io.BytesIO(xlsx)).active if st == 200 and zipfile.is_zipfile(io.BytesIO(xlsx)) else None
    restants = [c.value for ligne in (feuille.iter_rows() if feuille else []) for c in ligne if isinstance(c.value, str) and '{{' in c.value]
    check(f'R {libelle} : Excel rempli, aucun champ {{}} restant', feuille is not None and not restants, restants[:5])
    st, r = call(SUP, 'GET', f'/rapports/documents/par-execution/{statut}')
    etat = r.get('data', {}) if isinstance(r, dict) else {}
    check(f"R {libelle} : état du document (son modèle, son PDF dans l'historique)",
          st == 200 and etat.get('source', {}).get('modele') and len(etat.get('documents', [])) == 1, (st, r))
    st, modele = binaire(SUP, f'/rapports/documents/par-execution/{statut}/modele')
    check(f'R {libelle} : Excel du modèle du document téléchargeable (superviseur)', st == 200 and zipfile.is_zipfile(io.BytesIO(modele)), st)
    if type_execution == 'RESULTAT_CONTROLE_POSTE' and feuille is not None:
        check('R résultat de contrôle au poste : défauts du document et pied de page « formulaire.version »',
              feuille['A8'].value == '1' and feuille['C8'].value and (feuille.oddFooter.right.text or '').count('.') == 1,
              (feuille['A8'].value, feuille['C8'].value, feuille.oddFooter.right.text))
    if type_execution == 'ECHANTILLONNAGE':
        check("R opérateur : PDF des documents d'exécution refusé (403)", binaire(OP, f'/rapports/documents/par-execution/{statut}/pdf')[0] == 403)
        check('R responsable qualité : Excel du modèle réservé au superviseur (403)', binaire(RQ_R, f'/rapports/documents/par-execution/{statut}/modele')[0] == 403)

# ── S. Un OF garde le modèle en vigueur à la première utilisation de son formulaire, même si le modèle change ensuite ──
import uuid, openpyxl
def envoyer_modele(code, nom, contenu, formulaire):
    limite = 'e2e' + uuid.uuid4().hex
    fin_ligne = '\r\n'
    corps = (f'--{limite}{fin_ligne}Content-Disposition: form-data; name="fichier"; filename="{nom}"{fin_ligne}'
             f'Content-Type: application/octet-stream{fin_ligne}{fin_ligne}').encode()
    corps += contenu + (f'{fin_ligne}--{limite}{fin_ligne}Content-Disposition: form-data; name="formulaireId"{fin_ligne}{fin_ligne}'
                        f'{formulaire}{fin_ligne}--{limite}--{fin_ligne}').encode()
    req = urllib.request.Request(BASE + f'/rapports/modeles/{code}/versions', data=corps, method='POST',
        headers={'Authorization': 'Bearer ' + ADMIN, 'Content-Type': f'multipart/form-data; boundary={limite}'})
    try:
        with urllib.request.urlopen(req, timeout=120) as r: return r.status, json.loads(r.read().decode())
    except urllib.error.HTTPError as e: return e.code, {}

def modele_fiche(statut_id):
    r = data(call(SUP, 'GET', f'/rapports/documents/par-execution/{statut_id}')[1]) or {}
    return (r.get('source', {}).get('modele') or {}).get('versionId', ''), (r.get('modeleActuel') or {}).get('versionId', '')

fiche_1, of_1, form_1, doc_1 = (sql("SELECT TOP 1 CONCAT(LOWER(CAST(s.Id AS VARCHAR(36))), '|', LOWER(CAST(s.ExecControleOfId AS VARCHAR(36))), '|', "
    "LOWER(CAST(d.FormulaireId AS VARCHAR(36))), '|', LOWER(CAST(s.DocId AS VARCHAR(36)))) FROM Exec_ControleDocumentStatut s "
    "JOIN Document_Entete d ON d.Id = s.DocId WHERE s.TypeDocument = 'RESULTAT_CONTROLE_POSTE' ORDER BY s.CreationOrder") or '|||').split('|')
modele_avant, _ = modele_fiche(fiche_1)
st, r = call(SUP, 'POST', f'/rapports/formulaires/{form_1}/demande-modele', {'motif': 'Mise en page pendant un OF', 'changementVersion': 'SAME_VERSION'})
demande = data(r) if st == 200 else None
st_f, fichier = binaire(ADMIN, f'/rapports/modeles/versions/{modele_avant}/fichier')
classeur = openpyxl.load_workbook(io.BytesIO(fichier)) if st_f == 200 else None
if classeur:
    classeur.active['Z1'] = 'Mise en page OF'
sortie = io.BytesIO(); classeur and classeur.save(sortie)
st_e, r_e = envoyer_modele('RESULTAT_CONTROLE_POSTE', 'mise_en_page_of.xlsx', sortie.getvalue(), form_1)
nouveau = ((r_e.get('data') or {}).get('version') or {}).get('id', '')
st_t, r_t = call(ADMIN, 'POST', f'/rapports/demandes-modele/{demande}/traiter', {'versionId': nouveau})
# Deux fiches créées après ce changement : l'une dans le même OF (formulaire déjà utilisé), l'autre dans un autre OF.
autre_of = sql(f"SELECT TOP 1 LOWER(CAST(o.Id AS VARCHAR(36))) FROM Exec_ControleOF o WHERE o.Id <> '{of_1}' AND NOT EXISTS "
               f"(SELECT 1 FROM Exec_ControleDocumentStatut s WHERE s.ExecControleOfId = o.Id AND s.DocId = '{doc_1}')")
for of_cible in (of_1, autre_of):
    sql(f"INSERT INTO Exec_ControleDocumentStatut (ExecControleOfId, TypeDocument, PosteCode, MachineCode, DocId) "
        f"SELECT '{of_cible}', TypeDocument, PosteCode, MachineCode, DocId FROM Exec_ControleDocumentStatut WHERE Id = '{fiche_1}'")
fiche_meme_of = sql(f"SELECT TOP 1 LOWER(CAST(Id AS VARCHAR(36))) FROM Exec_ControleDocumentStatut WHERE ExecControleOfId='{of_1}' AND DocId='{doc_1}' ORDER BY CreationOrder DESC")
fiche_autre_of = sql(f"SELECT TOP 1 LOWER(CAST(Id AS VARCHAR(36))) FROM Exec_ControleDocumentStatut WHERE ExecControleOfId='{autre_of}' AND DocId='{doc_1}' ORDER BY CreationOrder DESC")
meme_of, actuel = modele_fiche(fiche_meme_of)
autre, _ = modele_fiche(fiche_autre_of)
check("S nouveau modèle (même version) pendant un OF : une fiche créée ensuite dans cet OF garde le modèle de l'OF ; un autre OF prend le nouveau",
      (st, st_t) == (200, 200) and bool(nouveau) and fiche_meme_of != fiche_1 and meme_of.lower() == modele_avant.lower()
      and actuel.lower() == nouveau.lower() and autre.lower() == nouveau.lower(), (st, st_e, st_t, r_t, modele_avant, meme_of, autre, nouveau))

print(f'e2e64: {ok} OK / {ko} KO')
