-- =================================================================================
-- SopalTraceDB — Données de DÉMONSTRATION (facultatif, jamais en production)
-- Préparations magasin d'exemple, plans de vérification machine, OF d'exemple. Signées par le compte technique SYSTEM.
-- =================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requis pour écrire dans les tables à index filtrés
GO
USE SopalTraceDB;
GO

-- Préparation Magasinier → rend l'OF visible dans le tableau de bord Opérateur
INSERT INTO dbo.Mag_PreparationOF (Id, NumeroOF, MatriculeMagasinier, Statut, DateDebut, DateFin)
VALUES 
    (NEWID(), 'OF26021488', '33333', 'TERMINE', '2026-02-03', '2026-02-03');

-- Préparation Magasinier pour l'OF du Corps → le rend visible dans le tableau de bord Opérateur
INSERT INTO dbo.Mag_PreparationOF (Id, NumeroOF, MatriculeMagasinier, Statut, DateDebut, DateFin)
VALUES 
    (NEWID(), 'OF26029030', '33333', 'TERMINE', '2026-05-19', '2026-05-19');

-- NOUVEL OF EXEMPLE : OF24120004 pour Corps r-gaz moyenne carotte (2630201.1.9)
DECLARE @PrepId2412 UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.Mag_PreparationOF (Id, NumeroOF, MatriculeMagasinier, Statut, DateDebut, DateFin)
VALUES 
    (@PrepId2412, 'OF24120004', '33333', 'TERMINE', '2024-12-09', '2024-12-09');

-- Enregistrement du Lot scanné par le magasinier pour la nomenclature M1180RP
INSERT INTO dbo.Mag_PreparationOF_Lot (Id, PreparationOFId, CodeArticle, NumeroLotScanne, QualityControlReport, Quantite, DateScan)
VALUES 
    (NEWID(), @PrepId2412, 'M1180RP', '24120004', '61946', 1000, '2024-12-09');

-- 3. Préparation Magasinier
DECLARE @PrepIdN25B0 UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.Mag_PreparationOF (Id, NumeroOF, MatriculeMagasinier, Statut, DateDebut, DateFin)
VALUES 
    (@PrepIdN25B0, 'OF2606N25B0', '33333', 'TERMINE', '2026-06-29', '2026-06-29');

-- 4. Lot scanné par le magasinier
INSERT INTO dbo.Mag_PreparationOF_Lot (Id, PreparationOFId, CodeArticle, NumeroLotScanne, QualityControlReport, Quantite, DateScan)
VALUES 
    (NEWID(), @PrepIdN25B0, 'M1180RP', 'LOT-N25B0-TEST', 'QC-99999', 2000, '2026-06-29');

DECLARE @PrepIdASS01 UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.Mag_PreparationOF (Id, NumeroOF, MatriculeMagasinier, Statut, DateDebut, DateFin)
VALUES (@PrepIdASS01, 'OF2607ASS01', '33333', 'TERMINE', GETDATE(), GETDATE());

-- Étiquette avec le n° de rapport de contrôle : point de départ de la recherche des documents fournisseur de l'OF.
INSERT INTO dbo.Mag_PreparationOF_Lot (Id, PreparationOFId, CodeArticle, NumeroLotScanne, QualityControlReport, Quantite, DateScan)
VALUES (NEWID(), @PrepIdASS01, '2903201-1', 'LOT-CORPS-01', 'RC2607001', 1000, GETDATE());

DECLARE @PrepIdASS02 UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.Mag_PreparationOF (Id, NumeroOF, MatriculeMagasinier, Statut, DateDebut, DateFin)
VALUES (@PrepIdASS02, 'OF2607ASS02', '33333', 'TERMINE', GETDATE(), GETDATE());

INSERT INTO dbo.Mag_PreparationOF_Lot (Id, PreparationOFId, CodeArticle, NumeroLotScanne, Quantite, DateScan)
VALUES (NEWID(), @PrepIdASS02, '2903202-1', 'LOT-SOUPAPE-02', 500, GETDATE());

-- Ajout de scans magasinier pour OF2607ASS02 (qui a ete declare plus haut)
DECLARE @PrepIdASS02_2 UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Mag_PreparationOF WHERE NumeroOF = 'OF2607ASS02');

INSERT INTO dbo.Mag_PreparationOF_Lot (Id, PreparationOFId, CodeArticle, NumeroLotScanne, Quantite, DateScan)
VALUES 
(NEWID(), @PrepIdASS02_2, 'ECROU-001', 'LOT-ECROUS-01', 500, GETDATE()),
(NEWID(), @PrepIdASS02_2, 'LIMITEUR-001', 'LOT-LIMITEUR-01', 500, GETDATE()),
(NEWID(), @PrepIdASS02_2, 'PISTON-001', 'LOT-PISTON-01', 500, GETDATE()),
(NEWID(), @PrepIdASS02_2, 'GOUPILLE-001', 'LOT-GOUPILLE-01', 500, GETDATE()),
(NEWID(), @PrepIdASS02_2, 'GOUPILLE-001', 'LOT-GOUPILLE-02', 500, GETDATE()),
(NEWID(), @PrepIdASS02_2, 'JOINT-ANTI-SERREUR', 'LOT-JOINT-01', 500, GETDATE());
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requis pour écrire dans les tables à index filtrés
GO
USE SopalTraceDB;
GO

-- ================================================================================
-- Plan de vérification machine MAS19
-- ================================================================================
-- Nettoyage au cas où on ré-exécute
DECLARE @MachineCode NVARCHAR(50) = 'MAS19';

DECLARE @OldDocId UNIQUEIDENTIFIER;
DECLARE OldDocCursor CURSOR FOR SELECT Id FROM [Document_Entete] WHERE TypeDocumentCode = 'VERIF_MACHINE' AND MachineCode = @MachineCode;
OPEN OldDocCursor;
FETCH NEXT FROM OldDocCursor INTO @OldDocId;
WHILE @@FETCH_STATUS = 0
BEGIN
    DELETE FROM [Document_VerifMachine_MatricePiece] WHERE EcheanceId IN (
        SELECT e.Id FROM [Document_VerifMachine_Echeance] e
        JOIN [Document_Ligne] l ON e.LigneId = l.Id
        WHERE l.EnteteId = @OldDocId
    );
    DELETE FROM [Document_VerifMachine_Echeance] WHERE LigneId IN (
        SELECT Id FROM [Document_Ligne] WHERE EnteteId = @OldDocId
    );
    DELETE FROM [Document_Ligne] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_VerifMachine_Famille] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_Entete] WHERE Id = @OldDocId;
    
    FETCH NEXT FROM OldDocCursor INTO @OldDocId;
END
CLOSE OldDocCursor;
DEALLOCATE OldDocCursor;

-- 1. La machine est créée par 02_SeedData_SopalTraceDB.sql
IF NOT EXISTS (SELECT 1 FROM [Machine] WHERE CodeMachine = @MachineCode)
    THROW 50000, 'Machine MAS19 absente : exécuter d''abord 02_SeedData_SopalTraceDB.sql.', 1;

-- 2. Ensure Periodicites
DECLARE @PerDemId UNIQUEIDENTIFIER, @PerPauseId UNIQUEIDENTIFIER, @PerFinId UNIQUEIDENTIFIER;
SELECT TOP 1 @PerDemId = Id FROM [PeriodiciteMachine] WHERE Code = 'DEM';
IF @PerDemId IS NULL BEGIN SET @PerDemId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerDemId, 'DEM', 'Au démarrage', 1, 1, 'DEMARRAGE'); END

SELECT TOP 1 @PerPauseId = Id FROM [PeriodiciteMachine] WHERE Code = 'PAU';
IF @PerPauseId IS NULL BEGIN SET @PerPauseId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerPauseId, 'PAU', 'Après la pause', 2, 1, 'APRES_PAUSE'); END

SELECT TOP 1 @PerFinId = Id FROM [PeriodiciteMachine] WHERE Code = 'FIN';
IF @PerFinId IS NULL BEGIN SET @PerFinId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerFinId, 'FIN', 'A la fin du poste', 3, 1, 'FIN_POSTE'); END

-- 3. Ensure MoyenDetection
DECLARE @MoyenPRCLS UNIQUEIDENTIFIER, @MoyenPRCLI UNIQUEIDENTIFIER, @MoyenPRNCLS UNIQUEIDENTIFIER, @MoyenPRNCLI UNIQUEIDENTIFIER;

SELECT TOP 1 @MoyenPRCLS = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC_LS';
IF @MoyenPRCLS IS NULL BEGIN SET @MoyenPRCLS = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRCLS, 'PRC_LS', 'PRC LS', 1); END

SELECT TOP 1 @MoyenPRCLI = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC_LI';
IF @MoyenPRCLI IS NULL BEGIN SET @MoyenPRCLI = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRCLI, 'PRC_LI', 'PRC LI', 1); END

SELECT TOP 1 @MoyenPRNCLS = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRNC_LS';
IF @MoyenPRNCLS IS NULL BEGIN SET @MoyenPRNCLS = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRNCLS, 'PRNC_LS', 'PRNC LS', 1); END

SELECT TOP 1 @MoyenPRNCLI = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRNC_LI';
IF @MoyenPRNCLI IS NULL BEGIN SET @MoyenPRNCLI = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRNCLI, 'PRNC_LI', 'PRNC LI', 1); END

-- 4. Ensure Familles
DECLARE @Fam25B UNIQUEIDENTIFIER, @Fam25AX UNIQUEIDENTIFIER, @Fam25AW UNIQUEIDENTIFIER, @Fam25AU UNIQUEIDENTIFIER;

SELECT TOP 1 @Fam25B = Id FROM [Ref_FamilleCorps] WHERE Code = 'C_25B0A01';
IF @Fam25B IS NULL BEGIN SET @Fam25B = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@Fam25B, 'C_25B0A01', 'Corps (25B0A01)'); END

SELECT TOP 1 @Fam25AX = Id FROM [Ref_FamilleCorps] WHERE Code = 'C_25AXA01';
IF @Fam25AX IS NULL BEGIN SET @Fam25AX = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@Fam25AX, 'C_25AXA01', 'Corps (25AXA01)'); END

SELECT TOP 1 @Fam25AW = Id FROM [Ref_FamilleCorps] WHERE Code = 'C_25AWA01';
IF @Fam25AW IS NULL BEGIN SET @Fam25AW = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@Fam25AW, 'C_25AWA01', 'Corps (25AWA01)'); END

SELECT TOP 1 @Fam25AU = Id FROM [Ref_FamilleCorps] WHERE Code = 'C_25UA01' OR Code = 'C_25AUA01';
IF @Fam25AU IS NULL BEGIN SET @Fam25AU = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@Fam25AU, 'C_25AUA01', 'Corps (25AUA01)'); END

-- 5. Ensure PieceReference
DECLARE @PR_RBG09 UNIQUEIDENTIFIER, @PR_RBG08 UNIQUEIDENTIFIER, @PR_RBG07 UNIQUEIDENTIFIER, @PR_RBG10 UNIQUEIDENTIFIER;

SELECT TOP 1 @PR_RBG09 = Id FROM [PieceReference] WHERE Code = 'RBG09';
IF @PR_RBG09 IS NULL BEGIN SET @PR_RBG09 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@PR_RBG09, 'RBG09', 'RBG09', 'PRC'); END

SELECT TOP 1 @PR_RBG08 = Id FROM [PieceReference] WHERE Code = 'RBG08';
IF @PR_RBG08 IS NULL BEGIN SET @PR_RBG08 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@PR_RBG08, 'RBG08', 'RBG08', 'PRC'); END

