SET QUOTED_IDENTIFIER ON;
-- Employés supplémentaires de l'annuaire pour les E2E : un second superviseur et un second administrateur
-- (tests de modifications concurrentes entre deux personnes du même rôle).
INSERT INTO dbo.AUTILIS (USR_0, INTUSR_0, ENAFLG_0, CODMET_0, ADDEML_0) VALUES
('44445', 'Superviseur Test 2', 2, 'SUPERVISEUR_QUALITE', 'superviseur2@s.com'),
('66667', 'Admin Test 2', 2, 'ADMIN', 'admin2@s.com');

-- Comptes de test déjà activés : identifiant = 00000000-0000-0000-0000-0000000<matricule>, tampon de sécurité connu,
-- mot de passe Passw0rdE2E! (empreinte BCrypt). Le rôle du compte est celui porté par les jetons des suites.
INSERT INTO dbo.UtilisateursApp (Id, Matricule, NomComplet, Email, MotDePasseHash, RoleApp, AccountStatus, SecurityStamp)
SELECT CAST('00000000-0000-0000-0000-0000000' + a.USR_0 AS UNIQUEIDENTIFIER), a.USR_0, a.INTUSR_0, a.ADDEML_0,
       '$2a$11$vZ/Jzy.0tuOJE8MdYiAO5.cZ2uEjKHeqMhDbNttX2ch6E1sr5o9Qq', a.CODMET_0, 'ACTIVE', '11111111-2222-3333-4444-555555555555'
FROM dbo.AUTILIS a
WHERE a.USR_0 IN ('22222', '33333', '44444', '44445', '55555', '66666', '66667');

-- Leurs rôles pré-attribués (script 04) sont donc sans objet.
DELETE FROM dbo.PreassignedRole WHERE Matricule IN ('22222', '33333', '44444', '55555', '66666');
