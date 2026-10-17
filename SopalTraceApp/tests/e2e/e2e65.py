"""
Contrôles de production de l'assemblage planifiés par le serveur :
- une pièce toutes les 60 / effectif par poste minutes (fiche d'échantillonnage), par équipe et par poste ;
- période de production ouverte à l'étape « Production », fermée par la pause, le réglage ou la fin de poste ;
  la reprise en ouvre une nouvelle (le calcul repart de zéro, la numérotation continue) ;
- réglage commencé : contrôles suspendus jusqu'à un réglage conforme sur tous les critères ;
- contrôle au poste : un document par jour et par poste, commun aux équipes, chacune valide ses heures ;
- fabrication : même mécanisme, une suite de contrôles par section de périodicité horaire, qui suit l'état de
  l'OF (en cours, réglage, pause, clôture) ; une section « série » n'a jamais d'occurrence.
Le temps est avancé en reculant le début des périodes en base.
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

TYPE = {t: sql(f"SELECT Id FROM TypeSection WHERE TypeNormalise='{t}'") for t in ('ECHANTILLONNAGE_2859', 'REGLAGE')}
POSTE = 'PAS71'

def avancement(exec_id, equipe=None):
    st, r = call(OP, 'GET', f'/operateur/of/{exec_id}/assemblage/avancement?posteCode={POSTE}' + (f'&equipe={equipe}' if equipe else ''))
    return r if st == 200 else {'erreur': (st, r)}
def tache(av, code):
    return next((t for t in av.get('taches', []) if t['code'] == code), {})
def alertes(exec_id, equipe=None):
    st, r = call(OP, 'GET', f'/operateur/of/{exec_id}/alertes-actives?posteCode={POSTE}' + (f'&equipe={equipe}' if equipe else ''))
    return [o for t in (r or []) for o in t.get('occurrences', [])] if st == 200 else []
def periodes(exec_id):
    """(motif de fin, contrôles planifiés) de chaque période, dans l'ordre."""
    lignes = sql(f"SELECT ISNULL(EndReason,'OUVERTE')+':'+CAST(PlannedControlCount AS VARCHAR) FROM Exec_ProductionPeriod "
                 f"WHERE ExecControleOfId='{exec_id}' ORDER BY StartedAt, CASE WHEN EndedAt IS NULL THEN 1 ELSE 0 END")
    return lignes.split() if lignes else []
def avancer(exec_id, intervalles):
    """Recule le début de la période ouverte de N intervalles (le temps passe pour la planification)."""
    sql(f"UPDATE Exec_ProductionPeriod SET StartedAt = DATEADD(ms, -CAST({intervalles} * IntervalMinutes * 60000 AS INT), StartedAt) "
        f"WHERE ExecControleOfId='{exec_id}' AND EndedAt IS NULL")
def nb_controles(exec_id, session_filtre=''):
    return int(sql(f"SELECT COUNT(DISTINCT HeureNotifPrevue) FROM Exec_Prelevement_Intermediaire "
                   f"WHERE ExecControleOFId='{exec_id}' AND PosteCode='{POSTE}' {session_filtre}") or 0)
def saisir(exec_id, section, moment, resultat='C'):
    return call(OP, 'POST', f'/ExecPlanAssemblage/{exec_id}/resultat', {
        'posteCode': POSTE, 'sectionId': section['id'], 'moment': moment,
        'lignesControle': [{'ligneId': l['id'], 'resultat': resultat, 'nc': 'Jeu' if resultat == 'NC' else None} for l in section['lignesPlan']]})

# ───────────── Référentiel ─────────────
call(ADMIN, 'POST', '/documents-echantillonnage', {'niveauControle': 'II', 'typePlan': 'SIMPLE', 'modeControle': 'NORMAL', 'valeurNqa': 1.5,
    'critereAcceptationAc': 1, 'critereRejetRe': 2, 'remarques': '', 'legendeMoyens': '', 'refFormulaireCodeReference': 'FE-ECHAN-01'})
st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'PLAN_ASS', 'nom': 'Plan RBGFM PAS71 production', 'familleProduitFiniCode': 'RBGFM',
    'posteCode': POSTE, 'refFormulaireCodeReference': 'FE-ASS-RBGFM-PROD',
    'sections': [{'ordreAffiche': 1, 'libelleSection': 'Production', 'typeSectionId': TYPE['ECHANTILLONNAGE_2859'],
                  'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Étanchéité'}, {'ordreAffiche': 2, 'libelleAffiche': 'Aspect'}]},
                 {'ordreAffiche': 2, 'libelleSection': 'Réglage', 'typeSectionId': TYPE['REGLAGE'],
                  'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Couple de serrage'}]}]})
