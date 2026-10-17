-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Fiches de réponse : chaque fiche remplie par les opérateurs a son formulaire (code + version) et son modèle dédié
-- (déjà présent dans 01 et 02 pour une base neuve).
--   1. Résultats du contrôle en cours de fabrication : formulaires désignés par famille (donc type de robinet) et par
--      poste s'il existe ; la fiche reçoit son formulaire à sa création (ses lignes viennent du plan suivi par l'OF).
--   2. Registre de traçabilité : nouveau type REGISTRE_TRACABILITE (fiche de réponse) et son formulaire FE0129. Son
--      modèle par défaut devient le modèle dédié de ce formulaire (même fichier, même historique) ; les fiches de
--      registre déjà remplies reçoivent ce formulaire et gardent exactement le même modèle.
--   3. Règle en base : une fiche ne reçoit qu'un formulaire de son type.
--   4. Nom normalisé des modèles : « désignation du formulaire — CODE.version.numéro du modèle ».
--
-- Suppose la migration 2026-10-16_Natures_Et_Modeles_Dedies.sql déjà appliquée.
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

IF COL_LENGTH('dbo.ReportTemplateVersion', 'FormGroupId') IS NULL
BEGIN
    RAISERROR('Appliquer d''abord la migration 2026-10-16_Natures_Et_Modeles_Dedies.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRY
BEGIN TRANSACTION;

-- ─────────────────────────── 1. Résultats du contrôle en cours de fabrication ───────────────────────────
UPDATE dbo.FormRole SET ScopeByFamily = 1, ScopeByWorkstation = 1, ScopeByValveType = 1
WHERE Code = 'RESULTAT_CONTROLE_CF' AND (ScopeByFamily = 0 OR ScopeByWorkstation = 0 OR ScopeByValveType = 0);

-- ─────────────────────────── 3. Règle : une fiche reçoit un formulaire de son type ───────────────────────────
EXEC(N'CREATE OR ALTER TRIGGER TR_ExecControleDocumentStatut_Form ON dbo.Exec_ControleDocumentStatut
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN dbo.Ref_Formulaire f ON f.Id = i.FormId
               WHERE i.FormId IS NOT NULL
                 AND NOT ((i.TypeDocument = ''PLAN_ASS'' AND f.Role = ''RESULTAT_CONTROLE_ASSEMBLAGE'')
                       OR (i.TypeDocument = ''RESULTAT_CONTROLE_CF'' AND f.Role = ''RESULTAT_CONTROLE_CF'')
                       OR (i.TypeDocument = ''REGISTRE_TRACA'' AND f.Role = ''REGISTRE_TRACABILITE'')))
        THROW 50003, N''Une fiche ne reçoit qu''''un formulaire de son type (résultats d''''assemblage, résultats en cours de fabrication, registre).'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE d.FormId IS NOT NULL AND (i.FormId IS NULL OR i.FormId <> d.FormId))
        THROW 50003, N''Le formulaire d''''un document d''''exécution ne change jamais.'', 1;
END;');

-- ─────────────────────────── 2. Registre de traçabilité ───────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.FormRole WHERE Code = 'REGISTRE_TRACABILITE')
    INSERT INTO dbo.FormRole (Code, Label, DisplayOrder, Nature, ScopeByFamily, ScopeByWorkstation, ScopeByMachine, ScopeByNature, ScopeByValveType)
    VALUES ('REGISTRE_TRACABILITE', N'Registre de traçabilité', 90, 'RESPONSE_SHEET', 0, 0, 0, 0, 0);

-- Formulaire du registre : celui en cours s'il existe, sinon FE0129 (version 0), en cours dès que des fiches existent.
DECLARE @registre UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Ref_Formulaire WHERE Role = 'REGISTRE_TRACABILITE' AND Statut <> 'ARCHIVE');
IF @registre IS NULL
BEGIN
    DECLARE @code VARCHAR(30) = CASE WHEN EXISTS (SELECT 1 FROM dbo.Ref_Formulaire WHERE CodeReference = 'FE0129') THEN 'FE0129-REGISTRE' ELSE 'FE0129' END;
    SET @registre = NEWID();
    INSERT INTO dbo.Ref_Formulaire (Id, CodeReference, Designation, Version, Statut, CreeLe, Role)
    VALUES (@registre, @code, 'Registre de traçabilité assemblage', 0,
            CASE WHEN EXISTS (SELECT 1 FROM dbo.Exec_ControleDocumentStatut WHERE TypeDocument = 'REGISTRE_TRACA') THEN 'ACTIF' ELSE 'BROUILLON' END,
            SYSDATETIME(), 'REGISTRE_TRACABILITE');
