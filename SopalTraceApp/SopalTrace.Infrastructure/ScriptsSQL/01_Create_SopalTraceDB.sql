-- =================================================================================
-- SCRIPT MASTER RESET : SOPALTRACE V6.9.9
-- Cible : Microsoft SQL Server (T-SQL)
-- Inclus : Data Seed complet
-- Modification : FK explicites pour RegleEchantillonnageId dans les sections
--                Correction erreur de syntaxe (virgule finale Plan_PF_Section)
-- =================================================================================

USE master;
GO

IF DB_ID('SopalTraceDB') IS NOT NULL
BEGIN
    ALTER DATABASE SopalTraceDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE SopalTraceDB;
END
GO

CREATE DATABASE SopalTraceDB;
GO

USE SopalTraceDB;
GO

-- =================================================================================
-- PARTIE 1 : AUTHENTIFICATION ET SÉCURITÉ
-- =================================================================================
CREATE TABLE dbo.UtilisateursApp (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Matricule VARCHAR(20) NOT NULL UNIQUE, 
    NomComplet VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    MotDePasseHash VARCHAR(255) NOT NULL, 
    RoleApp VARCHAR(50) NOT NULL,         
    IntituleMetier VARCHAR(100) NULL,     
    CodeRecuperation VARCHAR(6) NULL,     
    DateExpirationCode DATETIME NULL,
    DateCreation DATETIME DEFAULT GETDATE(),
    DateDerniereConnexion DATETIME NULL,
    EstActif BIT DEFAULT 1
);
GO
CREATE INDEX IX_UtilisateursApp_Matricule ON dbo.UtilisateursApp(Matricule);
GO

CREATE TABLE dbo.RefreshTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UtilisateurId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.UtilisateursApp(Id) ON DELETE CASCADE,
    Token VARCHAR(255) NOT NULL UNIQUE, 
    JwtId VARCHAR(100) NOT NULL,        
    DateCreation DATETIME DEFAULT GETDATE(),
    DateExpiration DATETIME NOT NULL,
    EstRevoque BIT DEFAULT 0
);
GO

CREATE TABLE dbo.JournalConnexions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Matricule VARCHAR(20) NOT NULL,        
    Action VARCHAR(50) NOT NULL,            
    Details VARCHAR(255) NULL,              
    DateAction DATETIME DEFAULT GETDATE()
);
GO

-- =================================================================================
-- PARTIE 2 : CACHE ERP SAGE X3
-- =================================================================================
CREATE TABLE dbo.AUTILIS (
    USR_0 VARCHAR(5) PRIMARY KEY,
    INTUSR_0 VARCHAR(100) NOT NULL,
    ENAFLG_0 INT NOT NULL DEFAULT 1,
    CODMET_0 VARCHAR(20) NOT NULL,
    ADDEML_0 VARCHAR(150) NULL
);
GO

CREATE TABLE dbo.ATEXTRA (
    CODFIC_0 VARCHAR(50) NOT NULL,
    ZONE_0 VARCHAR(50) NOT NULL,
    IDENT1_0 VARCHAR(50) NOT NULL,
    LANGUE_0 VARCHAR(3) NOT NULL,
    TEXTE_0 VARCHAR(255) NOT NULL,
    PRIMARY KEY (CODFIC_0, ZONE_0, IDENT1_0, LANGUE_0)
);
GO

CREATE TABLE dbo.ITMMASTER (
    CodeArticle VARCHAR(30) PRIMARY KEY,
    Designation VARCHAR(100),
    Designation2 VARCHAR(100),
    FamilleProduitFini VARCHAR(30) NULL,
    FamilleCorpsCode VARCHAR(50) NULL,
    Statut VARCHAR(10)
);
GO

CREATE TABLE dbo.MFGHEAD (
    NumeroOF VARCHAR(30) PRIMARY KEY,
    CodeArticle VARCHAR(30) NOT NULL REFERENCES dbo.ITMMASTER(CodeArticle),
    QuantitePrevue FLOAT,
    StatutOF VARCHAR(10)
);
GO