check("plan d'assemblage (production + réglage) actif", st in (200, 201), (st, r))
st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'CTRL_POSTE', 'nom': 'Contrôle poste PAS71', 'posteCode': POSTE,
    'sections': [{'ordreAffiche': 1, 'libelleSection': 'Lignes de Contrôle', 'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Fuite'}]}]})
check('contrôle au poste PAS71 actif', st in (200, 201), (st, r))
# Formulaire « PAS71 soupape » : poste et famille portés par le référentiel, jamais déduits du texte.
st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'CTRL_POSTE', 'nom': 'Contrôle poste PAS71 soupape', 'posteCode': POSTE,
    'refFormulaireCodeReference': 'FE-RC-PAS71_SOUPAPE',
    'sections': [{'ordreAffiche': 1, 'libelleSection': 'Lignes de Contrôle', 'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Soupape'}]}]})
soupape = data(r).get('id') if isinstance(data(r), dict) else None
check('document créé sur le formulaire soupape : famille RBGFA_SOUPAPE reprise du formulaire', st in (200, 201) and sql(
    f"SELECT PosteCode+'/'+ISNULL(FamilleProduitFiniCode,'-') FROM Document_Entete WHERE Id='{soupape}'") == 'PAS71/RBGFA_SOUPAPE', (st, r))
st, r = call(SUP, 'POST', '/Document', {'typeDocumentCode': 'CTRL_POSTE', 'nom': 'Mauvais poste', 'posteCode': 'PAS72',
    'refFormulaireCodeReference': 'FE-RC-PAS71_SOUPAPE', 'sections': []})
check('formulaire du PAS71 refusé pour le PAS72', st == 400 and 'PAS71' in json.dumps(r, ensure_ascii=False), (st, r))

# ───────────── Démarrage → première période de production ─────────────
st, r = call(OP, 'POST', '/operateur/of/start-assemblage', {'numeroOf': 'OF2607ASS01', 'posteCodes': [POSTE], 'equipe': 'E1'})
check('OF démarré par E1', st == 200, (st, r)); of = data(r)['id'] if st == 200 else None
check('aucune période avant la production', periodes(of) == [], periodes(of))
call(OP, 'POST', f'/ExecEchantillonnage/init/{of}', {'posteCode': POSTE, 'nbPostes': 1, 'instrumentCodes': [], 'equipe': 'E1'})
call(OP, 'POST', f'/Operateur/of/{of}/verif-machine/cloturer-tous?posteCode={POSTE}')
av = avancement(of)
check('poste en production', av.get('phase') == 'PRODUCTION', av)
check('période ouverte dès la production', periodes(of) == ['OUVERTE:0'], periodes(of))
effectif = int(sql(f"SELECT TOP 1 e.EffectifParPosteAb FROM Exec_Echantillonnage e JOIN Exec_ControleDocumentStatut s "
                   f"ON s.Id=e.ExecControleDocumentStatutId WHERE s.ExecControleOfId='{of}'") or 0)
intervalle = float(sql(f"SELECT IntervalMinutes FROM Exec_ProductionPeriod WHERE ExecControleOfId='{of}'") or 0)
check('intervalle = 60 / effectif par poste de la fiche', effectif > 0 and abs(intervalle - 60.0 / effectif) < 1e-6, (effectif, intervalle))
check('tâche de production : jamais « faite », annonce le prochain contrôle',
      tache(av, 'CONTROLES_PRODUCTION').get('etat') == 'A_FAIRE' and 'prochain contrôle' in tache(av, 'CONTROLES_PRODUCTION').get('detail', ''),
      tache(av, 'CONTROLES_PRODUCTION'))

# ───────────── Le temps passe : contrôles créés par le serveur ─────────────
avancer(of, 3.5)
occ = alertes(of)
check('3 contrôles dus créés (un par intervalle écoulé)', nb_controles(of) == 3 and len(occ) == 3, (nb_controles(of), len(occ)))
check('occurrences rattachées au poste, à la session et à la période', sql(
    f"SELECT COUNT(*) FROM Exec_Prelevement_Intermediaire WHERE ExecControleOFId='{of}' AND (PosteCode IS NULL OR SessionId IS NULL OR ProductionPeriodId IS NULL)") == '0')
check('numérotation par équipe et par poste : 1, 2, 3', sorted(o['numeroOccurrence'] for o in occ) == [1, 2, 3], [o['numeroOccurrence'] for o in occ])
check('une occurrence par section de production (2 lignes)', all(len(o['caracteristiques']) == 2 for o in occ), occ[:1])
check('les deux premiers en retard (le suivant est déjà dû)', sum(1 for o in occ if o['estEnRetard']) == 2, [o['estEnRetard'] for o in occ])
av = avancement(of)
check("assistant : 3 contrôles à faire", tache(av, 'CONTROLES_PRODUCTION').get('compteur') == 3, tache(av, 'CONTROLES_PRODUCTION'))
check('lecture répétée : rien de recréé', alertes(of) and nb_controles(of) == 3)

