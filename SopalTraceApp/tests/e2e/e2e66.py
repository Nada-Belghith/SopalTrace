"""
Vérifications machine par session d'équipe : un document par machine, poste et session (une session par prise de
poste). Ce qu'une équipe a vérifié (démarrage, fin de poste) ne vaut jamais pour une autre équipe, ni pour la même
équipe lors d'une prise de poste suivante. Une seule équipe à la fois sur l'OF.
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

POSTE = 'PAS72'
def avancement(of, equipe):
    st, r = call(OP, 'GET', f'/operateur/of/{of}/assemblage/avancement?posteCode={POSTE}&equipe={equipe}')
    return r if st == 200 else {'erreur': (st, r)}
def tache_vm(av, periodicite):
    return next((t for t in av.get('taches', []) if t['code'] == 'VERIF_MACHINE' and t.get('periodicite') == periodicite), {})
def machines(of, periodicite):
    st, r = call(OP, 'GET', f'/operateur/of/{of}/poste/{POSTE}/verif-machines?periodicite={periodicite}')
    return r if st == 200 else {'erreur': (st, r), 'machines': []}
def points(code, periodicite):
    return sql(f"SELECT g.Id FROM Document_VerifMachine_Echeance g JOIN Document_Ligne l ON l.Id=g.LigneId "
               f"JOIN Document_Entete e ON e.Id=l.EnteteId JOIN PeriodiciteMachine p ON p.Id=g.PeriodiciteMachineId "
               f"WHERE e.TypeDocumentCode='VERIF_MACHINE' AND e.Statut='ACTIF' AND e.MachineCode='{code}' "
               f"AND p.ControlMoment = '{periodicite}'").split()
def valider_toutes(of, periodicite):
    """Ouvre (« Remplir ») et valide chaque machine prévue à ce moment ; renvoie les codes des statuts."""
    statuts = {}
    for m in machines(of, periodicite).get('machines', []):
        if m['etat'] in ('NON_PREVUE', 'PLAN_MANQUANT'): continue
        statut = m.get('statutId')
        if not statut:
            st, r = call(OP, 'POST', f'/operateur/of/{of}/poste/{POSTE}/verif-machines', {'machineCode': m['machineCode']})
            statut = r.get('id') if st == 200 else None
        reponses = [{'documentVerifMachineEcheanceId': g, 'conforme': True} for g in points(m['machineCode'], periodicite)]
        st, _ = call(OP, 'POST', f'/operateur/verif-machine/{statut}/valider', {'periodicite': periodicite, 'reponses': reponses})
        statuts[m['machineCode']] = (statut, st)
    return statuts
def etats(of, periodicite):
    return {m['machineCode']: m['etat'] for m in machines(of, periodicite).get('machines', []) if m['etat'] not in ('NON_PREVUE', 'PLAN_MANQUANT')}

# Plan sans section de lot : le démarrage ne dépend que des vérifications machine.
st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'PLAN_ASS', 'nom': 'Plan RBGFM PAS72', 'familleProduitFiniCode': 'RBGFM',
    'posteCode': POSTE, 'refFormulaireCodeReference': 'FE-ASS-RBGFM-PAS72-VM',
    'sections': [{'ordreAffiche': 1, 'libelleSection': 'Production', 'typeSectionId': sql("SELECT Id FROM TypeSection WHERE TypeNormalise='ECHANTILLONNAGE_2859'"),
                  'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Étanchéité'}]}]})
check("plan d'assemblage PAS72 actif", st in (200, 201), (st, r))
call(ADMIN, 'POST', '/documents-echantillonnage', {'niveauControle': 'II', 'typePlan': 'SIMPLE', 'modeControle': 'NORMAL', 'valeurNqa': 1.5,
    'critereAcceptationAc': 1, 'critereRejetRe': 2, 'remarques': '', 'legendeMoyens': '', 'refFormulaireCodeReference': 'FE-ECHAN-01'})

# ───────────── E1 : démarrage, pause, reprise, fin de poste ─────────────
st, r = call(OP, 'POST', '/operateur/of/start-assemblage', {'numeroOf': 'OF2607ASS01', 'posteCodes': [POSTE], 'equipe': 'E1'})
check('E1 démarre l\'OF', st == 200, (st, r)); of = data(r)['id'] if st == 200 else None
call(OP, 'POST', f'/ExecEchantillonnage/init/{of}', {'posteCode': POSTE, 'nbPostes': 1, 'instrumentCodes': [], 'equipe': 'E1'})
e1_dem = valider_toutes(of, 'DEMARRAGE')
check('E1 : vérifications de démarrage validées', e1_dem and all(s == 204 for _, s in e1_dem.values()), e1_dem)
check('E1 en production', avancement(of, 'E1').get('phase') == 'PRODUCTION')
call(OP, 'POST', f'/Operateur/of/{of}/pause', {'raison': 'Relève', 'typePause': 'AUTRE'})
st, r = call(OP, 'POST', f'/Operateur/of/{of}/ajouter-postes', {'posteCodes': [POSTE], 'equipe': 'E2'})
check('E2 ne commence pas pendant la pause de E1', st == 409, (st, r))
call(OP, 'POST', f'/Operateur/of/{of}/reprendre?equipe=E1')
call(OP, 'POST', f'/operateur/of/{of}/assemblage/demande-cloture', {'mode': 'FIN_POSTE', 'posteCode': POSTE})
e1_fin = valider_toutes(of, 'FIN_POSTE')
check('E1 : vérifications de fin de poste validées', e1_fin and all(s == 204 for _, s in e1_fin.values()), e1_fin)
check('E1 : tâche « vérification machine en fin de poste » faite', tache_vm(avancement(of, 'E1'), 'FIN_POSTE').get('etat') == 'FAIT')
st, r = call(ADMIN, 'POST', f'/Operateur/of/{of}/fin-poste')
check('E1 termine son poste', st == 204, (st, r))

# ───────────── E2 : ses propres documents ─────────────
st, r = call(OP, 'POST', f'/Operateur/of/{of}/ajouter-postes', {'posteCodes': [POSTE], 'equipe': 'E2'})
check('E2 prend l\'OF après la fin de poste de E1', st in (200, 204), (st, r))
check('E2 : vérifications de démarrage à faire (celles de E1 ne comptent pas)',
      etats(of, 'DEMARRAGE') and set(etats(of, 'DEMARRAGE').values()) == {'A_FAIRE'}, etats(of, 'DEMARRAGE'))
e2_dem = valider_toutes(of, 'DEMARRAGE')
check('E2 : documents distincts de ceux de E1', set(s for s, _ in e2_dem.values()).isdisjoint(s for s, _ in e1_dem.values()), (e1_dem, e2_dem))
call(OP, 'POST', f'/operateur/of/{of}/assemblage/demande-cloture', {'mode': 'FIN_POSTE', 'posteCode': POSTE})
check('E2 : vérifications de fin de poste à faire (celles de E1 ne comptent pas)',
      tache_vm(avancement(of, 'E2'), 'FIN_POSTE').get('etat') == 'A_FAIRE' and set(etats(of, 'FIN_POSTE').values()) == {'A_FAIRE'}, etats(of, 'FIN_POSTE'))
call(ADMIN, 'POST', f'/Operateur/of/{of}/fin-poste')

# ───────────── E1 revient plus tard : nouvelle prise de poste, nouveaux documents ─────────────
st, r = call(OP, 'POST', f'/Operateur/of/{of}/ajouter-postes', {'posteCodes': [POSTE], 'equipe': 'E1'})
check('E1 ouvre une nouvelle session', st in (200, 204), (st, r))
check('E1 (nouvelle session) : vérifications de démarrage à faire', set(etats(of, 'DEMARRAGE').values()) == {'A_FAIRE'}, etats(of, 'DEMARRAGE'))
call(OP, 'POST', f'/operateur/of/{of}/assemblage/demande-cloture', {'mode': 'FIN_OF', 'posteCode': POSTE})
av = avancement(of, 'E1')
check("E1 : clôture d'OF demandée sur un poste démarré → ses vérifications de fin de poste à faire",
      tache_vm(av, 'FIN_POSTE').get('etat') == 'A_FAIRE' and any('Vérification machine' in m for m in av.get('manquants', [])), (tache_vm(av, 'FIN_POSTE'), av.get('manquants')))
check('documents de vérification : un par machine et par session', sql(
    f"SELECT COUNT(*) FROM (SELECT MachineCode, SessionId FROM Exec_ControleDocumentStatut WHERE ExecControleOfId='{of}' "
    f"AND TypeDocument='VERIF_MACHINE' GROUP BY MachineCode, SessionId HAVING COUNT(*) > 1) d") == '0')

# Clôture forcée par l'administrateur : les vérifications non faites de E1 ne deviennent pas « faites ».
termines_avant = sql(f"SELECT COUNT(*) FROM Exec_ControleDocumentStatut WHERE ExecControleOfId='{of}' AND TypeDocument='VERIF_MACHINE' AND Equipe='E1' AND EstTermine=1")
st, r = call(ADMIN, 'POST', f'/Operateur/of/{of}/close')
check("clôture forcée : les vérifications de fin de poste non faites de E1 restent non faites",
      st == 204 and sql(f"SELECT COUNT(*) FROM Exec_ControleDocumentStatut WHERE ExecControleOfId='{of}' AND TypeDocument='VERIF_MACHINE' AND Equipe='E1' AND EstTermine=1") == termines_avant,
      (st, r, termines_avant))
st, r = call(OP, 'GET', f'/operateur/of/{of}/poste/{POSTE}/verif-machines/historique?equipe=E2')
check("historique : E2 sans fin de poste", st == 200 and r and not any(x.get('finPoste') for x in r), (st, r))

print(f'e2e66: {ok} OK / {ko} KO')
