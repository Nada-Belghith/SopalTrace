# Authentification et administration des rôles, de bout en bout (vrais jetons émis par /Auth/login).
import base64, hashlib, hmac, json, time, urllib.request, urllib.error
from configuration import BASE, DB, SECRET, sql as _sql
MDP = 'Passw0rdE2E!'

def call(method, path, body=None, tok=None, cookie=None):
    headers = {'Content-Type': 'application/json'}
    if tok: headers['Authorization'] = 'Bearer ' + tok
    if cookie: headers['Cookie'] = 'refreshToken=' + cookie
    req = urllib.request.Request(BASE + path, method=method, data=json.dumps(body).encode() if body is not None else None, headers=headers)
    try:
        with urllib.request.urlopen(req) as r:
            t = r.read().decode(); return r.status, (json.loads(t) if t else None), r.headers
    except urllib.error.HTTPError as e:
        t = e.read().decode(); return e.code, (json.loads(t) if t.strip().startswith(('{', '[')) else t[:300]), e.headers

def cookie_de(headers):
    for v in headers.get_all('Set-Cookie') or []:
        if v.startswith('refreshToken=') and not v.startswith('refreshToken=;'):
            return v.split(';', 1)[0].split('=', 1)[1]
    return None

def login(mat, mdp=MDP):
    st, r, h = call('POST', '/Auth/login', {'matricule': mat, 'motDePasse': mdp})
    return st, r, cookie_de(h)

def sql(q):
    return _sql(q)

ok = ko = 0
def check(label, cond, info=''):
    global ok, ko
    if cond: ok += 1; print('OK ', label)
    else: ko += 1; print('KO ', label, str(info)[:400])
def msg(r): return r.get('message', '') if isinstance(r, dict) else str(r)

# ── Connexion ──
st, r, _ = login('00000')
check('matricule inconnu : 401 sans erreur serveur', st == 401, (st, r))
st, r, c_op = login('55555')
op = r.get('token') if st == 200 else None
check('connexion opérateur : jeton + cookie de rafraîchissement', st == 200 and op and c_op, (st, r))
st, r, c_admin = login('66666')
admin = r.get('token') if st == 200 else None
check('connexion administrateur', st == 200 and admin, (st, r))

# ── Accès à l'administration ──
check("l'opérateur n'accède pas à l'administration (403)", call('GET', '/admin/users', tok=op)[0] == 403)
st, r, _ = call('GET', '/admin/users?search=555', tok=admin)
check("l'administrateur liste les comptes (recherche)", st == 200 and r['total'] >= 1 and r['items'][0]['matricule'] == '55555', (st, r))
st, r, _ = call('GET', '/admin/roles', tok=admin)
check('rôles lus dans AppRole', st == 200 and {x['code'] for x in r} >= {'ADMIN', 'OPERATEUR', 'MAGASINIER'}, (st, r))
st, r, _ = call('GET', '/admin/dashboard', tok=admin)
check('tableau de bord : compteurs réels', st == 200 and r['totalAccounts'] == 7 and r['activeAccounts'] == 7
      and r['pendingApprovalAccounts'] == 0 and r['usersWithOpenSession'] >= 2, (st, r))

# ── Changement de rôle : effet immédiat ──
check("le jeton de l'opérateur est accepté avant le changement", call('GET', '/referentiels/verif-machine', tok=op)[0] == 200)
st, r, _ = call('PUT', '/admin/users/55555/role', {'role': 'MAGASINIER'}, tok=admin)
check('rôle changé par l\'administrateur', st == 200 and r['role'] == 'MAGASINIER' and r['activeSessions'] == 0, (st, r))
check("l'ancien jeton est refusé aussitôt (401), sans attendre son expiration", call('GET', '/referentiels/verif-machine', tok=op)[0] == 401)
check("l'ancien cookie ne permet plus de rafraîchir", call('POST', '/Auth/refresh', cookie=c_op)[0] == 401)
st, r, c_op = login('55555')
check('reconnexion : le nouveau jeton porte le nouveau rôle', st == 200 and r['roleApp'] == 'MAGASINIER', (st, r))
check('changement tracé dans AuditLog (avant/après, auteur)', sql(
    "SELECT COUNT(*) FROM AuditLog WHERE TableName='UtilisateursApp' AND UserMatricule='66666' AND Changes LIKE '%RoleApp%MAGASINIER%'") == '1')