premier = min(occ, key=lambda o: o['numeroOccurrence'])
st, r = call(OP, 'POST', f"/operateur/occurrence/{premier['id']}/repondre",
             {'resultat': 'C', 'associatedIds': premier['associatedIds'],
              'lignes': [{'lignePlanId': c['lignePlanId'], 'resultat': 'C'} for c in premier['caracteristiques']]})
check('réponse au premier contrôle', st in (200, 204), (st, r))
check('assistant : 2 contrôles à faire', tache(avancement(of), 'CONTROLES_PRODUCTION').get('compteur') == 2)
check("occurrence répondue gardée (historique)", sql(
    f"SELECT COUNT(*) FROM Exec_Prelevement_Intermediaire WHERE ExecControleOFId='{of}' AND EstRepondu=1") == '1')

# ───────────── Pause : pas de contrôle ; reprise : nouveau calcul ─────────────
call(OP, 'POST', f'/Operateur/of/{of}/pause', {'raison': 'Réunion', 'typePause': 'AUTRE'})
avancement(of)
check('pause → période fermée (PAUSE)', periodes(of) == ['PAUSE:3'], periodes(of))
check("fin de période = heure de la pause", sql(
    f"SELECT COUNT(*) FROM Exec_ProductionPeriod p JOIN Exec_ControleOf_Session s ON s.Id=p.SessionId "
    f"WHERE p.ExecControleOfId='{of}' AND p.EndedAt = s.SuspendedAt") == '1')
sql(f"UPDATE Exec_ProductionPeriod SET StartedAt = DATEADD(hour, -2, StartedAt) WHERE ExecControleOfId='{of}'")
alertes(of)
check('pendant la pause : aucun nouveau contrôle', nb_controles(of) == 3, nb_controles(of))
call(OP, 'POST', f'/Operateur/of/{of}/reprendre')
avancement(of)
check('reprise → nouvelle période, calcul reparti de zéro', periodes(of) == ['PAUSE:3', 'OUVERTE:0'], periodes(of))
avancer(of, 1.5)
occ = alertes(of)
check('contrôle de la nouvelle période numéroté à la suite (#4)', 4 in [o['numeroOccurrence'] for o in occ] and nb_controles(of) == 4,
      ([o['numeroOccurrence'] for o in occ], nb_controles(of)))

# ───────────── Réglage : contrôles suspendus jusqu'à un réglage conforme ─────────────
st, _ = call(OP, 'POST', f'/operateur/of/{of}/assemblage/reglage?posteCode={POSTE}')
av = avancement(of)
check('réglage commencé → période fermée (SETTING)', st == 204 and periodes(of)[-1] == 'SETTING:1', (st, periodes(of)))
check('assistant : réglage en cours, contrôles suspendus',
      av.get('reglageEnCours') is True and 'Réglage en cours' in tache(av, 'CONTROLES_PRODUCTION').get('detail', ''), av)
sql(f"UPDATE Exec_ProductionPeriod SET StartedAt = DATEADD(hour, -2, StartedAt) WHERE ExecControleOfId='{of}'")
alertes(of)
check('pendant le réglage : aucun nouveau contrôle', nb_controles(of) == 4 and len(periodes(of)) == 2, (nb_controles(of), periodes(of)))
st, r = call(OP, 'GET', f'/ExecPlanAssemblage/{of}?posteCode={POSTE}')
reglage = next((s for s in (r or {}).get('sectionsALaDemande', []) if s['contexte'] == 'REGLAGE'), None)
st, r = saisir(of, reglage, 'REGLAGE', 'NC')
check('réglage non conforme → toujours en cours', st == 204 and avancement(of).get('reglageEnCours') is True, (st, r))
st, r = call(OP, 'DELETE', f'/operateur/of/{of}/assemblage/reglage?posteCode={POSTE}')
check("annulation refusée après une saisie de réglage", st == 400, (st, r))
st, r = saisir(of, reglage, 'REGLAGE', 'C')
av = avancement(of)
check('réglage conforme sur tous les critères → production reprise (nouvelle période)',
      st == 204 and av.get('reglageEnCours') is False and periodes(of)[-1] == 'OUVERTE:0' and len(periodes(of)) == 3, (st, periodes(of), av.get('reglageEnCours')))
st, _ = call(OP, 'POST', f'/operateur/of/{of}/assemblage/reglage?posteCode={POSTE}')
st2, _ = call(OP, 'DELETE', f'/operateur/of/{of}/assemblage/reglage?posteCode={POSTE}')
check('réglage annulé avant toute saisie → la production reprend',
      (st, st2) == (204, 204) and periodes(of)[-2:] == ['SETTING_CANCELLED:0', 'OUVERTE:0'], periodes(of))

