-- =================================================================================
-- SopalTraceDB — OUTIL DE DÉVELOPPEMENT : repartir sans formulaires ni modèles Excel
--
-- Pour découvrir le fonctionnement pas à pas : le superviseur qualité crée lui-même ses formulaires (page Formulaires),
-- l'administrateur leur envoie et rattache leurs modèles Excel (page Modèles Excel), puis les documents sont créés.
--
-- À exécuter sur une base NEUVE (scripts 01, 02, 03, et 04 si besoin), avant tout usage : la base ne doit contenir aucun
-- document créé par un utilisateur ni aucune fiche d'OF (la migration s'arrête sinon). Les plans de vérification machine
-- de démonstration (script 04), liés aux formulaires de départ, sont retirés avec eux.
--
-- Vide : formulaires (colonnes, valeurs proposées, équipes, journal des codes), rattachements de départ, versions des
-- modèles Excel, leurs rattachements et les PDF produits. Garde : le catalogue des modèles (types de documents qu'un
-- fichier Excel peut mettre en forme), les référentiels, les utilisateurs, les données Sage.
--
-- Démarrer ensuite l'API avec Rapports__ImporterModelesLivres=false, sinon elle réimporte les modèles livrés.
-- JAMAIS EN PRODUCTION. Tout ou rien (transaction).
-- =================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
USE SopalTraceDB;
GO

IF EXISTS (SELECT 1 FROM dbo.Exec_ControleDocumentStatut)
   OR EXISTS (SELECT 1 FROM dbo.Document_Entete WHERE NOT (TypeDocumentCode = 'VERIF_MACHINE' AND CreePar = 'SYSTEM'))
BEGIN
    RAISERROR('Base déjà utilisée (documents ou fiches d''OF) : recréez-la avec les scripts 01 à 04, puis relancez cet outil.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRY
BEGIN TRANSACTION;

-- Protections ISO (suppression interdite) suspendues le temps du nettoyage, rétablies avant la validation.
DISABLE TRIGGER TR_GeneratedDocument_Immutable ON dbo.GeneratedDocument;
DISABLE TRIGGER TR_ReportTemplateAssignment_Rules ON dbo.ReportTemplateAssignment;
DISABLE TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion;
DISABLE TRIGGER TR_FormCodeChange_Immutable ON dbo.FormCodeChange;
DISABLE TRIGGER trg_no_del_Ref_Formulaire_ColonneDef ON dbo.Ref_Formulaire_ColonneDef;
DISABLE TRIGGER trg_no_del_Ref_Formulaire_Equipe ON dbo.Ref_Formulaire_Equipe;

-- 1. Modèles Excel : PDF produits, rattachements, versions (le catalogue ReportTemplate reste).
DELETE FROM dbo.GeneratedDocument;
IF OBJECT_ID('dbo.ReportTemplateRequest') IS NOT NULL DELETE FROM dbo.ReportTemplateRequest;
DELETE FROM dbo.ReportTemplateAssignment;
DELETE FROM dbo.ReportTemplateVersion;
DELETE FROM dbo.ReportTemplateInitialForm;

-- 2. Plans de vérification machine de démonstration (liés aux formulaires de départ).
DECLARE @Plans TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);
INSERT INTO @Plans SELECT Id FROM dbo.Document_Entete;
DELETE p FROM dbo.Document_VerifMachine_MatricePiece p
    JOIN dbo.Document_VerifMachine_Echeance e ON e.Id = p.EcheanceId
    JOIN dbo.Document_Ligne l ON l.Id = e.LigneId WHERE l.EnteteId IN (SELECT Id FROM @Plans);
DELETE e FROM dbo.Document_VerifMachine_Echeance e
    JOIN dbo.Document_Ligne l ON l.Id = e.LigneId WHERE l.EnteteId IN (SELECT Id FROM @Plans);
DELETE FROM dbo.Document_Ligne WHERE EnteteId IN (SELECT Id FROM @Plans);
DELETE FROM dbo.Document_VerifMachine_Famille WHERE EnteteId IN (SELECT Id FROM @Plans);
DELETE FROM dbo.Document_Entete WHERE Id IN (SELECT Id FROM @Plans);

-- 3. Formulaires.
DELETE FROM dbo.FormCodeChange;
DELETE v FROM dbo.ColumnAllowedValue v JOIN dbo.Ref_Formulaire_ColonneDef c ON c.Id = v.ColumnDefinitionId;
DELETE FROM dbo.Ref_Formulaire_ColonneDef;
DELETE FROM dbo.Ref_Formulaire_Equipe;
DELETE FROM dbo.Ref_Formulaire;

ENABLE TRIGGER TR_GeneratedDocument_Immutable ON dbo.GeneratedDocument;
ENABLE TRIGGER TR_ReportTemplateAssignment_Rules ON dbo.ReportTemplateAssignment;
ENABLE TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion;
ENABLE TRIGGER TR_FormCodeChange_Immutable ON dbo.FormCodeChange;
ENABLE TRIGGER trg_no_del_Ref_Formulaire_ColonneDef ON dbo.Ref_Formulaire_ColonneDef;
ENABLE TRIGGER trg_no_del_Ref_Formulaire_Equipe ON dbo.Ref_Formulaire_Equipe;

COMMIT TRANSACTION;
PRINT 'Formulaires et modèles vidés : créez les formulaires (page Formulaires, superviseur qualité) puis rattachez leurs modèles (page Modèles Excel, administrateur).';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
GO

SET NOEXEC OFF;
GO