check('changement tracé dans le journal des connexions', sql(
    "SELECT COUNT(*) FROM JournalConnexions WHERE Matricule='55555' AND Action='ROLE_MODIFIE' AND Details LIKE '%66666%'") == '1')
check('le tampon de sécurité n\'apparaît jamais dans l\'audit', sql("SELECT COUNT(*) FROM AuditLog WHERE Changes LIKE '%SecurityStamp%'") == '0')
call('PUT', '/admin/users/55555/role', {'role': 'OPERATEUR'}, tok=admin)

# ── Garde-fous ──
st, r, _ = call('PUT', '/admin/users/66666/role', {'role': 'OPERATEUR'}, tok=admin)
check("l'administrateur ne change pas son propre rôle (400)", st == 400, (st, r))
st, r, _ = call('PUT', '/admin/users/66666/disabled', {'isDisabled': True}, tok=admin)
check("l'administrateur ne se désactive pas lui-même (400)", st == 400, (st, r))
st, r, _ = call('PUT', '/admin/users/55555/role', {'role': 'DIRECTEUR'}, tok=admin)
check('rôle inconnu refusé (400)', st == 400, (st, r))
st, r, _ = call('POST', '/admin/users/99999/unlock', tok=admin)
check('compte inconnu : 404', st == 404, (st, r))
# Promouvoir 44444 administrateur puis le rétrograder : autorisé tant qu'un autre administrateur reste.
call('PUT', '/admin/users/44444/role', {'role': 'ADMIN'}, tok=admin)
st, r, _ = call('PUT', '/admin/users/44444/role', {'role': 'SUPERVISEUR_QUALITE'}, tok=admin)
check('rétrograder un administrateur quand un autre reste', st == 200, (st, r))

# ── Désactivation ──
st, r, _ = call('PUT', '/admin/users/55555/disabled', {'isDisabled': True}, tok=admin)
check('compte désactivé', st == 200 and r['account']['status'] == 'DISABLED', (st, r))
st, r, _ = login('55555')
check('connexion refusée pour un compte désactivé', st == 401 and 'désactivé' in msg(r), (st, r))
call('PUT', '/admin/users/55555/disabled', {'isDisabled': False}, tok=admin)
st, r, c_op = login('55555')
check('réactivé : connexion de nouveau possible', st == 200, (st, r))

# ── Verrouillage après 5 échecs ──
for _ in range(4): login('22222', 'Mauvais1!')
st, r, _ = login('22222', 'Mauvais1!')
check('5e échec : compte verrouillé', st == 401 and 'verrouillé' in msg(r), (st, r))
st, r, _ = login('22222')
check('même avec le bon mot de passe, le compte reste verrouillé', st == 401 and 'verrouillé' in msg(r), (st, r))
st, r, _ = call('GET', '/admin/users?status=LOCKED', tok=admin)
check('le compte apparaît dans le filtre « Verrouillé »', st == 200 and [x['matricule'] for x in r['items']] == ['22222'], (st, r))
call('POST', '/admin/users/22222/unlock', tok=admin)
st, r, _ = login('22222')
check("déverrouillé par l'administrateur : connexion possible", st == 200, (st, r))

# ── Rafraîchissement : rotation et détection de réutilisation ──
st, r, h = call('POST', '/Auth/refresh', cookie=c_op)
c_op2 = cookie_de(h)
check('rafraîchissement : nouveau jeton et nouveau cookie', st == 200 and c_op2 and c_op2 != c_op, (st, r))
check('un jeton rafraîchi fonctionne', call('GET', '/referentiels/verif-machine', tok=r.get('token'))[0] == 200)
st, r, _ = call('POST', '/Auth/refresh', cookie=c_op)
check("réutilisation de l'ancien cookie : refusée", st == 401, (st, r))
check('… et toutes les sessions sont fermées (le cookie récent ne marche plus)', call('POST', '/Auth/refresh', cookie=c_op2)[0] == 401)
check('réutilisation tracée', int(sql("SELECT COUNT(*) FROM JournalConnexions WHERE Matricule='55555' AND Action='SESSION_REVOQUEE'") or 0) >= 1)