# ───────────── Contrôle au poste : document du jour commun aux équipes ─────────────
st, r = call(OP, 'GET', f'/ExecRcPoste/{of}?posteCode={POSTE}&equipe=E1')
statut = r.get('execControleDocumentStatutId') if st == 200 else None
ligne = r['lignes'][0]['docLigneId'] if st == 200 else None
check("article manuel sur PAS71 : document général du poste, jamais celui des robinets avec soupape",
      st == 200 and r.get('planNom', '').startswith('Contrôle poste PAS71') and 'soupape' not in r.get('planNom', ''), (st, r.get('planNom') if isinstance(r, dict) else r))
check('document du jour sans équipe propriétaire', st == 200 and sql(
    f"SELECT ISNULL(Equipe,'AUCUNE') FROM Exec_ControleDocumentStatut WHERE Id='{statut}'") == 'AUCUNE', (st, r))
check('contrôle au poste à faire par E1', tache(avancement(of), 'CONTROLE_POSTE').get('etat') == 'A_FAIRE')
check("équipes et heures du document : E1 de 6 h à 14 h, saisie ouverte à E1 seulement",
      st == 200 and r.get('equipeSaisie') == 'E1' and r.get('peutSaisir') and r.get('tranchesEquipe', [])[:2] == ['06-07', '07-08']
      and [e['code'] for e in r.get('equipes', [])] == ['E1', 'E2', 'E3'], (r.get('equipeSaisie'), r.get('tranchesEquipe'), r.get('motifLectureSeule')))
st, r = call(OP, 'POST', f'/ExecRcPoste/{statut}/valider-equipe')
check("validation refusée tant qu'aucune heure n'est saisie", st == 400, (st, r))
st, r = call(OP, 'PUT', f'/ExecRcPoste/{statut}', {'execControleDocumentStatutId': statut,
    'lignes': [{'docLigneId': ligne, 'heures': [{'trancheHoraire': '06-07', 'nbNcParHeure': 2}, {'trancheHoraire': '14-15', 'nbNcParHeure': 9}]}],
    'reponses': [{'trancheHoraire': '06-07', 'totalRealiseHeure': 40}]})
check("heures d'une autre équipe refusées (14-15 n'est pas à E1)", st == 400 and '14-15' in json.dumps(r, ensure_ascii=False), (st, r))
st, r = call(OP, 'PUT', f'/ExecRcPoste/{statut}', {'execControleDocumentStatutId': statut, 'nbPiecesRebuteesEquipe': 2,
    'lignes': [{'docLigneId': ligne, 'heures': [{'trancheHoraire': '06-07', 'nbNcParHeure': 2}]}],
    'reponses': [{'trancheHoraire': '06-07', 'totalRealiseHeure': 40}]})
check("E1 enregistre ses heures (équipe et opérateur gardés)", st == 200 and sql(
    f"SELECT STRING_AGG(TrancheHoraire+'='+CAST(Quantite AS VARCHAR)+'/'+ISNULL(Equipe,'-'), ',') FROM Exec_ControleLigne_Reponse "
    f"WHERE ExecControleDocumentStatutId='{statut}'") == '06-07=2/E1', (st, r))
check("cumul des pièces conformes de l'OF sur le poste affiché avec la quantité lancée",
      (r.get('cumulPiecesConformes'), r.get('quantiteLancee'), r.get('quantiteLanceeDepassee')) == (40, 1000, False), r)
st, r2 = call(OP, 'PUT', f'/ExecRcPoste/{statut}', {'execControleDocumentStatutId': statut, 'nbPiecesRebuteesEquipe': 2,
    'derniereSaisieEquipe': r.get('derniereSaisieEquipe'),
    'lignes': [{'docLigneId': ligne, 'heures': [{'trancheHoraire': '06-07', 'nbNcParHeure': 2}]}],
    'reponses': [{'trancheHoraire': '06-07', 'totalRealiseHeure': 2000}]})
check("plus de pièces conformes que la quantité lancée de l'OF : saisie refusée",
      st == 400 and 'quantité lancée' in json.dumps(r2, ensure_ascii=False) and sql(
          f"SELECT TotalRealiseHeure FROM Exec_RC_Poste_Reponse WHERE ExecControleDocumentStatutId='{statut}'") == '40', (st, r2))