SELECT TOP 1 @PR_RBG07 = Id FROM [PieceReference] WHERE Code = 'RBG07';
IF @PR_RBG07 IS NULL BEGIN SET @PR_RBG07 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@PR_RBG07, 'RBG07', 'RBG07', 'PRNC'); END

SELECT TOP 1 @PR_RBG10 = Id FROM [PieceReference] WHERE Code = 'RBG10';
IF @PR_RBG10 IS NULL BEGIN SET @PR_RBG10 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@PR_RBG10, 'RBG10', 'RBG10', 'PRNC'); END

-- 6. INSERT ENTETE
DECLARE @DocId UNIQUEIDENTIFIER = NEWID();
-- Formulaire de la machine (créé par 02) : le document prend sa version et le formulaire passe actif avec lui.
DECLARE @FormId UNIQUEIDENTIFIER, @FormVersion INT;
SELECT TOP 1 @FormId = Id, @FormVersion = Version FROM [Ref_Formulaire]
WHERE Role = 'VERIF_MACHINE' AND MachineCode = @MachineCode AND Statut <> 'ARCHIVE' ORDER BY Version DESC;
IF @FormId IS NULL
    THROW 50000, 'Formulaire de vérification de la machine absent : exécuter d''abord 02_ReferenceData_SopalTraceDB.sql.', 1;
UPDATE [Ref_Formulaire] SET Statut = 'ACTIF' WHERE Id = @FormId AND Statut = 'BROUILLON';
INSERT INTO [Document_Entete] (TypeDocumentCode, Id, MachineCode, Nom, Version, Statut, FormulaireId, CreePar, CreeLe)
VALUES ('VERIF_MACHINE', @DocId, @MachineCode, 'Document de vérification machine - ' + @MachineCode, @FormVersion, 'ACTIF', @FormId, 'SYSTEM', SYSDATETIME());

-- 7. INSERT FAMILLES (Colonnes)
DECLARE @DocFam1 UNIQUEIDENTIFIER = NEWID();
DECLARE @DocFam2 UNIQUEIDENTIFIER = NEWID();
DECLARE @DocFam3 UNIQUEIDENTIFIER = NEWID();
DECLARE @DocFam4 UNIQUEIDENTIFIER = NEWID();

INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam1, @DocId, @Fam25B, 1);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam2, @DocId, @Fam25AX, 2);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam3, @DocId, @Fam25AW, 3);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam4, @DocId, @Fam25AU, 4);

-- 8. INSERT LIGNE RISQUE
DECLARE @LigneRisqueId UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_Ligne] (Id, EnteteId, TypeLigne, LibelleAffiche, Instruction, OrdreAffiche)
VALUES (@LigneRisqueId, @DocId, 'RISQUE', 'Mauvais tarage de soupape', 'détection de fuite à 26 bar', 1);

-- 9. INSERT ECHEANCES
DECLARE @Periodes TABLE (Id UNIQUEIDENTIFIER, Ordre INT);
INSERT INTO @Periodes VALUES (@PerDemId, 1), (@PerPauseId, 2), (@PerFinId, 3);

DECLARE @CurrentPerId UNIQUEIDENTIFIER;
DECLARE @PerOrdre INT;

DECLARE PerCursor CURSOR FOR SELECT Id, Ordre FROM @Periodes ORDER BY Ordre;
OPEN PerCursor;
FETCH NEXT FROM PerCursor INTO @CurrentPerId, @PerOrdre;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Insert the 4 Echeances
    DECLARE @Ech1 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Ech2 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Ech3 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Ech4 UNIQUEIDENTIFIER = NEWID();

    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech1, @LigneRisqueId, @CurrentPerId, @MoyenPRCLS, 1);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech2, @LigneRisqueId, @CurrentPerId, @MoyenPRCLI, 2);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech3, @LigneRisqueId, @CurrentPerId, @MoyenPRNCLS, 3);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech4, @LigneRisqueId, @CurrentPerId, @MoyenPRNCLI, 4);

    -- Insert MatricePiece ONLY for @Fam25B (the other 3 columns are crossed out in the image)
    INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @Ech1, @DocFam1, @PR_RBG09, 'PRC');
    INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @Ech2, @DocFam1, @PR_RBG08, 'PRC');
    INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @Ech3, @DocFam1, @PR_RBG07, 'PRNC');
    INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @Ech4, @DocFam1, @PR_RBG10, 'PRNC');

    FETCH NEXT FROM PerCursor INTO @CurrentPerId, @PerOrdre;
END
CLOSE PerCursor;
DEALLOCATE PerCursor;

PRINT 'SUCCES : Le document VerifMachine MAS19 a été généré !';
GO

-- ================================================================================
-- Plan de vérification machine MAS22
-- ================================================================================
-- Nettoyage au cas où on ré-exécute
DECLARE @MachineCode NVARCHAR(50) = 'MAS22';

DECLARE @OldDocId UNIQUEIDENTIFIER;
DECLARE OldDocCursor CURSOR FOR SELECT Id FROM [Document_Entete] WHERE TypeDocumentCode = 'VERIF_MACHINE' AND MachineCode = @MachineCode;
OPEN OldDocCursor;
FETCH NEXT FROM OldDocCursor INTO @OldDocId;
WHILE @@FETCH_STATUS = 0
BEGIN
    DELETE FROM [Document_VerifMachine_MatricePiece] WHERE EcheanceId IN (
        SELECT e.Id FROM [Document_VerifMachine_Echeance] e
        JOIN [Document_Ligne] l ON e.LigneId = l.Id
        WHERE l.EnteteId = @OldDocId
    );
    DELETE FROM [Document_VerifMachine_Echeance] WHERE LigneId IN (
        SELECT Id FROM [Document_Ligne] WHERE EnteteId = @OldDocId
    );
    DELETE FROM [Document_Ligne] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_VerifMachine_Famille] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_Entete] WHERE Id = @OldDocId;
    
    FETCH NEXT FROM OldDocCursor INTO @OldDocId;
END
CLOSE OldDocCursor;
DEALLOCATE OldDocCursor;

-- 1. Ensure Periodicites
DECLARE @PerDemId UNIQUEIDENTIFIER, @PerPauseId UNIQUEIDENTIFIER, @PerFinId UNIQUEIDENTIFIER;
SELECT TOP 1 @PerDemId = Id FROM [PeriodiciteMachine] WHERE Code = 'DEM';
IF @PerDemId IS NULL BEGIN SET @PerDemId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerDemId, 'DEM', 'Au démarrage', 1, 1, 'DEMARRAGE'); END

SELECT TOP 1 @PerPauseId = Id FROM [PeriodiciteMachine] WHERE Code = 'PAU';
IF @PerPauseId IS NULL BEGIN SET @PerPauseId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerPauseId, 'PAU', 'Après la pause', 2, 1, 'APRES_PAUSE'); END

SELECT TOP 1 @PerFinId = Id FROM [PeriodiciteMachine] WHERE Code = 'FIN';
IF @PerFinId IS NULL BEGIN SET @PerFinId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerFinId, 'FIN', 'A la fin du poste', 3, 1, 'FIN_POSTE'); END

-- 2. Ensure MoyenDetection
DECLARE @MoyenPRC UNIQUEIDENTIFIER, @MoyenPRCFEC UNIQUEIDENTIFIER, @MoyenPRCFENC UNIQUEIDENTIFIER;
SELECT TOP 1 @MoyenPRC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC';
IF @MoyenPRC IS NULL BEGIN SET @MoyenPRC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRC, 'PRC', 'PRC', 1); END

SELECT TOP 1 @MoyenPRCFEC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC+FEC';
IF @MoyenPRCFEC IS NULL BEGIN SET @MoyenPRCFEC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRCFEC, 'PRC+FEC', 'PRC+FEC', 1); END

SELECT TOP 1 @MoyenPRCFENC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC+FENC';
IF @MoyenPRCFENC IS NULL BEGIN SET @MoyenPRCFENC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRCFENC, 'PRC+FENC', 'PRC+FENC', 1); END

-- 3. Ensure Familles
DECLARE @FamRCF01 UNIQUEIDENTIFIER, @FamRCF02 UNIQUEIDENTIFIER, @FamRCF03 UNIQUEIDENTIFIER, @FamRCF04 UNIQUEIDENTIFIER;
SELECT TOP 1 @FamRCF01 = Id FROM [Ref_FamilleCorps] WHERE Code = '30, 35' OR Code = 'C_30_35';
IF @FamRCF01 IS NULL BEGIN SET @FamRCF01 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF01, 'C_30_35', 'Famille Corps (30, 35)'); END
SELECT TOP 1 @FamRCF02 = Id FROM [Ref_FamilleCorps] WHERE Code = '23' OR Code = 'C_23';
IF @FamRCF02 IS NULL BEGIN SET @FamRCF02 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF02, 'C_23', 'Famille Corps (23)'); END
SELECT TOP 1 @FamRCF03 = Id FROM [Ref_FamilleCorps] WHERE Code = '40,43, 44' OR Code = 'C_40_43_44';
IF @FamRCF03 IS NULL BEGIN SET @FamRCF03 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF03, 'C_40_43_44', 'Famille Corps (40,43, 44)'); END
SELECT TOP 1 @FamRCF04 = Id FROM [Ref_FamilleCorps] WHERE Code = '49' OR Code = 'C_49';
IF @FamRCF04 IS NULL BEGIN SET @FamRCF04 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF04, 'C_49', 'Famille Corps (49)'); END

-- 4. Ensure PieceReferences
DECLARE @P_RCF01 UNIQUEIDENTIFIER, @P_RCF02 UNIQUEIDENTIFIER, @P_RCF03 UNIQUEIDENTIFIER, @P_RCF04 UNIQUEIDENTIFIER;
DECLARE @P_TMI01 UNIQUEIDENTIFIER, @P_TMI02 UNIQUEIDENTIFIER, @P_TMI03 UNIQUEIDENTIFIER, @P_TMI04 UNIQUEIDENTIFIER;
DECLARE @P_TMX01 UNIQUEIDENTIFIER, @P_TMX02 UNIQUEIDENTIFIER, @P_TMX03 UNIQUEIDENTIFIER, @P_TMX04 UNIQUEIDENTIFIER;
DECLARE @P_RSG01 UNIQUEIDENTIFIER, @P_RSG02 UNIQUEIDENTIFIER, @P_RSG03 UNIQUEIDENTIFIER, @P_RSG04 UNIQUEIDENTIFIER;
DECLARE @P_RSJ01 UNIQUEIDENTIFIER, @P_RSJ02 UNIQUEIDENTIFIER, @P_RSJ03 UNIQUEIDENTIFIER, @P_RSJ04 UNIQUEIDENTIFIER;
DECLARE @P_FUI24 UNIQUEIDENTIFIER, @P_FUI25 UNIQUEIDENTIFIER;

-- Macro pour insérer des PieceReference rapidement :
-- On ne peut pas le faire par boucle facilement en SQL, donc on écrit les selects.
SELECT TOP 1 @P_RCF01 = Id FROM [PieceReference] WHERE Code = 'RCF01'; IF @P_RCF01 IS NULL BEGIN SET @P_RCF01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RCF01, 'RCF01', 'RCF01', 'PRC'); END
SELECT TOP 1 @P_RCF02 = Id FROM [PieceReference] WHERE Code = 'RCF02'; IF @P_RCF02 IS NULL BEGIN SET @P_RCF02 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RCF02, 'RCF02', 'RCF02', 'PRC'); END
SELECT TOP 1 @P_RCF03 = Id FROM [PieceReference] WHERE Code = 'RCF03'; IF @P_RCF03 IS NULL BEGIN SET @P_RCF03 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RCF03, 'RCF03', 'RCF03', 'PRC'); END
SELECT TOP 1 @P_RCF04 = Id FROM [PieceReference] WHERE Code = 'RCF04'; IF @P_RCF04 IS NULL BEGIN SET @P_RCF04 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RCF04, 'RCF04', 'RCF04', 'PRC'); END

