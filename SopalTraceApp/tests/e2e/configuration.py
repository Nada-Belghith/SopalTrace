"""
Configuration commune des suites E2E, lue dans les variables d'environnement.
Valeurs par défaut : poste Windows avec SQL Server LocalDB.

  E2E_API_URL     adresse de l'API testée              (http://127.0.0.1:5197/api)
  E2E_DB          base de test, recréée par run_e2e.py (SopalTraceDB_E2E) — jamais la base de développement
  E2E_SQLCMD      commande sqlcmd sans la base         (sqlcmd -I -S (localdb)\\MSSQLLocalDB)
  E2E_JWT_SECRET  clé JWT de l'API lancée pour les tests
"""
import os
import shlex
import subprocess

BASE = os.environ.get('E2E_API_URL', 'http://127.0.0.1:5197/api')
DB = os.environ.get('E2E_DB', 'SopalTraceDB_E2E')
SECRET = os.environ.get('E2E_JWT_SECRET', 'SecretDeTestLocalUniquementPourE2E_2026_xyz').encode()
SQLCMD = shlex.split(os.environ.get('E2E_SQLCMD', r'sqlcmd -I -S (localdb)\MSSQLLocalDB'), posix=(os.name != 'nt'))

if DB.lower() in ('sopaltracedb',):
    raise SystemExit("E2E_DB ne doit jamais désigner la base de développement : les suites la recréent.")


def sql(requete, base=None):
    """Exécute une requête et renvoie la sortie brute (sans en-têtes)."""
    return subprocess.run([*SQLCMD, '-d', base or DB, '-h', '-1', '-W', '-Q', 'SET NOCOUNT ON;' + requete],
                          capture_output=True, text=True).stdout.strip()
