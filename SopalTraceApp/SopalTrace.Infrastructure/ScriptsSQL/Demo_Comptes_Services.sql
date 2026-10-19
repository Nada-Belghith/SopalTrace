-- =================================================================================
-- SopalTraceDB — Comptes de DÉMONSTRATION des services qui déposent les documents externes (jamais en production)
--
-- Quatre employés de test dans la copie de l'annuaire Sage (AUTILIS) et leur compte SopalTrace, déjà actif :
--   88881  Achats Test                     SERVICE_ACHATS      bons de commande fournisseur
--   88882  Commercial Test                 SERVICE_COMMERCIAL  bons de livraison, bons de commande client
--   88883  Qualite Reception Test          SERVICE_QUALITE     rapports de contrôle, certificats, rapports du produit fini
--   88884  Developpement Industriel Test   SERVICE_DI          plans de définition du produit fini
-- Connexion : matricule + mot de passe Passw0rdE2E! (le changer ensuite depuis « Mot de passe oublié »).
--
-- À exécuter APRÈS les scripts 01 à 04 (rôles SERVICE_* créés par 02). Rejouable : un employé ou un
-- compte déjà présent est laissé tel quel.
-- =================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
USE SopalTraceDB;
GO

IF EXISTS (SELECT 1 FROM (VALUES ('SERVICE_ACHATS'), ('SERVICE_COMMERCIAL'), ('SERVICE_QUALITE'), ('SERVICE_DI')) r(Code)
           WHERE NOT EXISTS (SELECT 1 FROM dbo.AppRole a WHERE a.Code = r.Code))
BEGIN
    RAISERROR('Exécuter d''abord les scripts 01 et 02 (rôles des services).', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRANSACTION;

DECLARE @employes TABLE (Matricule VARCHAR(20), Nom VARCHAR(100), Role VARCHAR(50), Email VARCHAR(150));
INSERT INTO @employes VALUES
('88881', 'Achats Test', 'SERVICE_ACHATS', 'achats@s.com'),
('88882', 'Commercial Test', 'SERVICE_COMMERCIAL', 'commercial@s.com'),
('88883', 'Qualite Reception Test', 'SERVICE_QUALITE', 'qualite.reception@s.com'),
('88884', 'Developpement Industriel Test', 'SERVICE_DI', 'di@s.com');

-- Annuaire Sage (copie) : employé actif (ENAFLG_0 = 2) avec son métier et son e-mail.
INSERT INTO dbo.AUTILIS (USR_0, INTUSR_0, ENAFLG_0, CODMET_0, ADDEML_0)
SELECT e.Matricule, e.Nom, 2, e.Role, e.Email FROM @employes e
WHERE NOT EXISTS (SELECT 1 FROM dbo.AUTILIS a WHERE a.USR_0 = e.Matricule);

INSERT INTO dbo.ATEXTRA (CODFIC_0, ZONE_0, IDENT1_0, LANGUE_0, TEXTE_0)
SELECT 'AUTILIS', 'INTUSR', e.Matricule, 'FRA', e.Nom FROM @employes e
WHERE NOT EXISTS (SELECT 1 FROM dbo.ATEXTRA x WHERE x.CODFIC_0 = 'AUTILIS' AND x.ZONE_0 = 'INTUSR' AND x.IDENT1_0 = e.Matricule AND x.LANGUE_0 = 'FRA');

-- Compte SopalTrace actif avec son rôle (empreinte BCrypt de Passw0rdE2E!).
INSERT INTO dbo.UtilisateursApp (Matricule, NomComplet, Email, MotDePasseHash, RoleApp, AccountStatus, ReviewedBy, ReviewedAt, ReviewComment)
SELECT e.Matricule, e.Nom, e.Email, '$2a$11$vZ/Jzy.0tuOJE8MdYiAO5.cZ2uEjKHeqMhDbNttX2ch6E1sr5o9Qq', e.Role, 'ACTIVE',
       'SYSTEM', SYSDATETIME(), 'Compte de démonstration (script Demo_Comptes_Services.sql)'
FROM @employes e
WHERE NOT EXISTS (SELECT 1 FROM dbo.UtilisateursApp u WHERE u.Matricule = e.Matricule)
  AND NOT EXISTS (SELECT 1 FROM dbo.UtilisateursApp u WHERE u.Email = e.Email);

-- Leur rôle pré-attribué (données de démonstration) est sans objet : le compte existe.
DELETE FROM dbo.PreassignedRole WHERE Matricule IN (SELECT Matricule FROM @employes);

COMMIT TRANSACTION;

SELECT u.Matricule, u.NomComplet, u.RoleApp, u.AccountStatus FROM dbo.UtilisateursApp u
WHERE u.Matricule IN ('88881', '88882', '88883', '88884') ORDER BY u.Matricule;
PRINT 'Comptes de démonstration des services prêts : matricule + Passw0rdE2E!';
GO

SET NOEXEC OFF;
GO