SELECT TOP 1 @P_TMI01 = Id FROM [PieceReference] WHERE Code = 'TMI01'; IF @P_TMI01 IS NULL BEGIN SET @P_TMI01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_TMI01, 'TMI01', 'TMI01', 'PRC'); END
SELECT TOP 1 @P_TMI02 = Id FROM [PieceReference] WHERE Code = 'TMI02'; IF @P_TMI02 IS NULL BEGIN SET @P_TMI02 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_TMI02, 'TMI02', 'TMI02', 'PRC'); END
SELECT TOP 1 @P_TMI03 = Id FROM [PieceReference] WHERE Code = 'TMI03'; IF @P_TMI03 IS NULL BEGIN SET @P_TMI03 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_TMI03, 'TMI03', 'TMI03', 'PRC'); END
SELECT TOP 1 @P_TMI04 = Id FROM [PieceReference] WHERE Code = 'TMI04'; IF @P_TMI04 IS NULL BEGIN SET @P_TMI04 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_TMI04, 'TMI04', 'TMI04', 'PRC'); END

SELECT TOP 1 @P_TMX01 = Id FROM [PieceReference] WHERE Code = 'TMX01'; IF @P_TMX01 IS NULL BEGIN SET @P_TMX01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_TMX01, 'TMX01', 'TMX01', 'PRC'); END
SELECT TOP 1 @P_TMX02 = Id FROM [PieceReference] WHERE Code = 'TMX02'; IF @P_TMX02 IS NULL BEGIN SET @P_TMX02 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_TMX02, 'TMX02', 'TMX02', 'PRC'); END
SELECT TOP 1 @P_TMX03 = Id FROM [PieceReference] WHERE Code = 'TMX03'; IF @P_TMX03 IS NULL BEGIN SET @P_TMX03 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_TMX03, 'TMX03', 'TMX03', 'PRC'); END
SELECT TOP 1 @P_TMX04 = Id FROM [PieceReference] WHERE Code = 'TMX04'; IF @P_TMX04 IS NULL BEGIN SET @P_TMX04 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_TMX04, 'TMX04', 'TMX04', 'PRC'); END

SELECT TOP 1 @P_RSG01 = Id FROM [PieceReference] WHERE Code = 'RSG01'; IF @P_RSG01 IS NULL BEGIN SET @P_RSG01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RSG01, 'RSG01', 'RSG01', 'PRC'); END
SELECT TOP 1 @P_RSG02 = Id FROM [PieceReference] WHERE Code = 'RSG02'; IF @P_RSG02 IS NULL BEGIN SET @P_RSG02 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RSG02, 'RSG02', 'RSG02', 'PRC'); END
SELECT TOP 1 @P_RSG03 = Id FROM [PieceReference] WHERE Code = 'RSG03'; IF @P_RSG03 IS NULL BEGIN SET @P_RSG03 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RSG03, 'RSG03', 'RSG03', 'PRC'); END
SELECT TOP 1 @P_RSG04 = Id FROM [PieceReference] WHERE Code = 'RSG04'; IF @P_RSG04 IS NULL BEGIN SET @P_RSG04 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RSG04, 'RSG04', 'RSG04', 'PRC'); END

SELECT TOP 1 @P_RSJ01 = Id FROM [PieceReference] WHERE Code = 'RSJ01'; IF @P_RSJ01 IS NULL BEGIN SET @P_RSJ01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RSJ01, 'RSJ01', 'RSJ01', 'PRC'); END
SELECT TOP 1 @P_RSJ02 = Id FROM [PieceReference] WHERE Code = 'RSJ02'; IF @P_RSJ02 IS NULL BEGIN SET @P_RSJ02 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RSJ02, 'RSJ02', 'RSJ02', 'PRC'); END
SELECT TOP 1 @P_RSJ03 = Id FROM [PieceReference] WHERE Code = 'RSJ03'; IF @P_RSJ03 IS NULL BEGIN SET @P_RSJ03 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RSJ03, 'RSJ03', 'RSJ03', 'PRC'); END
SELECT TOP 1 @P_RSJ04 = Id FROM [PieceReference] WHERE Code = 'RSJ04'; IF @P_RSJ04 IS NULL BEGIN SET @P_RSJ04 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RSJ04, 'RSJ04', 'RSJ04', 'PRC'); END

SELECT TOP 1 @P_FUI24 = Id FROM [PieceReference] WHERE Code = 'FUI24'; IF @P_FUI24 IS NULL BEGIN SET @P_FUI24 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_FUI24, 'FUI24', 'FUI24', 'FEC'); END
SELECT TOP 1 @P_FUI25 = Id FROM [PieceReference] WHERE Code = 'FUI25'; IF @P_FUI25 IS NULL BEGIN SET @P_FUI25 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_FUI25, 'FUI25', 'FUI25', 'FENC'); END


-- 5. INSERT ENTETE
DECLARE @DocId UNIQUEIDENTIFIER = NEWID();
-- Formulaire de la machine (créé par 02) : le document prend sa version et le formulaire passe actif avec lui.
DECLARE @FormId UNIQUEIDENTIFIER, @FormVersion INT;
SELECT TOP 1 @FormId = Id, @FormVersion = Version FROM [Ref_Formulaire]
WHERE Role = 'VERIF_MACHINE' AND MachineCode = @MachineCode AND Statut <> 'ARCHIVE' ORDER BY Version DESC;
IF @FormId IS NULL
    THROW 50000, 'Formulaire de vérification de la machine absent : exécuter d''abord 02_ReferenceData_SopalTraceDB.sql.', 1;
UPDATE [Ref_Formulaire] SET Statut = 'ACTIF' WHERE Id = @FormId AND Statut = 'BROUILLON';
INSERT INTO [Document_Entete] (TypeDocumentCode, Id, MachineCode, Nom, Version, Statut, FormulaireId, CreePar, CreeLe)
VALUES ('VERIF_MACHINE', @DocId, @MachineCode, 'Document de vérification machine - ' + @MachineCode, @FormVersion, 'ACTIF', @FormId, 'SYSTEM', SYSDATETIME());

-- 6. INSERT FAMILLES (Colonnes)
DECLARE @DocFam1 UNIQUEIDENTIFIER = NEWID();
DECLARE @DocFam2 UNIQUEIDENTIFIER = NEWID();
DECLARE @DocFam3 UNIQUEIDENTIFIER = NEWID();
DECLARE @DocFam4 UNIQUEIDENTIFIER = NEWID();

INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam1, @DocId, @FamRCF01, 1);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam2, @DocId, @FamRCF02, 2);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam3, @DocId, @FamRCF03, 3);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam4, @DocId, @FamRCF04, 4);

-- 7. INSERT LIGNES
DECLARE @L1 UNIQUEIDENTIFIER = NEWID();
DECLARE @L2 UNIQUEIDENTIFIER = NEWID();
DECLARE @L3 UNIQUEIDENTIFIER = NEWID();
DECLARE @L4 UNIQUEIDENTIFIER = NEWID();
DECLARE @L5 UNIQUEIDENTIFIER = NEWID();
DECLARE @L6 UNIQUEIDENTIFIER = NEWID();
DECLARE @L7 UNIQUEIDENTIFIER = NEWID();

INSERT INTO [Document_Ligne] (Id, EnteteId, TypeLigne, LibelleAffiche, Instruction, OrdreAffiche) VALUES (@L1, @DocId, 'CONFORMITE', 'Pièce référence conforme', 'Passage de la pièce dans les stations 2, 4 et 7', 1);
INSERT INTO [Document_Ligne] (Id, EnteteId, TypeLigne, LibelleAffiche, Instruction, OrdreAffiche) VALUES (@L2, @DocId, 'RISQUE', 'Nombre de tour < 2,8', 'Servomoteur dans la 6''eme Station', 2);
INSERT INTO [Document_Ligne] (Id, EnteteId, TypeLigne, LibelleAffiche, Instruction, OrdreAffiche) VALUES (@L3, @DocId, 'RISQUE', 'Nombre de tour > 3,8', 'Servomoteur dans la 6''eme Station', 3);
INSERT INTO [Document_Ligne] (Id, EnteteId, TypeLigne, LibelleAffiche, Instruction, OrdreAffiche) VALUES (@L4, @DocId, 'RISQUE', 'Robinet sans goupille carré', 'Capteur d''objet photo Sensor dans la 7''éme station', 4);
INSERT INTO [Document_Ligne] (Id, EnteteId, TypeLigne, LibelleAffiche, Instruction, OrdreAffiche) VALUES (@L5, @DocId, 'RISQUE', 'Manque de joint auto-serreur', 'Capteur d''objet photo Sensor dans la 7''éme station', 5);
INSERT INTO [Document_Ligne] (Id, EnteteId, TypeLigne, LibelleAffiche, Instruction, OrdreAffiche) VALUES (@L6, @DocId, 'RISQUE', 'Etanchéité Interne', 'Test d''étanchéité interne 30 bars du robinet fermé', 6);
INSERT INTO [Document_Ligne] (Id, EnteteId, TypeLigne, LibelleAffiche, Instruction, OrdreAffiche) VALUES (@L7, @DocId, 'RISQUE', 'Fuite interne', 'Test d''étanchéité interne 0,1 bar du robinet fermé', 7);

-- 8. ECHEANCES ET MATRICE
-- L1: DEM, PAU -> RCF01..04
DECLARE @L1E1 UNIQUEIDENTIFIER = NEWID(), @L1E2 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L1E1, @L1, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L1E2, @L1, @PerPauseId, @MoyenPRC, 2);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L1E1, @DocFam1, @P_RCF01, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L1E1, @DocFam2, @P_RCF02, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L1E1, @DocFam3, @P_RCF03, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L1E1, @DocFam4, @P_RCF04, 'PRC');
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L1E2, @DocFam1, @P_RCF01, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L1E2, @DocFam2, @P_RCF02, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L1E2, @DocFam3, @P_RCF03, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L1E2, @DocFam4, @P_RCF04, 'PRC');

-- L2: DEM -> TMI01..04
DECLARE @L2E1 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L2E1, @L2, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L2E1, @DocFam1, @P_TMI01, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L2E1, @DocFam2, @P_TMI02, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L2E1, @DocFam3, @P_TMI03, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L2E1, @DocFam4, @P_TMI04, 'PRC');

-- L3: DEM -> TMX01..04
DECLARE @L3E1 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L3E1, @L3, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L3E1, @DocFam1, @P_TMX01, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L3E1, @DocFam2, @P_TMX02, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L3E1, @DocFam3, @P_TMX03, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L3E1, @DocFam4, @P_TMX04, 'PRC');

-- L4: DEM -> RSG01..04
DECLARE @L4E1 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L4E1, @L4, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L4E1, @DocFam1, @P_RSG01, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L4E1, @DocFam2, @P_RSG02, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L4E1, @DocFam3, @P_RSG03, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L4E1, @DocFam4, @P_RSG04, 'PRC');