# ── Déconnexion sans jeton d'accès (expiré) ──
st, r, c_op = login('55555')
st, _, _ = call('POST', '/Auth/logout', cookie=c_op)
check('déconnexion avec le seul cookie', st == 200)
check('après déconnexion, le cookie ne rafraîchit plus', call('POST', '/Auth/refresh', cookie=c_op)[0] == 401)

# ── Jeton falsifié : rôle différent de celui du compte ──
def b64(d): return base64.urlsafe_b64encode(d).rstrip(b'=').decode()
def jwt(mat, role):
    h = b64(json.dumps({'alg': 'HS256', 'typ': 'JWT'}).encode()); now = int(time.time())
    p = b64(json.dumps({'sub': '00000000-0000-0000-0000-0000000' + mat, 'stamp': '11111111-2222-3333-4444-555555555555', 'matricule': mat,
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
        'iss': 'SopalTraceApi', 'aud': 'SopalTraceVueJs', 'iat': now, 'nbf': now, 'exp': now + 3600}).encode())
    return f'{h}.{p}.' + b64(hmac.new(SECRET, f'{h}.{p}'.encode(), hashlib.sha256).digest())
check('jeton signé mais rôle différent du compte (opérateur « ADMIN ») : refusé (401)',
      call('GET', '/admin/users', tok=jwt('33333', 'ADMIN'))[0] == 401)

# ── Demande d'accès : réponse neutre, file des demandes, décision tracée, lien à usage unique ──
sql("""INSERT INTO AUTILIS (USR_0, INTUSR_0, ENAFLG_0, CODMET_0, ADDEML_0) VALUES
       ('81001', 'Demandeur Approuve', 2, 'TECHNICIEN', 'demandeur1@s.com'),
       ('81002', 'Demandeur Refuse', 2, 'OPERATEUR', 'demandeur2@s.com'),
       ('81003', 'Employe Preattribue', 2, 'TECHNICIEN', 'demandeur3@s.com'),
       ('81004', 'Meme E-mail Que 77777', 2, 'OPERATEUR', 'nada.belghith@enis.u-sfax.tn')""")
def demander(mat, email):
    st, r, _ = call('POST', '/Auth/register', {'matricule': mat, 'email': email})
    return st, msg(r)
def statut(mat):
    return sql(f"SELECT AccountStatus+'|'+ISNULL(RoleApp,'-')+'|'+CASE WHEN MotDePasseHash IS NULL THEN 'sans-mdp' ELSE 'mdp' END"
               f"+'|'+CASE WHEN ActivationTokenHash IS NULL THEN 'sans-lien' ELSE 'lien' END FROM UtilisateursApp WHERE Matricule='{mat}'")

st, neutre = demander('81001', 'pirate@exemple.com')
check("e-mail différent de l'ERP : 200, aucun compte créé", st == 200 and statut('81001') == '', (st, neutre))
check('matricule inconnu : exactement la même réponse', demander('99998', 'x@exemple.com') == (200, neutre))
check('e-mail partagé par deux employés (77777 / 81004) : même réponse, aucun compte',
      demander('77777', 'nada.belghith@enis.u-sfax.tn') == (200, neutre) and statut('77777') == '')
check('motifs réels tracés dans le journal', int(sql("SELECT COUNT(*) FROM JournalConnexions WHERE Action='INSCRIPTION_REFUSEE' AND Matricule IN ('81001','99998','77777')")) == 3)

check('demande conforme : même réponse', demander('81001', 'demandeur1@s.com') == (200, neutre))
check('demande en attente : sans rôle (métier ERP inconnu), sans mot de passe, sans lien', statut('81001') == 'PENDING_APPROVAL|-|sans-mdp|sans-lien', statut('81001'))
check('nouvelle demande identique : pas de doublon', demander('81001', 'demandeur1@s.com')[0] == 200
      and sql("SELECT COUNT(*) FROM UtilisateursApp WHERE Matricule='81001'") == '1'
      and sql("SELECT COUNT(*) FROM JournalConnexions WHERE Matricule='81001' AND Action='INSCRIPTION_DOUBLON'") == '1')
