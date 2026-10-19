import base64, hashlib, hmac, json, time, urllib.request, urllib.error
from configuration import BASE, DB, SECRET, sql as _sql
def b64(d): return base64.urlsafe_b64encode(d).rstrip(b'=').decode()
def jwt(mat, role):
    h=b64(json.dumps({'alg':'HS256','typ':'JWT'}).encode()); now=int(time.time())
    p=b64(json.dumps({'sub': '00000000-0000-0000-0000-0000000' + mat, 'stamp': '11111111-2222-3333-4444-555555555555','matricule':mat,'http://schemas.microsoft.com/ws/2008/06/identity/claims/role':role,'iss':'SopalTraceApi','aud':'SopalTraceVueJs','iat':now,'nbf':now,'exp':now+3600}).encode())
    return f'{h}.{p}.'+b64(hmac.new(SECRET,f'{h}.{p}'.encode(),hashlib.sha256).digest())
def call(tok, method, path, body=None):
    req=urllib.request.Request(BASE+path, method=method, data=json.dumps(body).encode() if body is not None else None, headers={'Authorization':'Bearer '+tok,'Content-Type':'application/json'})
    try:
        with urllib.request.urlopen(req) as r: t=r.read().decode(); return r.status, (json.loads(t) if t else None)
    except urllib.error.HTTPError as e:
        t=e.read().decode(); return e.code, (json.loads(t) if t.strip().startswith(('{','[')) else t[:200])
ADMIN=jwt('44444','SUPERVISEUR_QUALITE'); OP=jwt('55555','OPERATEUR')
# Clôture de nettoyage : seul un administrateur peut forcer la fin d'un OF d'assemblage (contrôles de fin : e2e64).
ADMIN_SUPERVISION = jwt('66666', 'ADMIN')
ok=ko=0
def check(l,c,i=''):
    global ok,ko
    if c: ok+=1; print('OK ',l)
    else: ko+=1; print('KO ',l,i)
st,r=call(ADMIN,'GET','/referentiels/fabrication'); txt=json.dumps(r)
check('referentiels fabrication 200', st==200, (st,str(r)[:200])); check('plus de defautheque', 'efautheque' not in txt)
st,r=call(ADMIN,'GET','/referentiels/verif-machine'); txt=json.dumps(r)
check('referentiels verif machine 200', st==200, (st,str(r)[:200])); check('pieces sans familleDesc', 'familleDesc' not in txt)
for path in ['/hub/modeles','/hub/plans','/hub/structures']:
    st,r=call(ADMIN,'GET',path); check(f'{path} 200', st==200, (st,str(r)[:200]))
# Champ d'application sans formulaire (famille RBGFM sur PAS71) : le code du nouveau formulaire est indiqué, jamais fabriqué.
st,r=call(ADMIN,'POST','/Document',{'typeDocumentCode':'PLAN_ASS','nom':'Plan test PAS71','familleProduitFiniCode':'RBGFM','posteCode':'PAS71','sections':[]})
check('sans formulaire pour ce champ d\'application ni code indiqué : refus clair', st==400 and 'Aucun formulaire' in str(r), (st,str(r)[:300]))
st,r=call(ADMIN,'POST','/Document',{'typeDocumentCode':'PLAN_ASS','nom':'Plan test PAS71','familleProduitFiniCode':'RBGFM','posteCode':'PAS71','refFormulaireCodeReference':'FE-ASS-TEST-40','sections':[]})
check('creation plan assemblage', st in (200,201), (st,str(r)[:300]))
doc_id = (r or {}).get('id') if isinstance(r,dict) else None
if isinstance(r,dict) and not doc_id: doc_id=r.get('data',{}).get('id') if isinstance(r.get('data'),dict) else None
if doc_id:
    st,r=call(ADMIN,'GET',f'/Document/{doc_id}'); d=r.get('data',r) if isinstance(r,dict) else {}
    # Le type de robinet n'est pas stocké sur le document : il se déduit de la famille.
    check('typeRobinetCode déduit de la famille RBGFM = MAN', st==200 and d.get('typeRobinetCode')=='MAN', (st,str(r)[:300]))
