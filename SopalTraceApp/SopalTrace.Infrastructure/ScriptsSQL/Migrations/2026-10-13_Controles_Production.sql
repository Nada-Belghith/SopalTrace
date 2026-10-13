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
--   7. Une session ouverte par équipe et par OF (plusieurs équipes en même temps, chacune sur ses postes).
--   8. Exec_SessionPost : postes démarrés par chaque session (contrôles de démarrage, de fin de poste, LOT_OF).
--   9. Une fiche d'échantillonnage par poste de l'OF.
--  10. Postes indépendants : fin de poste demandée et terminée poste par poste, une seule équipe à la fois
--      par poste ; la session ne porte plus que la clôture de l'OF.
--  11. Un registre de traçabilité par poste : poste obligatoire et affecté à l'OF, équipe qui a tenu le poste.
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

-- ─────────────────────────── 7. Une session ouverte par équipe et par OF ───────────────────────────
-- Plusieurs équipes peuvent travailler en même temps sur l'OF, chacune sur ses postes (étape 10). Une équipe a au
-- plus une session ouverte par OF : on garde la session en production, sinon la plus récente.
EXEC (N'UPDATE s SET Statut = ''TERMINE'', DateFin = SYSDATETIME(), SuspendedAt = NULL, ClosingRequest = NULL
FROM dbo.Exec_ControleOf_Session s
WHERE s.Statut IN (''EN_COURS'', ''EN_PAUSE'')
  AND EXISTS (SELECT 1 FROM dbo.Exec_ControleOf_Session g
              WHERE g.ExecControleOfId = s.ExecControleOfId AND g.Equipe = s.Equipe AND g.Id <> s.Id AND g.Statut IN (''EN_COURS'', ''EN_PAUSE'')
                AND (CASE WHEN g.Statut = ''EN_COURS'' THEN 1 ELSE 0 END > CASE WHEN s.Statut = ''EN_COURS'' THEN 1 ELSE 0 END
                     OR (g.Statut = s.Statut AND g.DateDebut > s.DateDebut)))');
IF @@ROWCOUNT > 0 PRINT '7. Sessions ouvertes en double d''une même équipe terminées.';
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecControleOfSession_OneRunningTeam')
    EXEC (N'DROP INDEX UX_ExecControleOfSession_OneRunningTeam ON dbo.Exec_ControleOf_Session');
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecControleOfSession_OneOpenSession')
    EXEC (N'DROP INDEX UX_ExecControleOfSession_OneOpenSession ON dbo.Exec_ControleOf_Session');
-- Une ancienne version de cet index (sans DateFin) est remplacée.
IF EXISTS (SELECT 1 FROM sys.indexes i
           WHERE i.name = 'UX_ExecControleOfSession_OneOpenSessionPerTeam'
             AND NOT EXISTS (SELECT 1 FROM sys.index_columns c WHERE c.object_id = i.object_id AND c.index_id = i.index_id
                             AND COL_NAME(c.object_id, c.column_id) = 'DateFin'))
    EXEC (N'DROP INDEX UX_ExecControleOfSession_OneOpenSessionPerTeam ON dbo.Exec_ControleOf_Session');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecControleOfSession_OneOpenSessionPerTeam')
BEGIN
    EXEC (N'CREATE UNIQUE INDEX UX_ExecControleOfSession_OneOpenSessionPerTeam
        ON dbo.Exec_ControleOf_Session (ExecControleOfId, Equipe, DateFin) WHERE Statut IN (''EN_COURS'', ''EN_PAUSE'')');
    PRINT '7. Une session ouverte par équipe et par OF.';
END

-- ─────────────────────────── 8. Postes démarrés par session ───────────────────────────
IF OBJECT_ID('dbo.Exec_SessionPost') IS NULL
BEGIN
    CREATE TABLE dbo.Exec_SessionPost (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        SessionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOf_Session(Id),
        PosteCode VARCHAR(30) NOT NULL REFERENCES dbo.PosteTravail(CodePoste),
        StartedAt DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
        StartedBy VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
        CONSTRAINT UQ_ExecSessionPost UNIQUE (SessionId, PosteCode)
    );
    -- Sessions existantes : un poste est démarré s'il a un document de l'équipe (plan d'assemblage ou
    -- vérification machine), à la date du premier.
    EXEC (N'INSERT INTO dbo.Exec_SessionPost (SessionId, PosteCode, StartedAt)
    SELECT d.SessionId, d.PosteCode, MIN(COALESCE(d.DateExecution, s.DateDebut))
    FROM dbo.Exec_ControleDocumentStatut d
    JOIN dbo.Exec_ControleOf_Session s ON s.Id = d.SessionId
    WHERE d.PosteCode IS NOT NULL AND d.TypeDocument IN (''PLAN_ASS'', ''VERIF_MACHINE'')
    GROUP BY d.SessionId, d.PosteCode');
    PRINT '8. Exec_SessionPost créée (postes démarrés des sessions existantes).';
END

-- ─────────────────────────── 9. Fiche d'échantillonnage par poste ───────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecControleDocumentStatut_SamplingPerPoste')
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Exec_ControleDocumentStatut WHERE TypeDocument = 'ECHANTILLONNAGE' AND PosteCode IS NOT NULL
               GROUP BY ExecControleOfId, PosteCode HAVING COUNT(*) > 1)
    BEGIN
        SELECT ExecControleOfId, PosteCode, COUNT(*) AS [Fiches d'échantillonnage en double]
        FROM dbo.Exec_ControleDocumentStatut WHERE TypeDocument = 'ECHANTILLONNAGE' AND PosteCode IS NOT NULL
        GROUP BY ExecControleOfId, PosteCode HAVING COUNT(*) > 1;
        THROW 50109, 'Plusieurs fiches d''échantillonnage pour un même poste d''un OF : garder une seule fiche par poste (liste affichée), puis relancer.', 1;
    END
    EXEC (N'CREATE UNIQUE INDEX UX_ExecControleDocumentStatut_SamplingPerPoste ON dbo.Exec_ControleDocumentStatut (ExecControleOfId, PosteCode)
        WHERE TypeDocument = ''ECHANTILLONNAGE'' AND PosteCode IS NOT NULL');
    PRINT '9. Une fiche d''échantillonnage par poste de l''OF.';
END

-- ─────────────────────────── 10. Postes indépendants ───────────────────────────
-- Chaque poste suit son propre parcours : la fin de poste se demande et se termine poste par poste, et une seule
-- équipe à la fois tient un poste (une autre équipe peut prendre un poste terminé pendant que la première
-- continue sur ses autres postes).
IF COL_LENGTH('dbo.Exec_SessionPost', 'ExecControleOfId') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.Exec_SessionPost ADD
        ExecControleOfId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleOF(Id),
        ClosingRequestedAt DATETIME2(3) NULL,
        EndedAt DATETIME2(3) NULL,
        EndedBy VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0)');
    -- Postes des sessions terminées : terminés avec leur session.
    EXEC (N'UPDATE p SET ExecControleOfId = s.ExecControleOfId,
        EndedAt = CASE WHEN s.Statut <> ''TERMINE'' THEN NULL
                       WHEN COALESCE(s.DateFin, p.StartedAt) < p.StartedAt THEN p.StartedAt
                       ELSE COALESCE(s.DateFin, p.StartedAt) END
    FROM dbo.Exec_SessionPost p JOIN dbo.Exec_ControleOf_Session s ON s.Id = p.SessionId');
    -- Fin de poste demandée sur une session ouverte (ancien fonctionnement : pour tous ses postes).
    EXEC (N'UPDATE p SET ClosingRequestedAt = CASE WHEN SYSDATETIME() < p.StartedAt THEN p.StartedAt ELSE SYSDATETIME() END
    FROM dbo.Exec_SessionPost p JOIN dbo.Exec_ControleOf_Session s ON s.Id = p.SessionId
    WHERE s.Statut IN (''EN_COURS'', ''EN_PAUSE'') AND s.ClosingRequest = ''FIN_POSTE''');
    EXEC (N'ALTER TABLE dbo.Exec_SessionPost ALTER COLUMN ExecControleOfId UNIQUEIDENTIFIER NOT NULL');
    EXEC (N'ALTER TABLE dbo.Exec_SessionPost ADD
        CONSTRAINT CK_ExecSessionPost_Dates CHECK (
            (ClosingRequestedAt IS NULL OR ClosingRequestedAt >= StartedAt) AND (EndedAt IS NULL OR EndedAt >= StartedAt)),
        CONSTRAINT CK_ExecSessionPost_EndedBy CHECK (EndedBy IS NULL OR EndedAt IS NOT NULL)');
    PRINT '10. Exec_SessionPost : OF, fin de poste demandée et terminée par poste.';
END

-- La session ne porte plus que la clôture de l'OF.
EXEC (N'UPDATE dbo.Exec_ControleOf_Session SET ClosingRequest = NULL WHERE ClosingRequest = ''FIN_POSTE''');
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ExecControleOfSession_ClosingRequest')
BEGIN
    DECLARE @ancienneContrainte SYSNAME = (
        SELECT TOP 1 c.name FROM sys.check_constraints c
        WHERE c.parent_object_id = OBJECT_ID('dbo.Exec_ControleOf_Session') AND c.definition LIKE '%ClosingRequest%');
    IF @ancienneContrainte IS NOT NULL
        EXEC (N'ALTER TABLE dbo.Exec_ControleOf_Session DROP CONSTRAINT ' + @ancienneContrainte);
    EXEC (N'ALTER TABLE dbo.Exec_ControleOf_Session ADD CONSTRAINT CK_ExecControleOfSession_ClosingRequest CHECK (ClosingRequest = ''FIN_OF'')');
    PRINT '10. Session : seule la clôture de l''OF y est demandée.';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecSessionPost_OneTeamPerPoste')
BEGIN
    -- Colonnes ajoutées plus haut dans le même lot : requêtes compilées à l'exécution.
    EXEC (N'IF EXISTS (SELECT 1 FROM dbo.Exec_SessionPost WHERE EndedAt IS NULL GROUP BY ExecControleOfId, PosteCode HAVING COUNT(*) > 1)
    BEGIN
        SELECT ExecControleOfId, PosteCode, COUNT(*) AS [Équipes en cours sur le poste]
        FROM dbo.Exec_SessionPost WHERE EndedAt IS NULL GROUP BY ExecControleOfId, PosteCode HAVING COUNT(*) > 1;
        THROW 50113, ''Plusieurs équipes en cours sur un même poste d''''un OF : terminer le poste de l''''une d''''elles (liste affichée), puis relancer.'', 1;
    END');
    EXEC (N'CREATE UNIQUE INDEX UX_ExecSessionPost_OneTeamPerPoste ON dbo.Exec_SessionPost (ExecControleOfId, PosteCode) WHERE EndedAt IS NULL');
    PRINT '10. Une seule équipe à la fois par poste de l''OF.';
END

EXEC (N'CREATE OR ALTER TRIGGER dbo.TR_ExecSessionPost_Consistency ON dbo.Exec_SessionPost
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Exec_ControleOf_Session s ON s.Id = i.SessionId
               WHERE s.ExecControleOfId <> i.ExecControleOfId)
        THROW 50111, ''Le poste n''''appartient pas à l''''OF de la session.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Exec_ControleOf_Session s ON s.Id = i.SessionId
               WHERE i.EndedAt IS NULL AND s.Statut = ''TERMINE'')
        THROW 50112, ''Session d''''équipe terminée : aucun poste ne peut y rester en cours.'', 1;
END;');
EXEC (N'CREATE OR ALTER TRIGGER dbo.TR_ExecControleOfSession_Consistency ON dbo.Exec_ControleOf_Session
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Exec_SessionPost p ON p.SessionId = i.Id
               WHERE i.Statut = ''TERMINE'' AND p.EndedAt IS NULL)
        THROW 50112, ''Session d''''équipe terminée : aucun poste ne peut y rester en cours.'', 1;
    IF EXISTS (SELECT 1 FROM dbo.Exec_ControleOf_Session s
               JOIN (SELECT DISTINCT ExecControleOfId FROM inserted) i ON i.ExecControleOfId = s.ExecControleOfId
               WHERE s.Statut IN (''EN_COURS'', ''EN_PAUSE'')
               GROUP BY s.ExecControleOfId
               HAVING COUNT(*) > 1 AND MAX(CASE WHEN s.ClosingRequest = ''FIN_OF'' THEN 1 ELSE 0 END) = 1)
        THROW 50110, ''Clôture de l''''OF en cours : une seule équipe peut être ouverte sur l''''OF.'', 1;
END;');
PRINT '10. Triggers de cohérence des postes et des sessions.';

-- ─────────────────────────── 11. Un registre de traçabilité par poste ───────────────────────────
-- Chaque ligne appartient au registre d'un poste de l'OF ; une ligne d'équipe vient d'une session qui a tenu ce
-- poste ; la confirmation des lots d'une équipe porte sur un poste. Lignes et confirmations sans poste : rattachées
-- au poste de l'OF quand il n'en a qu'un, sinon à celui de la session quand elle n'en a tenu qu'un.
EXEC (N'UPDATE r SET PosteCode = COALESCE(
        (SELECT MIN(p.PosteCode) FROM dbo.Exec_ControleOf_Poste p WHERE p.ExecControleOfId = r.ExecControleOfId
         HAVING COUNT(*) = 1),
        (SELECT MIN(s.PosteCode) FROM dbo.Exec_SessionPost s WHERE s.SessionId = r.SessionId HAVING COUNT(*) = 1))
    FROM dbo.ExecRegistreTracabilite r WHERE r.PosteCode IS NULL');
EXEC (N'UPDATE d SET PosteCode = COALESCE(
        (SELECT MIN(p.PosteCode) FROM dbo.Exec_ControleOf_Poste p WHERE p.ExecControleOfId = d.ExecControleOfId
         HAVING COUNT(*) = 1),
        (SELECT MIN(s.PosteCode) FROM dbo.Exec_SessionPost s WHERE s.SessionId = d.SessionId HAVING COUNT(*) = 1))
    FROM dbo.Exec_ControleDocumentStatut d WHERE d.TypeDocument = ''REGISTRE_TRACA'' AND d.PosteCode IS NULL');
EXEC (N'IF EXISTS (SELECT 1 FROM dbo.ExecRegistreTracabilite WHERE PosteCode IS NULL)
          OR EXISTS (SELECT 1 FROM dbo.Exec_ControleDocumentStatut WHERE TypeDocument = ''REGISTRE_TRACA'' AND PosteCode IS NULL)
          OR EXISTS (SELECT 1 FROM dbo.ExecRegistreTracabilite r WHERE NOT EXISTS (
                SELECT 1 FROM dbo.Exec_ControleOf_Poste p WHERE p.ExecControleOfId = r.ExecControleOfId AND p.PosteCode = r.PosteCode))
    BEGIN
        SELECT ''ligne'' AS [Registre sans poste de l''''OF], Id, ExecControleOfId, PosteCode FROM dbo.ExecRegistreTracabilite r
        WHERE r.PosteCode IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Exec_ControleOf_Poste p
                                                 WHERE p.ExecControleOfId = r.ExecControleOfId AND p.PosteCode = r.PosteCode)
        UNION ALL
        SELECT ''confirmation'', Id, ExecControleOfId, PosteCode FROM dbo.Exec_ControleDocumentStatut
        WHERE TypeDocument = ''REGISTRE_TRACA'' AND PosteCode IS NULL;
        THROW 50115, ''Registre de traçabilité : lignes ou confirmations sans poste de l''''OF (liste affichée) : renseigner leur poste, puis relancer.'', 1;
    END');
IF EXISTS (SELECT 1 FROM sys.foreign_keys f JOIN sys.foreign_key_columns k ON k.constraint_object_id = f.object_id
           WHERE f.parent_object_id = OBJECT_ID('dbo.ExecRegistreTracabilite') AND f.referenced_object_id = OBJECT_ID('dbo.PosteTravail'))
BEGIN
    DECLARE @fkPoste SYSNAME = (SELECT TOP 1 f.name FROM sys.foreign_keys f
        WHERE f.parent_object_id = OBJECT_ID('dbo.ExecRegistreTracabilite') AND f.referenced_object_id = OBJECT_ID('dbo.PosteTravail'));
    EXEC (N'ALTER TABLE dbo.ExecRegistreTracabilite DROP CONSTRAINT ' + @fkPoste);
END
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ExecRegistreTracabilite') AND name = 'PosteCode' AND is_nullable = 1)
BEGIN
    EXEC (N'ALTER TABLE dbo.ExecRegistreTracabilite ALTER COLUMN PosteCode VARCHAR(30) NOT NULL');
    PRINT '11. ExecRegistreTracabilite : poste obligatoire.';
END
IF OBJECT_ID('dbo.FK_ExecRegistreTracabilite_OfPoste') IS NULL
    EXEC (N'ALTER TABLE dbo.ExecRegistreTracabilite ADD CONSTRAINT FK_ExecRegistreTracabilite_OfPoste FOREIGN KEY (ExecControleOfId, PosteCode)
        REFERENCES dbo.Exec_ControleOf_Poste (ExecControleOfId, PosteCode)');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ExecRegistreTracabilite_Poste')
    EXEC (N'CREATE INDEX IX_ExecRegistreTracabilite_Poste ON dbo.ExecRegistreTracabilite (ExecControleOfId, PosteCode, DateHeure)');
IF OBJECT_ID('dbo.CK_ExecControleDocumentStatut_RegistrePoste') IS NULL
    EXEC (N'ALTER TABLE dbo.Exec_ControleDocumentStatut ADD CONSTRAINT CK_ExecControleDocumentStatut_RegistrePoste
        CHECK (TypeDocument <> ''REGISTRE_TRACA'' OR PosteCode IS NOT NULL)');
EXEC (N'CREATE OR ALTER TRIGGER dbo.TR_ExecRegistreTracabilite_Consistency ON dbo.ExecRegistreTracabilite
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i
               WHERE i.SessionId IS NOT NULL
                 AND NOT EXISTS (SELECT 1 FROM dbo.Exec_SessionPost p
                                 WHERE p.SessionId = i.SessionId AND p.ExecControleOfId = i.ExecControleOfId AND p.PosteCode = i.PosteCode))
        THROW 50114, ''Registre de traçabilité : l''''équipe n''''a pas tenu ce poste de l''''OF.'', 1;
END;');
PRINT '11. Un registre de traçabilité par poste.';

COMMIT TRANSACTION;
PRINT 'Migration terminée.';
GO
SET NOEXEC OFF;
GO