check('demande en attente : connexion impossible (identifiants invalides)', login('81001', 'Nimporte1!')[0] == 401)
demander('81002', 'demandeur2@s.com')
check('rôle ERP connu : seulement proposé', statut('81002') == 'PENDING_APPROVAL|OPERATEUR|sans-mdp|sans-lien', statut('81002'))

st, r, _ = call('GET', '/admin/dashboard', tok=admin)
check('tableau de bord : 2 demandes à traiter', st == 200 and r['pendingApprovalAccounts'] == 2, (st, r))
st, r, _ = call('GET', '/admin/users?status=PENDING_APPROVAL', tok=admin)
check('file des demandes : les 2 demandes, la plus ancienne en tête', st == 200 and [x['matricule'] for x in r['items']] == ['81001', '81002'], (st, r))

op = login('55555')[1]['token']
st, r, _ = call('POST', '/admin/users/81001/approve', {'role': 'MAGASINIER'}, tok=op)
check("l'opérateur ne valide pas une demande (403)", st == 403, st)
st, r, _ = call('POST', '/admin/users/81001/approve', {'role': ''}, tok=admin)
check('valider sans rôle : 400', st == 400, (st, r))
st, r, _ = call('PUT', '/admin/users/81001/role', {'role': 'MAGASINIER'}, tok=admin)
check('changer le rôle d\'une demande : 400 (passer par Approuver)', st == 400, (st, r))
st, r, _ = call('POST', '/admin/users/81001/approve', {'role': 'MAGASINIER', 'comment': 'Renfort magasin'}, tok=admin)
check('demande approuvée : invitation avec le rôle choisi', st == 200 and r['account']['status'] == 'INVITED' and r['account']['role'] == 'MAGASINIER', (st, r))
check('décision tracée sur le compte (qui, quand, commentaire)', sql(
    "SELECT ReviewedBy+'|'+ReviewComment+'|'+CASE WHEN ReviewedAt IS NULL THEN 'sans-date' ELSE 'date' END FROM UtilisateursApp WHERE Matricule='81001'")
      == '66666|Renfort magasin|date')
check('décision tracée dans le journal', sql(
    "SELECT COUNT(*) FROM JournalConnexions WHERE Matricule='81001' AND Action='DEMANDE_APPROUVEE' AND Details LIKE '%MAGASINIER%66666%'") == '1')
check('lien à usage unique créé (empreinte seule en base)', statut('81001') == 'INVITED|MAGASINIER|sans-mdp|lien')
st, r, _ = call('POST', '/admin/users/81001/approve', {'role': 'OPERATEUR'}, tok=admin)
check('second traitement de la même demande : refusé (400), décision conservée', st == 400 and statut('81001').startswith('INVITED|MAGASINIER'), (st, r))

st, r, _ = call('POST', '/admin/users/81002/reject', {'reason': 'non'}, tok=admin)
check('refus sans motif suffisant : 400', st == 400, (st, r))
st, r, _ = call('POST', '/admin/users/81002/reject', {'reason': 'Pas de poste sur la ligne'}, tok=admin)
check('demande refusée avec motif', st == 200 and r['status'] == 'REJECTED' and r['reviewComment'] == 'Pas de poste sur la ligne', (st, r))
check('refus tracé', sql("SELECT COUNT(*) FROM JournalConnexions WHERE Matricule='81002' AND Action='DEMANDE_REFUSEE'") == '1')
check('demande refusée : une nouvelle inscription ne rouvre rien', demander('81002', 'demandeur2@s.com')[0] == 200 and statut('81002').startswith('REJECTED'))

# Le lien d'activation est envoyé par e-mail ; pour le test, un jeton connu remplace celui de la base.
JETON = 'jeton-e2e-81001-' + 'x' * 20
sql(f"UPDATE UtilisateursApp SET ActivationTokenHash='{hashlib.sha256(JETON.encode()).hexdigest().upper()}' WHERE Matricule='81001'")
st, r, _ = call('POST', '/Auth/setup-password', {'token': 'faux-jeton', 'nouveauMotDePasse': 'Passw0rd81!'})
check('lien inconnu : 401', st == 401, (st, r))
st, r, _ = call('POST', '/Auth/setup-password', {'token': JETON, 'nouveauMotDePasse': 'faible'})
check('mot de passe faible : refusé sans consommer le lien', 'critères' in msg(r) and statut('81001') == 'INVITED|MAGASINIER|sans-mdp|lien', (st, r))
st, r, _ = call('POST', '/Auth/setup-password', {'token': JETON, 'nouveauMotDePasse': 'Passw0rd81!'})
check('activation par le lien : compte actif, lien effacé', st == 200 and statut('81001') == 'ACTIVE|MAGASINIER|mdp|sans-lien', (st, statut('81001')))
st, r, _ = call('POST', '/Auth/setup-password', {'token': JETON, 'nouveauMotDePasse': 'Autre0rd81!'})
check('lien réutilisé : 401', st == 401, (st, r))
st, r, _ = login('81001', 'Passw0rd81!')
check('connexion avec le rôle accordé', st == 200 and r['roleApp'] == 'MAGASINIER', (st, r))