CREATE TABLE dbo.MFGMAT (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NumeroOF VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD(NumeroOF),
    CodeArticle VARCHAR(30) NOT NULL REFERENCES dbo.ITMMASTER(CodeArticle),
    QuantiteRequise FLOAT NOT NULL,
    QuantiteSortie FLOAT NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.SDELIVERY (
    NumeroBL VARCHAR(30) PRIMARY KEY,
    CodeClient VARCHAR(30) NOT NULL, 
    DateExpedition DATE NOT NULL,        
    StatutBL VARCHAR(10) NOT NULL  
);
GO

-- =================================================================================
-- PARTIE 3 : MODULE MAGASIN / STOCK
-- =================================================================================
CREATE TABLE dbo.Mag_PreparationOF (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NumeroOF VARCHAR(30) NOT NULL,
    MatriculeMagasinier VARCHAR(20) NOT NULL REFERENCES dbo.UtilisateursApp(Matricule),
    Statut VARCHAR(20) NOT NULL DEFAULT 'EN_COURS' CHECK (Statut IN ('EN_COURS', 'TERMINE')),
    DateDebut DATETIME DEFAULT GETDATE(),
    DateFin DATETIME NULL
);
GO

CREATE TABLE dbo.Mag_PreparationOF_Lot (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PreparationOFId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Mag_PreparationOF(Id) ON DELETE CASCADE,
    CodeComposant VARCHAR(30) NOT NULL,
    NumeroLotScanne VARCHAR(50) NOT NULL,
    Quantite FLOAT NOT NULL,
    DateScan DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.Mag_ExpeditionBL (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NumeroBL VARCHAR(30) NOT NULL,
    MatriculeMagasinier VARCHAR(20) NOT NULL REFERENCES dbo.UtilisateursApp(Matricule),
    Statut VARCHAR(20) NOT NULL DEFAULT 'EN_COURS' CHECK (Statut IN ('EN_COURS', 'TERMINE')),
    DateDebut DATETIME DEFAULT GETDATE(),
    DateFin DATETIME NULL
);
GO

CREATE TABLE dbo.Mag_ExpeditionBL_ScanOF (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExpeditionBLId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Mag_ExpeditionBL(Id) ON DELETE CASCADE,
    NumeroOFScanne VARCHAR(30) NOT NULL,
    DateScan DATETIME DEFAULT GETDATE()
);
GO

-- =================================================================================
-- PARTIE 4 : DONNÉES DE BASE QUALITÉ ET RÉFÉRENTIELS
-- =================================================================================
CREATE TABLE dbo.TypeRobinet ( Code VARCHAR(10) PRIMARY KEY, Libelle VARCHAR(60) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );

CREATE TABLE dbo.FamilleProduitFini (
    Code VARCHAR(30) PRIMARY KEY,
    Designation VARCHAR(250) NOT NULL,
    TypeRobinetCode VARCHAR(10) NULL REFERENCES dbo.TypeRobinet(Code),
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.NatureComposant ( 
    Code VARCHAR(20) PRIMARY KEY, 
    Libelle VARCHAR(60) NOT NULL, 
    TypeLotAttendu VARCHAR(30) NULL, 
    EstGenerique BIT NOT NULL DEFAULT 0, 
    Actif BIT NOT NULL DEFAULT 1 
);

CREATE TABLE dbo.Operation ( Code VARCHAR(20) PRIMARY KEY, Libelle VARCHAR(80) NOT NULL, OrdreProcess INT NOT NULL DEFAULT 0, Actif BIT NOT NULL DEFAULT 1 );

CREATE TABLE dbo.NatureComposant_Operation (
    NatureComposantCode VARCHAR(20) NOT NULL REFERENCES dbo.NatureComposant(Code),
    OperationCode VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code),
    OrdreGamme INT NOT NULL DEFAULT 1,
    EstObligatoire BIT NOT NULL DEFAULT 1,
    PRIMARY KEY (NatureComposantCode, OperationCode)
);

CREATE TABLE dbo.Instrument ( CodeInstrument VARCHAR(40) PRIMARY KEY, Designation VARCHAR(100) NOT NULL, Categorie VARCHAR(40), PrecisionLecture FLOAT, Unite VARCHAR(10), DateEtalonnage DATE, DateProchaineVerif DATE, Statut VARCHAR(20) NOT NULL DEFAULT 'ACTIF', Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.PosteTravail ( CodePoste VARCHAR(30) PRIMARY KEY, Libelle VARCHAR(100) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
GO

CREATE TABLE dbo.Machine (
    CodeMachine VARCHAR(30) PRIMARY KEY, 
    Libelle VARCHAR(100) NOT NULL,
    TypeRobinetCode VARCHAR(10) NULL REFERENCES dbo.TypeRobinet(Code),
    OperationCode VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code),
    TypeAffectation VARCHAR(15) NOT NULL DEFAULT 'INDEPENDANTE' CHECK (TypeAffectation IN ('INDEPENDANTE', 'POSTE')),
    RoleMachine VARCHAR(20) NULL CHECK (RoleMachine IN ('BEE', 'MAS_ASS', 'MARQUAGE', 'USINAG', 'TRONC', 'ESTOMP')),
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Ref_FamilleCorps ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(50) NOT NULL UNIQUE, Designation VARCHAR(150) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
GO

-- NOUVELLE TABLE : LIAISON MACHINE <-> FAMILLES DE CORPS
CREATE TABLE dbo.Machine_FamilleCorps (
    MachineCode VARCHAR(30) NOT NULL REFERENCES dbo.Machine(CodeMachine) ON DELETE CASCADE,
    RefFamilleCorpsId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_FamilleCorps(Id) ON DELETE CASCADE,
    PRIMARY KEY (MachineCode, RefFamilleCorpsId)
);
GO

CREATE TABLE dbo.PosteTravail_Machine (
    CodePoste VARCHAR(30) NOT NULL REFERENCES dbo.PosteTravail(CodePoste),
    CodeMachine VARCHAR(30) NOT NULL REFERENCES dbo.Machine(CodeMachine),
    PRIMARY KEY (CodePoste, CodeMachine)
);
GO

CREATE TABLE dbo.Ref_Formulaire (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeReference VARCHAR(30) NOT NULL UNIQUE,
    Designation VARCHAR(150) NOT NULL,         
    OperationCode VARCHAR(20) REFERENCES dbo.Operation(Code),
    PosteCode VARCHAR(30) REFERENCES dbo.PosteTravail(CodePoste),
    MachineCode VARCHAR(30) REFERENCES dbo.Machine(CodeMachine),
    Version INT NOT NULL DEFAULT 0,
    Actif BIT NOT NULL DEFAULT 1,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.TypeCaracteristique ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, Libelle VARCHAR(80) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.TypeControle ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, Libelle VARCHAR(80) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.MoyenControle ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(40) NOT NULL UNIQUE, Libelle VARCHAR(100) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.Periodicite ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, Libelle VARCHAR(200) NOT NULL, FrequenceNum INT, FrequenceUnite VARCHAR(100), OrdreAffichage INT NOT NULL DEFAULT 0, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.Ref_RegleEchantillonnage ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, Libelle VARCHAR(250) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.TypeSection ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(300) NOT NULL UNIQUE, Libelle VARCHAR(100) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.Defautheque ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, Description VARCHAR(200), Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.RisqueDefaut ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), CodeDefaut VARCHAR(30) NOT NULL UNIQUE, LibelleDefaut VARCHAR(100) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.NQA ( Id INT IDENTITY(1,1) PRIMARY KEY, ValeurNQA FLOAT NOT NULL UNIQUE );
GO

CREATE TABLE dbo.Ref_MoyenDetection ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(50) NOT NULL UNIQUE, Designation VARCHAR(100) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
GO

CREATE TABLE dbo.PieceReference ( 
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), 
    Code VARCHAR(30) NOT NULL UNIQUE, 
    TypePiece VARCHAR(10) NOT NULL CHECK (TypePiece IN ('PRC','PRNC','FEC','FENC')), 
    Designation VARCHAR(150), 
    FamilleDesc VARCHAR(60), 
    Actif BIT NOT NULL DEFAULT 1 
);
GO

CREATE TABLE dbo.OutilControle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(40) NOT NULL UNIQUE,
    Libelle VARCHAR(150) NOT NULL,
    TypeControleId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.TypeControle(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER REFERENCES dbo.TypeCaracteristique(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    PeriodiciteDefautId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    LimiteSpecTexteDefaut VARCHAR(100),
    InstructionDefaut NVARCHAR(MAX),
    Actif BIT NOT NULL DEFAULT 1
);
GO

-- =================================================================================
-- PARTIE 4.1 : 
-- =================================================================================
ALTER TABLE dbo.ITMMASTER ADD 
    TypeRobinetCode VARCHAR(10) NULL REFERENCES dbo.TypeRobinet(Code),
    NatureComposantCode VARCHAR(20) NULL REFERENCES dbo.NatureComposant(Code);
GO

ALTER TABLE dbo.ITMMASTER ADD CONSTRAINT FK_ITMMASTER_FamilleProduitFini 
    FOREIGN KEY (FamilleProduitFini) REFERENCES dbo.FamilleProduitFini(Code);
GO

ALTER TABLE dbo.ITMMASTER ADD CONSTRAINT FK_ITMMASTER_FamilleCorps 
    FOREIGN KEY (FamilleCorpsCode) REFERENCES dbo.Ref_FamilleCorps(Code);
GO

-- =================================================================================
-- PARTIE 4.5 : BOUCLIER ISO 9001 (TRIGGERS INSTEAD OF DELETE)
-- =================================================================================
CREATE OR ALTER PROCEDURE dbo.sp_RaiseDeleteError (@TableName VARCHAR(100))
AS
BEGIN
    DECLARE @Msg VARCHAR(200) = 'La suppression physique est interdite (ISO 9001) sur la table ' + @TableName + '. Utilisez Actif = 0 ou Statut = ''ARCHIVE''.';
    RAISERROR(@Msg, 16, 1);
END;
GO

CREATE TRIGGER trg_no_del_TypeRobinet ON dbo.TypeRobinet INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeRobinet'; END;
GO
CREATE TRIGGER trg_no_del_FamilleProduitFini ON dbo.FamilleProduitFini INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'FamilleProduitFini'; END;
GO
CREATE TRIGGER trg_no_del_NatureComposant ON dbo.NatureComposant INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'NatureComposant'; END;
GO
CREATE TRIGGER trg_no_del_Operation ON dbo.Operation INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Operation'; END;
GO
CREATE TRIGGER trg_no_del_Instrument ON dbo.Instrument INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Instrument'; END;
GO
CREATE TRIGGER trg_no_del_Machine ON dbo.Machine INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Machine'; END;
GO
CREATE TRIGGER trg_no_del_PosteTravail ON dbo.PosteTravail INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'PosteTravail'; END;
GO
CREATE TRIGGER trg_no_del_TypeCar ON dbo.TypeCaracteristique INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeCaracteristique'; END;
GO
CREATE TRIGGER trg_no_del_TypeCtrl ON dbo.TypeControle INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeControle'; END;
GO
CREATE TRIGGER trg_no_del_Moyen ON dbo.MoyenControle INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'MoyenControle'; END;
GO
CREATE TRIGGER trg_no_del_Perio ON dbo.Periodicite INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Periodicite'; END;
GO
CREATE TRIGGER trg_no_del_PieceRef ON dbo.PieceReference INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'PieceReference'; END;
GO
CREATE TRIGGER trg_no_del_Risque ON dbo.RisqueDefaut INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'RisqueDefaut'; END;
GO
CREATE TRIGGER trg_no_del_TypeSec ON dbo.TypeSection INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeSection'; END;
GO
CREATE TRIGGER trg_no_del_Defaut ON dbo.Defautheque INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Defautheque'; END;
GO
CREATE TRIGGER trg_no_del_RefForm ON dbo.Ref_Formulaire INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_Formulaire'; END;
GO
CREATE TRIGGER trg_no_del_RefFam ON dbo.Ref_FamilleCorps INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_FamilleCorps'; END;
GO
CREATE TRIGGER trg_no_del_RefMoy ON dbo.Ref_MoyenDetection INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_MoyenDetection'; END;
GO
CREATE TRIGGER trg_no_del_RefRegleEchan ON dbo.Ref_RegleEchantillonnage INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_RegleEchantillonnage'; END;
GO

-- =================================================================================
-- PARTIE 5 : PLAN D'ÉCHANTILLONNAGE
-- =================================================================================
CREATE TABLE dbo.Plan_Echantillonnage_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NiveauControle VARCHAR(5) NOT NULL CHECK (NiveauControle IN ('I','II','III')),
    TypePlan VARCHAR(10) NOT NULL CHECK (TypePlan IN ('SIMPLE','DOUBLE')),
    ModeControle VARCHAR(15) NOT NULL CHECK (ModeControle IN ('NORMAL','REDUIT','RENFORCE')),
    NqaId INT NOT NULL REFERENCES dbo.NQA(Id),
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'ACTIF' CHECK (Statut IN ('ACTIF','ARCHIVE')), 
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20) NULL,
    ModifieLe DATETIME NULL,
    CommentaireVersion NVARCHAR(MAX),
    Remarques NVARCHAR(MAX) NULL,
    LegendeMoyens NVARCHAR(MAX) NULL
);
GO

CREATE TRIGGER trg_no_del_PlanEchan ON dbo.Plan_Echantillonnage_Entete INSTEAD OF DELETE AS 
BEGIN 
    IF EXISTS (SELECT 1 FROM deleted WHERE Statut IN ('ACTIF', 'ARCHIVE'))
        EXEC dbo.sp_RaiseDeleteError 'Plan_Echantillonnage_Entete'; 
    ELSE 
        DELETE FROM dbo.Plan_Echantillonnage_Entete WHERE Id IN (SELECT Id FROM deleted);
END;
GO

CREATE TABLE dbo.Plan_Echantillonnage_Regle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FicheEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Echantillonnage_Entete(Id) ON DELETE CASCADE,
    TailleMinLot INT,
    TailleMaxLot INT,
    LettreCode VARCHAR(5) NOT NULL,
    EffectifEchantillon_A INT NOT NULL,
    NbPostes_B INT NOT NULL DEFAULT 1,
    EffectifParPoste_AB INT,
    CritereAcceptation_Ac INT NOT NULL,
    CritereRejet_Re INT NOT NULL,
    UNIQUE (FicheEnteteId, LettreCode)
);
GO

-- =================================================================================
-- PARTIE 6 : PLANS DE FABRICATION (USINAGE) - SANS FAMILLE
-- =================================================================================
CREATE TABLE dbo.Modele_Fab_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(60) NOT NULL,
    Libelle VARCHAR(150) NOT NULL,
    NatureComposantCode VARCHAR(20) NOT NULL REFERENCES dbo.NatureComposant(Code),
    OperationCode VARCHAR(20) NULL REFERENCES dbo.Operation(Code),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id), 
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    Notes NVARCHAR(MAX),
    FamilleProduitFiniCode VARCHAR(30) NULL REFERENCES dbo.FamilleProduitFini(Code),
    LegendeMoyens NVARCHAR(MAX) NULL, 
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20) NULL,
    ModifieLe DATETIME NULL,
    ArchiveLe DATETIME,
    ArchivePar VARCHAR(20)
);
GO