-- L5: DEM -> RSJ01..04
DECLARE @L5E1 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L5E1, @L5, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L5E1, @DocFam1, @P_RSJ01, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L5E1, @DocFam2, @P_RSJ02, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L5E1, @DocFam3, @P_RSJ03, 'PRC'); INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @L5E1, @DocFam4, @P_RSJ04, 'PRC');

-- L6 & L7: Etanchéité Interne & Fuite interne
-- They both use DEM, PAU, FIN and have PRC, PRC_FEC, PRC_FENC.
-- They both use RCF01..04 and FUI24, FUI25.

DECLARE @CurrentLigne UNIQUEIDENTIFIER;
DECLARE CurLigne CURSOR FOR SELECT @L6 UNION ALL SELECT @L7;
OPEN CurLigne;
FETCH NEXT FROM CurLigne INTO @CurrentLigne;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- DEM
    DECLARE @E1 UNIQUEIDENTIFIER = NEWID(), @E2 UNIQUEIDENTIFIER = NEWID(), @E3 UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E1, @CurrentLigne, @PerDemId, @MoyenPRC, 1);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E2, @CurrentLigne, @PerDemId, @MoyenPRCFEC, 2);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E3, @CurrentLigne, @PerDemId, @MoyenPRCFENC, 3);
    
    -- PAU
    DECLARE @E4 UNIQUEIDENTIFIER = NEWID(), @E5 UNIQUEIDENTIFIER = NEWID(), @E6 UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E4, @CurrentLigne, @PerPauseId, @MoyenPRC, 4);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E5, @CurrentLigne, @PerPauseId, @MoyenPRCFEC, 5);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E6, @CurrentLigne, @PerPauseId, @MoyenPRCFENC, 6);
    
    -- FIN
    DECLARE @E7 UNIQUEIDENTIFIER = NEWID(), @E8 UNIQUEIDENTIFIER = NEWID(), @E9 UNIQUEIDENTIFIER = NEWID();
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E7, @CurrentLigne, @PerFinId, @MoyenPRC, 7);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E8, @CurrentLigne, @PerFinId, @MoyenPRCFEC, 8);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E9, @CurrentLigne, @PerFinId, @MoyenPRCFENC, 9);
    
    -- Fill Matrice for all 9 Echeances
    DECLARE @CurEch UNIQUEIDENTIFIER;
    DECLARE @Ordre INT = 1;
    DECLARE CurEch CURSOR FOR SELECT @E1, 1 UNION ALL SELECT @E2, 2 UNION ALL SELECT @E3, 3 UNION ALL SELECT @E4, 4 UNION ALL SELECT @E5, 5 UNION ALL SELECT @E6, 6 UNION ALL SELECT @E7, 7 UNION ALL SELECT @E8, 8 UNION ALL SELECT @E9, 9 ORDER BY 2;
    OPEN CurEch;
    FETCH NEXT FROM CurEch INTO @CurEch, @Ordre;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Familles
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, @DocFam1, @P_RCF01, 'PRC');
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, @DocFam2, @P_RCF02, 'PRC');
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, @DocFam3, @P_RCF03, 'PRC');
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, @DocFam4, @P_RCF04, 'PRC');
        
        -- Fuite etalons for FEC/FENC (Ordres 2,5,8 -> FEC/FUI24, Ordres 3,6,9 -> FENC/FUI25)
        IF @Ordre IN (2, 5, 8) BEGIN
            INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, NULL, @P_FUI24, 'FEC');
        END
        IF @Ordre IN (3, 6, 9) BEGIN
            INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, NULL, @P_FUI25, 'FENC');
        END
        
        FETCH NEXT FROM CurEch INTO @CurEch, @Ordre;
    END
    CLOSE CurEch;
    DEALLOCATE CurEch;
    
    FETCH NEXT FROM CurLigne INTO @CurrentLigne;
END
CLOSE CurLigne;
DEALLOCATE CurLigne;

PRINT 'SUCCES : Le document VerifMachine MAS22 a été généré !';
GO

-- ================================================================================
-- Plan de vérification machine MAS26
-- ================================================================================
DECLARE @MachineCode NVARCHAR(50) = 'MAS26';
DECLARE @DocId UNIQUEIDENTIFIER = NEWID();

-- Lignes
DECLARE @Ligne1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne3Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne4Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne5Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne6Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne7Id UNIQUEIDENTIFIER = NEWID();

-- 0. CLEANUP OLD DOCUMENTS
DECLARE @OldDocId UNIQUEIDENTIFIER;
DECLARE OldDocCursor CURSOR FOR SELECT Id FROM [Document_Entete] WHERE TypeDocumentCode = 'VERIF_MACHINE' AND MachineCode = @MachineCode;
OPEN OldDocCursor;
FETCH NEXT FROM OldDocCursor INTO @OldDocId;
WHILE @@FETCH_STATUS = 0
BEGIN
    DELETE FROM [Document_VerifMachine_MatricePiece] WHERE EcheanceId IN (SELECT Id FROM [Document_VerifMachine_Echeance] WHERE LigneId IN (SELECT Id FROM [Document_Ligne] WHERE EnteteId = @OldDocId));
    DELETE FROM [Document_VerifMachine_Echeance] WHERE LigneId IN (SELECT Id FROM [Document_Ligne] WHERE EnteteId = @OldDocId);
    DELETE FROM [Document_Ligne] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_VerifMachine_Famille] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_Entete] WHERE Id = @OldDocId;
    FETCH NEXT FROM OldDocCursor INTO @OldDocId;
END
CLOSE OldDocCursor;
DEALLOCATE OldDocCursor;

-- 1. Check Machine
IF NOT EXISTS (SELECT 1 FROM [Machine] WHERE CodeMachine = @MachineCode)
BEGIN
    PRINT 'ATTENTION: La machine MAS26 n''existe pas dans la base de données. Veuillez la créer d''abord via l''interface.';
    RETURN;
END

-- 2. Ensure Periodicite (Uniquement DEMARRAGE pour cette machine)
DECLARE @PerDemId UNIQUEIDENTIFIER;
SELECT TOP 1 @PerDemId = Id FROM [PeriodiciteMachine] WHERE Code = 'DEM';
IF @PerDemId IS NULL BEGIN SET @PerDemId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerDemId, 'DEM', 'Au démarrage de la machine', 1, 1, 'DEMARRAGE'); END

-- 3. Ensure Moyens Detection (Juste PRC)
DECLARE @MoyenPRC UNIQUEIDENTIFIER;
SELECT TOP 1 @MoyenPRC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC';
IF @MoyenPRC IS NULL BEGIN SET @MoyenPRC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRC, 'PRC', 'PRC', 1); END

-- 4. Ensure PieceReferences
DECLARE @P_BCF01 UNIQUEIDENTIFIER, @P_BSJ01 UNIQUEIDENTIFIER, @P_BSB01 UNIQUEIDENTIFIER, @P_BSF01 UNIQUEIDENTIFIER;
DECLARE @P_BJR01 UNIQUEIDENTIFIER, @P_BCL01 UNIQUEIDENTIFIER, @P_BCC01 UNIQUEIDENTIFIER, @P_BCN01 UNIQUEIDENTIFIER, @P_BCQ01 UNIQUEIDENTIFIER;

SELECT TOP 1 @P_BCF01 = Id FROM [PieceReference] WHERE Code = 'BCF01'; IF @P_BCF01 IS NULL BEGIN SET @P_BCF01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BCF01, 'BCF01', 'BCF01', 'PRC'); END
SELECT TOP 1 @P_BSJ01 = Id FROM [PieceReference] WHERE Code = 'BSJ01'; IF @P_BSJ01 IS NULL BEGIN SET @P_BSJ01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BSJ01, 'BSJ01', 'BSJ01', 'PRC'); END
SELECT TOP 1 @P_BSB01 = Id FROM [PieceReference] WHERE Code = 'BSB01'; IF @P_BSB01 IS NULL BEGIN SET @P_BSB01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BSB01, 'BSB01', 'BSB01', 'PRC'); END
SELECT TOP 1 @P_BSF01 = Id FROM [PieceReference] WHERE Code = 'BSF01'; IF @P_BSF01 IS NULL BEGIN SET @P_BSF01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BSF01, 'BSF01', 'BSF01', 'PRC'); END
SELECT TOP 1 @P_BJR01 = Id FROM [PieceReference] WHERE Code = 'BJR01'; IF @P_BJR01 IS NULL BEGIN SET @P_BJR01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BJR01, 'BJR01', 'BJR01', 'PRC'); END
SELECT TOP 1 @P_BCL01 = Id FROM [PieceReference] WHERE Code = 'BCL01'; IF @P_BCL01 IS NULL BEGIN SET @P_BCL01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BCL01, 'BCL01', 'BCL01', 'PRC'); END
SELECT TOP 1 @P_BCC01 = Id FROM [PieceReference] WHERE Code = 'BCC01'; IF @P_BCC01 IS NULL BEGIN SET @P_BCC01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BCC01, 'BCC01', 'BCC01', 'PRC'); END
SELECT TOP 1 @P_BCN01 = Id FROM [PieceReference] WHERE Code = 'BCN01'; IF @P_BCN01 IS NULL BEGIN SET @P_BCN01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BCN01, 'BCN01', 'BCN01', 'PRC'); END
SELECT TOP 1 @P_BCQ01 = Id FROM [PieceReference] WHERE Code = 'BCQ01'; IF @P_BCQ01 IS NULL BEGIN SET @P_BCQ01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BCQ01, 'BCQ01', 'BCQ01', 'PRC'); END


-- 5. INSERT ENTETE
-- Formulaire de la machine (créé par 02) : le document prend sa version et le formulaire passe actif avec lui.
DECLARE @FormId UNIQUEIDENTIFIER, @FormVersion INT;
SELECT TOP 1 @FormId = Id, @FormVersion = Version FROM [Ref_Formulaire]
WHERE Role = 'VERIF_MACHINE' AND MachineCode = @MachineCode AND Statut <> 'ARCHIVE' ORDER BY Version DESC;
IF @FormId IS NULL
    THROW 50000, 'Formulaire de vérification de la machine absent : exécuter d''abord 02_ReferenceData_SopalTraceDB.sql.', 1;
UPDATE [Ref_Formulaire] SET Statut = 'ACTIF' WHERE Id = @FormId AND Statut = 'BROUILLON';
INSERT INTO [Document_Entete] (TypeDocumentCode, Id, MachineCode, Nom, Version, Statut, FormulaireId, CreePar, CreeLe)
VALUES ('VERIF_MACHINE', @DocId, @MachineCode, 'Vérification Machine MAS26', @FormVersion, 'ACTIF', @FormId, 'SYSTEM', SYSDATETIME());

-- Pas de Familles spécifiques pour MAS26 (la matrice n'a pas de FamilleId)

-- 6. INSERT LIGNES
INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne1Id, @DocId, 1, 'Pièce référence conforme', 'Passage de la pièce dans la station 12', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne2Id, @DocId, 2, 'Pièce sans joint siège', 'Capteur d''objet photo Sensor dans la 6''éme station', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne3Id, @DocId, 3, 'Pièce sans bille', 'Capteur d''objet photo Sensor dans la 6''éme station', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne4Id, @DocId, 4, 'Pièce sans joint antifuite', 'Capteur d''objet photo Sensor dans la 12''éme station', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne5Id, @DocId, 5, 'Pièce avec joint anti-fuite non conforme/Mal monté', 'Capteur d''objet photo Sensor dans la 12''éme station', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne6Id, @DocId, 6, 'Contrôle de sertissage', 'Capteur de fin de cours dans la 8 éme station', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne7Id, @DocId, 7, 'Existence d''un passage pour le gaz à travers la boite à clapet', 'Capteur de pression dans la 11''éme station', 'RISQUE');