# Deux tablettes de E1 enregistrent au même instant : le verrou du poste les fait passer l'une après l'autre,
# la seconde voit la saisie de la première (409 « rechargez ») au lieu de l'écraser ou de dépasser la quantité.
import threading
resultats = []
def enregistrer(realisees):
    resultats.append(call(OP, 'PUT', f'/ExecRcPoste/{statut}', {'execControleDocumentStatutId': statut, 'nbPiecesRebuteesEquipe': 2,
        'derniereSaisieEquipe': r.get('derniereSaisieEquipe'),
        'lignes': [{'docLigneId': ligne, 'heures': [{'trancheHoraire': '06-07', 'nbNcParHeure': 2}]}],
        'reponses': [{'trancheHoraire': '06-07', 'totalRealiseHeure': realisees}]})[0])
fils = [threading.Thread(target=enregistrer, args=(n,)) for n in (41, 42)]
for f in fils: f.start()
for f in fils: f.join()
check("deux enregistrements simultanés du poste : un seul passe, l'autre doit recharger", sorted(resultats) == [200, 409], resultats)
call(OP, 'PUT', f'/ExecRcPoste/{statut}', {'execControleDocumentStatutId': statut, 'nbPiecesRebuteesEquipe': 2,
    'derniereSaisieEquipe': call(OP, 'GET', f'/ExecRcPoste/{of}?posteCode={POSTE}&equipe=E1')[1].get('derniereSaisieEquipe'),
    'lignes': [{'docLigneId': ligne, 'heures': [{'trancheHoraire': '06-07', 'nbNcParHeure': 2}]}],
    'reponses': [{'trancheHoraire': '06-07', 'totalRealiseHeure': 40}]})
st, r = call(OP, 'POST', f'/ExecRcPoste/{statut}/valider-equipe')
check('E1 valide ses heures', st == 200 and [v['equipe'] for v in r.get('validations', [])] == ['E1'], (st, r))
check('contrôle au poste fait pour E1', tache(avancement(of), 'CONTROLE_POSTE').get('etat') == 'FAIT')

# ───────────── Registre de traçabilité : lots vérifiés, lignes par équipe, annulation, confirmation ─────────────
for of_usinage in ('OF26080157', 'OF26050439', 'OF26021488'):   # corps 2903201-1, volants 3028S01-1 et 31B5201-1
    sql(f"INSERT INTO Exec_ControleOF (NumeroOF, OperationCode, TypeOf, Statut) VALUES ('{of_usinage}', 'USINAG', 'FAB', 'EN_COURS')")
# Corps et volant de l'article sont lus dans sa nomenclature : un volant hors nomenclature n'est jamais accepté.
sql("INSERT INTO BOMD_Nomenclature (ArticleParent, CodeComposant, CodeAlternative, QuantiteRequise) VALUES ('2511A01-1', '2903201-1', 9, 1)")
st, r = call(OP, 'POST', '/Operateur/tracabilite/ligne', {'execControleOfId': of, 'posteCode': POSTE, 'corps': 'OF26080157', 'volant': 'OF26050439',
    'composants': [{'codeArticle': '2903201-1', 'lotSelectionne': 'LOT-CORPS-01'}]})
check("registre : nomenclature sans volant → aucun volant accepté (message explicite)",
      st == 400 and 'ne contient aucun volant' in json.dumps(r, ensure_ascii=False), (st, r))
sql("INSERT INTO BOMD_Nomenclature (ArticleParent, CodeComposant, CodeAlternative, QuantiteRequise) VALUES ('2511A01-1', '3028S01-1', 9, 1)")
st, r = call(OP, 'POST', '/Operateur/tracabilite/ligne', {'execControleOfId': of, 'posteCode': POSTE, 'corps': 'OF26080157', 'volant': 'OF26021488',
    'composants': [{'codeArticle': '2903201-1', 'lotSelectionne': 'LOT-CORPS-01'}]})
check("registre : volant d'un autre article que celui de la nomenclature refusé",
      st == 400 and 'OF26021488' in json.dumps(r, ensure_ascii=False) and 'nomenclature' in json.dumps(r, ensure_ascii=False), (st, r))
st, r = call(OP, 'GET', f'/Operateur/tracabilite/{of}/search-lots?type=VOLANT&query=OF')
check('registre : seuls les volants de la nomenclature sont proposés', st == 200 and data(r) == ['OF26050439'], (st, r))
st, r = call(OP, 'GET', f'/Operateur/tracabilite/{of}?posteCode={POSTE}')
reg = data(r) if st == 200 else {}
dispo = {c['codeArticle']: c['lotsDisponibles'] for c in reg.get('composantsDisponibles', [])}
check('registre : E1 peut saisir, composants scannés par le magasin (par code article)',
      reg.get('peutSaisir') and reg.get('equipeSaisie') == 'E1' and dispo.get('2903201-1') == ['LOT-CORPS-01'], reg)