CREATE TRIGGER trg_no_del_ModeleFab ON dbo.Modele_Fab_Entete INSTEAD OF DELETE AS 
BEGIN 
    IF EXISTS (SELECT 1 FROM deleted WHERE Statut IN ('ACTIF', 'ARCHIVE'))
        EXEC dbo.sp_RaiseDeleteError 'Modele_Fab_Entete'; 
    ELSE 
        DELETE FROM dbo.Modele_Fab_Entete WHERE Id IN (SELECT Id FROM deleted);
END;
GO

CREATE TABLE dbo.Modele_Fab_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Modele_Fab_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    LibelleSection VARCHAR(200) NOT NULL,
    FrequenceLibelle VARCHAR(80),
    TypeSectionId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeSection(Id),
    PeriodiciteId UNIQUEIDENTIFIER NULL REFERENCES dbo.Periodicite(Id),
    RegleEchantillonnageId UNIQUEIDENTIFIER NULL CONSTRAINT FK_ModeleFabSection_Regle REFERENCES dbo.Ref_RegleEchantillonnage(Id)
);
GO

CREATE TABLE dbo.Modele_Fab_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Modele_Fab_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Modele_Fab_Section(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeCaracteristiqueId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(200) NULL,
    LimiteSpecTexte VARCHAR(100) NULL, 
    TypeControleId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeControle(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    MoyenTexteLibre VARCHAR(100) NULL, 
    InstrumentCode VARCHAR(40) REFERENCES dbo.Instrument(CodeInstrument), 
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    Instruction NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.Plan_Fab_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleSourceId UNIQUEIDENTIFIER  REFERENCES dbo.Modele_Fab_Entete(Id),
    CodeArticleSage VARCHAR(30) NOT NULL, 
    Designation VARCHAR(200),
    Nom VARCHAR(150) NOT NULL,
    Version INT NOT NULL DEFAULT 0,
    OperationCode VARCHAR(30),
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    DateApplication DATE,
    MachineDefautCode VARCHAR(30) REFERENCES dbo.Machine(CodeMachine),
    FamilleProduitFiniCode VARCHAR(30) NULL REFERENCES dbo.FamilleProduitFini(Code),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id), 
    LegendeMoyens NVARCHAR(MAX) NULL, 
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    CommentaireVersion NVARCHAR(MAX),
    Remarques NVARCHAR(MAX) NULL
);
GO