-- 7. INSERT ECHEANCES ET MATRICES

-- Ligne 1
DECLARE @E1 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E1, @Ligne1Id, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @E1, NULL, @P_BCF01, 'PRC');

-- Ligne 2
DECLARE @E2 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E2, @Ligne2Id, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @E2, NULL, @P_BSJ01, 'PRC');

-- Ligne 3
DECLARE @E3 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E3, @Ligne3Id, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @E3, NULL, @P_BSB01, 'PRC');

-- Ligne 4
DECLARE @E4 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E4, @Ligne4Id, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @E4, NULL, @P_BSF01, 'PRC');

-- Ligne 5
DECLARE @E5 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E5, @Ligne5Id, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @E5, NULL, @P_BJR01, 'PRC');

-- Ligne 6 (A deux pièces : BCL01 et BCC01)
DECLARE @E6 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E6, @Ligne6Id, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @E6, NULL, @P_BCL01, 'PRC');
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @E6, NULL, @P_BCC01, 'PRC');

-- Ligne 7 (A deux pièces : BCN01 et BCQ01)
DECLARE @E7 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@E7, @Ligne7Id, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @E7, NULL, @P_BCN01, 'PRC');
INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @E7, NULL, @P_BCQ01, 'PRC');

PRINT 'SUCCES : Le document VerifMachine MAS26 a été généré dans la base de données !';
GO

-- ================================================================================
-- Plan de vérification machine BEE46
-- ================================================================================
DECLARE @MachineCode NVARCHAR(50) = 'BEE46';
DECLARE @DocId UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne3Id UNIQUEIDENTIFIER = NEWID();

-- 0. CLEANUP OLD DOCUMENTS
DECLARE @OldDocId UNIQUEIDENTIFIER;
DECLARE OldDocCursor CURSOR FOR SELECT Id FROM [Document_Entete] WHERE TypeDocumentCode = 'VERIF_MACHINE' AND MachineCode = @MachineCode;
OPEN OldDocCursor;
FETCH NEXT FROM OldDocCursor INTO @OldDocId;
WHILE @@FETCH_STATUS = 0
BEGIN
    DELETE FROM [Document_VerifMachine_MatricePiece] WHERE EcheanceId IN (SELECT Id FROM [Document_VerifMachine_Echeance] WHERE LigneId IN (SELECT Id FROM [Document_Ligne] WHERE EnteteId = @OldDocId));
    DELETE FROM [Document_VerifMachine_Echeance] WHERE LigneId IN (SELECT Id FROM [Document_Ligne] WHERE EnteteId = @OldDocId);
    DELETE FROM [Document_Ligne] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_VerifMachine_Famille] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_Entete] WHERE Id = @OldDocId;
    FETCH NEXT FROM OldDocCursor INTO @OldDocId;
END
CLOSE OldDocCursor;
DEALLOCATE OldDocCursor;

-- 1. Check Machine
IF NOT EXISTS (SELECT 1 FROM [Machine] WHERE CodeMachine = @MachineCode)
BEGIN
    PRINT 'ATTENTION: La machine BEE46 n''existe pas dans la base de données. Veuillez la créer d''abord via l''interface.';
    RETURN;
END

-- 2. Ensure Periodicites
DECLARE @PerDemId UNIQUEIDENTIFIER, @PerPauseId UNIQUEIDENTIFIER, @PerFinId UNIQUEIDENTIFIER;
SELECT TOP 1 @PerDemId = Id FROM [PeriodiciteMachine] WHERE Code = 'DEM';
IF @PerDemId IS NULL BEGIN SET @PerDemId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerDemId, 'DEM', 'Au démarrage', 1, 1, 'DEMARRAGE'); END

SELECT TOP 1 @PerPauseId = Id FROM [PeriodiciteMachine] WHERE Code = 'PAU';
IF @PerPauseId IS NULL BEGIN SET @PerPauseId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerPauseId, 'PAU', 'Après la pause', 2, 1, 'APRES_PAUSE'); END

SELECT TOP 1 @PerFinId = Id FROM [PeriodiciteMachine] WHERE Code = 'FIN';
IF @PerFinId IS NULL BEGIN SET @PerFinId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerFinId, 'FIN', 'A la fin du poste', 3, 1, 'FIN_POSTE'); END

-- 3. Ensure Moyens Detection
DECLARE @MoyenPRC UNIQUEIDENTIFIER, @MoyenPRCFEC UNIQUEIDENTIFIER, @MoyenPRCFENC UNIQUEIDENTIFIER;
SELECT TOP 1 @MoyenPRC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC';
IF @MoyenPRC IS NULL BEGIN SET @MoyenPRC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRC, 'PRC', 'PRC', 1); END

SELECT TOP 1 @MoyenPRCFEC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC+FEC';
IF @MoyenPRCFEC IS NULL BEGIN SET @MoyenPRCFEC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRCFEC, 'PRC+FEC', 'PRC+FEC', 1); END

SELECT TOP 1 @MoyenPRCFENC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC+FENC';
IF @MoyenPRCFENC IS NULL BEGIN SET @MoyenPRCFENC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRCFENC, 'PRC+FENC', 'PRC+FENC', 1); END

-- 4. Ensure Familles
DECLARE @FamRCF01 UNIQUEIDENTIFIER, @FamRCF02 UNIQUEIDENTIFIER, @FamRCF03 UNIQUEIDENTIFIER, @FamRCF04 UNIQUEIDENTIFIER;
SELECT TOP 1 @FamRCF01 = Id FROM [Ref_FamilleCorps] WHERE Code = '25A8A01';
IF @FamRCF01 IS NULL BEGIN SET @FamRCF01 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF01, '25A8A01', 'Corps (25A8A01)'); END

SELECT TOP 1 @FamRCF02 = Id FROM [Ref_FamilleCorps] WHERE Code = '2588A01';
IF @FamRCF02 IS NULL BEGIN SET @FamRCF02 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF02, '2588A01', 'Corps (2588A01)'); END

SELECT TOP 1 @FamRCF03 = Id FROM [Ref_FamilleCorps] WHERE Code = '2576A01';
IF @FamRCF03 IS NULL BEGIN SET @FamRCF03 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF03, '2576A01', 'Corps (2576A01)'); END

SELECT TOP 1 @FamRCF04 = Id FROM [Ref_FamilleCorps] WHERE Code = '2519A01';
IF @FamRCF04 IS NULL BEGIN SET @FamRCF04 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF04, '2519A01', 'Corps (2519A01)'); END

-- 5. Ensure PieceReferences
DECLARE @P_BCF01 UNIQUEIDENTIFIER, @P_BCF02 UNIQUEIDENTIFIER, @P_BCF03 UNIQUEIDENTIFIER, @P_BCF04 UNIQUEIDENTIFIER;
DECLARE @P_FUI24 UNIQUEIDENTIFIER, @P_FUI25 UNIQUEIDENTIFIER;

SELECT TOP 1 @P_BCF01 = Id FROM [PieceReference] WHERE Code = 'BCF01'; IF @P_BCF01 IS NULL BEGIN SET @P_BCF01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BCF01, 'BCF01', 'BCF01', 'PRC'); END
SELECT TOP 1 @P_BCF02 = Id FROM [PieceReference] WHERE Code = 'BCF02'; IF @P_BCF02 IS NULL BEGIN SET @P_BCF02 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BCF02, 'BCF02', 'BCF02', 'PRC'); END
SELECT TOP 1 @P_BCF03 = Id FROM [PieceReference] WHERE Code = 'BCF03'; IF @P_BCF03 IS NULL BEGIN SET @P_BCF03 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BCF03, 'BCF03', 'BCF03', 'PRC'); END
SELECT TOP 1 @P_BCF04 = Id FROM [PieceReference] WHERE Code = 'BCF04'; IF @P_BCF04 IS NULL BEGIN SET @P_BCF04 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_BCF04, 'BCF04', 'BCF04', 'PRC'); END

SELECT TOP 1 @P_FUI24 = Id FROM [PieceReference] WHERE Code = 'FUI24'; IF @P_FUI24 IS NULL BEGIN SET @P_FUI24 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_FUI24, 'FUI24', 'FUI24', 'FEC'); END
SELECT TOP 1 @P_FUI25 = Id FROM [PieceReference] WHERE Code = 'FUI25'; IF @P_FUI25 IS NULL BEGIN SET @P_FUI25 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_FUI25, 'FUI25', 'FUI25', 'FENC'); END

-- 6. INSERT ENTETE
-- Formulaire de la machine (créé par 02) : le document prend sa version et le formulaire passe actif avec lui.
DECLARE @FormId UNIQUEIDENTIFIER, @FormVersion INT;
SELECT TOP 1 @FormId = Id, @FormVersion = Version FROM [Ref_Formulaire]
WHERE Role = 'VERIF_MACHINE' AND MachineCode = @MachineCode AND Statut <> 'ARCHIVE' ORDER BY Version DESC;
IF @FormId IS NULL
    THROW 50000, 'Formulaire de vérification de la machine absent : exécuter d''abord 02_ReferenceData_SopalTraceDB.sql.', 1;
UPDATE [Ref_Formulaire] SET Statut = 'ACTIF' WHERE Id = @FormId AND Statut = 'BROUILLON';
INSERT INTO [Document_Entete] (TypeDocumentCode, Id, MachineCode, Nom, Version, Statut, FormulaireId, CreePar, CreeLe)
VALUES ('VERIF_MACHINE', @DocId, @MachineCode, 'Vérification Machine BEE46', @FormVersion, 'ACTIF', @FormId, 'SYSTEM', SYSDATETIME());

-- 7. INSERT FAMILLES AU DOC
DECLARE @DocFam1 UNIQUEIDENTIFIER = NEWID(), @DocFam2 UNIQUEIDENTIFIER = NEWID(), @DocFam3 UNIQUEIDENTIFIER = NEWID(), @DocFam4 UNIQUEIDENTIFIER = NEWID();
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam1, @DocId, @FamRCF01, 1);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam2, @DocId, @FamRCF02, 2);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam3, @DocId, @FamRCF03, 3);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam4, @DocId, @FamRCF04, 4);

-- 8. INSERT LIGNES
INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne1Id, @DocId, 1, 'Fuite interne', 'Test d''étanchéité 0,1 bar fermé', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne2Id, @DocId, 2, 'Fuite interne', 'Test d''étanchéité 30 bar fermé', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne3Id, @DocId, 3, 'Fuite interne', 'Test d''étanchéité 30 bar ouvert', 'RISQUE');

-- 9. CURSOR POUR INSERER LES ECHEANCES SUR CHAQUE LIGNE
DECLARE @CurrentLigneId UNIQUEIDENTIFIER;
DECLARE LigneCursor CURSOR FOR SELECT @Ligne1Id UNION ALL SELECT @Ligne2Id UNION ALL SELECT @Ligne3Id;

