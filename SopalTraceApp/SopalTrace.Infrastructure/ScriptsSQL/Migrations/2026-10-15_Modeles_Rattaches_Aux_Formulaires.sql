-- =================================================================================
-- SopalTraceDB — Migration d'une base existante (sans la recréer)
--
-- Chaque formulaire reçoit son modèle de rapport (déjà présent dans 01/02 pour une base neuve) :
--   1. Ref_Formulaire : rôle RESULTAT_CONTROLE_ASSEMBLAGE (résultats du contrôle en cours d'assemblage) et type de
--      robinet du champ d'application ; formulaires FE-RC-ENCF-ASS-AUTO, -MANU, -PASxx_SOUPAPE.
--   2. Exec_ControleDocumentStatut.FormId : formulaire des résultats d'assemblage d'une fiche, fixé à sa création
--      (reprise des fiches existantes : formulaire le plus précis pour leur poste et leur article).
--   3. ReportTemplate : rôle des formulaires mis en forme (FormRole) à la place des variantes ; nouveaux modèles de
--      vérification MAS22, MAS26 et SER05 ; noms avec le code du formulaire Sopal.
--   4. ReportTemplateInitialForm : rattachements de départ (faits par l'API à l'import des modèles).
--   5. Triggers : rattachement au formulaire du même rôle ; « par défaut » réservé aux documents sans formulaire.
--
-- Suppose la migration 2026-10-14_Modeles_Par_Formulaire.sql déjà appliquée.
-- Rejouable : chaque étape vérifie si elle est déjà faite. Tout ou rien (transaction).
-- Faire une sauvegarde avant. Ensuite : redémarrer l'API (elle rattache les formulaires aux modèles livrés).
-- =================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
USE SopalTraceDB;
GO

IF OBJECT_ID('dbo.ReportTemplateAssignment') IS NULL
BEGIN
    RAISERROR('Appliquer d''abord la migration 2026-10-14_Modeles_Par_Formulaire.sql.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRY
BEGIN TRANSACTION;

-- ─────────────────────────── 1. Formulaires ───────────────────────────
DECLARE @controleRole SYSNAME = (SELECT TOP 1 name FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.Ref_Formulaire') AND definition LIKE '%RESULTAT_CONTROLE_POSTE%' AND definition NOT LIKE '%RESULTAT_CONTROLE_ASSEMBLAGE%');
IF @controleRole IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.Ref_Formulaire DROP CONSTRAINT ' + @controleRole);
    EXEC(N'ALTER TABLE dbo.Ref_Formulaire ADD CHECK (Role IN (''EN_COURS_DE_FABRICATION'',''EN_COURS_DE_ASSEMBLAGE'',''PRODUIT_FINI'',''RESULTAT_CONTROLE_CF'',''RESULTAT_CONTROLE_ASSEMBLAGE'',''RESULTAT_CONTROLE_POSTE'',''VERIF_MACHINE'',''ECHANTILLONNAGE''));');
END
IF COL_LENGTH('dbo.Ref_Formulaire', 'TypeRobinetCode') IS NULL
    ALTER TABLE dbo.Ref_Formulaire ADD TypeRobinetCode VARCHAR(10) NULL REFERENCES dbo.TypeRobinet(Code);

-- Résultats d'assemblage : rôle propre et champ d'application (poste + famille pour la soupape, type de robinet sinon).
EXEC(N'
UPDATE f SET Role = ''RESULTAT_CONTROLE_ASSEMBLAGE'', PosteCode = v.PosteCode, FamilleProduitFiniCode = v.Famille
FROM dbo.Ref_Formulaire f
JOIN (VALUES (''FE-RC-ENCF-ASS-PAS71_SOUPAPE'', ''PAS71'', ''RBGFA_SOUPAPE''),
             (''FE-RC-ENCF-ASS-PAS72_SOUPAPE'', ''PAS72'', ''RBGFA_SOUPAPE''),
             (''FE-RC-ENCF-ASS-PAS78_SOUPAPE'', ''PAS78'', ''RBGFA_SOUPAPE'')) v (Code, PosteCode, Famille) ON v.Code = f.CodeReference
WHERE f.Role <> ''RESULTAT_CONTROLE_ASSEMBLAGE'' OR f.PosteCode IS NULL;

IF NOT EXISTS (SELECT 1 FROM dbo.Ref_Formulaire WHERE CodeReference = ''FE-RC-ENCF-ASS-AUTO'')
    INSERT INTO dbo.Ref_Formulaire (Id, CodeReference, Designation, Version, Statut, CreeLe, Role, TypeRobinetCode)
    VALUES (NEWID(), ''FE-RC-ENCF-ASS-AUTO'', ''Résultats du CF Assemblage robinets automatiques'', 0, ''BROUILLON'', GETDATE(), ''RESULTAT_CONTROLE_ASSEMBLAGE'', ''AUTO'');
IF NOT EXISTS (SELECT 1 FROM dbo.Ref_Formulaire WHERE CodeReference = ''FE-RC-ENCF-ASS-MANU'')
    INSERT INTO dbo.Ref_Formulaire (Id, CodeReference, Designation, Version, Statut, CreeLe, Role, TypeRobinetCode)
    VALUES (NEWID(), ''FE-RC-ENCF-ASS-MANU'', ''Résultats du CF Assemblage robinets manuels'', 0, ''BROUILLON'', GETDATE(), ''RESULTAT_CONTROLE_ASSEMBLAGE'', ''MAN'');

-- Ancien formulaire commun FE-RC-ENCF-ASS (remplacé par AUTO et MANU) : supprimé s''il ne sert à rien, sinon archivé.
DECLARE @commun UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Ref_Formulaire WHERE CodeReference = ''FE-RC-ENCF-ASS'');
IF @commun IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Document_Entete WHERE FormulaireId = @commun)
       AND NOT EXISTS (SELECT 1 FROM dbo.Ref_Formulaire_ColonneDef WHERE FormulaireId = @commun)
       AND NOT EXISTS (SELECT 1 FROM dbo.Ref_Formulaire_Equipe WHERE FormulaireId = @commun)
       AND NOT EXISTS (SELECT 1 FROM dbo.ReportTemplateAssignment WHERE FormId = @commun)
        DELETE FROM dbo.Ref_Formulaire WHERE Id = @commun;
    ELSE
        UPDATE dbo.Ref_Formulaire SET Statut = ''ARCHIVE'' WHERE Id = @commun;
END');

-- ─────────────────────────── 2. Formulaire des fiches de résultats d'assemblage ───────────────────────────
IF COL_LENGTH('dbo.Exec_ControleDocumentStatut', 'FormId') IS NULL
    ALTER TABLE dbo.Exec_ControleDocumentStatut ADD FormId UNIQUEIDENTIFIER NULL REFERENCES dbo.Ref_Formulaire(Id);

-- Reprise : formulaire non archivé le plus précis (poste, famille, type de robinet) pour le poste et l'article de la fiche.
EXEC(N'
UPDATE s SET FormId = choix.Id
FROM dbo.Exec_ControleDocumentStatut s
JOIN dbo.Exec_ControleOF o ON o.Id = s.ExecControleOfId
JOIN dbo.MFGHEAD_OrdreFabrication ordre ON ordre.NumeroOF = o.NumeroOF
JOIN dbo.ProduitFini p ON p.CodeArticle = ordre.CodeArticle
JOIN dbo.FamilleProduitFini fam ON fam.Code = p.FamilleProduitFiniCode
CROSS APPLY (
    SELECT TOP 1 f.Id
    FROM dbo.Ref_Formulaire f
    WHERE f.Role = ''RESULTAT_CONTROLE_ASSEMBLAGE'' AND f.Statut <> ''ARCHIVE''
      AND (f.PosteCode IS NULL OR f.PosteCode = s.PosteCode)
      AND (f.FamilleProduitFiniCode IS NULL OR f.FamilleProduitFiniCode = p.FamilleProduitFiniCode)
      AND (f.TypeRobinetCode IS NULL OR f.TypeRobinetCode = fam.TypeRobinetCode)
    ORDER BY (CASE WHEN f.PosteCode IS NULL THEN 0 ELSE 1 END) + (CASE WHEN f.FamilleProduitFiniCode IS NULL THEN 0 ELSE 1 END)
           + (CASE WHEN f.TypeRobinetCode IS NULL THEN 0 ELSE 1 END) DESC, f.Version DESC
) choix
WHERE s.TypeDocument = ''PLAN_ASS'' AND s.FormId IS NULL;');

EXEC(N'CREATE OR ALTER TRIGGER TR_ExecControleDocumentStatut_Form ON dbo.Exec_ControleDocumentStatut
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN dbo.Ref_Formulaire f ON f.Id = i.FormId
               WHERE i.FormId IS NOT NULL AND (i.TypeDocument <> ''PLAN_ASS'' OR f.Role <> ''RESULTAT_CONTROLE_ASSEMBLAGE''))
        THROW 50003, N''Seule une fiche de résultats d''''assemblage reçoit un formulaire de résultats d''''assemblage.'', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE d.FormId IS NOT NULL AND (i.FormId IS NULL OR i.FormId <> d.FormId))
        THROW 50003, N''Le formulaire d''''un document d''''exécution ne change jamais.'', 1;
END;');

-- ─────────────────────────── 3. Modèles : rôle des formulaires, plus de variante ───────────────────────────
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ReportTemplate_DocumentType' AND object_id = OBJECT_ID('dbo.ReportTemplate'))
    DROP INDEX UX_ReportTemplate_DocumentType ON dbo.ReportTemplate;
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ReportTemplate_ExecutionType' AND object_id = OBJECT_ID('dbo.ReportTemplate'))
    DROP INDEX UX_ReportTemplate_ExecutionType ON dbo.ReportTemplate;
IF COL_LENGTH('dbo.ReportTemplate', 'VariantCode') IS NOT NULL
    ALTER TABLE dbo.ReportTemplate DROP COLUMN VariantCode;
IF COL_LENGTH('dbo.ReportTemplate', 'FormRole') IS NULL
    EXEC(N'ALTER TABLE dbo.ReportTemplate ADD FormRole VARCHAR(50) NULL CONSTRAINT CK_ReportTemplate_FormRole
        CHECK (FormRole IN (''EN_COURS_DE_FABRICATION'',''EN_COURS_DE_ASSEMBLAGE'',''PRODUIT_FINI'',''RESULTAT_CONTROLE_CF'',''RESULTAT_CONTROLE_ASSEMBLAGE'',''RESULTAT_CONTROLE_POSTE'',''VERIF_MACHINE'',''ECHANTILLONNAGE''));');

EXEC(N'
MERGE dbo.ReportTemplate AS t
USING (VALUES
    (''PLAN_ASS'', N''Plan de contrôle d''''assemblage (FE0594)'', ''PLAN_ASS'', NULL, ''EN_COURS_DE_ASSEMBLAGE''),
    (''ECHANTILLONNAGE'', N''Fiche d''''échantillonnage (FE0591)'', NULL, ''ECHANTILLONNAGE'', ''ECHANTILLONNAGE''),
    (''RESULTAT_CONTROLE_POSTE'', N''Résultat de contrôle au poste (FE0794)'', NULL, ''RESULTAT_CONTROLE_POSTE'', ''RESULTAT_CONTROLE_POSTE''),
    (''VERIF_MACHINE'', N''Rapport de vérification banc d''''essai BEE (FE0795)'', NULL, ''VERIF_MACHINE'', ''VERIF_MACHINE''),
    (''VERIF_MACHINE_MAS'', N''Rapport de vérification machine d''''assemblage MAS19 (FE0820)'', NULL, ''VERIF_MACHINE'', ''VERIF_MACHINE''),
    (''VERIF_MACHINE_MAS22'', N''Rapport de vérification machine d''''assemblage MAS22 (FE0529)'', NULL, ''VERIF_MACHINE'', ''VERIF_MACHINE''),
    (''VERIF_MACHINE_MAS26'', N''Rapport de vérification machine d''''assemblage MAS26 (FE0525)'', NULL, ''VERIF_MACHINE'', ''VERIF_MACHINE''),
    (''VERIF_MACHINE_SER05'', N''Rapport de vérification machine de marquage SER05 (FE0798)'', NULL, ''VERIF_MACHINE'', ''VERIF_MACHINE''),
    (''PLAN_ASS_RESULTATS'', N''Résultats du contrôle en cours d''''assemblage — poste 71 (FE0816)'', NULL, ''PLAN_ASS'', ''RESULTAT_CONTROLE_ASSEMBLAGE''),
    (''PLAN_ASS_RESULTATS_PAS72'', N''Résultats du contrôle en cours d''''assemblage — postes 72 et 78 (FE0817)'', NULL, ''PLAN_ASS'', ''RESULTAT_CONTROLE_ASSEMBLAGE''),
    (''REGISTRE_TRACA'', N''Registre de traçabilité assemblage (FE0129)'', NULL, ''REGISTRE_TRACA'', NULL)
) AS v (Code, Name, DocumentTypeCode, ExecutionDocumentTypeCode, FormRole) ON t.Code = v.Code
WHEN MATCHED THEN UPDATE SET Name = v.Name, FormRole = v.FormRole
WHEN NOT MATCHED THEN INSERT (Code, Name, DocumentTypeCode, ExecutionDocumentTypeCode, FormRole)
    VALUES (v.Code, v.Name, v.DocumentTypeCode, v.ExecutionDocumentTypeCode, v.FormRole);');

-- ─────────────────────────── 4. Rattachements de départ ───────────────────────────
IF OBJECT_ID('dbo.ReportTemplateInitialForm') IS NULL
    EXEC(N'CREATE TABLE dbo.ReportTemplateInitialForm (
        FormCodeReference  VARCHAR(30) PRIMARY KEY,
        ReportTemplateCode VARCHAR(30) NOT NULL REFERENCES dbo.ReportTemplate(Code)
    );');
EXEC(N'
INSERT INTO dbo.ReportTemplateInitialForm (FormCodeReference, ReportTemplateCode)
SELECT v.FormCodeReference, v.ReportTemplateCode
FROM (VALUES
    (''FE-ASS-MANU'', ''PLAN_ASS''), (''FE-ASS-PISTON'', ''PLAN_ASS''), (''FE-ASS-RBGFA-BAC-01'', ''PLAN_ASS''), (''FE-ASS-RBGFA-BAC-02'', ''PLAN_ASS''),
    (''FE-PF-RBGFA_SOUPAPE_PAS71'', ''PLAN_ASS''), (''FE-PF-RBGFA_SOUPAPE_PAS72'', ''PLAN_ASS''), (''FE-PF-RBGFA_SOUPAPE_PAS78'', ''PLAN_ASS''),
    (''FE-RC-ENCF-ASS-AUTO'', ''PLAN_ASS_RESULTATS''), (''FE-RC-ENCF-ASS-PAS71_SOUPAPE'', ''PLAN_ASS_RESULTATS''),
    (''FE-RC-ENCF-ASS-MANU'', ''PLAN_ASS_RESULTATS_PAS72''), (''FE-RC-ENCF-ASS-PAS72_SOUPAPE'', ''PLAN_ASS_RESULTATS_PAS72''),
    (''FE-RC-ENCF-ASS-PAS78_SOUPAPE'', ''PLAN_ASS_RESULTATS_PAS72''),
    (''FE-VM-BEE22'', ''VERIF_MACHINE''), (''FE-VM-BEE46'', ''VERIF_MACHINE''), (''FE-VM-BEE47'', ''VERIF_MACHINE''),
    (''FE-VM-MAS19'', ''VERIF_MACHINE_MAS''), (''FE-VM-MAS22'', ''VERIF_MACHINE_MAS22''), (''FE-VM-MAS26'', ''VERIF_MACHINE_MAS26''),
    (''FE-VM-SER05'', ''VERIF_MACHINE_SER05''),
    (''FE-RC-PAS71'', ''RESULTAT_CONTROLE_POSTE''), (''FE-RC-PAS71_SOUPAPE'', ''RESULTAT_CONTROLE_POSTE''), (''FE-RC-PAS72'', ''RESULTAT_CONTROLE_POSTE''),
    (''FE-RC-PAS72_SOUPAPE'', ''RESULTAT_CONTROLE_POSTE''), (''FE-RC-PAS78'', ''RESULTAT_CONTROLE_POSTE''), (''FE-RC-PAS78_SOUPAPE'', ''RESULTAT_CONTROLE_POSTE''),
    (''FE-RC-PAS79'', ''RESULTAT_CONTROLE_POSTE''), (''FE-ECHAN-01'', ''ECHANTILLONNAGE'')
) v (FormCodeReference, ReportTemplateCode)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ReportTemplateInitialForm i WHERE i.FormCodeReference = v.FormCodeReference);');

-- Plans de vérification sans formulaire : rattachés au formulaire de leur machine (FE-VM-machine), qui porte leur modèle.
EXEC(N'
UPDATE d SET FormulaireId = f.Id
FROM dbo.Document_Entete d
JOIN dbo.Ref_Formulaire f ON f.CodeReference = ''FE-VM-'' + d.MachineCode AND f.Statut <> ''ARCHIVE''
WHERE d.TypeDocumentCode = ''VERIF_MACHINE'' AND d.FormulaireId IS NULL;
UPDATE f SET Statut = ''ACTIF''
FROM dbo.Ref_Formulaire f
WHERE f.Statut = ''BROUILLON'' AND EXISTS (SELECT 1 FROM dbo.Document_Entete d WHERE d.FormulaireId = f.Id);');

-- ─────────────────────────── 5. Règles des rattachements ───────────────────────────
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
END;');

COMMIT TRANSACTION;
PRINT 'Migration 2026-10-15 (modèles rattachés aux formulaires) appliquée.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

SET NOEXEC OFF;
GO