def ligne_registre(corps='OF26080157', volant='OF26050439', lot='LOT-CORPS-01', composants=None):
    return call(OP, 'POST', '/Operateur/tracabilite/ligne', {'execControleOfId': of, 'posteCode': POSTE, 'corps': corps, 'volant': volant,
        'composants': composants if composants is not None else [{'codeArticle': '2903201-1', 'lotSelectionne': lot}]})
check('registre : corps obligatoire', ligne_registre(corps='')[0] == 400)
st, r = ligne_registre(corps='OF26050439')
check("registre : lot de corps qui n'est pas un corps refusé", st == 400 and 'corps' in json.dumps(r, ensure_ascii=False), (st, r))
st, r = ligne_registre(lot='LOT-INCONNU')
check('registre : lot de composant non scanné par le magasin refusé', st == 400 and 'scanné' in json.dumps(r, ensure_ascii=False), (st, r))
check('registre : composant oublié refusé', ligne_registre(composants=[])[0] == 400)
st, r = ligne_registre()
l1 = data(r) if st == 200 else {}
check('registre : ligne V1 avec équipe, poste et opérateur', st == 200 and (l1.get('version'), l1.get('equipe'), l1.get('posteCode'), l1.get('matriculeOperateur')) == (1, 'E1', POSTE, '55555'), (st, r))
st, r = ligne_registre()
l2 = data(r) if st == 200 else {}
check('registre : mêmes lots → même version', st == 200 and l2.get('version') == 1, (st, r))
st, r = call(OP, 'POST', f"/Operateur/tracabilite/ligne/{l2.get('id')}/annuler", {'raison': 'Saisie en double'})
check('registre : ligne annulée avec motif, gardée dans l\'historique', st == 200 and sql(
    f"SELECT CancellationReason+'/'+CancelledBy FROM ExecRegistreTracabilite WHERE Id='{l2.get('id')}'") == 'Saisie en double/55555', (st, r))
check('registre : annulation sans motif refusée', call(OP, 'POST', f"/Operateur/tracabilite/ligne/{l1.get('id')}/annuler", {'raison': ''})[0] == 400)
check('registre : à confirmer par E1', tache(avancement(of), 'TRACABILITE').get('etat') == 'A_FAIRE')
st, r = call(OP, 'POST', f'/Operateur/tracabilite/{of}/confirmer?posteCode={POSTE}')
check("registre : E1 confirme ses lots (tâche faite, registre toujours ouvert)", st == 200 and tache(avancement(of), 'TRACABILITE').get('etat') == 'FAIT'
      and data(call(OP, 'GET', f'/Operateur/tracabilite/{of}?posteCode={POSTE}')[1]).get('peutSaisir'), (st, r))

# ───────────── Deuxième équipe : ses propres contrôles et ses heures ─────────────
call(OP, 'POST', f'/Operateur/of/{of}/pause', {'raison': 'Relève', 'typePause': 'AUTRE'})
st, r = call(OP, 'POST', f'/Operateur/of/{of}/ajouter-postes', {'posteCodes': [POSTE], 'equipe': 'E2'})
check("E2 ne commence pas pendant la pause de E1", st == 409, (st, r))
call(OP, 'POST', f'/Operateur/of/{of}/reprendre?equipe=E1')
e1_avant = int(sql(f"SELECT COUNT(*) FROM Exec_Prelevement_Intermediaire WHERE ExecControleOFId='{of}' AND EstRepondu=0 "
                   f"AND SessionId=(SELECT TOP 1 Id FROM Exec_ControleOf_Session WHERE ExecControleOfId='{of}' AND Equipe='E1')") or 0)
call(ADMIN, 'POST', f'/Operateur/of/{of}/fin-poste')
st, r = call(OP, 'POST', f'/Operateur/of/{of}/ajouter-postes', {'posteCodes': [POSTE], 'equipe': 'E2'})
check("E2 prend l'OF après la fin de poste de E1", st in (200, 204), (st, r))
call(OP, 'POST', f'/Operateur/of/{of}/verif-machine/cloturer-tous?posteCode={POSTE}')
av2 = avancement(of, 'E2')
check('E2 en production : sa propre période', av2.get('phase') == 'PRODUCTION' and sql(
    f"SELECT COUNT(DISTINCT SessionId) FROM Exec_ProductionPeriod WHERE ExecControleOfId='{of}'") == '2', (av2.get('phase'), periodes(of)))
check('E2 : aucun contrôle hérité de E1', alertes(of, 'E2') == [] and tache(av2, 'CONTROLES_PRODUCTION').get('compteur') == 0,
      tache(av2, 'CONTROLES_PRODUCTION'))