CREATE TRIGGER trg_no_del_PlanFab ON dbo.Plan_Fab_Entete INSTEAD OF DELETE AS 
BEGIN 
    IF EXISTS (SELECT 1 FROM deleted WHERE Statut IN ('ACTIF', 'ARCHIVE'))
        EXEC dbo.sp_RaiseDeleteError 'Plan_Fab_Entete'; 
    ELSE 
        DELETE FROM dbo.Plan_Fab_Entete WHERE Id IN (SELECT Id FROM deleted);
END;
GO

CREATE TABLE dbo.Plan_Fab_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Fab_Entete(Id) ON DELETE CASCADE,
    ModeleSectionId UNIQUEIDENTIFIER REFERENCES dbo.Modele_Fab_Section(Id),
    OrdreAffiche INT NOT NULL DEFAULT 0,
    LibelleSection VARCHAR(300) NOT NULL,
    FrequenceLibelle VARCHAR(80),
    TypeSectionId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeSection(Id),
    PeriodiciteId UNIQUEIDENTIFIER NULL REFERENCES dbo.Periodicite(Id),
    RegleEchantillonnageId UNIQUEIDENTIFIER NULL CONSTRAINT FK_PlanFabSection_Regle REFERENCES dbo.Ref_RegleEchantillonnage(Id)
);
GO

CREATE TABLE dbo.Plan_Fab_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Fab_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Fab_Section(Id) ON DELETE CASCADE,
    ModeleLigneSourceId UNIQUEIDENTIFIER REFERENCES dbo.Modele_Fab_Ligne(Id),
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeCaracteristiqueId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(200) NULL,
    TypeControleId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeControle(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    MoyenTexteLibre VARCHAR(100) NULL, 
    InstrumentCode VARCHAR(40) REFERENCES dbo.Instrument(CodeInstrument),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    LimiteSpecTexte VARCHAR(100), 
    Observations NVARCHAR(MAX),
    Instruction NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0
);
GO

