-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Identité figée des formulaires (déjà présent dans 01 pour une base neuve). Une ligne de Ref_Formulaire = un code et
-- une version ; dès qu'un document ou une fiche l'utilise, elle ne change plus. Changer de code crée une nouvelle ligne
-- (même version) : l'ancienne et ses documents génériques sont archivés tels quels, les documents recréés à l'identique.
--   1. Ref_Formulaire.VersionGroupId : identifiant commun aux lignes d'un formulaire (versions et changements de code).
--      Les lignes existantes d'un même code forment un groupe.
--   2. Ref_Formulaire.CreationOrder : ordre de création dans la séquence de la base (FEMANU.1 avant DEMANU.1).
--   3. FormCodeChange : journal des changements de code (ligne remplacée, ligne qui la remplace). Une version
--      intermédiaire de cette migration (historique par document) est remplacée.
--   4. Un document générique actif par formulaire (hors modèles de fabrication), toujours sur un formulaire en cours ;
--      identité d'un formulaire utilisé figée (déclencheurs). La migration s'arrête en nommant les cas à corriger.
--   5. Procédures de recopie (structure d'un formulaire, document complet) utilisées par le changement de code.
--
-- Suppose la migration 2026-10-15_Champ_Application_Formulaires.sql déjà appliquée.
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RefFormulaire_ChampApplication' AND object_id = OBJECT_ID('dbo.Ref_Formulaire'))
BEGIN
    RAISERROR('Appliquer d''abord la migration 2026-10-15_Champ_Application_Formulaires.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRY
BEGIN TRANSACTION;

-- ─────────────────────────── 1. Groupe des lignes d'un formulaire ───────────────────────────
IF COL_LENGTH('dbo.Ref_Formulaire', 'VersionGroupId') IS NULL
BEGIN
    ALTER TABLE dbo.Ref_Formulaire ADD VersionGroupId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_RefFormulaire_VersionGroupId DEFAULT NEWID();
    -- Jusqu'ici les versions d'un formulaire étaient celles d'un même code : elles reçoivent l'identifiant de la première.
    EXEC(N'WITH Versions AS (
        SELECT VersionGroupId, FIRST_VALUE(VersionGroupId) OVER (PARTITION BY CodeReference ORDER BY Version, Id) AS Groupe
        FROM dbo.Ref_Formulaire)
    UPDATE Versions SET VersionGroupId = Groupe;');
END
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RefFormulaire_VersionGroup' AND object_id = OBJECT_ID('dbo.Ref_Formulaire'))
    DROP INDEX UX_RefFormulaire_VersionGroup ON dbo.Ref_Formulaire;

-- ─────────────────────────── 2. Ordre de création des formulaires ───────────────────────────
IF COL_LENGTH('dbo.Ref_Formulaire', 'CreationOrder') IS NULL
BEGIN
    ALTER TABLE dbo.Ref_Formulaire ADD CreationOrder BIGINT NULL;
    -- Numéros réservés dans la séquence de la base, attribués dans l'ordre de création connu (date, puis version).
    EXEC(N'DECLARE @nombre BIGINT = (SELECT COUNT(*) FROM dbo.Ref_Formulaire), @premier SQL_VARIANT;
    IF @nombre > 0
    BEGIN
        EXEC sys.sp_sequence_get_range @sequence_name = N''dbo.CreationOrderSequence'', @range_size = @nombre, @range_first_value = @premier OUTPUT;
        WITH Ordre AS (SELECT CreationOrder, ROW_NUMBER() OVER (ORDER BY CreeLe, Version, Id) AS Rang FROM dbo.Ref_Formulaire)
        UPDATE Ordre SET CreationOrder = CAST(@premier AS BIGINT) + Rang - 1;
    END');
    ALTER TABLE dbo.Ref_Formulaire ALTER COLUMN CreationOrder BIGINT NOT NULL;
    ALTER TABLE dbo.Ref_Formulaire ADD CONSTRAINT DF_RefFormulaire_CreationOrder DEFAULT (NEXT VALUE FOR dbo.CreationOrderSequence) FOR CreationOrder;
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefFormulaire_VersionGroup' AND object_id = OBJECT_ID('dbo.Ref_Formulaire'))
    EXEC(N'CREATE INDEX IX_RefFormulaire_VersionGroup ON dbo.Ref_Formulaire (VersionGroupId, CreationOrder);');

-- ─────────────────────────── 3. Journal des changements de code ───────────────────────────
-- Version intermédiaire (historique par document, codes renommés sur place) : remplacée par le journal des lignes.
IF OBJECT_ID('dbo.FormCodeChangeDocument') IS NOT NULL
    DROP TABLE dbo.FormCodeChangeDocument;
IF COL_LENGTH('dbo.FormCodeChange', 'FormId') IS NOT NULL
    DROP TABLE dbo.FormCodeChange;
IF OBJECT_ID('dbo.FormCodeChange') IS NULL
    CREATE TABLE dbo.FormCodeChange (
        Id             BIGINT IDENTITY(1,1) PRIMARY KEY,
        PreviousFormId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_Formulaire(Id),
        NewFormId      UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_Formulaire(Id),
        ChangedBy      VARCHAR(20)      NULL REFERENCES dbo.AUTILIS(USR_0),
        ChangedAt      DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT UX_FormCodeChange_PreviousForm UNIQUE (PreviousFormId),
        CONSTRAINT UX_FormCodeChange_NewForm UNIQUE (NewFormId),
        CONSTRAINT CK_FormCodeChange_DifferentForms CHECK (PreviousFormId <> NewFormId)
    );

-- ─────────────────────────── 4. Documents génériques et formulaires ───────────────────────────
DECLARE @cas NVARCHAR(MAX) = (
    SELECT STRING_AGG(CONVERT(NVARCHAR(MAX), CONCAT(f.CodeReference, '.', f.Version)), N', ')
    FROM (SELECT d.FormulaireId FROM dbo.Document_Entete d
          WHERE d.Statut = 'ACTIF' AND d.Portee = 'GENERIQUE' AND d.TypeDocumentCode <> 'MODELE_FAB' AND d.FormulaireId IS NOT NULL
          GROUP BY d.FormulaireId HAVING COUNT(*) > 1) x
    JOIN dbo.Ref_Formulaire f ON f.Id = x.FormulaireId);
IF @cas IS NOT NULL
BEGIN
    DECLARE @message NVARCHAR(2048) = LEFT(N'Plusieurs documents génériques actifs sur le même formulaire (' + @cas
        + N') : archivez les documents en trop puis rejouez la migration.', 2048);
    THROW 50003, @message, 1;
END
SET @cas = (
    SELECT STRING_AGG(CONVERT(NVARCHAR(MAX), CONCAT(d.Nom, ' (', f.CodeReference, '.', f.Version, ')')), N', ')
    FROM dbo.Document_Entete d JOIN dbo.Ref_Formulaire f ON f.Id = d.FormulaireId
    WHERE d.Statut = 'ACTIF' AND d.Portee = 'GENERIQUE' AND f.Statut = 'ARCHIVE');
IF @cas IS NOT NULL
BEGIN
    DECLARE @message2 NVARCHAR(2048) = LEFT(N'Documents génériques actifs sur un formulaire archivé (' + @cas
        + N') : archivez-les ou créez leur nouvelle version, puis rejouez la migration.', 2048);
    THROW 50003, @message2, 1;
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_DocumentEntete_GenericActiveForm' AND object_id = OBJECT_ID('dbo.Document_Entete'))
    EXEC(N'CREATE UNIQUE INDEX UX_DocumentEntete_GenericActiveForm ON dbo.Document_Entete (FormulaireId)
    WHERE Statut = ''ACTIF'' AND Portee = ''GENERIQUE'' AND TypeDocumentCode <> ''MODELE_FAB'' AND FormulaireId IS NOT NULL;');

-- ─────────────────────────── 5. Déclencheurs et procédures (identiques à 01) ───────────────────────────
IF OBJECT_ID('dbo.TR_FormCodeChange_Immutable') IS NULL
    EXEC(N'CREATE TRIGGER TR_FormCodeChange_Immutable ON dbo.FormCodeChange
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 50002, N''L''''historique des codes de formulaire ne peut être ni modifié ni supprimé (ISO 9001).'', 1;
END;');
IF OBJECT_ID('dbo.TR_RefFormulaire_Identity') IS NULL
    EXEC(N'CREATE TRIGGER TR_RefFormulaire_Identity ON dbo.Ref_Formulaire
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE (i.CodeReference <> d.CodeReference OR i.Version <> d.Version OR i.VersionGroupId <> d.VersionGroupId
                      OR i.CreationOrder <> d.CreationOrder
                      OR ISNULL(i.Role, '''') <> ISNULL(d.Role, '''')
                      OR ISNULL(i.PosteCode, '''') <> ISNULL(d.PosteCode, '''')
                      OR ISNULL(i.FamilleProduitFiniCode, '''') <> ISNULL(d.FamilleProduitFiniCode, '''')
                      OR ISNULL(i.TypeRobinetCode, '''') <> ISNULL(d.TypeRobinetCode, '''')
                      OR ISNULL(i.MachineCode, '''') <> ISNULL(d.MachineCode, '''')
                      OR ISNULL(i.NatureArticleCode, '''') <> ISNULL(d.NatureArticleCode, ''''))
                 AND (EXISTS (SELECT 1 FROM dbo.Document_Entete x WHERE x.FormulaireId = i.Id)
                      OR EXISTS (SELECT 1 FROM dbo.Exec_ControleDocumentStatut s WHERE s.FormId = i.Id)))
        THROW 50003, N''Ce formulaire est déjà utilisé : son code, sa version et son champ d''''application ne changent plus. Changer de code crée un nouveau formulaire.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.Document_Entete x ON x.FormulaireId = i.Id
               WHERE i.Statut = ''ARCHIVE'' AND x.Statut = ''ACTIF'' AND x.Portee = ''GENERIQUE'')
        THROW 50003, N''Un formulaire n''''est archivé qu''''après ses documents génériques actifs.'', 1;
END;');
IF OBJECT_ID('dbo.TR_DocumentEntete_FormInProgress') IS NULL
    EXEC(N'CREATE TRIGGER TR_DocumentEntete_FormInProgress ON dbo.Document_Entete
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.Ref_Formulaire f ON f.Id = i.FormulaireId
               WHERE i.Statut = ''ACTIF'' AND i.Portee = ''GENERIQUE'' AND f.Statut = ''ARCHIVE'')
        THROW 50003, N''Un document générique actif s''''appuie sur un formulaire en cours, jamais sur un formulaire archivé.'', 1;
END;');
IF OBJECT_ID('dbo.CopyFormStructure') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.CopyFormStructure
    @SourceFormId UNIQUEIDENTIFIER,
    @TargetFormId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF EXISTS (SELECT 1 FROM dbo.Ref_Formulaire_ColonneDef WHERE FormulaireId = @TargetFormId)
       OR EXISTS (SELECT 1 FROM dbo.Ref_Formulaire_Equipe WHERE FormulaireId = @TargetFormId)
        THROW 50004, N''Le formulaire cible a déjà une structure.'', 1;

    INSERT INTO dbo.Ref_Formulaire_ColonneDef (FormulaireId, CleColonne, LabelAffiche, TypeValeur, InsertAfter, TargetTable, Actif)
    SELECT @TargetFormId, CleColonne, LabelAffiche, TypeValeur, InsertAfter, TargetTable, Actif
    FROM dbo.Ref_Formulaire_ColonneDef WHERE FormulaireId = @SourceFormId;

    INSERT INTO dbo.ColumnAllowedValue (ColumnDefinitionId, Value, DisplayOrder, Active)
    SELECT cible.Id, v.Value, v.DisplayOrder, v.Active
    FROM dbo.ColumnAllowedValue v
    JOIN dbo.Ref_Formulaire_ColonneDef source ON source.Id = v.ColumnDefinitionId AND source.FormulaireId = @SourceFormId
    JOIN dbo.Ref_Formulaire_ColonneDef cible ON cible.FormulaireId = @TargetFormId AND cible.CleColonne = source.CleColonne;

    INSERT INTO dbo.Ref_Formulaire_Equipe (FormulaireId, TeamCode, NomEquipe, HeureDebut, HeureFin, OrdreAffiche, Actif)
    SELECT @TargetFormId, TeamCode, NomEquipe, HeureDebut, HeureFin, OrdreAffiche, Actif
    FROM dbo.Ref_Formulaire_Equipe WHERE FormulaireId = @SourceFormId;
END;');
IF OBJECT_ID('dbo.CopyDocumentToForm') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.CopyDocumentToForm
    @SourceDocumentId UNIQUEIDENTIFIER,
    @FormId           UNIQUEIDENTIFIER,
    @Status           VARCHAR(20),
    @CreatedBy        VARCHAR(20),
    @VersionComment   NVARCHAR(MAX),
    @NewDocumentId    UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Document_Entete WHERE Id = @SourceDocumentId)
        THROW 50004, N''Document à recréer introuvable.'', 1;

    SET @NewDocumentId = NEWID();
    INSERT INTO dbo.Document_Entete (Id, TypeDocumentCode, Portee, CodeArticleSageVersionne, DocumentSourceId, Nom, Designation,
        Version, Statut, OperationCode, FormulaireId, LegendeMoyens, Remarques, CommentaireVersion, CreePar,
        NatureArticleCode, FamilleProduitFiniCode, PosteCode, MachineCode)
    SELECT @NewDocumentId, d.TypeDocumentCode, d.Portee, d.CodeArticleSageVersionne, d.DocumentSourceId, d.Nom, d.Designation,
        f.Version, @Status, d.OperationCode, f.Id, d.LegendeMoyens, d.Remarques, @VersionComment, @CreatedBy,
        d.NatureArticleCode, d.FamilleProduitFiniCode, d.PosteCode, d.MachineCode
    FROM dbo.Document_Entete d CROSS JOIN dbo.Ref_Formulaire f
    WHERE d.Id = @SourceDocumentId AND f.Id = @FormId;

    DECLARE @Sections TABLE (OldId UNIQUEIDENTIFIER PRIMARY KEY, NewId UNIQUEIDENTIFIER NOT NULL);
    INSERT INTO @Sections SELECT Id, NEWID() FROM dbo.Document_Section WHERE EnteteId = @SourceDocumentId;
    INSERT INTO dbo.Document_Section (Id, EnteteId, OrdreAffiche, LibelleSection, TypeSectionId, PeriodiciteId, Notes, NqaId)
    SELECT m.NewId, @NewDocumentId, s.OrdreAffiche, s.LibelleSection, s.TypeSectionId, s.PeriodiciteId, s.Notes, s.NqaId
    FROM dbo.Document_Section s JOIN @Sections m ON m.OldId = s.Id;

    DECLARE @Lines TABLE (OldId UNIQUEIDENTIFIER PRIMARY KEY, NewId UNIQUEIDENTIFIER NOT NULL);
    INSERT INTO @Lines SELECT Id, NEWID() FROM dbo.Document_Ligne WHERE EnteteId = @SourceDocumentId;
    INSERT INTO dbo.Document_Ligne (Id, EnteteId, SectionId, OrdreAffiche, CaracteristiqueId, LibelleAffiche, TypeCaracteristiqueId,
        TypeControleId, MoyenControleId, MoyenTexteLibre, InstrumentCode, PeriodiciteId, LimiteSpecTexte, EstCritique,
        Instruction, Observations, MachineCode, RisqueDefautId, TypeLigne)
    SELECT m.NewId, @NewDocumentId, ms.NewId, l.OrdreAffiche, l.CaracteristiqueId, l.LibelleAffiche, l.TypeCaracteristiqueId,
        l.TypeControleId, l.MoyenControleId, l.MoyenTexteLibre, l.InstrumentCode, l.PeriodiciteId, l.LimiteSpecTexte, l.EstCritique,
        l.Instruction, l.Observations, l.MachineCode, l.RisqueDefautId, l.TypeLigne
    FROM dbo.Document_Ligne l JOIN @Lines m ON m.OldId = l.Id LEFT JOIN @Sections ms ON ms.OldId = l.SectionId;

    INSERT INTO dbo.Document_Ligne_ExtraColonne (LigneId, CleColonne, ValeurColonne, OrdreAffiche)
    SELECT m.NewId, v.CleColonne, v.ValeurColonne, v.OrdreAffiche
    FROM dbo.Document_Ligne_ExtraColonne v JOIN @Lines m ON m.OldId = v.LigneId;

    INSERT INTO dbo.DocumentLineImage (LineId, ContentType, Content)
    SELECT m.NewId, i.ContentType, i.Content FROM dbo.DocumentLineImage i JOIN @Lines m ON m.OldId = i.LineId;

    INSERT INTO dbo.SamplingPlan (DocumentId, InspectionLevelCode, SamplingType, InspectionSeverity, AcceptableQualityLevelId,
        AcceptanceNumber, RejectionNumber)
    SELECT @NewDocumentId, InspectionLevelCode, SamplingType, InspectionSeverity, AcceptableQualityLevelId, AcceptanceNumber, RejectionNumber
    FROM dbo.SamplingPlan WHERE DocumentId = @SourceDocumentId;

    DECLARE @Families TABLE (OldId UNIQUEIDENTIFIER PRIMARY KEY, NewId UNIQUEIDENTIFIER NOT NULL);
    INSERT INTO @Families SELECT Id, NEWID() FROM dbo.Document_VerifMachine_Famille WHERE EnteteId = @SourceDocumentId;
    INSERT INTO dbo.Document_VerifMachine_Famille (Id, EnteteId, OrdreAffiche, RefFamilleCorpsId)
    SELECT m.NewId, @NewDocumentId, f.OrdreAffiche, f.RefFamilleCorpsId
    FROM dbo.Document_VerifMachine_Famille f JOIN @Families m ON m.OldId = f.Id;

    DECLARE @Deadlines TABLE (OldId UNIQUEIDENTIFIER PRIMARY KEY, NewId UNIQUEIDENTIFIER NOT NULL);
    INSERT INTO @Deadlines SELECT e.Id, NEWID() FROM dbo.Document_VerifMachine_Echeance e JOIN @Lines m ON m.OldId = e.LigneId;
    INSERT INTO dbo.Document_VerifMachine_Echeance (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche)
    SELECT me.NewId, ml.NewId, e.PeriodiciteMachineId, e.RefMoyenDetectionId, e.OrdreAffiche
    FROM dbo.Document_VerifMachine_Echeance e JOIN @Deadlines me ON me.OldId = e.Id JOIN @Lines ml ON ml.OldId = e.LigneId;

    INSERT INTO dbo.Document_VerifMachine_MatricePiece (EcheanceId, FamilleId, RoleVerif, PieceRefId)
    SELECT me.NewId, mf.NewId, p.RoleVerif, p.PieceRefId
    FROM dbo.Document_VerifMachine_MatricePiece p JOIN @Deadlines me ON me.OldId = p.EcheanceId
    LEFT JOIN @Families mf ON mf.OldId = p.FamilleId;
END;');

COMMIT TRANSACTION;
PRINT 'Migration 2026-10-15 (identité figée des formulaires) appliquée.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

SET NOEXEC OFF;
GO
