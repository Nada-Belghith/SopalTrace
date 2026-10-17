-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Modèles de rapport par version de formulaire, figés à la création des documents (déjà présents dans
-- 01_Create_SopalTraceDB.sql pour une base neuve) :
--   1. Séquence CreationOrderSequence et colonne CreationOrder (attribuée par la base, jamais modifiée) sur
--      Document_Entete et Exec_ControleDocumentStatut.
--   2. ReportTemplateVersion : statuts DRAFT / PUBLISHED (ACTIVE et RETIRED deviennent PUBLISHED),
--      ActivatedBy / ActivatedAt renommées PublishedBy / PublishedAt.
--   3. ReportTemplateAssignment : historique des affectations de modèles (par défaut, structure d'un formulaire,
--      révision).
--   4. Reprise : les documents existants et les versions déjà activées sont numérotés dans l'ordre de leurs dates
--      (CreeLe des documents, date d'exécution des documents d'exécution, date d'activation des versions) ; chaque
--      version déjà activée devient une affectation « par défaut » à sa date d'activation.
--
-- Suppose les migrations 2026-10-14_Modeles_Rapport.sql et 2026-10-14_Rapports_Execution.sql déjà appliquées.
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

IF COL_LENGTH('dbo.ReportTemplate', 'ExecutionDocumentTypeCode') IS NULL
BEGIN
    RAISERROR('Appliquer d''abord les migrations 2026-10-14_Modeles_Rapport.sql puis 2026-10-14_Rapports_Execution.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

-- Toute erreur, y compris dans une instruction dynamique, annule l'ensemble.
BEGIN TRY
BEGIN TRANSACTION;

-- ─────────────────────────── 1. Ordre de création ───────────────────────────
IF OBJECT_ID('dbo.CreationOrderSequence') IS NULL
    EXEC(N'CREATE SEQUENCE dbo.CreationOrderSequence AS BIGINT START WITH 1 INCREMENT BY 1;');
IF COL_LENGTH('dbo.Document_Entete', 'CreationOrder') IS NULL
    ALTER TABLE dbo.Document_Entete ADD CreationOrder BIGINT NULL;
IF COL_LENGTH('dbo.Exec_ControleDocumentStatut', 'CreationOrder') IS NULL
    ALTER TABLE dbo.Exec_ControleDocumentStatut ADD CreationOrder BIGINT NULL;

-- ─────────────────────────── 2. Versions de modèle ───────────────────────────
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ReportTemplateVersion_OneActive' AND object_id = OBJECT_ID('dbo.ReportTemplateVersion'))
    DROP INDEX UX_ReportTemplateVersion_OneActive ON dbo.ReportTemplateVersion;
IF OBJECT_ID('dbo.CK_ReportTemplateVersion_Activated') IS NOT NULL
    ALTER TABLE dbo.ReportTemplateVersion DROP CONSTRAINT CK_ReportTemplateVersion_Activated;
DECLARE @ancienControleStatut SYSNAME = (SELECT TOP 1 name FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.ReportTemplateVersion') AND definition LIKE '%RETIRED%');
IF @ancienControleStatut IS NOT NULL
    EXEC(N'ALTER TABLE dbo.ReportTemplateVersion DROP CONSTRAINT ' + @ancienControleStatut);
IF COL_LENGTH('dbo.ReportTemplateVersion', 'ActivatedAt') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.ReportTemplateVersion.ActivatedAt', 'PublishedAt', 'COLUMN';
    EXEC sp_rename 'dbo.ReportTemplateVersion.ActivatedBy', 'PublishedBy', 'COLUMN';
END
IF EXISTS (SELECT 1 FROM dbo.ReportTemplateVersion WHERE Status IN ('ACTIVE', 'RETIRED'))
    EXEC(N'UPDATE dbo.ReportTemplateVersion SET Status = ''PUBLISHED'' WHERE Status IN (''ACTIVE'', ''RETIRED'');');
IF OBJECT_ID('dbo.CK_ReportTemplateVersion_Status') IS NULL
    EXEC(N'ALTER TABLE dbo.ReportTemplateVersion ADD CONSTRAINT CK_ReportTemplateVersion_Status CHECK (Status IN (''DRAFT'', ''PUBLISHED''));');
IF OBJECT_ID('dbo.CK_ReportTemplateVersion_Published') IS NULL
    EXEC(N'ALTER TABLE dbo.ReportTemplateVersion ADD CONSTRAINT CK_ReportTemplateVersion_Published CHECK (
        (Status = ''DRAFT'' AND PublishedAt IS NULL) OR (Status = ''PUBLISHED'' AND PublishedAt IS NOT NULL));');

-- ─────────────────────────── 3 et 4. Affectations et reprise ───────────────────────────
IF OBJECT_ID('dbo.ReportTemplateAssignment') IS NULL
BEGIN
    EXEC(N'CREATE TABLE dbo.ReportTemplateAssignment (
        Id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
        ReportTemplateVersionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.ReportTemplateVersion(Id),
        Kind                    VARCHAR(20)      NOT NULL CONSTRAINT CK_ReportTemplateAssignment_Kind
            CHECK (Kind IN (''DEFAULT'', ''FORM_STRUCTURE'', ''FORM_REVISION'')),
        FormId                  UNIQUEIDENTIFIER NULL REFERENCES dbo.Ref_Formulaire(Id),
        CreationOrder           BIGINT           NOT NULL,
        Comment                 NVARCHAR(500)    NULL,
        AssignedBy              VARCHAR(20)      NULL REFERENCES dbo.AUTILIS(USR_0),
        AssignedAt              DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT CK_ReportTemplateAssignment_Form CHECK (
            (Kind = ''DEFAULT'' AND FormId IS NULL) OR (Kind <> ''DEFAULT'' AND FormId IS NOT NULL))
    );');

    -- Une seule numérotation, dans l'ordre des dates, pour les documents, les documents d'exécution et les versions activées.
    EXEC(N'
    DECLARE @chronologie TABLE (Nature CHAR(1) NOT NULL, Id UNIQUEIDENTIFIER NOT NULL, Position BIGINT NOT NULL);
    INSERT INTO @chronologie (Nature, Id, Position)
    SELECT Nature, Id, ROW_NUMBER() OVER (ORDER BY Instant, Nature, Id)
    FROM (
        SELECT ''A'' AS Nature, Id, PublishedAt AS Instant FROM dbo.ReportTemplateVersion WHERE Status = ''PUBLISHED''
        UNION ALL
        SELECT ''D'', Id, CreeLe FROM dbo.Document_Entete
        UNION ALL
        SELECT ''S'', s.Id, COALESCE(s.DateExecution, s.DateTermine, o.DateDebut)
        FROM dbo.Exec_ControleDocumentStatut s JOIN dbo.Exec_ControleOF o ON o.Id = s.ExecControleOfId
    ) evenements;

    UPDATE d SET CreationOrder = c.Position
    FROM dbo.Document_Entete d JOIN @chronologie c ON c.Nature = ''D'' AND c.Id = d.Id;
    UPDATE s SET CreationOrder = c.Position
    FROM dbo.Exec_ControleDocumentStatut s JOIN @chronologie c ON c.Nature = ''S'' AND c.Id = s.Id;
    INSERT INTO dbo.ReportTemplateAssignment (ReportTemplateVersionId, Kind, CreationOrder, Comment, AssignedBy, AssignedAt)
    SELECT v.Id, ''DEFAULT'', c.Position, N''Version activée avant les modèles par formulaire'', v.PublishedBy, v.PublishedAt
    FROM @chronologie c JOIN dbo.ReportTemplateVersion v ON c.Nature = ''A'' AND v.Id = c.Id;

    DECLARE @redemarrage NVARCHAR(200) = N''ALTER SEQUENCE dbo.CreationOrderSequence RESTART WITH ''
        + CAST(ISNULL((SELECT MAX(Position) FROM @chronologie), 0) + 1 AS NVARCHAR(20));
    EXEC(@redemarrage);');

    EXEC(N'CREATE UNIQUE INDEX UX_ReportTemplateAssignment_CreationOrder ON dbo.ReportTemplateAssignment(CreationOrder);');
    EXEC(N'ALTER TABLE dbo.ReportTemplateAssignment ADD CONSTRAINT DF_ReportTemplateAssignment_CreationOrder
        DEFAULT (NEXT VALUE FOR dbo.CreationOrderSequence) FOR CreationOrder;');
    EXEC(N'CREATE INDEX IX_ReportTemplateAssignment_Form ON dbo.ReportTemplateAssignment(FormId) WHERE FormId IS NOT NULL;');
    EXEC(N'CREATE INDEX IX_ReportTemplateAssignment_Version ON dbo.ReportTemplateAssignment(ReportTemplateVersionId);');
END

-- Lignes créées entre deux exécutions partielles (sécurité) : numérotées à la suite.
EXEC(N'UPDATE dbo.Document_Entete SET CreationOrder = NEXT VALUE FOR dbo.CreationOrderSequence WHERE CreationOrder IS NULL;');
EXEC(N'UPDATE dbo.Exec_ControleDocumentStatut SET CreationOrder = NEXT VALUE FOR dbo.CreationOrderSequence WHERE CreationOrder IS NULL;');

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Document_Entete') AND name = 'CreationOrder' AND is_nullable = 1)
    EXEC(N'ALTER TABLE dbo.Document_Entete ALTER COLUMN CreationOrder BIGINT NOT NULL;');
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Exec_ControleDocumentStatut') AND name = 'CreationOrder' AND is_nullable = 1)
    EXEC(N'ALTER TABLE dbo.Exec_ControleDocumentStatut ALTER COLUMN CreationOrder BIGINT NOT NULL;');
IF OBJECT_ID('dbo.DF_DocumentEntete_CreationOrder') IS NULL
    EXEC(N'ALTER TABLE dbo.Document_Entete ADD CONSTRAINT DF_DocumentEntete_CreationOrder DEFAULT (NEXT VALUE FOR dbo.CreationOrderSequence) FOR CreationOrder;');
IF OBJECT_ID('dbo.DF_ExecControleDocumentStatut_CreationOrder') IS NULL
    EXEC(N'ALTER TABLE dbo.Exec_ControleDocumentStatut ADD CONSTRAINT DF_ExecControleDocumentStatut_CreationOrder DEFAULT (NEXT VALUE FOR dbo.CreationOrderSequence) FOR CreationOrder;');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Document_Entete_CreationOrder' AND object_id = OBJECT_ID('dbo.Document_Entete'))
    EXEC(N'CREATE UNIQUE INDEX UX_Document_Entete_CreationOrder ON dbo.Document_Entete(CreationOrder);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecControleDocumentStatut_CreationOrder' AND object_id = OBJECT_ID('dbo.Exec_ControleDocumentStatut'))
    EXEC(N'CREATE UNIQUE INDEX UX_ExecControleDocumentStatut_CreationOrder ON dbo.Exec_ControleDocumentStatut(CreationOrder);');

-- ─────────────────────────── Triggers ───────────────────────────
EXEC(N'CREATE OR ALTER TRIGGER TR_DocumentEntete_CreationOrder ON dbo.Document_Entete
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(CreationOrder) AND EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id WHERE i.CreationOrder <> d.CreationOrder)
        THROW 50003, N''L''''ordre de création d''''un document ne change jamais.'', 1;
END;');

EXEC(N'CREATE OR ALTER TRIGGER TR_ExecControleDocumentStatut_CreationOrder ON dbo.Exec_ControleDocumentStatut
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(CreationOrder) AND EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id WHERE i.CreationOrder <> d.CreationOrder)
        THROW 50003, N''L''''ordre de création d''''un document d''''exécution ne change jamais.'', 1;
END;');

EXEC(N'CREATE OR ALTER TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
        THROW 50002, N''Une version de modèle de rapport ne peut pas être supprimée (ISO 9001).'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE i.ReportTemplateCode <> d.ReportTemplateCode OR i.VersionNumber <> d.VersionNumber
                  OR i.FileName <> d.FileName OR i.ContentSha256 <> d.ContentSha256
                  OR ISNULL(i.UploadedBy, '''') <> ISNULL(d.UploadedBy, '''') OR i.UploadedAt <> d.UploadedAt)
        THROW 50003, N''Le fichier d''''une version de modèle ne peut pas être modifié : envoyez une nouvelle version.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE d.Status = ''PUBLISHED'' AND (i.Status <> ''PUBLISHED'' OR i.PublishedAt <> d.PublishedAt
                                                 OR ISNULL(i.PublishedBy, '''') <> ISNULL(d.PublishedBy, '''')))
        THROW 50003, N''Une version de modèle publiée ne change plus.'', 1;
END;');

EXEC(N'CREATE OR ALTER TRIGGER TR_ReportTemplateAssignment_Rules ON dbo.ReportTemplateAssignment
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 50002, N''L''''historique des affectations de modèles ne peut être ni modifié ni supprimé (ISO 9001).'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               WHERE v.Status <> ''PUBLISHED'')
        THROW 50003, N''Seule une version de modèle publiée peut être affectée.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               JOIN dbo.ReportTemplate t ON t.Code = v.ReportTemplateCode
               JOIN dbo.Ref_Formulaire f ON f.Id = i.FormId
               WHERE t.DocumentTypeCode IS NULL OR f.Statut = ''ARCHIVE'')
        THROW 50003, N''Un modèle n''''est affecté qu''''à une version de formulaire non archivée, et seulement un modèle de document qualité.'', 1;
END;');

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
               WHERE v.Status <> ''PUBLISHED''
                  OR (i.DocumentEnteteId IS NOT NULL AND ISNULL(t.DocumentTypeCode, '''') <> e.TypeDocumentCode)
                  OR (i.ExecControleDocumentStatutId IS NOT NULL AND ISNULL(t.ExecutionDocumentTypeCode, '''') <> s.TypeDocument))
        THROW 50003, N''Un document généré utilise une version publiée du modèle de son type de document.'', 1;
END;');

COMMIT TRANSACTION;
PRINT 'Migration 2026-10-14 (modèles par formulaire) appliquée.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

SET NOEXEC OFF;
GO
