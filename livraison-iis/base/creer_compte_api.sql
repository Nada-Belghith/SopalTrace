-- Compte SQL utilisé par l'API (à exécuter dans SSMS après la restauration de SopalTraceDB.bak).
-- Remplacer le mot de passe ; il doit être identique à celui de appsettings.Production.json.
-- Note : la base contient déjà un RÔLE nommé sopaltrace_app, d'où l'utilisateur sopaltrace_app_user.

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'sopaltrace_app')
    CREATE LOGIN sopaltrace_app WITH PASSWORD = 'A_REMPLACER', CHECK_POLICY = OFF;
GO

USE SopalTraceDB;
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'sopaltrace_app_user')
    ALTER USER sopaltrace_app_user WITH LOGIN = sopaltrace_app;
ELSE
    CREATE USER sopaltrace_app_user FOR LOGIN sopaltrace_app;
GO

ALTER ROLE sopaltrace_app ADD MEMBER sopaltrace_app_user;
ALTER ROLE db_owner ADD MEMBER sopaltrace_app_user;
GO

PRINT 'Compte sopaltrace_app prêt.';
