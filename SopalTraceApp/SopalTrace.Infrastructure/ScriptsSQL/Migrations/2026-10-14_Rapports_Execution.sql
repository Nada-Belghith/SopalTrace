-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Documents générés depuis les documents d'exécution (réponses des opérateurs), déjà présents dans la PARTIE 17
-- de 01_Create_SopalTraceDB.sql pour une base neuve :
--   1. ReportTemplate : type de document qualité OU type de document d'exécution, et variante (ex. code machine).
--   2. GeneratedDocument : source document qualité OU document d'exécution (Exec_ControleDocumentStatut).
--   3. TR_GeneratedDocument_Template : le modèle correspond au type de sa source.
--   4. Modèles de la fiche d'échantillonnage, du résultat de contrôle au poste et des rapports de vérification
--      machine (banc d'essai par défaut, variante MAS pour les machines d'assemblage) et des résultats du contrôle
--      en cours de fabrication d'assemblage par poste, registre de traçabilité assemblage.
--
-- Suppose la migration 2026-10-14_Modeles_Rapport.sql déjà appliquée.
-- Rejouable : chaque étape vérifie si elle est déjà faite. Tout ou rien (transaction).
-- Faire une sauvegarde avant. Ensuite : redémarrer l'API (le code attend ce schéma).
-- =================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
USE SopalTraceDB;
GO

IF OBJECT_ID('dbo.GeneratedDocument') IS NULL
BEGIN
    RAISERROR('Appliquer d''abord la migration 2026-10-14_Modeles_Rapport.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRANSACTION;

-- ─────────────────────────── 1. ReportTemplate ───────────────────────────
IF COL_LENGTH('dbo.ReportTemplate', 'ExecutionDocumentTypeCode') IS NULL
BEGIN
    ALTER TABLE dbo.ReportTemplate ADD
        ExecutionDocumentTypeCode VARCHAR(50) NULL CONSTRAINT FK_ReportTemplate_ExecutionDocumentType REFERENCES dbo.ExecutionDocumentType(Code),
        VariantCode VARCHAR(30) NULL;
END

-- L'ancien index unique (un modèle par type de document) empêche de rendre la colonne facultative.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ReportTemplate_DocumentType' AND object_id = OBJECT_ID('dbo.ReportTemplate')
           AND has_filter = 0)
    DROP INDEX UX_ReportTemplate_DocumentType ON dbo.ReportTemplate;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ReportTemplate') AND name = 'DocumentTypeCode' AND is_nullable = 0)
    ALTER TABLE dbo.ReportTemplate ALTER COLUMN DocumentTypeCode VARCHAR(30) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ReportTemplate_DocumentType' AND object_id = OBJECT_ID('dbo.ReportTemplate'))
    EXEC(N'CREATE UNIQUE INDEX UX_ReportTemplate_DocumentType ON dbo.ReportTemplate(DocumentTypeCode, VariantCode) WHERE DocumentTypeCode IS NOT NULL;');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ReportTemplate_ExecutionType' AND object_id = OBJECT_ID('dbo.ReportTemplate'))
    EXEC(N'CREATE UNIQUE INDEX UX_ReportTemplate_ExecutionType ON dbo.ReportTemplate(ExecutionDocumentTypeCode, VariantCode) WHERE ExecutionDocumentTypeCode IS NOT NULL;');
IF OBJECT_ID('dbo.CK_ReportTemplate_Source') IS NULL
    EXEC(N'ALTER TABLE dbo.ReportTemplate ADD CONSTRAINT CK_ReportTemplate_Source CHECK (
        (DocumentTypeCode IS NOT NULL AND ExecutionDocumentTypeCode IS NULL) OR (DocumentTypeCode IS NULL AND ExecutionDocumentTypeCode IS NOT NULL));');

-- ─────────────────────────── 2. GeneratedDocument ───────────────────────────
IF COL_LENGTH('dbo.GeneratedDocument', 'ExecControleDocumentStatutId') IS NULL
    ALTER TABLE dbo.GeneratedDocument ADD ExecControleDocumentStatutId UNIQUEIDENTIFIER NULL
        CONSTRAINT FK_GeneratedDocument_ExecControleDocumentStatut REFERENCES dbo.Exec_ControleDocumentStatut(Id);

