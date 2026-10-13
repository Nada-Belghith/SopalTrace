-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Contrôles périodiques planifiés par le serveur (assemblage et fabrication), déjà présents dans
-- 01_Create_SopalTraceDB.sql pour une base neuve :
--   1. Exec_ProductionPeriod : périodes de production, par équipe et par poste (assemblage) ou par section
--      du plan (fabrication) ; pause, réglage, fin de poste et clôture les ferment, la reprise en ouvre une
--      nouvelle. Une première version de ce script (périodes d'assemblage seulement) est complétée.
--   2. Exec_Prelevement_Intermediaire : poste et période de l'occurrence ; unicité par équipe et par poste.
--   3. Occurrences non répondues de l'ancien calcul supprimées (replanifiées à la reprise de la production).
--   4. Ref_Formulaire : poste et famille du formulaire (ex. contrôle au poste « PAS71 soupape ») ; les
--      documents de contrôle au poste existants reprennent ceux de leur formulaire.
--   5. Contrôle au poste : équipe, opérateur et date des pièces réalisées ; pièces rebutées par équipe ;
--      équipes et horaires par défaut des formulaires qui n'en ont pas.
--   6. Registre de traçabilité : équipe, poste, opérateur et annulation (avec motif) des lignes ; code
--      article des composants.
--
-- Suppose la migration 2026-10-12_Articles_Assemblage.sql déjà appliquée.
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

IF COL_LENGTH('dbo.Exec_ControleOf_Session', 'HadLunchBreak') IS NULL
BEGIN
    RAISERROR('Appliquer d''abord la migration 2026-10-12_Articles_Assemblage.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRANSACTION;

DECLARE @sql NVARCHAR(MAX);

-- ─────────────────────────── 1. Périodes de production ───────────────────────────
IF OBJECT_ID('dbo.Exec_ProductionPeriod') IS NULL
BEGIN
    CREATE TABLE dbo.Exec_ProductionPeriod (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        ExecControleOfId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id),
        SessionId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleOf_Session(Id),
        PosteCode VARCHAR(30) NULL REFERENCES dbo.PosteTravail(CodePoste),
        SectionId UNIQUEIDENTIFIER NULL REFERENCES dbo.Document_Section(Id),
        StartedAt DATETIME2(3) NOT NULL,
        IntervalMinutes FLOAT NULL CHECK (IntervalMinutes > 0),
        PlannedControlCount INT NOT NULL DEFAULT 0 CHECK (PlannedControlCount >= 0),
        EndedAt DATETIME2(3) NULL,
        EndReason VARCHAR(20) NULL CHECK (EndReason IN ('PAUSE', 'SETTING', 'SETTING_CANCELLED', 'END_OF_SHIFT', 'STOPPED')),
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT CK_ExecProductionPeriod_End CHECK (
            (EndedAt IS NULL AND EndReason IS NULL) OR (EndedAt IS NOT NULL AND EndReason IS NOT NULL AND EndedAt >= StartedAt)),
        CONSTRAINT CK_ExecProductionPeriod_Scope CHECK (
            (SessionId IS NOT NULL AND PosteCode IS NOT NULL AND SectionId IS NULL)
            OR (SessionId IS NULL AND PosteCode IS NULL AND SectionId IS NOT NULL))
    );
    EXEC (N'CREATE INDEX IX_ExecProductionPeriod_Exec ON dbo.Exec_ProductionPeriod (ExecControleOfId, EndedAt)');
    PRINT '1. Exec_ProductionPeriod créée.';
END
ELSE IF COL_LENGTH('dbo.Exec_ProductionPeriod', 'SectionId') IS NULL
BEGIN
    -- Première version (assemblage seulement) : équipe et poste deviennent facultatifs, section ajoutée.
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecProductionPeriod_OneOpenPerPoste' AND object_id = OBJECT_ID('dbo.Exec_ProductionPeriod'))
        EXEC (N'DROP INDEX UX_ExecProductionPeriod_OneOpenPerPoste ON dbo.Exec_ProductionPeriod');
    SET @sql = NULL;
    SELECT @sql = STRING_AGG(N'ALTER TABLE dbo.Exec_ProductionPeriod DROP CONSTRAINT ' + QUOTENAME(fk.name), N'; ')
    FROM sys.foreign_keys fk JOIN sys.foreign_key_columns k ON k.constraint_object_id = fk.object_id
    JOIN sys.columns c ON c.object_id = k.parent_object_id AND c.column_id = k.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.Exec_ProductionPeriod') AND c.name IN ('SessionId', 'PosteCode');
    IF @sql IS NOT NULL EXEC sp_executesql @sql;
    EXEC (N'ALTER TABLE dbo.Exec_ProductionPeriod ALTER COLUMN SessionId UNIQUEIDENTIFIER NULL');
    EXEC (N'ALTER TABLE dbo.Exec_ProductionPeriod ALTER COLUMN PosteCode VARCHAR(30) NULL');
    EXEC (N'ALTER TABLE dbo.Exec_ProductionPeriod ADD
        FOREIGN KEY (SessionId) REFERENCES dbo.Exec_ControleOf_Session(Id),
        FOREIGN KEY (PosteCode) REFERENCES dbo.PosteTravail(CodePoste),
        SectionId UNIQUEIDENTIFIER NULL REFERENCES dbo.Document_Section(Id)');
    EXEC (N'ALTER TABLE dbo.Exec_ProductionPeriod ADD CONSTRAINT CK_ExecProductionPeriod_Scope CHECK (
        (SessionId IS NOT NULL AND PosteCode IS NOT NULL AND SectionId IS NULL)
        OR (SessionId IS NULL AND PosteCode IS NULL AND SectionId IS NOT NULL))');
    PRINT '1. Exec_ProductionPeriod : périodes de fabrication (par section) possibles.';
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecProductionPeriod_OneOpenPerPoste' AND object_id = OBJECT_ID('dbo.Exec_ProductionPeriod'))
    EXEC (N'CREATE UNIQUE INDEX UX_ExecProductionPeriod_OneOpenPerPoste ON dbo.Exec_ProductionPeriod (SessionId, PosteCode)
        WHERE EndedAt IS NULL AND SessionId IS NOT NULL');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecProductionPeriod_OneOpenPerSection' AND object_id = OBJECT_ID('dbo.Exec_ProductionPeriod'))
    EXEC (N'CREATE UNIQUE INDEX UX_ExecProductionPeriod_OneOpenPerSection ON dbo.Exec_ProductionPeriod (ExecControleOfId, SectionId)
        WHERE EndedAt IS NULL AND SectionId IS NOT NULL');

-- ─────────────────────────── 2. Occurrences par poste ───────────────────────────
IF COL_LENGTH('dbo.Exec_Prelevement_Intermediaire', 'PosteCode') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.Exec_Prelevement_Intermediaire ADD
        PosteCode VARCHAR(30) NULL REFERENCES dbo.PosteTravail(CodePoste),
        ProductionPeriodId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ProductionPeriod(Id)');
    PRINT '2. Exec_Prelevement_Intermediaire : PosteCode et ProductionPeriodId ajoutées.';
END

IF OBJECT_ID('dbo.UQ_ExecPrelevementIntermediaire_Occurrence') IS NULL
BEGIN
    -- Ancienne unicité (nom généré par SQL Server) : par OF, section, tranche et numéro.
    SELECT @sql = STRING_AGG(N'ALTER TABLE dbo.Exec_Prelevement_Intermediaire DROP CONSTRAINT ' + QUOTENAME(k.name), N'; ')
    FROM sys.key_constraints k
    WHERE k.parent_object_id = OBJECT_ID('dbo.Exec_Prelevement_Intermediaire') AND k.type = 'UQ';
    IF @sql IS NOT NULL EXEC sp_executesql @sql;
    EXEC (N'ALTER TABLE dbo.Exec_Prelevement_Intermediaire ADD CONSTRAINT UQ_ExecPrelevementIntermediaire_Occurrence
        UNIQUE (ExecControleOFId, SectionId, SessionId, PosteCode, TrancheHoraire, NumeroOccurrence)');
    PRINT '2. Unicité des occurrences par équipe et par poste.';
END

-- ─────────────────────────── 3. Occurrences de l'ancien calcul ───────────────────────────
-- Non répondues et sans période (assemblage comme fabrication) : le serveur replanifie à la reprise de la
-- production. Les contrôles de réglage de la fabrication (tranches REGLAGE...) ne sont pas concernés.
EXEC (N'DELETE i FROM dbo.Exec_Prelevement_Intermediaire i
JOIN dbo.Exec_ControleOF o ON o.Id = i.ExecControleOFId
WHERE i.ProductionPeriodId IS NULL AND i.EstRepondu = 0 AND o.Statut <> ''CLOTURE'' AND i.TrancheHoraire NOT LIKE ''REGLAGE%''');

-- ─────────────────────────── 4. Champ d'application des formulaires ───────────────────────────
IF COL_LENGTH('dbo.Ref_Formulaire', 'PosteCode') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.Ref_Formulaire ADD
        PosteCode VARCHAR(30) NULL REFERENCES dbo.PosteTravail(CodePoste),
        FamilleProduitFiniCode VARCHAR(30) NULL REFERENCES dbo.FamilleProduitFini(Code)');
    PRINT '4. Ref_Formulaire : PosteCode et FamilleProduitFiniCode ajoutées.';
END

-- Formulaires de contrôle au poste livrés (toutes leurs versions), s'ils n'ont pas encore de champ d'application.
EXEC (N'UPDATE f SET PosteCode = v.PosteCode, FamilleProduitFiniCode = v.FamilleProduitFiniCode
FROM dbo.Ref_Formulaire f
JOIN (VALUES
    (''FE-RC-PAS71'', ''PAS71'', NULL),
    (''FE-RC-PAS72'', ''PAS72'', NULL),
    (''FE-RC-PAS78'', ''PAS78'', NULL),
    (''FE-RC-PAS71_SOUPAPE'', ''PAS71'', ''RBGFA_SOUPAPE''),
    (''FE-RC-PAS72_SOUPAPE'', ''PAS72'', ''RBGFA_SOUPAPE''),
    (''FE-RC-PAS78_SOUPAPE'', ''PAS78'', ''RBGFA_SOUPAPE'')
) v (CodeReference, PosteCode, FamilleProduitFiniCode) ON v.CodeReference = f.CodeReference
WHERE f.PosteCode IS NULL AND f.FamilleProduitFiniCode IS NULL');

-- Documents de contrôle au poste : poste et famille de leur formulaire (corrige un document « soupape » créé
-- sans famille, qui s'appliquait alors à tous les articles du poste).
EXEC (N'UPDATE d SET PosteCode = f.PosteCode,
                     FamilleProduitFiniCode = ISNULL(f.FamilleProduitFiniCode, d.FamilleProduitFiniCode)
FROM dbo.Document_Entete d
JOIN dbo.Ref_Formulaire f ON f.Id = d.FormulaireId
WHERE d.TypeDocumentCode = ''CTRL_POSTE'' AND f.PosteCode IS NOT NULL
  AND (ISNULL(d.PosteCode, '''') <> f.PosteCode
       OR (f.FamilleProduitFiniCode IS NOT NULL AND ISNULL(d.FamilleProduitFiniCode, '''') <> f.FamilleProduitFiniCode))');

-- ─────────────────────────── 5. Contrôle au poste par équipe ───────────────────────────
IF COL_LENGTH('dbo.Exec_RC_Poste_Reponse', 'Equipe') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.Exec_RC_Poste_Reponse ADD
        Equipe VARCHAR(10) NULL REFERENCES dbo.Team(Code),
        MatriculeOperateur VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
        DateSaisie DATETIME2(3) NULL');
    PRINT '5. Exec_RC_Poste_Reponse : équipe, opérateur et date de saisie.';
END

IF COL_LENGTH('dbo.Exec_RC_Poste_Bilan', 'Id') IS NULL
BEGIN
    -- Un bilan par document devient un bilan par équipe : nouvelle clé, équipe, opérateur, date.
    SET @sql = NULL;
    SELECT @sql = N'ALTER TABLE dbo.Exec_RC_Poste_Bilan DROP CONSTRAINT ' + QUOTENAME(name)
    FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('dbo.Exec_RC_Poste_Bilan') AND type = 'PK';
    IF @sql IS NOT NULL EXEC sp_executesql @sql;
    EXEC (N'ALTER TABLE dbo.Exec_RC_Poste_Bilan ADD
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() WITH VALUES,
        Equipe VARCHAR(10) NULL REFERENCES dbo.Team(Code),
        MatriculeOperateur VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
        DateSaisie DATETIME2(3) NULL');
    EXEC (N'ALTER TABLE dbo.Exec_RC_Poste_Bilan ADD PRIMARY KEY (Id),
        CONSTRAINT UQ_ExecRcPosteBilan_Equipe UNIQUE (ExecControleDocumentStatutId, Equipe)');
    PRINT '5. Exec_RC_Poste_Bilan : pièces rebutées par équipe.';
END

EXEC (N'INSERT INTO dbo.Ref_Formulaire_Equipe (FormulaireId, TeamCode, NomEquipe, HeureDebut, HeureFin, OrdreAffiche)
SELECT f.Id, e.TeamCode, e.NomEquipe, e.HeureDebut, e.HeureFin, e.OrdreAffiche
FROM dbo.Ref_Formulaire f
CROSS JOIN (VALUES (''E1'', N''Équipe 1'', 6, 14, 1), (''E2'', N''Équipe 2'', 14, 22, 2), (''E3'', N''Équipe 3'', 22, 6, 3))
    e (TeamCode, NomEquipe, HeureDebut, HeureFin, OrdreAffiche)
WHERE f.Role = ''RESULTAT_CONTROLE_POSTE''
  AND NOT EXISTS (SELECT 1 FROM dbo.Ref_Formulaire_Equipe x WHERE x.FormulaireId = f.Id)');

-- ─────────────────────────── 6. Registre de traçabilité ───────────────────────────
IF COL_LENGTH('dbo.ExecRegistreTracabilite', 'SessionId') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.ExecRegistreTracabilite ADD
        SessionId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleOf_Session(Id),
        Equipe VARCHAR(10) NULL REFERENCES dbo.Team(Code),
        PosteCode VARCHAR(30) NULL REFERENCES dbo.PosteTravail(CodePoste),
        MatriculeOperateur VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
        CancelledAt DATETIME2(3) NULL,
        CancelledBy VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
        CancellationReason VARCHAR(250) NULL');
    EXEC (N'ALTER TABLE dbo.ExecRegistreTracabilite ADD CONSTRAINT CK_ExecRegistreTracabilite_Cancellation CHECK (
        CancelledAt IS NULL OR (CancelledBy IS NOT NULL AND CancellationReason IS NOT NULL))');
    PRINT '6. ExecRegistreTracabilite : équipe, poste, opérateur, annulation.';
END
IF COL_LENGTH('dbo.ExecRegistreTracabiliteComposant', 'CodeArticle') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.ExecRegistreTracabiliteComposant ADD CodeArticle VARCHAR(30) NULL REFERENCES dbo.Article(CodeArticle)');
    -- Lignes existantes : article retrouvé par sa désignation parmi les lots scannés de l''OF.
    EXEC (N'UPDATE c SET CodeArticle = a.CodeArticle
    FROM dbo.ExecRegistreTracabiliteComposant c
    JOIN dbo.ExecRegistreTracabilite r ON r.Id = c.RegistreTracabiliteId
    JOIN dbo.Exec_ControleOF o ON o.Id = r.ExecControleOfId
    JOIN dbo.Mag_PreparationOF p ON p.NumeroOF = o.NumeroOF
    JOIN dbo.Mag_PreparationOF_Lot l ON l.PreparationOFId = p.Id
    JOIN dbo.Article a ON a.CodeArticle = l.CodeArticle AND a.Designation = c.DesignationComposant');
    PRINT '6. ExecRegistreTracabiliteComposant : code article.';
END

COMMIT TRANSACTION;
PRINT 'Migration terminée.';
GO
SET NOEXEC OFF;
GO