END
DECLARE @groupe UNIQUEIDENTIFIER = (SELECT VersionGroupId FROM dbo.Ref_Formulaire WHERE Id = @registre);
DECLARE @codeRegistre VARCHAR(30) = (SELECT CodeReference FROM dbo.Ref_Formulaire WHERE Id = @registre);

IF EXISTS (SELECT 1 FROM dbo.ReportTemplate WHERE Code = 'REGISTRE_TRACA' AND FormRole IS NULL)
BEGIN
    -- Modèle par défaut → modèle dédié du formulaire : même fichier, même ordre de l'historique (les fiches gardent
    -- exactement le même modèle).
    DISABLE TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion;
    DISABLE TRIGGER TR_ReportTemplateAssignment_Rules ON dbo.ReportTemplateAssignment;
    UPDATE dbo.ReportTemplate SET FormRole = 'REGISTRE_TRACABILITE' WHERE Code = 'REGISTRE_TRACA';
    UPDATE v SET FormGroupId = @groupe, Name = LEFT(t.Name + N' — ' + @codeRegistre, 200)
    FROM dbo.ReportTemplateVersion v JOIN dbo.ReportTemplate t ON t.Code = v.ReportTemplateCode
    WHERE v.ReportTemplateCode = 'REGISTRE_TRACA' AND v.FormGroupId IS NULL;
    UPDATE a SET Kind = 'FORM_STRUCTURE', FormId = @registre
    FROM dbo.ReportTemplateAssignment a JOIN dbo.ReportTemplateVersion v ON v.Id = a.ReportTemplateVersionId
    WHERE v.ReportTemplateCode = 'REGISTRE_TRACA' AND a.Kind = 'DEFAULT';
    ENABLE TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion;
    ENABLE TRIGGER TR_ReportTemplateAssignment_Rules ON dbo.ReportTemplateAssignment;
END

-- Fiches de registre déjà remplies : elles reçoivent le formulaire du registre.
UPDATE dbo.Exec_ControleDocumentStatut SET FormId = @registre
WHERE TypeDocument = 'REGISTRE_TRACA' AND FormId IS NULL;

IF EXISTS (SELECT 1 FROM dbo.ReportTemplateAssignment a JOIN dbo.ReportTemplateVersion v ON v.Id = a.ReportTemplateVersionId
           LEFT JOIN dbo.Ref_Formulaire f ON f.Id = a.FormId
           WHERE v.ReportTemplateCode = 'REGISTRE_TRACA' AND (a.Kind = 'DEFAULT' OR f.VersionGroupId <> v.FormGroupId))
    THROW 50003, N'Reprise incomplète : un modèle du registre n''est pas dédié à son formulaire.', 1;

-- ─────────────────────────── 4. Nom normalisé des modèles ───────────────────────────
-- « Désignation du formulaire — CODE.version.numéro du modèle », d'après la ligne de formulaire où le modèle a été
-- rattaché la première fois (sinon la ligne la plus récente du formulaire). Seul le nom change.
DISABLE TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion;
UPDATE v SET Name = LEFT(COALESCE(NULLIF(LTRIM(RTRIM(f.Designation)), ''), t.Name) + N' — ' + f.CodeReference
                         + '.' + CAST(f.Version AS VARCHAR(10)) + '.' + CAST(v.VersionNumber AS VARCHAR(10)), 200)
FROM dbo.ReportTemplateVersion v
JOIN dbo.ReportTemplate t ON t.Code = v.ReportTemplateCode
CROSS APPLY (SELECT TOP 1 l.Designation, l.CodeReference, l.Version
             FROM dbo.Ref_Formulaire l
             LEFT JOIN dbo.ReportTemplateAssignment a ON a.FormId = l.Id AND a.ReportTemplateVersionId = v.Id
             WHERE l.VersionGroupId = v.FormGroupId
             ORDER BY CASE WHEN a.Id IS NULL THEN 1 ELSE 0 END, a.CreationOrder, l.CreationOrder DESC) f
WHERE v.FormGroupId IS NOT NULL;
ENABLE TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion;

COMMIT TRANSACTION;
PRINT 'Migration 2026-10-17 (fiches de réponse : résultats en cours de fabrication, registre de traçabilité) appliquée.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

SET NOEXEC OFF;
GO
