-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Applique les changements de schéma de la refonte articles / produits finis et de l'assistant
-- d'assemblage, déjà présents dans 01_Create_SopalTraceDB.sql pour une base neuve :
--   1. Article : origine (fabriqué / acheté) déduite du type, nature « PF » réservée aux produits finis ;
--      NatureArticle.Origine supprimée.
--   2. ProduitFini : seul un article PF peut y figurer.
--   3. Nomenclature : seul un article fabriqué a une composition ; pas de composant de lui-même,
--      pas de quantité négative.
--   4. FamilleProduitFini.TypeRobinetCode obligatoire ; Document_Entete.TypeRobinetCode supprimée
--      (le type se déduit de la famille).
--   5. Critères Ac / Re libres (contraintes supprimées).
--   6. Session d'équipe : pause déjeuner prise, fin de poste / d'OF demandée ; plusieurs équipes ouvertes
--      sur un OF (une seule en production, les autres en pause).
--   7. PeriodiciteMachine.ControlMoment : moment de contrôle explicite (DEMARRAGE, APRES_PAUSE, FIN_POSTE).
--
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

-- La migration suppose les refontes précédentes (comptes, règles d'échantillonnage) : sinon, recréer la base.
-- (SET NOEXEC ON : dans SSMS, les lots suivants ne s'exécutent pas non plus.)
IF COL_LENGTH('dbo.UtilisateursApp', 'AccountStatus') IS NULL OR OBJECT_ID('dbo.Ref_RegleEchantillonnage') IS NOT NULL
BEGIN
    RAISERROR('Base trop ancienne pour cette migration (comptes / règles d''échantillonnage) : recréer la base avec les scripts 01 à 04.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRANSACTION;

DECLARE @sql NVARCHAR(MAX);

-- ─────────────────────────── 1. NatureArticle / Article ───────────────────────────
-- Origine retirée de la nature (contrainte CHECK puis colonne).
IF COL_LENGTH('dbo.NatureArticle', 'Origine') IS NOT NULL
BEGIN
    SELECT @sql = STRING_AGG(N'ALTER TABLE dbo.NatureArticle DROP CONSTRAINT ' + QUOTENAME(c.name), N'; ')
    FROM sys.objects c
    JOIN sys.columns col ON col.object_id = c.parent_object_id
    WHERE c.parent_object_id = OBJECT_ID('dbo.NatureArticle') AND col.name = 'Origine'
      AND ((c.type = 'C' AND col.column_id = (SELECT parent_column_id FROM sys.check_constraints WHERE object_id = c.object_id))
        OR (c.type = 'D' AND col.column_id = (SELECT parent_column_id FROM sys.default_constraints WHERE object_id = c.object_id)));
    IF @sql IS NOT NULL EXEC sp_executesql @sql;
    EXEC (N'ALTER TABLE dbo.NatureArticle DROP COLUMN Origine');
    PRINT '1. NatureArticle.Origine supprimée.';
END

-- Données de démonstration : l'écrou est un composant, pas un produit fini.
IF NOT EXISTS (SELECT 1 FROM dbo.NatureArticle WHERE Code = 'ECROU')
    INSERT INTO dbo.NatureArticle (Code, Libelle) VALUES ('ECROU', N'Écrou');
UPDATE dbo.Article SET NatureArticleCode = 'ECROU' WHERE CodeArticle = 'ECROU-001' AND NatureArticleCode = 'PF' AND TypeArticle = 'COMPOSANT';

IF COL_LENGTH('dbo.Article', 'Origin') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.Article ADD Origin AS CAST(CASE WHEN TypeArticle = ''COMPOSANT'' THEN ''PURCHASED'' ELSE ''MANUFACTURED'' END AS VARCHAR(12)) PERSISTED NOT NULL');
    PRINT '1. Article.Origin ajoutée.';
END

IF EXISTS (SELECT 1 FROM dbo.Article WHERE (NatureArticleCode = 'PF' AND TypeArticle <> 'PF') OR (NatureArticleCode <> 'PF' AND TypeArticle = 'PF'))
BEGIN
    SELECT CodeArticle, NatureArticleCode, TypeArticle AS [Article incohérent (nature PF <=> type PF)]
    FROM dbo.Article WHERE (NatureArticleCode = 'PF' AND TypeArticle <> 'PF') OR (NatureArticleCode <> 'PF' AND TypeArticle = 'PF');
    THROW 50101, 'Articles incohérents (nature PF sans type PF ou l''inverse) : corriger la liste affichée puis relancer.', 1;
END
IF OBJECT_ID('dbo.CK_Article_FinishedProductNature') IS NULL
    EXEC (N'ALTER TABLE dbo.Article ADD CONSTRAINT CK_Article_FinishedProductNature CHECK (
        (NatureArticleCode = ''PF'' AND TypeArticle = ''PF'') OR (NatureArticleCode <> ''PF'' AND TypeArticle <> ''PF''))');
IF OBJECT_ID('dbo.UQ_Article_Type') IS NULL
    EXEC (N'ALTER TABLE dbo.Article ADD CONSTRAINT UQ_Article_Type UNIQUE (CodeArticle, TypeArticle)');
IF OBJECT_ID('dbo.UQ_Article_Origin') IS NULL
    EXEC (N'ALTER TABLE dbo.Article ADD CONSTRAINT UQ_Article_Origin UNIQUE (CodeArticle, Origin)');

-- ─────────────────────────── 2. ProduitFini : seulement des articles PF ───────────────────────────
IF COL_LENGTH('dbo.ProduitFini', 'ArticleType') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.ProduitFini p JOIN dbo.Article a ON a.CodeArticle = p.CodeArticle WHERE a.TypeArticle <> 'PF')
    BEGIN
        SELECT p.CodeArticle, a.TypeArticle AS [Produit fini dont l'article n'est pas PF]
        FROM dbo.ProduitFini p JOIN dbo.Article a ON a.CodeArticle = p.CodeArticle WHERE a.TypeArticle <> 'PF';
        THROW 50102, 'ProduitFini contient des articles qui ne sont pas des PF : corriger la liste affichée puis relancer.', 1;
    END

    -- Ancienne clé étrangère simple CodeArticle → Article, remplacée par la clé composite.
    SELECT @sql = STRING_AGG(N'ALTER TABLE dbo.ProduitFini DROP CONSTRAINT ' + QUOTENAME(fk.name), N'; ')
    FROM sys.foreign_keys fk
    WHERE fk.parent_object_id = OBJECT_ID('dbo.ProduitFini') AND fk.referenced_object_id = OBJECT_ID('dbo.Article');
    IF @sql IS NOT NULL EXEC sp_executesql @sql;

    EXEC (N'ALTER TABLE dbo.ProduitFini ADD ArticleType AS CAST(''PF'' AS VARCHAR(20)) PERSISTED NOT NULL');
    EXEC (N'ALTER TABLE dbo.ProduitFini ADD CONSTRAINT FK_ProduitFini_FinishedProductArticle FOREIGN KEY (CodeArticle, ArticleType)
        REFERENCES dbo.Article(CodeArticle, TypeArticle) ON DELETE CASCADE');
    PRINT '2. ProduitFini limité aux articles PF.';
END

-- ─────────────────────────── 3. Nomenclature ───────────────────────────
IF COL_LENGTH('dbo.BOMD_Nomenclature', 'ParentOrigin') IS NULL
BEGIN
    -- Un article acheté n'a pas de composition : ces lignes sont incohérentes et retirées (affichées avant).
    -- (SQL dynamique : Article.Origin peut avoir été créée plus haut dans ce même lot.)
    EXEC (N'IF EXISTS (SELECT 1 FROM dbo.BOMD_Nomenclature b JOIN dbo.Article a ON a.CodeArticle = b.ArticleParent WHERE a.Origin = ''PURCHASED'')
    BEGIN
        SELECT b.ArticleParent, b.CodeComposant, b.CodeAlternative, b.QuantiteRequise AS [Nomenclature retirée (parent acheté)]
        FROM dbo.BOMD_Nomenclature b JOIN dbo.Article a ON a.CodeArticle = b.ArticleParent WHERE a.Origin = ''PURCHASED'';
        DELETE b FROM dbo.BOMD_Nomenclature b JOIN dbo.Article a ON a.CodeArticle = b.ArticleParent WHERE a.Origin = ''PURCHASED'';
    END');
    DELETE FROM dbo.BOMD_Nomenclature WHERE ArticleParent = CodeComposant;
    IF EXISTS (SELECT 1 FROM dbo.BOMD_Nomenclature WHERE QuantiteRequise < 0)
        THROW 50103, 'Nomenclature : quantités négatives à corriger avant la migration.', 1;

    SELECT @sql = STRING_AGG(N'ALTER TABLE dbo.BOMD_Nomenclature DROP CONSTRAINT ' + QUOTENAME(fk.name), N'; ')
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.BOMD_Nomenclature') AND c.name = 'ArticleParent';
    IF @sql IS NOT NULL EXEC sp_executesql @sql;

    EXEC (N'ALTER TABLE dbo.BOMD_Nomenclature ADD ParentOrigin AS CAST(''MANUFACTURED'' AS VARCHAR(12)) PERSISTED NOT NULL');
    EXEC (N'ALTER TABLE dbo.BOMD_Nomenclature ADD CONSTRAINT FK_BillOfMaterials_ManufacturedParent FOREIGN KEY (ArticleParent, ParentOrigin)
        REFERENCES dbo.Article(CodeArticle, Origin)');
    PRINT '3. Nomenclature : parent obligatoirement fabriqué.';
END
IF OBJECT_ID('dbo.CK_BillOfMaterials_NotItsOwnComponent') IS NULL
    EXEC (N'ALTER TABLE dbo.BOMD_Nomenclature ADD CONSTRAINT CK_BillOfMaterials_NotItsOwnComponent CHECK (ArticleParent <> CodeComposant)');
IF OBJECT_ID('dbo.CK_BillOfMaterials_NonNegativeQuantity') IS NULL
    EXEC (N'ALTER TABLE dbo.BOMD_Nomenclature ADD CONSTRAINT CK_BillOfMaterials_NonNegativeQuantity CHECK (QuantiteRequise >= 0)');

-- ─────────────────────────── 4. Type de robinet porté par la famille ───────────────────────────
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FamilleProduitFini') AND name = 'TypeRobinetCode' AND is_nullable = 1)
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.FamilleProduitFini WHERE TypeRobinetCode IS NULL)
    BEGIN
        SELECT Code AS [Famille sans type de robinet] FROM dbo.FamilleProduitFini WHERE TypeRobinetCode IS NULL;
        THROW 50104, 'Familles sans type de robinet : renseigner TypeRobinetCode pour la liste affichée puis relancer.', 1;
    END
    EXEC (N'ALTER TABLE dbo.FamilleProduitFini ALTER COLUMN TypeRobinetCode VARCHAR(10) NOT NULL');
    PRINT '4. FamilleProduitFini.TypeRobinetCode obligatoire.';
END

IF COL_LENGTH('dbo.Document_Entete', 'TypeRobinetCode') IS NOT NULL
BEGIN
    SELECT @sql = STRING_AGG(N'ALTER TABLE dbo.Document_Entete DROP CONSTRAINT ' + QUOTENAME(fk.name), N'; ')
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
    JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.Document_Entete') AND c.name = 'TypeRobinetCode';
    IF @sql IS NOT NULL EXEC sp_executesql @sql;
    EXEC (N'ALTER TABLE dbo.Document_Entete DROP COLUMN TypeRobinetCode');
    PRINT '4. Document_Entete.TypeRobinetCode supprimée (déduite de la famille).';
END

-- ─────────────────────────── 5. Critères Ac / Re libres ───────────────────────────
IF OBJECT_ID('dbo.CK_SamplingPlan_Criteria') IS NOT NULL
    ALTER TABLE dbo.SamplingPlan DROP CONSTRAINT CK_SamplingPlan_Criteria;
IF OBJECT_ID('dbo.CK_ExecEchantillonnage_Criteres') IS NOT NULL
    ALTER TABLE dbo.Exec_Echantillonnage DROP CONSTRAINT CK_ExecEchantillonnage_Criteres;

-- ─────────────────────────── 6. Session d'équipe ───────────────────────────
IF COL_LENGTH('dbo.Exec_ControleOf_Session', 'HadLunchBreak') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.Exec_ControleOf_Session ADD HadLunchBreak BIT NOT NULL CONSTRAINT DF_ExecControleOfSession_HadLunchBreak DEFAULT 0');
    PRINT '6. Session : HadLunchBreak ajoutée.';
END
IF COL_LENGTH('dbo.Exec_ControleOf_Session', 'ClosingRequest') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.Exec_ControleOf_Session ADD ClosingRequest VARCHAR(10) NULL
        CONSTRAINT CK_ExecControleOfSession_ClosingRequest CHECK (ClosingRequest IN (''FIN_POSTE'', ''FIN_OF''))');
    PRINT '6. Session : ClosingRequest ajoutée.';
END

-- Plusieurs équipes ouvertes sur un OF : une seule en production, une session ouverte par équipe.
IF OBJECT_ID('dbo.TR_ExecControleOfSession_SingleActive') IS NOT NULL
    DROP TRIGGER dbo.TR_ExecControleOfSession_SingleActive;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecControleOfSession_OneRunningTeam')
    CREATE UNIQUE INDEX UX_ExecControleOfSession_OneRunningTeam
        ON dbo.Exec_ControleOf_Session (ExecControleOfId, Statut) WHERE Statut = 'EN_COURS';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExecControleOfSession_OneOpenSessionPerTeam')
    CREATE UNIQUE INDEX UX_ExecControleOfSession_OneOpenSessionPerTeam
        ON dbo.Exec_ControleOf_Session (ExecControleOfId, Equipe) WHERE Statut IN ('EN_COURS', 'EN_PAUSE');

-- ─────────────────────────── 7. Moment de contrôle des périodicités machine ───────────────────────────
IF COL_LENGTH('dbo.PeriodiciteMachine', 'ControlMoment') IS NULL
BEGIN
    EXEC (N'ALTER TABLE dbo.PeriodiciteMachine ADD ControlMoment VARCHAR(20) NULL
        CONSTRAINT CK_PeriodiciteMachine_ControlMoment CHECK (ControlMoment IN (''DEMARRAGE'', ''APRES_PAUSE'', ''FIN_POSTE''))');
    PRINT '7. PeriodiciteMachine.ControlMoment ajoutée.';
END
EXEC (N'UPDATE dbo.PeriodiciteMachine
      SET ControlMoment = CASE Code WHEN ''DEM'' THEN ''DEMARRAGE'' WHEN ''PAU'' THEN ''APRES_PAUSE'' WHEN ''FIN'' THEN ''FIN_POSTE'' END
      WHERE ControlMoment IS NULL AND Code IN (''DEM'', ''PAU'', ''FIN'')');

COMMIT TRANSACTION;
PRINT 'Migration terminée.';
GO
SET NOEXEC OFF;
GO

-- À vérifier : périodicités machine sans moment (elles n'apparaissent pas dans l'assistant d'assemblage).
-- Renseigner leur moment au besoin : UPDATE dbo.PeriodiciteMachine SET ControlMoment = 'DEMARRAGE' WHERE Code = '...';
SELECT Code, Libelle AS [Périodicité machine sans moment de contrôle]
FROM dbo.PeriodiciteMachine WHERE ControlMoment IS NULL;
GO
