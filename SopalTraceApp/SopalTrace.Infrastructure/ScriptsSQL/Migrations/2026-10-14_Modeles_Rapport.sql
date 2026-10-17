-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Modèles de rapport et documents générés (PARTIE 17 de 01_Create_SopalTraceDB.sql pour une base neuve) :
--   1. ReportTemplate : modèle Excel d'un type de document (PLAN_ASS : plan de contrôle d'assemblage).
--   2. ReportTemplateVersion : versions du fichier (brouillon, active, retirée), contenu jamais modifié.
--   3. GeneratedDocument : PDF et Excel remplis, figés, avec la version du modèle et l'empreinte des données.
-- Le fichier de la première version est importé par l'API à son démarrage.
--
-- Suppose la migration 2026-10-13_Controles_Production.sql déjà appliquée.
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

IF OBJECT_ID('dbo.Exec_SessionPost') IS NULL
BEGIN
    RAISERROR('Appliquer d''abord la migration 2026-10-13_Controles_Production.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRANSACTION;

IF OBJECT_ID('dbo.ReportTemplate') IS NULL
    EXEC(N'CREATE TABLE dbo.ReportTemplate (
    Code             VARCHAR(30)   PRIMARY KEY,
    Name             NVARCHAR(150) NOT NULL,
    -- Type de document mis en forme par ce modèle.
    DocumentTypeCode VARCHAR(30)   NOT NULL REFERENCES dbo.TypeDocument(Code)
);');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ReportTemplate_DocumentType' AND object_id = OBJECT_ID('dbo.ReportTemplate'))
    EXEC(N'CREATE UNIQUE INDEX UX_ReportTemplate_DocumentType ON dbo.ReportTemplate(DocumentTypeCode);');

IF OBJECT_ID('dbo.ReportTemplateVersion') IS NULL
    EXEC(N'CREATE TABLE dbo.ReportTemplateVersion (
    Id                 UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ReportTemplateCode VARCHAR(30)    NOT NULL REFERENCES dbo.ReportTemplate(Code),
    VersionNumber      INT            NOT NULL CHECK (VersionNumber > 0),
    FileName           NVARCHAR(200)  NOT NULL,
    Content            VARBINARY(MAX) NOT NULL,
    ContentSha256      AS CONVERT(CHAR(64), HASHBYTES(''SHA2_256'', Content), 2) PERSISTED,
    Status             VARCHAR(20)    NOT NULL DEFAULT ''DRAFT''
        CHECK (Status IN (''DRAFT'', ''ACTIVE'', ''RETIRED'')),
    -- Avertissements de la vérification du fichier (JSON), acceptés à l''activation.
    ValidationWarnings NVARCHAR(MAX)  NULL,
    Comment            NVARCHAR(500)  NULL,
    -- NULL : modèle livré avec l''application.
    UploadedBy         VARCHAR(20)    NULL REFERENCES dbo.AUTILIS(USR_0),
    UploadedAt         DATETIME2(3)   NOT NULL DEFAULT SYSDATETIME(),
    ActivatedBy        VARCHAR(20)    NULL REFERENCES dbo.AUTILIS(USR_0),
    ActivatedAt        DATETIME2(3)   NULL,
    CONSTRAINT UQ_ReportTemplateVersion_Number UNIQUE (ReportTemplateCode, VersionNumber),
    -- Une version active ou retirée a forcément été activée.
    CONSTRAINT CK_ReportTemplateVersion_Activated CHECK (Status = ''DRAFT'' OR ActivatedAt IS NOT NULL)
);');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ReportTemplateVersion_OneActive' AND object_id = OBJECT_ID('dbo.ReportTemplateVersion'))
    EXEC(N'CREATE UNIQUE INDEX UX_ReportTemplateVersion_OneActive ON dbo.ReportTemplateVersion(ReportTemplateCode, Status) WHERE Status = ''ACTIVE'';');