-- =================================================================================
-- PARTIE 7 : PLANS D'ASSEMBLAGE (AVEC FAMILLE_PRODUIT)
-- =================================================================================
CREATE TABLE dbo.Plan_Ass_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OperationCode VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code), 
    FamilleProduitFiniCode VARCHAR(30) NULL REFERENCES dbo.FamilleProduitFini(Code),
    NatureComposantCode VARCHAR(20) NULL REFERENCES dbo.NatureComposant(Code),
    PosteCode VARCHAR(30) NULL REFERENCES dbo.PosteTravail(CodePoste),         
    Designation VARCHAR(200),
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id), 
    LegendeMoyens NVARCHAR(MAX) NULL, 
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    ArchiveLe DATETIME NULL,                                                   
    ArchivePar VARCHAR(20) NULL,                                               
    Remarques NVARCHAR(MAX) NULL
);
GO

CREATE TRIGGER trg_no_del_PlanAss ON dbo.Plan_Ass_Entete INSTEAD OF DELETE AS 
BEGIN 
    IF EXISTS (SELECT 1 FROM deleted WHERE Statut IN ('ACTIF', 'ARCHIVE'))
        EXEC dbo.sp_RaiseDeleteError 'Plan_Ass_Entete'; 
    ELSE 
        DELETE FROM dbo.Plan_Ass_Entete WHERE Id IN (SELECT Id FROM deleted);
END;
GO

CREATE TABLE dbo.Plan_Ass_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Ass_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeSectionId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeSection(Id),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id), 
    LibelleSection VARCHAR(300) NOT NULL,
    NormeReference VARCHAR(40), 
    NqaId INT REFERENCES dbo.NQA(Id),
    Notes NVARCHAR(MAX),
    RegleEchantillonnageId UNIQUEIDENTIFIER NULL CONSTRAINT FK_PlanAssSection_Regle REFERENCES dbo.Ref_RegleEchantillonnage(Id)
);
GO

CREATE TABLE dbo.Plan_Ass_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Ass_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Ass_Section(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeCaracteristiqueId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(250) NULL, 
    TypeControleId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeControle(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    MoyenTexteLibre VARCHAR(100) NULL, 
    MachineCode VARCHAR(30) REFERENCES dbo.Machine(CodeMachine), 
    InstrumentCode VARCHAR(40) REFERENCES dbo.Instrument(CodeInstrument), 
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    LimiteSpecTexte VARCHAR(100), 
    EstVerifPresence BIT NOT NULL DEFAULT 0,
    DefauthequeId UNIQUEIDENTIFIER REFERENCES dbo.Defautheque(Id),
    RefPlanProduit VARCHAR(60),
    Instruction NVARCHAR(MAX),
    Observations NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0
);
GO

-- =================================================================================
-- PARTIE 8 : PLAN PRODUIT FINI (SANS FAMILLE)
-- =================================================================================
CREATE TABLE dbo.Plan_PF_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FamilleProduitFiniCode VARCHAR(30) NULL REFERENCES dbo.FamilleProduitFini(Code),
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    CommentaireVersion NVARCHAR(MAX),
    Remarques NVARCHAR(MAX) NULL,
    LegendeMoyens NVARCHAR(MAX) NULL
);
GO

CREATE TRIGGER trg_no_del_PlanPF ON dbo.Plan_PF_Entete INSTEAD OF DELETE AS 
BEGIN 
    IF EXISTS (SELECT 1 FROM deleted WHERE Statut IN ('ACTIF', 'ARCHIVE'))
        EXEC dbo.sp_RaiseDeleteError 'Plan_PF_Entete'; 
    ELSE 
        DELETE FROM dbo.Plan_PF_Entete WHERE Id IN (SELECT Id FROM deleted);
END;
GO

CREATE TABLE dbo.Plan_PF_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_PF_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeSectionId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeSection(Id),
    LibelleSection VARCHAR(300) NOT NULL,
    PeriodiciteId UNIQUEIDENTIFIER NULL REFERENCES dbo.Periodicite(Id),
    RegleEchantillonnageId UNIQUEIDENTIFIER NULL CONSTRAINT FK_PlanPFSection_Regle REFERENCES dbo.Ref_RegleEchantillonnage(Id),
    Notes NVARCHAR(MAX)
);
GO

CREATE TABLE dbo.Plan_PF_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_PF_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_PF_Section(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeCaracteristiqueId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(250),
    TypeControleId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.TypeControle(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    InstrumentCode VARCHAR(40) REFERENCES dbo.Instrument(CodeInstrument),
    MoyenTexteLibre VARCHAR(100) NULL, 
    LimiteSpecTexte VARCHAR(100), 
    DefauthequeId UNIQUEIDENTIFIER REFERENCES dbo.Defautheque(Id),
    Instruction NVARCHAR(MAX),
    Observations NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0
);
GO

-- =================================================================================
-- PARTIE 9 : PLANS DE NON-CONFORMITÉ / TALLY
-- =================================================================================
CREATE TABLE dbo.Plan_NC_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PosteCode VARCHAR(30) NOT NULL REFERENCES dbo.PosteTravail(CodePoste),
    Nom VARCHAR(150) NOT NULL,
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20) NULL,
    ModifieLe DATETIME NULL,
    Remarques NVARCHAR(MAX) NULL,
    LegendeMoyens NVARCHAR(MAX) NULL
);
GO

CREATE TABLE dbo.Plan_NC_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanNCEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_NC_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL,
    MachineCode VARCHAR(30) NOT NULL REFERENCES dbo.Machine(CodeMachine), 
    RisqueDefautId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.RisqueDefaut(Id), 
    UNIQUE (PlanNCEnteteId, OrdreAffiche)
);
GO

-- =================================================================================
-- PARTIE 10 : PLANS DE VÉRIFICATION MACHINE (MATRICE 3D)
-- =================================================================================
CREATE TABLE dbo.Plan_VerifMachine_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    MachineCode VARCHAR(30) NOT NULL REFERENCES dbo.Machine(CodeMachine),
    Nom VARCHAR(150) NOT NULL,
    Version INT DEFAULT 0,
    Statut VARCHAR(20) DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE')),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME DEFAULT GETDATE(),
    ModifiePar VARCHAR(20) NULL,
    ModifieLe DATETIME NULL,
    Remarques NVARCHAR(MAX) NULL,
    LegendeMoyens NVARCHAR(MAX) NULL
);
GO

