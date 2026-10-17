-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Nature des types de formulaires et modèle Excel dédié à chaque formulaire (déjà présents dans 01 et 02 pour une base
-- neuve).
--   1. FormRole.Nature : DOCUMENT (contenu défini par le superviseur qualité) ou RESPONSE_SHEET (fiche de réponse remplie
--      par les opérateurs : résultats du contrôle en cours d'assemblage et de fabrication).
--   2. Modèle dédié : chaque version de modèle appartient à un seul formulaire (FormGroupId = VersionGroupId de ses
--      lignes), porte un nom propre à ce formulaire (Name) et est numérotée dans ce formulaire. Une version partagée
--      aujourd'hui par plusieurs formulaires est copiée à l'identique (même fichier, même empreinte) pour chacun ; ses
--      rattachements suivent la copie de leur formulaire. Les documents gardent donc exactement le même modèle ; les
--      documents générés déjà conservés ne changent pas.
--   3. Règles en base : une version n'est rattachée qu'au formulaire auquel elle est dédiée.
--   4. ReportTemplateRequest : demandes de modèle (ce que l'administrateur doit rattacher, et pourquoi).
--
-- Suppose la migration 2026-10-15_Roles_Et_Type_Robinet.sql déjà appliquée.
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

IF OBJECT_ID('dbo.FormRole') IS NULL
BEGIN
    RAISERROR('Appliquer d''abord la migration 2026-10-15_Roles_Et_Type_Robinet.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRY
BEGIN TRANSACTION;

-- ─────────────────────────── 1. Nature des types de formulaires ───────────────────────────
IF COL_LENGTH('dbo.FormRole', 'Nature') IS NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.FormRole ADD Nature VARCHAR(20) NOT NULL DEFAULT ''DOCUMENT''
        CONSTRAINT CK_FormRole_Nature CHECK (Nature IN (''DOCUMENT'', ''RESPONSE_SHEET''));');
    EXEC(N'UPDATE dbo.FormRole SET Nature = ''RESPONSE_SHEET'' WHERE Code IN (''RESULTAT_CONTROLE_ASSEMBLAGE'', ''RESULTAT_CONTROLE_CF'');');
END

-- ─────────────────────────── 2. Modèle dédié à chaque formulaire ───────────────────────────
IF COL_LENGTH('dbo.ReportTemplateVersion', 'FormGroupId') IS NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.ReportTemplateVersion ADD FormGroupId UNIQUEIDENTIFIER NULL, Name NVARCHAR(200) NULL;');

    -- Reprise de l'existant : le contenu ne change pas, seuls le formulaire propriétaire, le nom et le numéro sont posés.
    DISABLE TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion;
    DISABLE TRIGGER TR_ReportTemplateAssignment_Rules ON dbo.ReportTemplateAssignment;
    IF OBJECT_ID('dbo.UQ_ReportTemplateVersion_Number', 'UQ') IS NOT NULL
        ALTER TABLE dbo.ReportTemplateVersion DROP CONSTRAINT UQ_ReportTemplateVersion_Number;

    EXEC(N'
    -- Formulaires qui utilisent chaque version, dans l''ordre de leur premier rattachement : le premier garde la version,
    -- chacun des suivants reçoit sa copie.
    SELECT v.Id AS VersionId, f.VersionGroupId AS GroupId,
           ROW_NUMBER() OVER (PARTITION BY v.Id ORDER BY MIN(a.CreationOrder)) AS Rang,
           NEWID() AS CopieId
    INTO #Proprietaires
    FROM dbo.ReportTemplateAssignment a
    JOIN dbo.ReportTemplateVersion v ON v.Id = a.ReportTemplateVersionId
    JOIN dbo.Ref_Formulaire f ON f.Id = a.FormId
    GROUP BY v.Id, f.VersionGroupId;

    UPDATE v SET FormGroupId = p.GroupId
    FROM dbo.ReportTemplateVersion v JOIN #Proprietaires p ON p.VersionId = v.Id AND p.Rang = 1;

    INSERT INTO dbo.ReportTemplateVersion (Id, ReportTemplateCode, FormGroupId, VersionNumber, FileName, Content, Status,
        ValidationWarnings, Comment, UploadedBy, UploadedAt, PublishedBy, PublishedAt)
    SELECT p.CopieId, v.ReportTemplateCode, p.GroupId, v.VersionNumber, v.FileName, v.Content, v.Status,
        v.ValidationWarnings, v.Comment, v.UploadedBy, v.UploadedAt, v.PublishedBy, v.PublishedAt
    FROM #Proprietaires p JOIN dbo.ReportTemplateVersion v ON v.Id = p.VersionId
    WHERE p.Rang > 1;

    UPDATE a SET ReportTemplateVersionId = p.CopieId
    FROM dbo.ReportTemplateAssignment a
    JOIN dbo.Ref_Formulaire f ON f.Id = a.FormId
    JOIN #Proprietaires p ON p.VersionId = a.ReportTemplateVersionId AND p.GroupId = f.VersionGroupId AND p.Rang > 1;

    -- Numéro dans le formulaire, dans l''ordre d''envoi.
    WITH n AS (
        SELECT VersionNumber, ROW_NUMBER() OVER (PARTITION BY FormGroupId ORDER BY UploadedAt, VersionNumber, ReportTemplateCode, Id) AS Numero
        FROM dbo.ReportTemplateVersion WHERE FormGroupId IS NOT NULL)
    UPDATE n SET VersionNumber = Numero;

    -- Nom propre au formulaire (code de sa ligne la plus récente) ; modèle par défaut : nom du type.
    UPDATE v SET Name = LEFT(t.Name + COALESCE(N'' — '' + (SELECT TOP 1 f.CodeReference FROM dbo.Ref_Formulaire f
                                                        WHERE f.VersionGroupId = v.FormGroupId ORDER BY f.CreationOrder DESC), N''''), 200)
    FROM dbo.ReportTemplateVersion v JOIN dbo.ReportTemplate t ON t.Code = v.ReportTemplateCode;

    ALTER TABLE dbo.ReportTemplateVersion ALTER COLUMN Name NVARCHAR(200) NOT NULL;
    ');

    EXEC(N'CREATE UNIQUE INDEX UX_ReportTemplateVersion_FormNumber ON dbo.ReportTemplateVersion(FormGroupId, VersionNumber) WHERE FormGroupId IS NOT NULL;');
    EXEC(N'CREATE UNIQUE INDEX UX_ReportTemplateVersion_DefaultNumber ON dbo.ReportTemplateVersion(ReportTemplateCode, VersionNumber) WHERE FormGroupId IS NULL;');

    ENABLE TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion;
    ENABLE TRIGGER TR_ReportTemplateAssignment_Rules ON dbo.ReportTemplateAssignment;
END

-- ─────────────────────────── 3. Règles en base ───────────────────────────
EXEC(N'CREATE OR ALTER TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
        THROW 50002, N''Une version de modèle de rapport ne peut pas être supprimée (ISO 9001).'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE i.ReportTemplateCode <> d.ReportTemplateCode OR i.VersionNumber <> d.VersionNumber
                  OR ISNULL(i.FormGroupId, ''00000000-0000-0000-0000-000000000000'') <> ISNULL(d.FormGroupId, ''00000000-0000-0000-0000-000000000000'')
                  OR i.Name <> d.Name
                  OR i.FileName <> d.FileName OR i.ContentSha256 <> d.ContentSha256
                  OR ISNULL(i.UploadedBy, '''') <> ISNULL(d.UploadedBy, '''') OR i.UploadedAt <> d.UploadedAt)
        THROW 50003, N''Le fichier d''''une version de modèle ne peut pas être modifié : envoyez une nouvelle version.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE d.Status = ''PUBLISHED'' AND (i.Status <> ''PUBLISHED'' OR i.PublishedAt <> d.PublishedAt
                                                 OR ISNULL(i.PublishedBy, '''') <> ISNULL(d.PublishedBy, '''')))
        THROW 50003, N''Une version de modèle publiée ne change plus.'', 1;
END;');

EXEC(N'CREATE OR ALTER TRIGGER TR_ReportTemplateVersion_Owner ON dbo.ReportTemplateVersion
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.ReportTemplate t ON t.Code = i.ReportTemplateCode
               WHERE (t.FormRole IS NULL AND i.FormGroupId IS NOT NULL)
                  OR (i.FormGroupId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ref_Formulaire f
                                                               WHERE f.VersionGroupId = i.FormGroupId AND f.Role = t.FormRole)))
        THROW 50003, N''Un modèle appartient à un formulaire dont son type met en forme les documents (même rôle).'', 1;
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
               WHERE f.Statut = ''ARCHIVE'' OR t.FormRole IS NULL OR f.Role IS NULL OR f.Role <> t.FormRole)
        THROW 50003, N''Un modèle n''''est rattaché qu''''à une version de formulaire non archivée dont il met en forme les documents (même rôle).'', 1;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               JOIN dbo.ReportTemplate t ON t.Code = v.ReportTemplateCode
               WHERE i.Kind = ''DEFAULT'' AND t.FormRole IS NOT NULL)
        THROW 50003, N''Un modèle des documents d''''un formulaire se rattache au formulaire, pas comme modèle par défaut.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               LEFT JOIN dbo.Ref_Formulaire f ON f.Id = i.FormId
               WHERE (i.Kind = ''DEFAULT'' AND v.FormGroupId IS NOT NULL)
                  OR (i.Kind <> ''DEFAULT'' AND ISNULL(v.FormGroupId, ''00000000-0000-0000-0000-000000000000'') <> f.VersionGroupId))
        THROW 50003, N''Un modèle n''''est rattaché qu''''au formulaire auquel il est dédié.'', 1;
END;');

-- Contrôle de la reprise : chaque rattachement vise une version dédiée à son formulaire.
DECLARE @ecarts INT;
EXEC sp_executesql N'SELECT @n = COUNT(*) FROM dbo.ReportTemplateAssignment a
    JOIN dbo.ReportTemplateVersion v ON v.Id = a.ReportTemplateVersionId
    LEFT JOIN dbo.Ref_Formulaire f ON f.Id = a.FormId
    WHERE (a.FormId IS NULL AND v.FormGroupId IS NOT NULL)
       OR (a.FormId IS NOT NULL AND ISNULL(v.FormGroupId, ''00000000-0000-0000-0000-000000000000'') <> f.VersionGroupId);',
    N'@n INT OUTPUT', @n = @ecarts OUTPUT;
IF @ecarts > 0
    THROW 50003, N'Reprise incomplète : des rattachements ne visent pas une version dédiée à leur formulaire.', 1;

-- ─────────────────────────── 4. Demandes de modèle ───────────────────────────
IF OBJECT_ID('dbo.ReportTemplateRequest') IS NULL
BEGIN
    EXEC(N'CREATE TABLE dbo.ReportTemplateRequest (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    FormId        UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_Formulaire(Id),
    Origin        VARCHAR(20)    NOT NULL
        CONSTRAINT CK_ReportTemplateRequest_Origin CHECK (Origin IN (''NEW_FORM'', ''NEW_STRUCTURE'', ''SUPERVISOR'')),
    Reason        NVARCHAR(500)  NULL,
    VersionChange VARCHAR(20)    NULL
        CONSTRAINT CK_ReportTemplateRequest_VersionChange CHECK (VersionChange IN (''SAME_VERSION'', ''NEXT_VERSION'')),
    Status        VARCHAR(20)    NOT NULL DEFAULT ''PENDING''
        CONSTRAINT CK_ReportTemplateRequest_Status CHECK (Status IN (''PENDING'', ''DONE'', ''CANCELLED'')),
    RequestedBy   VARCHAR(20)    NULL REFERENCES dbo.AUTILIS(USR_0),
    RequestedAt   DATETIME2(3)   NOT NULL DEFAULT SYSDATETIME(),
    HandledBy     VARCHAR(20)    NULL REFERENCES dbo.AUTILIS(USR_0),
    HandledAt     DATETIME2(3)   NULL,
    AssignmentId  BIGINT         NULL REFERENCES dbo.ReportTemplateAssignment(Id),
    CONSTRAINT CK_ReportTemplateRequest_VersionChoice CHECK (
        (Origin = ''SUPERVISOR'' AND VersionChange IS NOT NULL) OR (Origin <> ''SUPERVISOR'' AND VersionChange IS NULL)),
    CONSTRAINT CK_ReportTemplateRequest_Handled CHECK (
        (Status = ''PENDING'' AND HandledAt IS NULL AND AssignmentId IS NULL)
        OR (Status = ''DONE'' AND HandledAt IS NOT NULL AND AssignmentId IS NOT NULL)
        OR (Status = ''CANCELLED'' AND HandledAt IS NOT NULL))
);');
    EXEC(N'CREATE UNIQUE INDEX UX_ReportTemplateRequest_Pending ON dbo.ReportTemplateRequest(FormId) WHERE Status = ''PENDING'';');
END

COMMIT TRANSACTION;
PRINT 'Migration 2026-10-16 (nature des types, modèle dédié à chaque formulaire, demandes de modèle) appliquée.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

SET NOEXEC OFF;
GO
