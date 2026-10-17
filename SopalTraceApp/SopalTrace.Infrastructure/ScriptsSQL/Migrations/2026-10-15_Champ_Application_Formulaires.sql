-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Le formulaire d'un document se trouve par son rôle et son champ d'application, jamais par son code (déjà présent dans
-- 01/02 pour une base neuve) :
--   1. Ref_Formulaire : machine (vérification machine) et nature (plan sans famille, ex. PISTON) au champ d'application.
--   2. Champ d'application des formulaires livrés (famille, poste, machine, nature), sur toutes leurs versions.
--   3. Réparation : un plan qui a perdu son poste ou sa nature en passant en nouvelle version (défaut corrigé dans
--      l'éditeur du plan d'assemblage) les reprend de son formulaire ou de sa version précédente.
--   4. Unicité : un seul formulaire en cours par rôle et champ d'application, un seul par code. Si deux formulaires en
--      cours ont le même champ d'application, la migration s'arrête en les nommant (à corriger avant de la rejouer).
--
-- Suppose la migration 2026-10-15_Modeles_Rattaches_Aux_Formulaires.sql déjà appliquée.
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

IF OBJECT_ID('dbo.ReportTemplateInitialForm') IS NULL
BEGIN
    RAISERROR('Appliquer d''abord la migration 2026-10-15_Modeles_Rattaches_Aux_Formulaires.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRY
BEGIN TRANSACTION;

-- ─────────────────────────── 1. Machine et nature ───────────────────────────
IF COL_LENGTH('dbo.Ref_Formulaire', 'MachineCode') IS NULL
    ALTER TABLE dbo.Ref_Formulaire ADD MachineCode VARCHAR(30) NULL REFERENCES dbo.Machine(CodeMachine);
IF COL_LENGTH('dbo.Ref_Formulaire', 'NatureArticleCode') IS NULL
    ALTER TABLE dbo.Ref_Formulaire ADD NatureArticleCode VARCHAR(20) NULL REFERENCES dbo.NatureArticle(Code);

-- ─────────────────────────── 2. Champ d'application des formulaires livrés ───────────────────────────
EXEC(N'
UPDATE f SET FamilleProduitFiniCode = v.Famille, PosteCode = v.Poste, MachineCode = v.Machine, NatureArticleCode = v.Nature
FROM dbo.Ref_Formulaire f
JOIN (VALUES
    (''FE-ASS-MANU'', ''RBGFM'', NULL, NULL, NULL),
    (''FE-ASS-PISTON'', NULL, NULL, NULL, ''PISTON''),
    (''FE-ASS-RBGFA-BAC-01'', ''RBGFA-BAC-01'', NULL, NULL, NULL),
    (''FE-ASS-RBGFA-BAC-02'', ''RBGFA-BAC-02'', NULL, NULL, NULL),
    (''FE-PF-RBGFA_SOUPAPE_PAS71'', ''RBGFA_SOUPAPE'', ''PAS71'', NULL, NULL),
    (''FE-PF-RBGFA_SOUPAPE_PAS72'', ''RBGFA_SOUPAPE'', ''PAS72'', NULL, NULL),
    (''FE-PF-RBGFA_SOUPAPE_PAS78'', ''RBGFA_SOUPAPE'', ''PAS78'', NULL, NULL),
    (''FE-PF-RBGFA'', ''RBGFA'', NULL, NULL, NULL),
    (''FE-PF-RBGFA-BAC-01'', ''RBGFA-BAC-01'', NULL, NULL, NULL),
    (''FE-PF-RBGFA-BAC-02'', ''RBGFA-BAC-02'', NULL, NULL, NULL),
    (''FE-PF-RBGFA_SOUPAPE'', ''RBGFA_SOUPAPE'', NULL, NULL, NULL),
    (''FE-PF-RBGFM'', ''RBGFM'', NULL, NULL, NULL),
    (''FE-VM-MAS19'', NULL, NULL, ''MAS19'', NULL),
    (''FE-VM-MAS20'', NULL, NULL, ''MAS20'', NULL),
    (''FE-VM-MAS22'', NULL, NULL, ''MAS22'', NULL),
    (''FE-VM-MAS26'', NULL, NULL, ''MAS26'', NULL),
    (''FE-VM-BEE22'', NULL, NULL, ''BEE22'', NULL),
    (''FE-VM-BEE46'', NULL, NULL, ''BEE46'', NULL),
    (''FE-VM-BEE47'', NULL, NULL, ''BEE47'', NULL),
    (''FE-VM-SER05'', NULL, NULL, ''SER05'', NULL)
) v (Code, Famille, Poste, Machine, Nature) ON v.Code = f.CodeReference
WHERE ISNULL(f.FamilleProduitFiniCode, '''') <> ISNULL(v.Famille, '''') OR ISNULL(f.PosteCode, '''') <> ISNULL(v.Poste, '''')
   OR ISNULL(f.MachineCode, '''') <> ISNULL(v.Machine, '''') OR ISNULL(f.NatureArticleCode, '''') <> ISNULL(v.Nature, '''');');

-- ─────────────────────────── 3. Réparation des plans ───────────────────────────
EXEC(N'
-- Poste et famille : ceux du formulaire du plan.
UPDATE d SET PosteCode = f.PosteCode
FROM dbo.Document_Entete d JOIN dbo.Ref_Formulaire f ON f.Id = d.FormulaireId
WHERE d.PosteCode IS NULL AND f.PosteCode IS NOT NULL;
UPDATE d SET FamilleProduitFiniCode = f.FamilleProduitFiniCode
FROM dbo.Document_Entete d JOIN dbo.Ref_Formulaire f ON f.Id = d.FormulaireId
WHERE d.FamilleProduitFiniCode IS NULL AND f.FamilleProduitFiniCode IS NOT NULL;
-- Nature : celle de la version précédente du même formulaire (même code), la plus récente qui en a une.
UPDATE d SET NatureArticleCode = precedente.NatureArticleCode
FROM dbo.Document_Entete d
JOIN dbo.Ref_Formulaire f ON f.Id = d.FormulaireId
CROSS APPLY (
    SELECT TOP 1 p.NatureArticleCode
    FROM dbo.Document_Entete p JOIN dbo.Ref_Formulaire pf ON pf.Id = p.FormulaireId
    WHERE pf.CodeReference = f.CodeReference AND p.TypeDocumentCode = d.TypeDocumentCode
      AND p.NatureArticleCode IS NOT NULL AND p.Version < d.Version
    ORDER BY p.Version DESC
) precedente
WHERE d.NatureArticleCode IS NULL AND d.TypeDocumentCode = ''PLAN_ASS'';');

-- ─────────────────────────── 4. Unicité ───────────────────────────
EXEC(N'DECLARE @doublons NVARCHAR(400) = (
    SELECT TOP 1 CONCAT(Role, N'' : '', MIN(CodeReference), N'', '', MAX(CodeReference))
    FROM dbo.Ref_Formulaire WHERE Statut IN (''BROUILLON'', ''ACTIF'')
    GROUP BY Role, FamilleProduitFiniCode, PosteCode, TypeRobinetCode, MachineCode, NatureArticleCode
    HAVING COUNT(*) > 1);
IF @doublons IS NOT NULL
BEGIN
    DECLARE @message NVARCHAR(2048) = LEFT(N''Plusieurs formulaires en cours ont le même rôle et le même champ d''''application ('' + @doublons
        + N'') : archivez ou précisez-les puis rejouez la migration.'', 2048);
    THROW 50003, @message, 1;
END
IF EXISTS (SELECT 1 FROM dbo.Ref_Formulaire WHERE Statut IN (''BROUILLON'', ''ACTIF'') GROUP BY CodeReference HAVING COUNT(*) > 1)
    THROW 50003, N''Un même code désigne plusieurs formulaires en cours ; corrigez-les puis rejouez la migration.'', 1;
');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RefFormulaire_ChampApplication' AND object_id = OBJECT_ID('dbo.Ref_Formulaire'))
    EXEC(N'CREATE UNIQUE INDEX UX_RefFormulaire_ChampApplication ON dbo.Ref_Formulaire
        (Role, FamilleProduitFiniCode, PosteCode, TypeRobinetCode, MachineCode, NatureArticleCode) WHERE Statut IN (''BROUILLON'', ''ACTIF'');');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RefFormulaire_CodeEnCours' AND object_id = OBJECT_ID('dbo.Ref_Formulaire'))
    EXEC(N'CREATE UNIQUE INDEX UX_RefFormulaire_CodeEnCours ON dbo.Ref_Formulaire (CodeReference) WHERE Statut IN (''BROUILLON'', ''ACTIF'');');

COMMIT TRANSACTION;
PRINT 'Migration 2026-10-15 (champ d''application des formulaires) appliquée.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

SET NOEXEC OFF;
GO