OPEN LigneCursor;
FETCH NEXT FROM LigneCursor INTO @CurrentLigneId;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @Ech1 UNIQUEIDENTIFIER = NEWID(), @Ech2 UNIQUEIDENTIFIER = NEWID(), @Ech3 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Ech4 UNIQUEIDENTIFIER = NEWID(), @Ech5 UNIQUEIDENTIFIER = NEWID(), @Ech6 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Ech7 UNIQUEIDENTIFIER = NEWID(), @Ech8 UNIQUEIDENTIFIER = NEWID(), @Ech9 UNIQUEIDENTIFIER = NEWID();

    -- Au demarrage
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech1, @CurrentLigneId, @PerDemId, @MoyenPRC, 1);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech2, @CurrentLigneId, @PerDemId, @MoyenPRCFEC, 2);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech3, @CurrentLigneId, @PerDemId, @MoyenPRCFENC, 3);
    
    -- Apres la pause
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech4, @CurrentLigneId, @PerPauseId, @MoyenPRC, 4);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech5, @CurrentLigneId, @PerPauseId, @MoyenPRCFEC, 5);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech6, @CurrentLigneId, @PerPauseId, @MoyenPRCFENC, 6);
    
    -- A la fin du poste
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech7, @CurrentLigneId, @PerFinId, @MoyenPRC, 7);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech8, @CurrentLigneId, @PerFinId, @MoyenPRCFEC, 8);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech9, @CurrentLigneId, @PerFinId, @MoyenPRCFENC, 9);

    -- MATRICES POUR CHAQUE ECHEANCE ET CHAQUE FAMILLE
    DECLARE @CurrentEchId UNIQUEIDENTIFIER;
    DECLARE EchCursor CURSOR FOR SELECT @Ech1 UNION ALL SELECT @Ech2 UNION ALL SELECT @Ech3 UNION ALL SELECT @Ech4 UNION ALL SELECT @Ech5 UNION ALL SELECT @Ech6 UNION ALL SELECT @Ech7 UNION ALL SELECT @Ech8 UNION ALL SELECT @Ech9;
    
    OPEN EchCursor;
    FETCH NEXT FROM EchCursor INTO @CurrentEchId;
    
    DECLARE @EchOrdre INT = 1;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- PRC Familles
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurrentEchId, @DocFam1, @P_BCF01, 'PRC');
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurrentEchId, @DocFam2, @P_BCF02, 'PRC');
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurrentEchId, @DocFam3, @P_BCF03, 'PRC');
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurrentEchId, @DocFam4, @P_BCF04, 'PRC');

        -- Fuite Etalon
        DECLARE @CurrentFuiId UNIQUEIDENTIFIER = NULL;
        DECLARE @Role VARCHAR(10) = NULL;
        
        IF @EchOrdre IN (2, 5, 8) BEGIN SET @CurrentFuiId = @P_FUI24; SET @Role = 'FEC'; END
        IF @EchOrdre IN (3, 6, 9) BEGIN SET @CurrentFuiId = @P_FUI25; SET @Role = 'FENC'; END
        
        IF @CurrentFuiId IS NOT NULL
        BEGIN
            INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurrentEchId, NULL, @CurrentFuiId, @Role);
        END
        
        SET @EchOrdre = @EchOrdre + 1;
        FETCH NEXT FROM EchCursor INTO @CurrentEchId;
    END
    CLOSE EchCursor;
    DEALLOCATE EchCursor;

    FETCH NEXT FROM LigneCursor INTO @CurrentLigneId;
END
CLOSE LigneCursor;
DEALLOCATE LigneCursor;

PRINT 'SUCCES : Le document VerifMachine BEE46 a été généré dans la base de données !';
GO

-- ================================================================================
-- Plan de vérification machine BEE47
-- ================================================================================
DECLARE @MachineCode NVARCHAR(50) = 'BEE47';
DECLARE @DocId UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne3Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Ligne4Id UNIQUEIDENTIFIER = NEWID();

-- 0. CLEANUP OLD DOCUMENTS
DECLARE @OldDocId UNIQUEIDENTIFIER;
DECLARE OldDocCursor CURSOR FOR SELECT Id FROM [Document_Entete] WHERE TypeDocumentCode = 'VERIF_MACHINE' AND MachineCode = 'BEE47';
OPEN OldDocCursor;
FETCH NEXT FROM OldDocCursor INTO @OldDocId;
WHILE @@FETCH_STATUS = 0
BEGIN
    DELETE FROM [Document_VerifMachine_MatricePiece] WHERE EcheanceId IN (SELECT Id FROM [Document_VerifMachine_Echeance] WHERE LigneId IN (SELECT Id FROM [Document_Ligne] WHERE EnteteId = @OldDocId));
    DELETE FROM [Document_VerifMachine_Echeance] WHERE LigneId IN (SELECT Id FROM [Document_Ligne] WHERE EnteteId = @OldDocId);
    DELETE FROM [Document_Ligne] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_VerifMachine_Famille] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_Entete] WHERE Id = @OldDocId;
    FETCH NEXT FROM OldDocCursor INTO @OldDocId;
END
CLOSE OldDocCursor;
DEALLOCATE OldDocCursor;

-- 1. Check Machine
IF NOT EXISTS (SELECT 1 FROM [Machine] WHERE CodeMachine = @MachineCode)
BEGIN
    PRINT 'ATTENTION: La machine BEE47 n''existe pas dans la base de données. Veuillez la créer d''abord via l''interface.';
    RETURN;
END

-- 2. Ensure Periodicites
DECLARE @PerDemId UNIQUEIDENTIFIER, @PerPauseId UNIQUEIDENTIFIER, @PerFinId UNIQUEIDENTIFIER;

SELECT TOP 1 @PerDemId = Id FROM [PeriodiciteMachine] WHERE Code = 'DEM';
IF @PerDemId IS NULL BEGIN SET @PerDemId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerDemId, 'DEM', 'Au démarrage de la machine', 1, 1, 'DEMARRAGE'); END

SELECT TOP 1 @PerPauseId = Id FROM [PeriodiciteMachine] WHERE Code = 'PAU';
IF @PerPauseId IS NULL BEGIN SET @PerPauseId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerPauseId, 'PAU', 'Après la pause', 2, 1, 'APRES_PAUSE'); END

SELECT TOP 1 @PerFinId = Id FROM [PeriodiciteMachine] WHERE Code = 'FIN';
IF @PerFinId IS NULL BEGIN SET @PerFinId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerFinId, 'FIN', 'A la fin du poste', 3, 1, 'FIN_POSTE'); END

-- 3. Ensure Moyens Detection
DECLARE @MoyenPRC UNIQUEIDENTIFIER, @MoyenPRCFEC UNIQUEIDENTIFIER, @MoyenPRCFENC UNIQUEIDENTIFIER;

SELECT TOP 1 @MoyenPRC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC';
IF @MoyenPRC IS NULL BEGIN SET @MoyenPRC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRC, 'PRC', 'PRC', 1); END

SELECT TOP 1 @MoyenPRCFEC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC+FEC' OR Code = 'PRC + FEC';
IF @MoyenPRCFEC IS NULL BEGIN SET @MoyenPRCFEC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRCFEC, 'PRC+FEC', 'PRC+FEC', 1); END

SELECT TOP 1 @MoyenPRCFENC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC+FENC' OR Code = 'PRC + FENC';
IF @MoyenPRCFENC IS NULL BEGIN SET @MoyenPRCFENC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRCFENC, 'PRC+FENC', 'PRC+FENC', 1); END

-- 4. Ensure Familles
DECLARE @FamRCF01 UNIQUEIDENTIFIER, @FamRCF02 UNIQUEIDENTIFIER, @FamRCF03 UNIQUEIDENTIFIER, @FamRCF04 UNIQUEIDENTIFIER;

SELECT TOP 1 @FamRCF01 = Id FROM [Ref_FamilleCorps] WHERE Code = '30, 35' OR Code = 'F_30_35';
IF @FamRCF01 IS NULL BEGIN SET @FamRCF01 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF01, '30, 35', 'Famille Corps (30, 35)'); END

SELECT TOP 1 @FamRCF02 = Id FROM [Ref_FamilleCorps] WHERE Code = '23' OR Code = 'F_23';
IF @FamRCF02 IS NULL BEGIN SET @FamRCF02 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF02, '23', 'Famille Corps (23)'); END

SELECT TOP 1 @FamRCF03 = Id FROM [Ref_FamilleCorps] WHERE Code = '40,43, 44' OR Code = 'FAM_40_43_44';
IF @FamRCF03 IS NULL BEGIN SET @FamRCF03 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF03, '40,43, 44', 'Famille Corps (40, 43, 44)'); END

SELECT TOP 1 @FamRCF04 = Id FROM [Ref_FamilleCorps] WHERE Code = '49' OR Code = 'FAM_49';
IF @FamRCF04 IS NULL BEGIN SET @FamRCF04 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF04, '49', 'Famille Corps (49)'); END

-- 5. Ensure Fuite Etalon
DECLARE @Fui24 UNIQUEIDENTIFIER, @Fui25 UNIQUEIDENTIFIER;

SELECT TOP 1 @Fui24 = Id FROM [PieceReference] WHERE Code = 'FUI24';
IF @Fui24 IS NULL BEGIN SET @Fui24 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@Fui24, 'FUI24', 'FUI24', 'FEC'); END

SELECT TOP 1 @Fui25 = Id FROM [PieceReference] WHERE Code = 'FUI25';
IF @Fui25 IS NULL BEGIN SET @Fui25 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@Fui25, 'FUI25', 'FUI25', 'FENC'); END

DECLARE @PR_RCF01 UNIQUEIDENTIFIER, @PR_RCF02 UNIQUEIDENTIFIER, @PR_RCF03 UNIQUEIDENTIFIER, @PR_RCF04 UNIQUEIDENTIFIER;
SELECT TOP 1 @PR_RCF01 = Id FROM [PieceReference] WHERE Code = 'RCF01';
IF @PR_RCF01 IS NULL BEGIN SET @PR_RCF01 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@PR_RCF01, 'RCF01', 'RCF01', 'PRC'); END
SELECT TOP 1 @PR_RCF02 = Id FROM [PieceReference] WHERE Code = 'RCF02';
IF @PR_RCF02 IS NULL BEGIN SET @PR_RCF02 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@PR_RCF02, 'RCF02', 'RCF02', 'PRC'); END
SELECT TOP 1 @PR_RCF03 = Id FROM [PieceReference] WHERE Code = 'RCF03';
IF @PR_RCF03 IS NULL BEGIN SET @PR_RCF03 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@PR_RCF03, 'RCF03', 'RCF03', 'PRC'); END
SELECT TOP 1 @PR_RCF04 = Id FROM [PieceReference] WHERE Code = 'RCF04';
IF @PR_RCF04 IS NULL BEGIN SET @PR_RCF04 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@PR_RCF04, 'RCF04', 'RCF04', 'PRC'); END


-- 6. INSERT ENTETE
-- Formulaire de la machine (créé par 02) : le document prend sa version et le formulaire passe actif avec lui.
DECLARE @FormId UNIQUEIDENTIFIER, @FormVersion INT;
SELECT TOP 1 @FormId = Id, @FormVersion = Version FROM [Ref_Formulaire]
WHERE Role = 'VERIF_MACHINE' AND MachineCode = @MachineCode AND Statut <> 'ARCHIVE' ORDER BY Version DESC;
IF @FormId IS NULL
    THROW 50000, 'Formulaire de vérification de la machine absent : exécuter d''abord 02_ReferenceData_SopalTraceDB.sql.', 1;
UPDATE [Ref_Formulaire] SET Statut = 'ACTIF' WHERE Id = @FormId AND Statut = 'BROUILLON';
INSERT INTO [Document_Entete] (TypeDocumentCode, Id, MachineCode, Nom, Version, Statut, FormulaireId, CreePar, CreeLe)
VALUES ('VERIF_MACHINE', @DocId, @MachineCode, 'Vérification Machine BEE47', @FormVersion, 'ACTIF', @FormId, 'SYSTEM', SYSDATETIME());