-- La contrainte unique et l'index sur DocumentEnteteId empêchent de rendre la colonne facultative : ils sont
-- remplacés par des index filtrés (une même donnée par version du modèle, pour chaque source).
IF OBJECT_ID('dbo.UQ_GeneratedDocument_Source') IS NOT NULL
    ALTER TABLE dbo.GeneratedDocument DROP CONSTRAINT UQ_GeneratedDocument_Source;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.GeneratedDocument') AND name = 'DocumentEnteteId' AND is_nullable = 0)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GeneratedDocument_Document' AND object_id = OBJECT_ID('dbo.GeneratedDocument'))
        DROP INDEX IX_GeneratedDocument_Document ON dbo.GeneratedDocument;
    ALTER TABLE dbo.GeneratedDocument ALTER COLUMN DocumentEnteteId UNIQUEIDENTIFIER NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_GeneratedDocument_Document' AND object_id = OBJECT_ID('dbo.GeneratedDocument'))
    EXEC(N'CREATE UNIQUE INDEX UX_GeneratedDocument_Document ON dbo.GeneratedDocument(DocumentEnteteId, ReportTemplateVersionId, SourceDataSha256) WHERE DocumentEnteteId IS NOT NULL;');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_GeneratedDocument_Execution' AND object_id = OBJECT_ID('dbo.GeneratedDocument'))
    EXEC(N'CREATE UNIQUE INDEX UX_GeneratedDocument_Execution ON dbo.GeneratedDocument(ExecControleDocumentStatutId, ReportTemplateVersionId, SourceDataSha256) WHERE ExecControleDocumentStatutId IS NOT NULL;');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GeneratedDocument_Document' AND object_id = OBJECT_ID('dbo.GeneratedDocument'))
    EXEC(N'CREATE INDEX IX_GeneratedDocument_Document ON dbo.GeneratedDocument(DocumentEnteteId, GeneratedAt DESC);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GeneratedDocument_Execution' AND object_id = OBJECT_ID('dbo.GeneratedDocument'))
    EXEC(N'CREATE INDEX IX_GeneratedDocument_Execution ON dbo.GeneratedDocument(ExecControleDocumentStatutId, GeneratedAt DESC);');
IF OBJECT_ID('dbo.CK_GeneratedDocument_Source') IS NULL
    EXEC(N'ALTER TABLE dbo.GeneratedDocument ADD CONSTRAINT CK_GeneratedDocument_Source CHECK (
        (DocumentEnteteId IS NOT NULL AND ExecControleDocumentStatutId IS NULL) OR (DocumentEnteteId IS NULL AND ExecControleDocumentStatutId IS NOT NULL));');

-- ─────────────────────────── 3. Modèle conforme à la source ───────────────────────────
EXEC(N'CREATE OR ALTER TRIGGER TR_GeneratedDocument_Template ON dbo.GeneratedDocument
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               JOIN dbo.ReportTemplate t ON t.Code = v.ReportTemplateCode
               LEFT JOIN dbo.Document_Entete e ON e.Id = i.DocumentEnteteId
               LEFT JOIN dbo.Exec_ControleDocumentStatut s ON s.Id = i.ExecControleDocumentStatutId
               WHERE v.Status = ''DRAFT''
                  OR (i.DocumentEnteteId IS NOT NULL AND ISNULL(t.DocumentTypeCode, '''') <> e.TypeDocumentCode)
                  OR (i.ExecControleDocumentStatutId IS NOT NULL AND ISNULL(t.ExecutionDocumentTypeCode, '''') <> s.TypeDocument))
        THROW 50003, N''Un document généré utilise une version activée du modèle de son type de document.'', 1;
END;');

-- ─────────────────────────── 4. Modèles des documents d'exécution ───────────────────────────
EXEC(N'
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplate WHERE Code = ''ECHANTILLONNAGE'')
    INSERT INTO dbo.ReportTemplate (Code, Name, ExecutionDocumentTypeCode) VALUES (''ECHANTILLONNAGE'', N''Fiche d''''échantillonnage'', ''ECHANTILLONNAGE'');
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplate WHERE Code = ''RESULTAT_CONTROLE_POSTE'')
    INSERT INTO dbo.ReportTemplate (Code, Name, ExecutionDocumentTypeCode) VALUES (''RESULTAT_CONTROLE_POSTE'', N''Résultat de contrôle au poste'', ''RESULTAT_CONTROLE_POSTE'');
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplate WHERE Code = ''VERIF_MACHINE'')
    INSERT INTO dbo.ReportTemplate (Code, Name, ExecutionDocumentTypeCode) VALUES (''VERIF_MACHINE'', N''Rapport de vérification machine (banc d''''essai)'', ''VERIF_MACHINE'');
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplate WHERE Code = ''VERIF_MACHINE_MAS'')
    INSERT INTO dbo.ReportTemplate (Code, Name, ExecutionDocumentTypeCode, VariantCode) VALUES (''VERIF_MACHINE_MAS'', N''Rapport de vérification machine d''''assemblage'', ''VERIF_MACHINE'', ''MAS'');
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplate WHERE Code = ''PLAN_ASS_RESULTATS'')
    INSERT INTO dbo.ReportTemplate (Code, Name, ExecutionDocumentTypeCode) VALUES (''PLAN_ASS_RESULTATS'', N''Résultats du contrôle en cours de fabrication d''''assemblage'', ''PLAN_ASS'');
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplate WHERE Code = ''PLAN_ASS_RESULTATS_PAS72'')
    INSERT INTO dbo.ReportTemplate (Code, Name, ExecutionDocumentTypeCode, VariantCode) VALUES (''PLAN_ASS_RESULTATS_PAS72'', N''Résultats du contrôle d''''assemblage du poste PAS72'', ''PLAN_ASS'', ''PAS72'');
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplate WHERE Code = ''REGISTRE_TRACA'')
    INSERT INTO dbo.ReportTemplate (Code, Name, ExecutionDocumentTypeCode) VALUES (''REGISTRE_TRACA'', N''Registre de traçabilité assemblage'', ''REGISTRE_TRACA'');');

COMMIT TRANSACTION;
PRINT 'Migration 2026-10-14 (rapports d''exécution) appliquée.';
GO

SET NOEXEC OFF;
GO