check("contrôles de E1 conservés après sa fin de poste (rien n'est effacé)", e1_avant > 0 and sql(
    f"SELECT COUNT(*) FROM Exec_Prelevement_Intermediaire WHERE ExecControleOFId='{of}' AND EstRepondu=0 "
    f"AND SessionId=(SELECT TOP 1 Id FROM Exec_ControleOf_Session WHERE ExecControleOfId='{of}' AND Equipe='E1')") == str(e1_avant), e1_avant)
check("période de E1 fermée par sa fin de poste", 'END_OF_SHIFT' in sql(f"SELECT STRING_AGG(EndReason, ',') FROM Exec_ProductionPeriod p "
      f"JOIN Exec_ControleOf_Session s ON s.Id=p.SessionId WHERE p.ExecControleOfId='{of}' AND s.Equipe='E1'"))
avancer(of, 1.2)
occ2 = alertes(of, 'E2')
check('E2 : premier contrôle numéroté #1 (numérotation par équipe)', [o['numeroOccurrence'] for o in occ2] == [1], [o['numeroOccurrence'] for o in occ2])
st, r = call(OP, 'GET', f'/ExecRcPoste/{of}?posteCode={POSTE}&equipe=E2')
check('E2 retrouve le même document du jour', st == 200 and r.get('execControleDocumentStatutId') == statut, (st, r))
check("contrôle au poste à faire par E2 (la validation de E1 ne compte pas)", tache(avancement(of, 'E2'), 'CONTROLE_POSTE').get('etat') == 'A_FAIRE')
check('E2 : ses propres heures (14 h–22 h)', r.get('equipeSaisie') == 'E2' and r.get('tranchesEquipe', [])[:1] == ['14-15'], r.get('tranchesEquipe'))
check("registre : la confirmation de E1 ne compte pas pour E2", tache(avancement(of, 'E2'), 'TRACABILITE').get('etat') == 'A_FAIRE')
st, r = call(OP, 'POST', f'/Operateur/tracabilite/{of}/confirmer?posteCode={POSTE}')
check("registre : E2 confirme les mêmes lots (ligne reprise à son nom)", st == 200 and tache(avancement(of, 'E2'), 'TRACABILITE').get('etat') == 'FAIT'
      and sql(f"SELECT COUNT(*) FROM ExecRegistreTracabilite WHERE ExecControleOfId='{of}' AND Equipe='E2' AND CancelledAt IS NULL") == '1', (st, r))
st, r = call(OP, 'PUT', f'/ExecRcPoste/{statut}', {'execControleDocumentStatutId': statut,
    'lignes': [{'docLigneId': ligne, 'heures': [{'trancheHoraire': '14-15', 'nbNcParHeure': 1}]}], 'reponses': []})
check("E2 saisit ses heures sans effacer celles de E1", st == 200 and sql(
    f"SELECT STRING_AGG(TrancheHoraire+'/'+ISNULL(Equipe,'-'), ',') WITHIN GROUP (ORDER BY TrancheHoraire) FROM Exec_ControleLigne_Reponse "
    f"WHERE ExecControleDocumentStatutId='{statut}'") == '06-07/E1,14-15/E2', (st, r))
check('pièces réalisées de E1 conservées, avec son équipe', sql(
    f"SELECT Equipe+'/'+MatriculeOperateur FROM Exec_RC_Poste_Reponse WHERE ExecControleDocumentStatutId='{statut}'") == 'E1/55555')
st, r = call(OP, 'PUT', f'/ExecRcPoste/{statut}', {'execControleDocumentStatutId': statut, 'nbPiecesRebuteesEquipe': 1,
    'lignes': [{'docLigneId': ligne, 'heures': [{'trancheHoraire': '14-15', 'nbNcParHeure': 3}]}], 'reponses': []})
check('seconde tablette de E2 sans recharger : refusée (409)', st == 409, (st, r))
st, r = call(OP, 'GET', f'/ExecRcPoste/statut/{statut}')
check('rebuts par équipe, total = somme', st == 200 and r['bilan']['nbPiecesRebutees'] == 2
      and sorted((x['equipe'], x['nbPiecesRebutees']) for x in r.get('rebutsParEquipe', [])) == [('E1', 2)], (st, r.get('rebutsParEquipe')))

call(ADMIN, 'POST', f'/Operateur/of/{of}/close')
avancement(of)
check("clôture de l'OF → plus aucune période ouverte", sql(
    f"SELECT COUNT(*) FROM Exec_ProductionPeriod WHERE ExecControleOfId='{of}' AND EndedAt IS NULL") == '0', periodes(of))

# ───────────── Fabrication : une suite par section de périodicité horaire ─────────────
sql("IF NOT EXISTS (SELECT 1 FROM MFGHEAD_OrdreFabrication WHERE NumeroOF='OF26FAB065') INSERT INTO MFGHEAD_OrdreFabrication "
    "(NumeroOF,CodeArticle,QuantitePrevue,QuantiteLancee,QuantiteReelle,StatutOF) VALUES ('OF26FAB065','2903201-1',100,100,0,'EN_COURS')")