-- 7. INSERT FAMILLES AU DOC
DECLARE @DocFam1 UNIQUEIDENTIFIER = NEWID(), @DocFam2 UNIQUEIDENTIFIER = NEWID(), @DocFam3 UNIQUEIDENTIFIER = NEWID(), @DocFam4 UNIQUEIDENTIFIER = NEWID();

INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam1, @DocId, @FamRCF01, 1);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam2, @DocId, @FamRCF02, 2);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam3, @DocId, @FamRCF03, 3);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam4, @DocId, @FamRCF04, 4);

-- 8. INSERT LIGNES
INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne1Id, @DocId, 1, 'Etanchéité Externe', 'Test d''étanchéité 0,1 bar ouvert Station 1 Unité 1 (BEE47-3)', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne2Id, @DocId, 2, 'Etanchéité Externe', 'Test d''étanchéité 0,1 bar ouvert Station 1 Unité 2 (MAS22-3)', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne3Id, @DocId, 3, 'Etanchéité Externe', 'Test d''étanchéité 30 bar ouvert Station 2 Unité 3 (BEE47-1)', 'RISQUE');

INSERT INTO [Document_Ligne] (Id, EnteteId, OrdreAffiche, LibelleAffiche, Instruction, TypeLigne)
VALUES (@Ligne4Id, @DocId, 4, 'Etanchéité Externe', 'Test d''étanchéité 30 bar ouvert Station 2 Unité 4 (BEE47-2)', 'RISQUE');

-- 9. CURSOR POUR INSERER LES ECHEANCES SUR CHAQUE LIGNE
DECLARE @CurrentLigneId UNIQUEIDENTIFIER;
DECLARE LigneCursor CURSOR FOR SELECT @Ligne1Id UNION ALL SELECT @Ligne2Id UNION ALL SELECT @Ligne3Id UNION ALL SELECT @Ligne4Id;

OPEN LigneCursor;
FETCH NEXT FROM LigneCursor INTO @CurrentLigneId;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @Ech1 UNIQUEIDENTIFIER = NEWID(), @Ech2 UNIQUEIDENTIFIER = NEWID(), @Ech3 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Ech4 UNIQUEIDENTIFIER = NEWID(), @Ech5 UNIQUEIDENTIFIER = NEWID(), @Ech6 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Ech7 UNIQUEIDENTIFIER = NEWID(), @Ech8 UNIQUEIDENTIFIER = NEWID(), @Ech9 UNIQUEIDENTIFIER = NEWID();

    -- Au demarrage
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech1, @CurrentLigneId, @PerDemId, @MoyenPRC, 1);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech2, @CurrentLigneId, @PerDemId, @MoyenPRCFEC, 2);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech3, @CurrentLigneId, @PerDemId, @MoyenPRCFENC, 3);
    
    -- Apres la pause
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech4, @CurrentLigneId, @PerPauseId, @MoyenPRC, 4);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech5, @CurrentLigneId, @PerPauseId, @MoyenPRCFEC, 5);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech6, @CurrentLigneId, @PerPauseId, @MoyenPRCFENC, 6);
    
    -- A la fin du poste
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech7, @CurrentLigneId, @PerFinId, @MoyenPRC, 7);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech8, @CurrentLigneId, @PerFinId, @MoyenPRCFEC, 8);
    INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@Ech9, @CurrentLigneId, @PerFinId, @MoyenPRCFENC, 9);

    -- MATRICES POUR CHAQUE ECHEANCE ET CHAQUE FAMILLE
    DECLARE @CurrentEchId UNIQUEIDENTIFIER;
    DECLARE EchCursor CURSOR FOR SELECT @Ech1 UNION ALL SELECT @Ech2 UNION ALL SELECT @Ech3 UNION ALL SELECT @Ech4 UNION ALL SELECT @Ech5 UNION ALL SELECT @Ech6 UNION ALL SELECT @Ech7 UNION ALL SELECT @Ech8 UNION ALL SELECT @Ech9;
    
    OPEN EchCursor;
    FETCH NEXT FROM EchCursor INTO @CurrentEchId;
    
    DECLARE @EchOrdre INT = 1;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Insert Famille matrices (Role = PRC, PieceRefId = RCF01/02/03/04) for every echeance
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurrentEchId, @DocFam1, @PR_RCF01, 'PRC');
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurrentEchId, @DocFam2, @PR_RCF02, 'PRC');
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurrentEchId, @DocFam3, @PR_RCF03, 'PRC');
        INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurrentEchId, @DocFam4, @PR_RCF04, 'PRC');

        -- Insert Fuite Etalon matrix (FamilleId = NULL) for FEC / FENC echeances
        DECLARE @CurrentFuiId UNIQUEIDENTIFIER = NULL;
        DECLARE @Role VARCHAR(10) = NULL;
        
        IF @EchOrdre IN (2, 5, 8) BEGIN SET @CurrentFuiId = @Fui24; SET @Role = 'FEC'; END
        IF @EchOrdre IN (3, 6, 9) BEGIN SET @CurrentFuiId = @Fui25; SET @Role = 'FENC'; END
        
        IF @CurrentFuiId IS NOT NULL
        BEGIN
            INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurrentEchId, NULL, @CurrentFuiId, @Role);
        END
        
        SET @EchOrdre = @EchOrdre + 1;
        FETCH NEXT FROM EchCursor INTO @CurrentEchId;
    END
    CLOSE EchCursor;
    DEALLOCATE EchCursor;

    FETCH NEXT FROM LigneCursor INTO @CurrentLigneId;
END
CLOSE LigneCursor;
DEALLOCATE LigneCursor;

PRINT 'SUCCES : Le document VerifMachine BEE47 a été généré dans la base de données !';
GO

-- ================================================================================
-- Plan de vérification machine SER05
-- ================================================================================
-- Nettoyage au cas où on ré-exécute
DECLARE @MachineCode NVARCHAR(50) = 'SER05';

DECLARE @OldDocId UNIQUEIDENTIFIER;
DECLARE OldDocCursor CURSOR FOR SELECT Id FROM [Document_Entete] WHERE TypeDocumentCode = 'VERIF_MACHINE' AND MachineCode = @MachineCode;
OPEN OldDocCursor;
FETCH NEXT FROM OldDocCursor INTO @OldDocId;
WHILE @@FETCH_STATUS = 0
BEGIN
    DELETE FROM [Document_VerifMachine_MatricePiece] WHERE EcheanceId IN (
        SELECT e.Id FROM [Document_VerifMachine_Echeance] e
        JOIN [Document_Ligne] l ON e.LigneId = l.Id
        WHERE l.EnteteId = @OldDocId
    );
    DELETE FROM [Document_VerifMachine_Echeance] WHERE LigneId IN (
        SELECT Id FROM [Document_Ligne] WHERE EnteteId = @OldDocId
    );
    DELETE FROM [Document_Ligne] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_VerifMachine_Famille] WHERE EnteteId = @OldDocId;
    DELETE FROM [Document_Entete] WHERE Id = @OldDocId;
    
    FETCH NEXT FROM OldDocCursor INTO @OldDocId;
END
CLOSE OldDocCursor;
DEALLOCATE OldDocCursor;

-- 1. Ensure Periodicites
DECLARE @PerDemId UNIQUEIDENTIFIER, @PerPauseId UNIQUEIDENTIFIER, @PerFinId UNIQUEIDENTIFIER;
SELECT TOP 1 @PerDemId = Id FROM [PeriodiciteMachine] WHERE Code = 'DEM';
IF @PerDemId IS NULL BEGIN SET @PerDemId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerDemId, 'DEM', 'Au démarrage', 1, 1, 'DEMARRAGE'); END

SELECT TOP 1 @PerPauseId = Id FROM [PeriodiciteMachine] WHERE Code = 'PAU';
IF @PerPauseId IS NULL BEGIN SET @PerPauseId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerPauseId, 'PAU', 'Après la pause', 2, 1, 'APRES_PAUSE'); END

SELECT TOP 1 @PerFinId = Id FROM [PeriodiciteMachine] WHERE Code = 'FIN';
IF @PerFinId IS NULL BEGIN SET @PerFinId = NEWID(); INSERT INTO [PeriodiciteMachine] (Id, Code, Libelle, OrdreAffichage, Actif, ControlMoment) VALUES (@PerFinId, 'FIN', 'A la fin du poste', 3, 1, 'FIN_POSTE'); END

-- 2. Ensure MoyenDetection
DECLARE @MoyenPRC UNIQUEIDENTIFIER, @MoyenPRNC UNIQUEIDENTIFIER;
SELECT TOP 1 @MoyenPRC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRC';
IF @MoyenPRC IS NULL BEGIN SET @MoyenPRC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRC, 'PRC', 'PRC', 1); END

SELECT TOP 1 @MoyenPRNC = Id FROM [Ref_MoyenDetection] WHERE Code = 'PRNC';
IF @MoyenPRNC IS NULL BEGIN SET @MoyenPRNC = NEWID(); INSERT INTO [Ref_MoyenDetection] (Id, Code, Designation, Actif) VALUES (@MoyenPRNC, 'PRNC', 'PRNC', 1); END

-- 3. Ensure Familles
DECLARE @FamRCF01 UNIQUEIDENTIFIER, @FamRCF02 UNIQUEIDENTIFIER, @FamRCF03 UNIQUEIDENTIFIER, @FamRCF04 UNIQUEIDENTIFIER;
SELECT TOP 1 @FamRCF01 = Id FROM [Ref_FamilleCorps] WHERE Code = '30, 35' OR Code = 'C_30_35';
IF @FamRCF01 IS NULL BEGIN SET @FamRCF01 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF01, 'C_30_35', 'Famille Corps (30, 35)'); END
SELECT TOP 1 @FamRCF02 = Id FROM [Ref_FamilleCorps] WHERE Code = '23' OR Code = 'C_23';
IF @FamRCF02 IS NULL BEGIN SET @FamRCF02 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF02, 'C_23', 'Famille Corps (23)'); END
SELECT TOP 1 @FamRCF03 = Id FROM [Ref_FamilleCorps] WHERE Code = '40,43, 44' OR Code = 'C_40_43_44';
IF @FamRCF03 IS NULL BEGIN SET @FamRCF03 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF03, 'C_40_43_44', 'Famille Corps (40,43, 44)'); END
SELECT TOP 1 @FamRCF04 = Id FROM [Ref_FamilleCorps] WHERE Code = '49' OR Code = 'C_49';
IF @FamRCF04 IS NULL BEGIN SET @FamRCF04 = NEWID(); INSERT INTO [Ref_FamilleCorps] (Id, Code, Designation) VALUES (@FamRCF04, 'C_49', 'Famille Corps (49)'); END

-- 4. Ensure PieceReferences
DECLARE @P_RCF05 UNIQUEIDENTIFIER, @P_RCF06 UNIQUEIDENTIFIER, @P_RCF07 UNIQUEIDENTIFIER;
DECLARE @P_RCN04 UNIQUEIDENTIFIER, @P_RCN05 UNIQUEIDENTIFIER, @P_RCN06 UNIQUEIDENTIFIER;