CREATE TABLE dbo.Plan_VerifMachine_Famille (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL,
    RefFamilleCorpsId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_FamilleCorps(Id),
    UNIQUE (PlanEnteteId, RefFamilleCorpsId)
);
GO

CREATE TABLE dbo.Plan_VerifMachine_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL,
    TypeLigne VARCHAR(20) DEFAULT 'RISQUE' CHECK (TypeLigne IN ('CONFORMITE', 'RISQUE')),
    LibelleRisque VARCHAR(250) NOT NULL,
    LibelleMethode VARCHAR(250) NULL
);
GO

CREATE TABLE dbo.Plan_VerifMachine_Echeance (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanLigneId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Ligne(Id) ON DELETE CASCADE,
    PeriodiciteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Periodicite(Id),
    RefMoyenDetectionId UNIQUEIDENTIFIER NULL REFERENCES dbo.Ref_MoyenDetection(Id), 
    OrdreAffiche INT NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.Plan_VerifMachine_MatricePiece (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EcheanceId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Echeance(Id) ON DELETE CASCADE,
    FamilleId UNIQUEIDENTIFIER NULL REFERENCES dbo.Plan_VerifMachine_Famille(Id),
    RoleVerif VARCHAR(10) CHECK (RoleVerif IN ('PRC', 'PRNC', 'FEC', 'FENC')),
    PieceRefId UNIQUEIDENTIFIER NULL REFERENCES dbo.PieceReference(Id), 
    UNIQUE (EcheanceId, FamilleId, RoleVerif)
);
GO

-- =================================================================================
-- PARTIE 12 : INDEX DE CONTRAINTES AVANCÉS (VERSIONING & UNICITÉ)
-- =================================================================================

-- 🟢 1. PLAN ASSEMBLAGE (ASS) -> AVEC FAMILLE
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanAss_Modele_Version ON dbo.Plan_Ass_Entete(OperationCode, FamilleProduitFiniCode, NatureComposantCode, PosteCode, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanAss_Maitre_Actif ON dbo.Plan_Ass_Entete(OperationCode, FamilleProduitFiniCode, NatureComposantCode, PosteCode) WHERE Statut = 'ACTIF';

-- 🟢 2. MODÈLE FABRICATION (USINAGE) -> SANS FAMILLE
CREATE UNIQUE NONCLUSTERED INDEX UQ_ModeleFab_Version ON dbo.Modele_Fab_Entete(Code, Libelle, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UX_ModeleFab_Actif ON dbo.Modele_Fab_Entete(Code, Libelle) WHERE Statut = 'ACTIF';

-- 🟢 3. PLAN FABRICATION (EN COURS) -> SANS FAMILLE (Lié à un article)
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanFab_Version ON dbo.Plan_Fab_Entete(CodeArticleSage, OperationCode, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanFab_Actif ON dbo.Plan_Fab_Entete(CodeArticleSage, OperationCode) WHERE Statut = 'ACTIF';

-- 🟢 4. PLAN PRODUIT FINI (PF) -> AVEC FAMILLE
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanPF_Version ON dbo.Plan_PF_Entete(FamilleProduitFiniCode, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanPF_Actif ON dbo.Plan_PF_Entete(FamilleProduitFiniCode) WHERE Statut = 'ACTIF';

-- (Autres index non modifiés)
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanEchan_Global_Version ON dbo.Plan_Echantillonnage_Entete(Version);
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanEchan_Global_Actif ON dbo.Plan_Echantillonnage_Entete(Statut) WHERE Statut = 'ACTIF';
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanNC_Version ON dbo.Plan_NC_Entete(PosteCode, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanNC_Actif ON dbo.Plan_NC_Entete(PosteCode) WHERE Statut = 'ACTIF';
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanVerif_Version ON dbo.Plan_VerifMachine_Entete(MachineCode, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanVerif_Actif ON dbo.Plan_VerifMachine_Entete(MachineCode) WHERE Statut = 'ACTIF';
GO

-- =================================================================================
-- PARTIE 13 : SEED DATA 
-- =================================================================================

-- 1. RÉFÉRENTIELS DE BASE
INSERT INTO dbo.Operation (Code, Libelle) VALUES 
('ASS', 'Assemblage'), ('TRONC', 'Tronçonnage'), ('ESTOMP', 'Estompage'), ('USINAG', 'Usinage');

INSERT INTO dbo.TypeRobinet (Code, Libelle) VALUES 
('MAN', 'Manuelle'), ('AUTO', 'Automatique'), ('SOUP', 'Automatique avec soupape');

-- 2. FAMILLES D'ARTICLES
INSERT INTO dbo.FamilleProduitFini (Code, Designation, TypeRobinetCode) VALUES 
('RBGFA-BAC-01', 'Robinet bouteille de gaz à fermeture automatique type boite à clapet à filetage droit', 'AUTO'),
('RBGFA-BAC-02', 'Robinet bouteille de gaz à fermeture automatique type boite à clapet à filetage conique', 'AUTO'),
('RBGFA', 'Robinet bouteille de gaz à fermeture automatique', 'AUTO'),
('RBGFM', 'Robinet bouteille de gaz à fermeture manuelle', 'MAN'),
('RBGFA_SOUPAPE', 'Robinet bouteille de gaz à fermeture automatique avec soupape', 'SOUP');

INSERT INTO dbo.NatureComposant (Code, Libelle) VALUES 
('CORPS', 'Corps'), ('VOLANT', 'Volant'), ('PISTON', 'Piston'), ('PF', 'Produit Fini');

-- 3. LIAISON GAMMES
INSERT INTO dbo.NatureComposant_Operation (NatureComposantCode, OperationCode, OrdreGamme, EstObligatoire) VALUES 
('CORPS', 'USINAG', 3, 1),
('CORPS', 'TRONC', 1, 1),
('CORPS', 'ESTOMP', 2, 1),
('VOLANT', 'TRONC', 1, 1),
('VOLANT', 'ESTOMP', 2, 1),
('VOLANT', 'USINAG', 3, 1),
('PF', 'ASS', 1, 1),
('PISTON', 'ASS', 1, 1);

-- 4. POSTES DE TRAVAIL
INSERT INTO dbo.PosteTravail (CodePoste, Libelle) VALUES 
('PAS71', 'Poste 71'), ('PAS72', 'Poste 72'), ('PAS78', 'Poste 78');

-- 5. MACHINES
INSERT INTO dbo.Machine (CodeMachine, Libelle, OperationCode, RoleMachine) VALUES 
('MAS19', 'Machine 19', 'ASS', 'MAS_ASS'), 
('MAS20', 'Machine 20', 'ASS', 'MAS_ASS'), 
('MAS22', 'Machine 22', 'ASS', 'MAS_ASS'), 
('MAS26', 'Machine 26', 'ASS', 'MAS_ASS'),
('BEE22', 'Banc 22', 'ASS', 'BEE'), 
('BEE46', 'Banc 46', 'ASS', 'BEE'), 
('BEE47', 'Banc 47', 'ASS', 'BEE'),
('SER05', 'Marquage 05', 'ASS', 'MARQUAGE');

-- 6. LIAISON : POSTES <-> MACHINES
INSERT INTO dbo.PosteTravail_Machine (CodePoste, CodeMachine) VALUES 
('PAS72', 'MAS26'), ('PAS72', 'BEE46'),
('PAS71', 'MAS22'), ('PAS71', 'MAS19'), ('PAS71', 'BEE47'),
('PAS78', 'SER05');

-- 7. DICTIONNAIRE DES RISQUES
INSERT INTO dbo.RisqueDefaut (CodeDefaut, LibelleDefaut) VALUES 
('R_30B_F', 'ESSAI DE 30 BAR (ROBINET FERME)'), ('R_01B_F', 'ESSAI DE 0,1 BAR (ROBINET FERME)'),
('R_30B_O', 'ESSAI DE 30 BAR (ROBINET OUVERT)'), ('R_01B_O', 'ESSAI DE 0,1 BAR (ROBINET OUVERT)'),
('R_GOUP', 'ABSENCE / MAUVAIS MONTAGE DE LA GOUPILLE'), ('R_PTDUR', 'PRESENCE DU POINT DURE'),
('R_J_AUTO', 'ABSENCE DU JOINT AUTOSERREUR'), ('R_J_ANTI', 'ABSENCE/ MAUVAIS MONTAGE JOINT ANTI-FUITE');

-- 8. MATRICE 3D - FAMILLES & MOYENS
INSERT INTO dbo.Ref_FamilleCorps (Code, Designation) VALUES 
('F_30_35', 'Famille Corps (30, 35)'), ('F_23', 'Famille Corps (23)'), 
('FAM_40_43_44', 'Famille Corps (40, 43, 44)'), ('FAM_49', 'Famille Corps (49)'),
('C_25B0A01', 'Corps (25B0A01)'), ('C_25AXA01', 'Corps (25AXA01)'), 
('C_25AWA01', 'Corps (25AWA01)'), ('C_25UA01', 'Corps (25UA01)');

INSERT INTO dbo.Ref_MoyenDetection (Code, Designation) VALUES 
('PRC', 'PRC'), ('PRNC', 'PRNC'), 
('PRC_FEC', 'PRC + FEC'), ('PRC_FENC', 'PRC + FENC'), 
('PRC_LS', 'PRC LS'), ('PRC_LI', 'PRC LI');

-- 9. MOYENS DE CONTRÔLE
INSERT INTO dbo.MoyenControle (Code, Libelle) VALUES 
('VISUEL', 'Visuel'), ('BANC_ESSAI', 'Banc d''essai'), ('PAC', 'P à C (Pied à coulisse)'),
('JAP', 'J à P (Jauge de profondeur)'), ('COMP', 'Comparateur'), ('CLD', 'Clé dynamométrique'),
('BAGUE_F', 'Bague filetée P-NP'), ('TAMPON_F', 'Tampon fileté P-NP'), ('DEFAUTHEQUE', 'Défauthèque');

-- 10. INSTRUMENTS DE CONTRÔLE
INSERT INTO dbo.Instrument (CodeInstrument, Designation, Categorie, Statut) VALUES 
('BAN59', 'BEE LAB : (BAN59)', 'BANC ESSAI', 'ACTIF'),
('BEE32', 'BEE LAB : (BEE32)', 'BANC ESSAI', 'ACTIF'),
('BAN47', 'BEE LAB : (BAN47)', 'BANC ESSAI', 'ACTIF'),
('PAC', 'PAC (Pied à coulisse)', 'DIMENSIONNEL', 'ACTIF'),
('JPC', 'JPC (Jauge de Profondeur)', 'DIMENSIONNEL', 'ACTIF'),
('CAN', 'CAN (Comparateur / Alésomètre)', 'DIMENSIONNEL', 'ACTIF'),
('CLD', 'CLD (Clé Dynamométrique)', 'MECANIQUE', 'ACTIF');

-- 11. TYPES DE SECTION
INSERT INTO dbo.TypeSection (Code, Libelle) VALUES 
('REGLAGE', 'aux réglages'),('REGLAGE_PROD', 'au réglage et en cours de production'), ('EN_COURS', 'par échantillonnage en cours de production'), ('ECHANT_NQA', 'par échantillonnage '),('OF', ' au niveau du lot (OF) ');

-- 12. NQA & PÉRIODICITÉS
INSERT INTO dbo.NQA (ValeurNQA) VALUES (0.65),(1.0),(1.5),(2.5),(4.0),(6.5);

INSERT INTO dbo.Periodicite (Code, Libelle, FrequenceNum, FrequenceUnite, OrdreAffichage) VALUES 
('DEM', 'Au démarrage', NULL, NULL, 0), 
('PAU', 'Après la pause', NULL, NULL, 0), 
('FIN', 'A la fin du poste', NULL, NULL, 0),
('SERIE_5P', 'une série de 5 pièces', 5, 'SERIE', 5),
('4P_1H', '4 pièces / heure', 4, '1_HEURE', 5),
('1P_1H', '1 pièce / heure', 1, '1_HEURE', 5),
('100PCT_1H', '100% des pièces/h', 100, 'PCT_HEURE', 5),
('1P_4H', '1 pièce / 4 heures', 1, '4_HEURE', 5);

INSERT INTO dbo.Ref_RegleEchantillonnage (Code, Libelle) VALUES 
('REG-9F43B', 'selon ISO 2859-1 : Tableau 2-A, avec un NQA de 0,65'),
('REG-43BD2', 'Selon FE0591 : Effectif de l’échantillon /poste (A/B) (p/h)'),
('REG-7A57N', 'la première et la dernière pièce et 0,01% du lot'),
('REG-7A57C', 'selon ISO 3951-1 : Tableau 2, avec un NQA de 0,65');



-- 13. TYPES CARACTÉRISTIQUES ET CONTRÔLE
INSERT INTO dbo.TypeCaracteristique (Code, Libelle) VALUES 
('VISUEL', 'Contrôle visuel'), ('DIM', 'Diamètre / Dimension'), ('ETANCH', 'Étanchéité'), ('Serrage', 'Couple de serrage');

INSERT INTO dbo.TypeControle (Code, Libelle) VALUES ('VISUEL', 'Visuel'), ('MESURE', 'Mesure'),('MANUEL', 'Manuel'), ('ATTRIBUT', 'Attribut');

-- 14. ARTICLES SAGE (ITMMASTER) 
INSERT INTO dbo.ITMMASTER (CodeArticle, Designation, FamilleProduitFini, FamilleCorpsCode, Statut, TypeRobinetCode, NatureComposantCode) VALUES 
('25A8B01-1', 'Boite à clapet', 'RBGFA-BAC-01', NULL, 'ACTIF', 'AUTO', 'PF'),
('25AKA01-1', 'R. de GAZ Boite GAZ', 'RBGFM', NULL, 'ACTIF', 'MAN', 'PF'),
('25B0A01-1', 'R de gaz avec soupape', 'RBGFA_SOUPAPE', NULL, 'ACTIF', 'SOUP', 'PF'),
('2588A01-1-A', 'R de gaz auto', 'RBGFA-BAC-01', NULL, 'ACTIF', 'AUTO', 'PF'),
('2582A01-1-D', 'R de gaz auto', 'RBGFA-BAC-02', NULL, 'ACTIF', 'AUTO', 'PF'),
('C-25B0A01', 'Corps laiton brut (Soupape)', 'RBGFA_SOUPAPE', 'FAM_49', 'ACTIF', 'SOUP', 'CORPS'),
('P-25B0A01', 'Piston laiton Asil', NULL, NULL, 'ACTIF', 'AUTO', 'PISTON'),
('m-25B0A01', 'Corps laiton brut (Soupape)', 'RBGFA_SOUPAPE', NULL, 'ACTIF', 'SOUP', 'CORPS'),
('n-25B0A01', 'Corps laiton brut (Soupape)', 'RBGFA_SOUPAPE', NULL, 'ACTIF', 'SOUP', 'CORPS'),
('b-25B0A01', 'Corps laiton brut (Soupape)', 'RBGFA_SOUPAPE', NULL, 'ACTIF', 'SOUP', 'CORPS'),
('V-25A8B01', 'Volant plastique noir', 'RBGFM', NULL, 'ACTIF', 'MAN', 'VOLANT');
GO
-- =================================================================================
-- PARTIE 14 : SEED DATA - UTILISATEURS (CACHE ERP SAGE X3 ET APPLICATION)
-- =================================================================================

-- 1. Ajout dans la table AUTILIS (Cache des utilisateurs de l'ERP Sage X3)
INSERT INTO dbo.AUTILIS (USR_0, INTUSR_0, ENAFLG_0, CODMET_0, ADDEML_0) VALUES 
('12345', 'Nada Belghith', 2, 'ADMIN', 'nada.belghith@enis.tn'),
('OP001', 'Operateur Test', 2, 'OPERATEUR', 'operateur@sopal.com'),
('QA001', 'Qualite Test', 2, 'QUALITE', 'qualite@sopal.com');
GO

-- 2. Ajout dans la table ATEXTRA (Traductions Sage X3 pour les noms d'utilisateurs)
INSERT INTO dbo.ATEXTRA (CODFIC_0, ZONE_0, IDENT1_0, LANGUE_0, TEXTE_0) VALUES 
('AUTILIS', 'INTUSR', '12345', 'FRA', 'Nada Belghith'),
('AUTILIS', 'INTUSR', 'OP001', 'FRA', 'Operateur Test'),
('AUTILIS', 'INTUSR', 'QA001', 'FRA', 'Qualite Test');
GO

-- =================================================================================
-- PARTIE 15 : LIAISON MACHINES <-> FAMILLES DE CORPS
-- =================================================================================

-- Pour SER05
INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'SER05', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('F_30_35', 'F_23', 'FAM_40_43_44', 'FAM_49');

-- Pour MAS22
INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'MAS22', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('F_30_35', 'F_23', 'FAM_40_43_44', 'FAM_49');

-- Pour BEE46
INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'BEE46', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('C_25B0A01', 'C_25AXA01', 'C_25AWA01', 'C_25UA01');

-- Pour BEE22
INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'BEE22', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('C_25B0A01', 'C_25AXA01', 'C_25AWA01', 'C_25UA01');
GO

PRINT '=======================================================';
PRINT ' LA BASE SOPALTRACE V6.9.9 A ÉTÉ GÉNÉRÉE AVEC SUCCÈS ! ';
PRINT '=======================================================';