# Tests E2E (recette automatique)

Chaque groupe de suites recrée une base de test à partir des scripts SQL du dépôt, démarre l'API dessus,
exécute les suites (appels HTTP réels + vérifications SQL), puis arrête l'API. Échec si un contrôle échoue
ou si l'API journalise une erreur serveur.

## En local (Windows, SQL Server LocalDB)

```bash
cd SopalTraceApp
dotnet build
python tests/e2e/run_e2e.py              # tous les groupes
python tests/e2e/run_e2e.py e2e62        # une suite
```

La base utilisée est `SopalTraceDB_E2E` (variable `E2E_DB`) : jamais la base de développement `SopalTraceDB`.

## En CI

Le job `e2e` de `.github/workflows/ci.yml` démarre un SQL Server Docker éphémère et fixe les variables
(`E2E_SQLCMD`, `E2E_CONNECTION`, `E2E_TMP`, `E2E_CONFIGURATION=Release`). Voir `configuration.py`.

## Comptes de test

`comptes_e2e.sql` crée les comptes 22222, 33333, 44444, 55555 et 66666 (mot de passe `Passw0rdE2E!`)
avec un tampon de sécurité connu, ce qui permet aux suites de fabriquer des jetons valides.