SELECT TOP 1 @P_RCF05 = Id FROM [PieceReference] WHERE Code = 'RCF05'; IF @P_RCF05 IS NULL BEGIN SET @P_RCF05 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RCF05, 'RCF05', 'RCF05', 'PRC'); END
SELECT TOP 1 @P_RCF06 = Id FROM [PieceReference] WHERE Code = 'RCF06'; IF @P_RCF06 IS NULL BEGIN SET @P_RCF06 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RCF06, 'RCF06', 'RCF06', 'PRC'); END
SELECT TOP 1 @P_RCF07 = Id FROM [PieceReference] WHERE Code = 'RCF07'; IF @P_RCF07 IS NULL BEGIN SET @P_RCF07 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RCF07, 'RCF07', 'RCF07', 'PRC'); END

SELECT TOP 1 @P_RCN04 = Id FROM [PieceReference] WHERE Code = 'RCN04'; IF @P_RCN04 IS NULL BEGIN SET @P_RCN04 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RCN04, 'RCN04', 'RCN04', 'PRNC'); END
SELECT TOP 1 @P_RCN05 = Id FROM [PieceReference] WHERE Code = 'RCN05'; IF @P_RCN05 IS NULL BEGIN SET @P_RCN05 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RCN05, 'RCN05', 'RCN05', 'PRNC'); END
SELECT TOP 1 @P_RCN06 = Id FROM [PieceReference] WHERE Code = 'RCN06'; IF @P_RCN06 IS NULL BEGIN SET @P_RCN06 = NEWID(); INSERT INTO [PieceReference] (Id, Code, Designation, TypePiece) VALUES (@P_RCN06, 'RCN06', 'RCN06', 'PRNC'); END

-- 5. INSERT ENTETE
DECLARE @DocId UNIQUEIDENTIFIER = NEWID();
-- Formulaire de la machine (créé par 02) : le document prend sa version et le formulaire passe actif avec lui.
DECLARE @FormId UNIQUEIDENTIFIER, @FormVersion INT;
SELECT TOP 1 @FormId = Id, @FormVersion = Version FROM [Ref_Formulaire]
WHERE Role = 'VERIF_MACHINE' AND MachineCode = @MachineCode AND Statut <> 'ARCHIVE' ORDER BY Version DESC;
IF @FormId IS NULL
    THROW 50000, 'Formulaire de vérification de la machine absent : exécuter d''abord 02_ReferenceData_SopalTraceDB.sql.', 1;
UPDATE [Ref_Formulaire] SET Statut = 'ACTIF' WHERE Id = @FormId AND Statut = 'BROUILLON';
INSERT INTO [Document_Entete] (TypeDocumentCode, Id, MachineCode, Nom, Version, Statut, FormulaireId, CreePar, CreeLe)
VALUES ('VERIF_MACHINE', @DocId, @MachineCode, 'Document de vérification machine - ' + @MachineCode, @FormVersion, 'ACTIF', @FormId, 'SYSTEM', SYSDATETIME());

-- 6. INSERT FAMILLES (Colonnes)
-- On insère les 4 colonnes pour que l'interface affiche l'entête de la Famille Corps (49)
DECLARE @DocFam1 UNIQUEIDENTIFIER = NEWID();
DECLARE @DocFam2 UNIQUEIDENTIFIER = NEWID();
DECLARE @DocFam3 UNIQUEIDENTIFIER = NEWID();
DECLARE @DocFam4 UNIQUEIDENTIFIER = NEWID();

INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam1, @DocId, @FamRCF01, 1);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam2, @DocId, @FamRCF02, 2);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam3, @DocId, @FamRCF03, 3);
INSERT INTO [Document_VerifMachine_Famille] (Id, EnteteId, RefFamilleCorpsId, OrdreAffiche) VALUES (@DocFam4, @DocId, @FamRCF04, 4);

-- 7. INSERT LIGNES
DECLARE @L1 UNIQUEIDENTIFIER = NEWID();
DECLARE @L2 UNIQUEIDENTIFIER = NEWID();

-- Méthode 1: Passage de la pièce de référence conforme
INSERT INTO [Document_Ligne] (Id, EnteteId, TypeLigne, LibelleAffiche, Instruction, OrdreAffiche) 
VALUES (@L1, @DocId, 'RISQUE', 'Passage d''une pièce bouchée', 'Passage de la pièce de référence conforme (Présence d''air au niveau du robinet)', 1);

-- Méthode 2: Pas de passage de la pièce de référence non conforme
INSERT INTO [Document_Ligne] (Id, EnteteId, TypeLigne, LibelleAffiche, Instruction, OrdreAffiche) 
VALUES (@L2, @DocId, 'RISQUE', 'Passage d''une pièce bouchée', 'Pas de passage de la pièce de référence non conforme (Absence d''air au niveau du robinet)', 2);

-- 8. ECHEANCES ET MATRICE
-- L1 : DEM, PAU, FIN -> PRC -> RCF05, RCF06, RCF07 (Famille 1, 2, 3)
DECLARE @L1E1 UNIQUEIDENTIFIER = NEWID(), @L1E2 UNIQUEIDENTIFIER = NEWID(), @L1E3 UNIQUEIDENTIFIER = NEWID();
-- Echeances
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L1E1, @L1, @PerDemId, @MoyenPRC, 1);
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L1E2, @L1, @PerPauseId, @MoyenPRC, 2);
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L1E3, @L1, @PerFinId, @MoyenPRC, 3);
-- Matrice
DECLARE @CurEch UNIQUEIDENTIFIER;
DECLARE CurEch CURSOR FOR SELECT @L1E1 UNION ALL SELECT @L1E2 UNION ALL SELECT @L1E3;
OPEN CurEch; FETCH NEXT FROM CurEch INTO @CurEch;
WHILE @@FETCH_STATUS = 0 BEGIN
    INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, @DocFam1, @P_RCF05, 'PRC');
    INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, @DocFam2, @P_RCF06, 'PRC');
    INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, @DocFam3, @P_RCF07, 'PRC');
    -- RIEN pour @DocFam4 (Famille 49)
    FETCH NEXT FROM CurEch INTO @CurEch;
END
CLOSE CurEch; DEALLOCATE CurEch;

-- L2 : DEM, PAU, FIN -> PRNC -> RCN04, RCN05, RCN06 (Famille 1, 2, 3)
DECLARE @L2E1 UNIQUEIDENTIFIER = NEWID(), @L2E2 UNIQUEIDENTIFIER = NEWID(), @L2E3 UNIQUEIDENTIFIER = NEWID();
-- Echeances
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L2E1, @L2, @PerDemId, @MoyenPRNC, 1);
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L2E2, @L2, @PerPauseId, @MoyenPRNC, 2);
INSERT INTO [Document_VerifMachine_Echeance] (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche) VALUES (@L2E3, @L2, @PerFinId, @MoyenPRNC, 3);
-- Matrice
DECLARE CurEch2 CURSOR FOR SELECT @L2E1 UNION ALL SELECT @L2E2 UNION ALL SELECT @L2E3;
OPEN CurEch2; FETCH NEXT FROM CurEch2 INTO @CurEch;
WHILE @@FETCH_STATUS = 0 BEGIN
    INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, @DocFam1, @P_RCN04, 'PRNC');
    INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, @DocFam2, @P_RCN05, 'PRNC');
    INSERT INTO [Document_VerifMachine_MatricePiece] (Id, EcheanceId, FamilleId, PieceRefId, RoleVerif) VALUES (NEWID(), @CurEch, @DocFam3, @P_RCN06, 'PRNC');
    -- RIEN pour @DocFam4 (Famille 49)
    FETCH NEXT FROM CurEch2 INTO @CurEch;
END
CLOSE CurEch2; DEALLOCATE CurEch2;

PRINT 'SUCCES : Le document VerifMachine SER05 a été généré !';
GO

-- ================================================================================
-- OF clôturés Corps / Volant (traçabilité)
-- ================================================================================
-- 1. Insert OF for CORPS (Clôturé)
INSERT INTO dbo.MFGHEAD_OrdreFabrication 
    (NumeroOF, CodeArticle, QuantitePrevue, QuantiteLancee, QuantiteReelle, StatutOF, DateDebut, DateFin)
VALUES 
    ('OFCORPS_CL', '2903201-1', 1000, 1000, 1000, 'TERMINE', SYSDATETIME(), SYSDATETIME());

INSERT INTO dbo.Mag_PreparationOF 
    (Id, NumeroOF, MatriculeMagasinier, Statut, DateDebut, DateFin)
VALUES 
    (NEWID(), 'OFCORPS_CL', '33333', 'TERMINE', SYSDATETIME(), SYSDATETIME());

INSERT INTO dbo.Exec_ControleOF 
    (Id, NumeroOF, OperationCode, TypeOf, Statut, DateDebut, DateFin)
VALUES 
    (NEWID(), 'OFCORPS_CL', 'USINAG', 'FAB', 'CLOTURE', SYSDATETIME(), SYSDATETIME());

-- 2. Insert OF for VOLANT (Clôturé)
INSERT INTO dbo.MFGHEAD_OrdreFabrication 
    (NumeroOF, CodeArticle, QuantitePrevue, QuantiteLancee, QuantiteReelle, StatutOF, DateDebut, DateFin)
VALUES 
    ('OFVOLANT_CL', 'VOLANT-001', 500, 500, 500, 'TERMINE', SYSDATETIME(), SYSDATETIME());

INSERT INTO dbo.Mag_PreparationOF 
    (Id, NumeroOF, MatriculeMagasinier, Statut, DateDebut, DateFin)
VALUES 
    (NEWID(), 'OFVOLANT_CL', '33333', 'TERMINE', SYSDATETIME(), SYSDATETIME());

INSERT INTO dbo.Exec_ControleOF 
    (Id, NumeroOF, OperationCode, TypeOf, Statut, DateDebut, DateFin)
VALUES 
    (NEWID(), 'OFVOLANT_CL', 'USINAG', 'FAB', 'CLOTURE', SYSDATETIME(), SYSDATETIME());
GO
GO

-- Plans de vérification de démonstration : chacun sur le formulaire de sa machine (FE-VM-<machine>), qui porte son modèle.
UPDATE d SET FormulaireId = f.Id
FROM dbo.Document_Entete d
JOIN dbo.Ref_Formulaire f ON f.CodeReference = 'FE-VM-' + d.MachineCode AND f.Statut <> 'ARCHIVE'
WHERE d.TypeDocumentCode = 'VERIF_MACHINE' AND d.FormulaireId IS NULL;
UPDATE f SET Statut = 'ACTIF'
FROM dbo.Ref_Formulaire f
WHERE f.Statut = 'BROUILLON' AND EXISTS (SELECT 1 FROM dbo.Document_Entete d WHERE d.FormulaireId = f.Id);
GO

-- ================================================================================
-- Rôles pré-attribués aux employés de test : à leur inscription (matricule + e-mail de l'ERP),
-- ils reçoivent directement leur lien d'activation. Le matricule 77777 n'en a pas : sa demande
-- passe par la validation d'un administrateur (écran « Gestion des habilitations »).
-- ================================================================================
INSERT INTO dbo.PreassignedRole (Matricule, RoleCode, AssignedBy) VALUES
('66666', 'ADMIN', 'SYSTEM'),
('22222', 'RESPONSABLE_QUALITE', 'SYSTEM'),
('44444', 'SUPERVISEUR_QUALITE', 'SYSTEM'),
('33333', 'MAGASINIER', 'SYSTEM'),
('55555', 'OPERATEUR', 'SYSTEM'),
('88881', 'SERVICE_ACHATS', 'SYSTEM'),
('88882', 'SERVICE_COMMERCIAL', 'SYSTEM'),
('88883', 'SERVICE_QUALITE', 'SYSTEM'),
('88884', 'SERVICE_DI', 'SYSTEM');
GO