tous = sql("SELECT Id FROM TypeSection WHERE TypeNormalise='TOUS_PIECES'")
horaire = sql("SELECT TOP 1 Id FROM Periodicite WHERE Unite='HEURE' AND NbrPieces=4 AND NbrHeures=1")
serie = sql("SELECT TOP 1 Id FROM Periodicite WHERE Unite='SERIE'")
st, r = call(ADMIN, 'POST', '/PlanFabrication', {'nom': '2903201-1', 'codeArticleSageVersionne': '2903201-1', 'operationCode': 'TRONC', 'statut': 'ACTIF',
    'sections': [{'ordreAffiche': 1, 'libelleSection': 'Production', 'typeSectionId': tous, 'periodiciteId': horaire,
                  'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Longueur', 'moyenTexteLibre': 'Réglet'}]},
                 {'ordreAffiche': 2, 'libelleSection': 'Série', 'typeSectionId': tous, 'periodiciteId': serie,
                  'lignes': [{'ordreAffiche': 1, 'libelleAffiche': 'Aspect', 'moyenTexteLibre': 'Visuel'}]}]})
check('F : plan de fabrication (section horaire + section série)', st == 200, (st, r))
st, r = call(OP, 'POST', '/operateur/of/start', {'numeroOf': 'OF26FAB065', 'operationCode': 'TRONC', 'machineCode': 'TRN26'})
fab = (r or {}).get('id') if isinstance(r, dict) else None
check('F : OF de fabrication démarré', st == 200, (st, r))
alertes_fab = lambda: call(OP, 'GET', f'/operateur/of/{fab}/alertes-actives')
def occ_fab():
    return int(sql(f"SELECT COUNT(*) FROM Exec_Prelevement_Intermediaire WHERE ExecControleOFId='{fab}'") or 0)
alertes_fab()
check('F : une seule suite (section horaire : 4 pièces / h → toutes les 15 min), aucune pour la série',
      periodes(fab) == ['OUVERTE:0'] and sql(f"SELECT CAST(IntervalMinutes AS VARCHAR) FROM Exec_ProductionPeriod WHERE ExecControleOfId='{fab}'") == '15', periodes(fab))
check('F : aucun contrôle avant son heure', occ_fab() == 0)
avancer(fab, 2.5); alertes_fab()
check('F : 2 contrôles dus, numérotés 1 et 2, rattachés à la période', sql(
    f"SELECT STRING_AGG(CAST(NumeroOccurrence AS VARCHAR), ',') WITHIN GROUP (ORDER BY NumeroOccurrence) FROM Exec_Prelevement_Intermediaire "
    f"WHERE ExecControleOFId='{fab}' AND ProductionPeriodId IS NOT NULL AND PosteCode IS NULL") == '1,2')
call(OP, 'POST', f'/Operateur/of/{fab}/pause', {'raison': 'Pause', 'typePause': 'AUTRE'}); alertes_fab()
check('F : pause → période fermée', periodes(fab) == ['PAUSE:2'], periodes(fab))
call(OP, 'POST', f'/Operateur/of/{fab}/reprendre'); alertes_fab()
avancer(fab, 1.5); alertes_fab()
check('F : reprise → nouveau calcul, numérotation continue (#3)', periodes(fab) == ['PAUSE:2', 'OUVERTE:1'] and sql(
    f"SELECT MAX(NumeroOccurrence) FROM Exec_Prelevement_Intermediaire WHERE ExecControleOFId='{fab}'") == '3', periodes(fab))
st, _ = call(OP, 'POST', f'/operateur/of/{fab}/mettre-en-reglage'); alertes_fab()
check('F : réglage → période fermée (SETTING)', st == 204 and periodes(fab)[-1] == 'SETTING:1', (st, periodes(fab)))
sql(f"UPDATE Exec_ProductionPeriod SET StartedAt = DATEADD(hour, -2, StartedAt) WHERE ExecControleOfId='{fab}'"); alertes_fab()
check('F : pendant le réglage : aucun nouveau contrôle', occ_fab() == 3, occ_fab())
st, r = call(OP, 'POST', f'/Operateur/of/{fab}/reprendre'); alertes_fab()
check('F : fin du réglage → nouvelle période', st == 204 and periodes(fab)[-1] == 'OUVERTE:0' and len(periodes(fab)) == 3, (st, r, periodes(fab)))
call(ADMIN, 'POST', f'/Operateur/of/{fab}/close'); alertes_fab()
check("F : clôture → plus aucune période ouverte", 'OUVERTE' not in ' '.join(periodes(fab)), periodes(fab))

print(f'e2e65: {ok} OK / {ko} KO')
