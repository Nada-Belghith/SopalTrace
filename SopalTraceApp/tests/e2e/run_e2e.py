"""
Lance les suites E2E. Pour chaque groupe : base de test recréée (scripts SQL du dépôt + comptes de test),
API démarrée sur cette base, suites exécutées, API arrêtée. Code de sortie 1 si un contrôle échoue
ou si l'API a produit une erreur serveur.

Usage (depuis SopalTraceApp/, après « dotnet build ») :
    python tests/e2e/run_e2e.py                 # tous les groupes
    python tests/e2e/run_e2e.py e2e61 e2e62     # un groupe formé de ces suites

Variables : voir configuration.py, plus
    E2E_CONFIGURATION   Debug (défaut) ou Release : sortie de build de l'API utilisée
    E2E_CONNECTION      chaîne de connexion de l'API (défaut : LocalDB + E2E_DB)
    E2E_TMP             dossier des scripts préparés ; avec SQL Server dans Docker, un dossier monté au même chemin
"""
import os
import re
import subprocess
import sys
import tempfile
import time
import urllib.request
from pathlib import Path

ICI = Path(__file__).resolve().parent
sys.path.insert(0, str(ICI))
from configuration import BASE, DB, SECRET, SQLCMD, sql  # noqa: E402

RACINE = ICI.parent.parent
SCRIPTS_SQL = RACINE / 'SopalTrace.Infrastructure' / 'ScriptsSQL'
# Production : 01 + 02 seulement. Les tests ajoutent la copie des tables Sage (03) et la démonstration (04).
SCRIPTS = ['01_Create_SopalTraceDB.sql', '02_ReferenceData_SopalTraceDB.sql',
           '03_SageMirrorData_SopalTraceDB.sql', '04_DemoData_SopalTraceDB.sql']
CONFIGURATION = os.environ.get('E2E_CONFIGURATION', 'Debug')
PORT = re.search(r':(\d+)/', BASE).group(1)

# Chaque groupe part d'une base neuve : les suites d'un même groupe partagent les OF de démonstration.
GROUPES = [
    ['e2e41', 'e2e44', 'e2e45', 'e2e55'],
    ['e2e43'], ['e2e42'], ['e2e40'], ['e2e52'], ['e2e54'], ['e2e60'], ['e2e61'], ['e2e62'], ['e2e63'], ['e2e64'], ['e2e65'], ['e2e66'],
]


def recreer_base():
    sql(f"IF DB_ID('{DB}') IS NOT NULL BEGIN ALTER DATABASE [{DB}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DB}]; END",
        base='master')
    racine_tmp = os.environ.get('E2E_TMP')
    if racine_tmp:
        os.makedirs(racine_tmp, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=racine_tmp) as dossier:
        for nom in SCRIPTS + ['comptes_e2e.sql']:
            source = (SCRIPTS_SQL / nom) if nom in SCRIPTS else (ICI / nom)
            texte = source.read_text(encoding='utf-8-sig')
            texte = re.sub(r'\bSopalTraceDB\b', DB, texte)
            cible = Path(dossier) / nom
            cible.write_text(texte, encoding='utf-8')
            base = 'master' if nom == SCRIPTS[0] else DB
            r = subprocess.run([*SQLCMD, '-d', base, '-b', '-f', '65001', '-i', str(cible)], capture_output=True, text=True)
            if r.returncode != 0:
                raise SystemExit(f'Échec de {nom} :\n{(r.stdout + r.stderr)[-2000:]}')


def demarrer_api(journal):
    dll = RACINE / 'SopalTrace.Api' / 'bin' / CONFIGURATION / 'net8.0' / 'SopalTrace.Api.dll'
    if not dll.exists():
        raise SystemExit(f'{dll} introuvable : lancer « dotnet build -c {CONFIGURATION} » avant.')
    env = dict(os.environ,
               ConnectionStrings__SopalTraceConnection=os.environ.get(
                   'E2E_CONNECTION', f'Server=(localdb)\\MSSQLLocalDB;Database={DB};Trusted_Connection=True;TrustServerCertificate=True'),
               Jwt__Secret=SECRET.decode(),
               Auth__RequetesParMinute='40',
               ASPNETCORE_ENVIRONMENT='Development',
               ASPNETCORE_URLS=f'http://127.0.0.1:{PORT}')
    api = subprocess.Popen(['dotnet', str(dll)], cwd=dll.parent, env=env, stdout=journal, stderr=subprocess.STDOUT)
    for _ in range(60):
        try:
            with urllib.request.urlopen(BASE + '/health', timeout=2) as r:
                if r.status == 200:
                    return api
        except Exception:
            time.sleep(2)
    api.kill()
    raise SystemExit("L'API n'a pas démarré (voir le journal).")


def executer(groupe):
    recreer_base()
    with tempfile.NamedTemporaryFile('w+', suffix='.log', delete=False, encoding='utf-8') as journal:
        api = demarrer_api(journal)
        echecs = 0
        try:
            for suite in groupe:
                r = subprocess.run([sys.executable, str(ICI / f'{suite}.py')], cwd=ICI, capture_output=True, text=True,
                                   encoding='utf-8', errors='replace', env=dict(os.environ, PYTHONIOENCODING='utf-8'))
                ko = [l for l in r.stdout.splitlines() if l.startswith('KO')]
                bilans = re.findall(r'(\d+) OK / (\d+) KO', r.stdout)
                print(f"{suite}: {' | '.join(f'{a} OK / {b} KO' for a, b in bilans) or 'aucun bilan'}")
                for l in ko:
                    print('   ' + l[:300])
                if r.returncode != 0 or not bilans:
                    print('   ' + (r.stderr.strip().splitlines() or ['sortie anormale'])[-1])
                    echecs += 1
                echecs += sum(int(b) for _, b in bilans)
        finally:
            api.terminate()
            try:
                api.wait(timeout=15)
            except subprocess.TimeoutExpired:
                api.kill()
        journal.seek(0)
        erreurs = len(re.findall(r'\[ERR\]| 500 ', journal.read()))
    print(f'   erreurs serveur : {erreurs}')
    return echecs + erreurs


def main():
    groupes = [sys.argv[1:]] if len(sys.argv) > 1 else GROUPES
    total = sum(executer(g) for g in groupes)
    sql(f"IF DB_ID('{DB}') IS NOT NULL BEGIN ALTER DATABASE [{DB}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DB}]; END",
        base='master')
    print('\nE2E : ' + ('tout est vert' if total == 0 else f'{total} échec(s)'))
    sys.exit(1 if total else 0)


if __name__ == '__main__':
    main()