st,r=call(OP,'GET','/operateur/ofs/assemblage-statut'); check('ofs assemblage 200', st==200, (st,str(r)[:200]))
st,r=call(OP,'POST','/operateur/of/start-assemblage',{'numeroOf':'OF2607ASS01','posteCodes':['PAS71'],'equipe':'E1'}); check('demarrage assemblage', st==200, (st,r))
if st==200:
    eid=r['id']; st,r=call(OP,'GET',f'/operateur/of/{eid}/assemblage-documents'); check('documents assemblage 200', st==200, (st,str(r)[:200]))
    st,r=call(ADMIN_SUPERVISION, 'POST', f'/operateur/of/{eid}/close'); check('cloture', st==204, (st,r))
print(f'\n{ok} OK / {ko} KO')
import subprocess
def sql(q):
    return _sql(q)
st,r=call(ADMIN,'POST','/Document',{'typeDocumentCode':'CTRL_POSTE','nom':'Controle poste PAS72','posteCode':'PAS72','familleProduitFiniCode':'RBGFM','refFormulaireCodeReference':'FE-RC-TEST-P3','colonneDefs':[{'key':'col_obs','label':'Observation','type':'TEXT'}],'sections':[]})
check('creation controle poste avec nouveau formulaire', st in (200,201), (st,str(r)[:300]))
role=sql("SELECT Role FROM Ref_Formulaire WHERE CodeReference='FE-RC-TEST-P3'")
check('formulaire cree avec le role RESULTAT_CONTROLE_POSTE (plus UNKNOWN)', role=='RESULTAT_CONTROLE_POSTE', role)
check('audit trace la creation du document', sql("SELECT COUNT(*) FROM AuditLog WHERE TableName='Document_Entete' AND Action='INSERT'")!='0')
print(f'\n{ok} OK / {ko} KO')
import json as _j
cfg=_j.dumps({'equipes':[{'nom':'Equipe Matin','debut':6,'fin':14},{'nom':'Equipe Soir','debut':14,'fin':22}],'customCols':[]})
st,r=call(ADMIN,'POST','/Document',{'typeDocumentCode':'CTRL_POSTE','nom':'Controle poste PAS78','posteCode':'PAS78','familleProduitFiniCode':'RBGFM','refFormulaireCodeReference':'FE-RC-EQUIPES','configurationColonnesJson':cfg,'sections':[]})
check('controle poste avec equipes', st in (200,201), (st,str(r)[:300]))
eq=sql("SELECT NomEquipe+'='+TeamCode FROM Ref_Formulaire_Equipe e JOIN Ref_Formulaire f ON f.Id=e.FormulaireId WHERE f.CodeReference='FE-RC-EQUIPES' AND e.Actif=1 ORDER BY e.OrdreAffiche")
check('equipes du formulaire -> E1, E2 par position', eq.split()==['Equipe=E1','Matin=E1'] or 'Matin=E1' in eq and 'Soir=E2' in eq, eq)
st,r=call(OP,'POST','/operateur/of/start-assemblage',{'numeroOf':'OF2607ASS02','posteCodes':['PAS71'],'equipe':'E2'})
check('demarrage avec equipe E2', st==200, (st,r))
if st==200: check('session enregistree en E2', sql(f"SELECT Equipe FROM Exec_ControleOf_Session WHERE ExecControleOfId='{r['id']}'")=='E2')
st,r=call(OP,'POST','/operateur/of/start-assemblage',{'numeroOf':'OF2607ASS02','posteCodes':['PAS71'],'equipe':'MATIN'})
check('equipe invalide refusee (400)', st==400, (st,r))
print(f'\n{ok} OK / {ko} KO')
