-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Rôles des formulaires en table, type de robinet repris de la famille (déjà présent dans 01 et 02 pour une base neuve).
--   1. FormRole : un rôle par type de document, avec son libellé, son ordre d'affichage et les critères permis de son
--      champ d'application. Un nouveau type de document s'ajoute par une ligne (plus de liste figée dans une contrainte).
--   2. Ref_Formulaire.Role et ReportTemplate.FormRole : la contrainte CHECK est remplacée par une clé étrangère.
--   3. Un formulaire avec famille porte le type de robinet de sa famille (repris pour les formulaires existants).
--   4. Déclencheur TR_RefFormulaire_Scope : champ d'application conforme au rôle ; nature et type de robinet seulement
--      sans famille. La migration s'arrête en nommant les formulaires à corriger.
--
-- Suppose la migration 2026-10-15_Identite_Formulaires.sql déjà appliquée.
-- Rejouable : chaque étape vérifie si elle est déjà faite. Tout ou rien (transaction).
-- Faire une sauvegarde avant. Ensuite : redémarrer l'API.
-- =================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
USE SopalTraceDB;
GO

IF OBJECT_ID('dbo.FormCodeChange') IS NULL
BEGIN
    RAISERROR('Appliquer d''abord la migration 2026-10-15_Identite_Formulaires.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRY
BEGIN TRANSACTION;

-- ─────────────────────────── 1. Rôles des formulaires ───────────────────────────
IF OBJECT_ID('dbo.FormRole') IS NULL
    EXEC(N'CREATE TABLE dbo.FormRole (
    Code               VARCHAR(50)   NOT NULL PRIMARY KEY,
    Label              NVARCHAR(100) NOT NULL,
    DisplayOrder       INT           NOT NULL DEFAULT 0,
    ScopeByFamily      BIT           NOT NULL DEFAULT 0,
    ScopeByWorkstation BIT           NOT NULL DEFAULT 0,
    ScopeByMachine     BIT           NOT NULL DEFAULT 0,
    ScopeByNature      BIT           NOT NULL DEFAULT 0,
    ScopeByValveType   BIT           NOT NULL DEFAULT 0,
    Active             BIT           NOT NULL DEFAULT 1
);');

EXEC(N'INSERT INTO dbo.FormRole (Code, Label, DisplayOrder, ScopeByFamily, ScopeByWorkstation, ScopeByMachine, ScopeByNature, ScopeByValveType)
SELECT v.* FROM (VALUES
    (''EN_COURS_DE_ASSEMBLAGE'', N''Plans d''''assemblage'', 10, 1, 1, 0, 1, 0),
    (''PRODUIT_FINI'', N''Plans produit fini'', 20, 1, 0, 0, 0, 0),
    (''RESULTAT_CONTROLE_POSTE'', N''Contrôles au poste'', 30, 1, 1, 0, 0, 0),
    (''RESULTAT_CONTROLE_ASSEMBLAGE'', N''Résultats du contrôle en cours d''''assemblage'', 40, 1, 1, 0, 0, 1),
    (''VERIF_MACHINE'', N''Vérifications machine'', 50, 0, 0, 1, 0, 0),
    (''ECHANTILLONNAGE'', N''Plan d''''échantillonnage'', 60, 0, 0, 0, 0, 0),
    (''RESULTAT_CONTROLE_CF'', N''Résultats du contrôle en cours de fabrication'', 70, 0, 0, 0, 0, 0),
    (''EN_COURS_DE_FABRICATION'', N''Modèles de fabrication'', 80, 0, 0, 0, 0, 0)
) v (Code, Label, DisplayOrder, ScopeByFamily, ScopeByWorkstation, ScopeByMachine, ScopeByNature, ScopeByValveType)
WHERE NOT EXISTS (SELECT 1 FROM dbo.FormRole r WHERE r.Code = v.Code);');

-- ─────────────────────────── 2. Contraintes CHECK remplacées par des clés étrangères ───────────────────────────
DECLARE @contrainte SYSNAME, @sql NVARCHAR(MAX);
DECLARE contraintes CURSOR LOCAL FAST_FORWARD FOR
    SELECT QUOTENAME(OBJECT_NAME(c.parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(c.name)
    FROM sys.check_constraints c
    JOIN sys.columns col ON col.object_id = c.parent_object_id AND col.column_id = c.parent_column_id
    WHERE (c.parent_object_id = OBJECT_ID('dbo.Ref_Formulaire') AND col.name = 'Role')
       OR (c.parent_object_id = OBJECT_ID('dbo.ReportTemplate') AND col.name = 'FormRole');
OPEN contraintes;
FETCH NEXT FROM contraintes INTO @contrainte;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = N'ALTER TABLE dbo.' + @contrainte;
    EXEC(@sql);
    FETCH NEXT FROM contraintes INTO @contrainte;
END
CLOSE contraintes; DEALLOCATE contraintes;

DECLARE @cas NVARCHAR(MAX) = (
    SELECT STRING_AGG(CONVERT(NVARCHAR(MAX), CONCAT(f.CodeReference, '.', f.Version, ' (', f.Role, ')')), N', ')
    FROM dbo.Ref_Formulaire f WHERE f.Role IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.FormRole r WHERE r.Code = f.Role));
IF @cas IS NOT NULL
BEGIN
    DECLARE @message NVARCHAR(2048) = LEFT(N'Formulaires dont le rôle est inconnu (' + @cas + N') : ajoutez le rôle à dbo.FormRole puis rejouez la migration.', 2048);
    THROW 50003, @message, 1;
END
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RefFormulaire_FormRole')
    EXEC(N'ALTER TABLE dbo.Ref_Formulaire ADD CONSTRAINT FK_RefFormulaire_FormRole FOREIGN KEY (Role) REFERENCES dbo.FormRole(Code);');
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ReportTemplate_FormRole')
    EXEC(N'ALTER TABLE dbo.ReportTemplate ADD CONSTRAINT FK_ReportTemplate_FormRole FOREIGN KEY (FormRole) REFERENCES dbo.FormRole(Code);');

-- ─────────────────────────── 3. Type de robinet repris de la famille ───────────────────────────
SET @cas = (
    SELECT STRING_AGG(CONVERT(NVARCHAR(MAX), CONCAT(f.CodeReference, '.', f.Version, ' (famille ', f.FamilleProduitFiniCode, ' : ', fa.TypeRobinetCode,
                      ', formulaire : ', f.TypeRobinetCode, ')')), N', ')
    FROM dbo.Ref_Formulaire f JOIN dbo.FamilleProduitFini fa ON fa.Code = f.FamilleProduitFiniCode
    WHERE f.TypeRobinetCode IS NOT NULL AND f.TypeRobinetCode <> fa.TypeRobinetCode);
IF @cas IS NOT NULL
BEGIN
    DECLARE @message2 NVARCHAR(2048) = LEFT(N'Formulaires dont le type de robinet diffère de celui de leur famille (' + @cas
        + N') : corrigez-les puis rejouez la migration.', 2048);
    THROW 50003, @message2, 1;
END;
-- Donnée déduite de la famille, pas un changement d'identité : la protection des formulaires utilisés est suspendue le
-- temps de la reprise.
DISABLE TRIGGER TR_RefFormulaire_Identity ON dbo.Ref_Formulaire;
UPDATE f SET TypeRobinetCode = fa.TypeRobinetCode
FROM dbo.Ref_Formulaire f JOIN dbo.FamilleProduitFini fa ON fa.Code = f.FamilleProduitFiniCode
WHERE f.TypeRobinetCode IS NULL;
ENABLE TRIGGER TR_RefFormulaire_Identity ON dbo.Ref_Formulaire;

-- ─────────────────────────── 4. Champ d'application conforme au rôle ───────────────────────────
SET @cas = (
    SELECT STRING_AGG(CONVERT(NVARCHAR(MAX), CONCAT(f.CodeReference, '.', f.Version, ' (', f.Role, ')')), N', ')
    FROM dbo.Ref_Formulaire f JOIN dbo.FormRole r ON r.Code = f.Role
    WHERE (f.FamilleProduitFiniCode IS NOT NULL AND r.ScopeByFamily = 0)
       OR (f.PosteCode IS NOT NULL AND r.ScopeByWorkstation = 0)
       OR (f.MachineCode IS NOT NULL AND r.ScopeByMachine = 0)
       OR (f.NatureArticleCode IS NOT NULL AND (r.ScopeByNature = 0 OR f.FamilleProduitFiniCode IS NOT NULL))
       OR (f.TypeRobinetCode IS NOT NULL AND f.FamilleProduitFiniCode IS NULL AND r.ScopeByValveType = 0));
IF @cas IS NOT NULL
BEGIN
    DECLARE @message3 NVARCHAR(2048) = LEFT(N'Formulaires dont le champ d''application utilise un critère que leur rôle ne prévoit pas (' + @cas
        + N') : corrigez-les puis rejouez la migration.', 2048);
    THROW 50003, @message3, 1;
END
IF OBJECT_ID('dbo.TR_RefFormulaire_Scope') IS NULL
    EXEC(N'CREATE TRIGGER TR_RefFormulaire_Scope ON dbo.Ref_Formulaire
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.FamilleProduitFini f ON f.Code = i.FamilleProduitFiniCode
               WHERE ISNULL(i.TypeRobinetCode, '''') <> f.TypeRobinetCode)
        THROW 50003, N''Le type de robinet d''''un formulaire avec famille est celui de sa famille.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.FormRole r ON r.Code = i.Role
               WHERE (i.FamilleProduitFiniCode IS NOT NULL AND r.ScopeByFamily = 0)
                  OR (i.PosteCode IS NOT NULL AND r.ScopeByWorkstation = 0)
                  OR (i.MachineCode IS NOT NULL AND r.ScopeByMachine = 0)
                  OR (i.NatureArticleCode IS NOT NULL AND (r.ScopeByNature = 0 OR i.FamilleProduitFiniCode IS NOT NULL))
                  OR (i.TypeRobinetCode IS NOT NULL AND i.FamilleProduitFiniCode IS NULL AND r.ScopeByValveType = 0))
        THROW 50003, N''Ce champ d''''application utilise un critère que le rôle du formulaire ne prévoit pas.'', 1;
END;');

COMMIT TRANSACTION;
PRINT 'Migration 2026-10-15 (rôles des formulaires, type de robinet repris de la famille) appliquée.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

SET NOEXEC OFF;
GO