# Lien expiré
sql("UPDATE UtilisateursApp SET AccountStatus='INVITED', MotDePasseHash=NULL, ActivationTokenExpiresAt=DATEADD(HOUR,-1,SYSDATETIME()), "
    f"ActivationTokenHash='{hashlib.sha256(b'jeton-expire').hexdigest().upper()}' WHERE Matricule='81002'")
st, r, _ = call('POST', '/Auth/setup-password', {'token': 'jeton-expire', 'nouveauMotDePasse': 'Passw0rd82!'})
check('lien expiré : 401', st == 401, (st, r))

# ── Rôle pré-attribué : invitation directe à l'inscription ──
st, r, _ = call('PUT', '/admin/preassigned-roles/81003', {'role': 'OPERATEUR'}, tok=admin)
check('pré-attribution enregistrée', st == 200 and r['fullName'] == 'Employe Preattribue' and r['role'] == 'OPERATEUR', (st, r))
st, r, _ = call('PUT', '/admin/preassigned-roles/99997', {'role': 'OPERATEUR'}, tok=admin)
check('pré-attribution hors ERP : 400', st == 400, (st, r))
st, r, _ = call('PUT', '/admin/preassigned-roles/55555', {'role': 'MAGASINIER'}, tok=admin)
check('pré-attribution pour un compte existant : 400', st == 400, (st, r))
st, r, _ = call('GET', '/admin/preassigned-roles', tok=admin)
check('liste des pré-attributions', st == 200 and [x['matricule'] for x in r] == ['81003'], (st, r))
demander('81003', 'demandeur3@s.com')
check('inscription avec rôle pré-attribué : invitée directement', statut('81003') == 'INVITED|OPERATEUR|sans-mdp|lien', statut('81003'))
check('pré-attribution consommée', sql("SELECT COUNT(*) FROM PreassignedRole WHERE Matricule='81003'") == '0')
st, r, _ = call('DELETE', '/admin/preassigned-roles/81003', tok=admin)
check('suppression d\'une pré-attribution inexistante : 404', st == 404, (st, r))

# ── Machine à états en base : contraintes ──
erreur = sql("BEGIN TRY UPDATE UtilisateursApp SET AccountStatus='ACTIVE' WHERE Matricule='81003'; SELECT 'accepte' END TRY "
                              "BEGIN CATCH SELECT 'refuse' END CATCH")
check('base : un compte sans mot de passe ne peut pas être ACTIVE', erreur == 'refuse', erreur)
erreur = sql("BEGIN TRY UPDATE UtilisateursApp SET RoleApp=NULL WHERE Matricule='55555'; SELECT 'accepte' END TRY BEGIN CATCH SELECT 'refuse' END CATCH")
check('base : un compte actif ne peut pas perdre son rôle', erreur == 'refuse', erreur)

# ── Journal ──
st, r, _ = call('GET', '/admin/connection-log?matricule=55555&pageSize=100', tok=admin)
actions = {x['action'] for x in r['items']} if st == 200 else set()
check('journal du compte : connexions, rôle, désactivation', {'CONNEXION', 'ROLE_MODIFIE', 'COMPTE_DESACTIVE', 'COMPTE_REACTIVE', 'DECONNEXION'} <= actions, actions)

# ── Limitation de débit des points d'authentification (en dernier : bloque l'IP une minute) ──
codes = [login('00000')[0] for _ in range(45)]
check('trop de requêtes d\'authentification : 429', 429 in codes, codes[-5:])

print(f'\n{ok} OK / {ko} KO')