IF OBJECT_ID('dbo.GeneratedDocument') IS NULL
    EXEC(N'CREATE TABLE dbo.GeneratedDocument (
    Id                      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    DocumentEnteteId        UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Entete(Id),
    ReportTemplateVersionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.ReportTemplateVersion(Id),
    -- Empreinte des données du document mises en forme : une même donnée n''est générée qu''une fois par version du modèle.
    SourceDataSha256        CHAR(64)       NOT NULL,
    FileName                NVARCHAR(200)  NOT NULL,
    PdfContent              VARBINARY(MAX) NOT NULL,
    PdfSha256               AS CONVERT(CHAR(64), HASHBYTES(''SHA2_256'', PdfContent), 2) PERSISTED,
    WorkbookContent         VARBINARY(MAX) NOT NULL,
    GeneratedAt             DATETIME2(3)   NOT NULL DEFAULT SYSDATETIME(),
    -- NULL : génération automatique (document créé ou modifié).
    RequestedBy             VARCHAR(20)    NULL REFERENCES dbo.AUTILIS(USR_0),
    CONSTRAINT UQ_GeneratedDocument_Source UNIQUE (DocumentEnteteId, ReportTemplateVersionId, SourceDataSha256)
);');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GeneratedDocument_Document' AND object_id = OBJECT_ID('dbo.GeneratedDocument'))
    EXEC(N'CREATE INDEX IX_GeneratedDocument_Document ON dbo.GeneratedDocument(DocumentEnteteId, GeneratedAt DESC);');

IF OBJECT_ID('dbo.TR_ReportTemplateVersion_Immutable') IS NULL
    EXEC(N'-- Le fichier d''une version ne change jamais ; une version n''est jamais supprimée et ne redevient pas brouillon.
CREATE TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted)
        THROW 50002, N''Une version de modèle de rapport ne peut pas être supprimée (ISO 9001).'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE i.ReportTemplateCode <> d.ReportTemplateCode OR i.VersionNumber <> d.VersionNumber
                  OR i.FileName <> d.FileName OR i.ContentSha256 <> d.ContentSha256
                  OR ISNULL(i.UploadedBy, '''') <> ISNULL(d.UploadedBy, '''') OR i.UploadedAt <> d.UploadedAt)
        THROW 50003, N''Le fichier d''''une version de modèle ne peut pas être modifié : envoyez une nouvelle version.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE i.Status = ''DRAFT'' AND d.Status <> ''DRAFT'')
        THROW 50003, N''Une version de modèle déjà activée ne redevient pas un brouillon.'', 1;
END;');

IF OBJECT_ID('dbo.TR_GeneratedDocument_Immutable') IS NULL
    EXEC(N'-- Un document généré est figé ; il est produit par une version activée du modèle de son type de document.
CREATE TRIGGER TR_GeneratedDocument_Immutable ON dbo.GeneratedDocument
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 50002, N''Un document généré ne peut être ni modifié ni supprimé (ISO 9001).'', 1;
END;');

IF OBJECT_ID('dbo.TR_GeneratedDocument_Template') IS NULL
    EXEC(N'CREATE TRIGGER TR_GeneratedDocument_Template ON dbo.GeneratedDocument
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               JOIN dbo.ReportTemplate t ON t.Code = v.ReportTemplateCode
               JOIN dbo.Document_Entete e ON e.Id = i.DocumentEnteteId
               WHERE v.Status = ''DRAFT'' OR t.DocumentTypeCode <> e.TypeDocumentCode)
        THROW 50003, N''Un document généré utilise une version activée du modèle de son type de document.'', 1;
END;');

IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplate WHERE Code = 'PLAN_ASS')
    INSERT INTO dbo.ReportTemplate (Code, Name, DocumentTypeCode)
    VALUES ('PLAN_ASS', N'Plan de contrôle d''assemblage', 'PLAN_ASS');

COMMIT TRANSACTION;
PRINT 'Migration 2026-10-14 appliquée : modèles de rapport et documents générés.';
GO

SET NOEXEC OFF;
GO
