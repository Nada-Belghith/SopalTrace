-- =================================================================================
-- SCRIPT MASTER RESET : SOPALTRACE V6.6 (Plan Echantillonnage Global Unique)
-- Cible : Microsoft SQL Server (T-SQL)
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
    FamilleProduit VARCHAR(30),
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
    Version INT NOT NULL DEFAULT 1,
    Actif BIT NOT NULL DEFAULT 1,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.TypeCaracteristique ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, Libelle VARCHAR(80) NOT NULL, UniteDefaut VARCHAR(10), EstNumerique BIT NOT NULL DEFAULT 1, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.TypeControle ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, Libelle VARCHAR(80) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.MoyenControle ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(40) NOT NULL UNIQUE, Libelle VARCHAR(100) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.Periodicite ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, Libelle VARCHAR(200) NOT NULL, FrequenceNum INT, FrequenceUnite VARCHAR(100), OrdreAffichage INT NOT NULL DEFAULT 0, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.TypeSection ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, Libelle VARCHAR(100) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.Defautheque ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, Description VARCHAR(200), Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.PieceReference ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(30) NOT NULL UNIQUE, TypePiece VARCHAR(10) NOT NULL CHECK (TypePiece IN ('PRC','PRNC','FEC','FENC')), Designation VARCHAR(150), FamilleDesc VARCHAR(60), MachineCode VARCHAR(30) NULL REFERENCES dbo.Machine(CodeMachine), Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.RisqueDefaut ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), CodeDefaut VARCHAR(30) NOT NULL UNIQUE, LibelleDefaut VARCHAR(100) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.NQA ( Id INT IDENTITY(1,1) PRIMARY KEY, ValeurNQA FLOAT NOT NULL UNIQUE );
GO

-- NOUVEAUX DICTIONNAIRES (POUR LA MATRICE DE VERIFICATION 3D)
CREATE TABLE dbo.Ref_FamilleCorps ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(50) NOT NULL UNIQUE, Designation VARCHAR(150) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
CREATE TABLE dbo.Ref_MoyenDetection ( Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), Code VARCHAR(50) NOT NULL UNIQUE, Designation VARCHAR(100) NOT NULL, Actif BIT NOT NULL DEFAULT 1 );
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
-- PARTIE 4.1 : LIAISON ERP <-> QUALITÉ
-- =================================================================================
ALTER TABLE dbo.ITMMASTER ADD 
    TypeRobinetCode VARCHAR(10) NULL REFERENCES dbo.TypeRobinet(Code),
    NatureComposantCode VARCHAR(20) NULL REFERENCES dbo.NatureComposant(Code);
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

-- =================================================================================
-- PARTIE 5 : PLAN D'ÉCHANTILLONNAGE (GLOBAL UNIQUE - V6.6)
-- =================================================================================
CREATE TABLE dbo.Plan_Echantillonnage_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    -- AUCUNE clé étrangère de contexte (Totalement Global)
    
    NiveauControle VARCHAR(5) NOT NULL CHECK (NiveauControle IN ('I','II','III')),
    TypePlan VARCHAR(10) NOT NULL CHECK (TypePlan IN ('SIMPLE','DOUBLE')),
    ModeControle VARCHAR(15) NOT NULL CHECK (ModeControle IN ('NORMAL','REDUIT','RENFORCE')),
    NqaId INT NOT NULL REFERENCES dbo.NQA(Id),
    Version INT NOT NULL DEFAULT 1,
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
-- PARTIE 6 : PLANS DE FABRICATION (USINAGE)
-- =================================================================================
CREATE TABLE dbo.Modele_Fab_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(60) NOT NULL,
    Libelle VARCHAR(150) NOT NULL,
    TypeRobinetCode VARCHAR(10) NULL REFERENCES dbo.TypeRobinet(Code),
    NatureComposantCode VARCHAR(20) NOT NULL REFERENCES dbo.NatureComposant(Code),
    OperationCode VARCHAR(20) NULL REFERENCES dbo.Operation(Code),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id), 
    Version INT NOT NULL DEFAULT 1,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    Notes NVARCHAR(MAX),
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
    FrequenceLibelle VARCHAR(80)
);
GO

CREATE TABLE dbo.Modele_Fab_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Modele_Fab_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Modele_Fab_Section(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeCaracteristiqueId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(200) NULL,
    ValeurNominale FLOAT NULL,
    ToleranceSuperieure FLOAT NULL,
    ToleranceInferieure FLOAT NULL,
    LimiteSpecTexte VARCHAR(100) NULL,
    Unite VARCHAR(10) NULL,
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
    Version INT NOT NULL DEFAULT 1,
    OperationCode VARCHAR(30),
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    DateApplication DATE,
    MachineDefautCode VARCHAR(30) REFERENCES dbo.Machine(CodeMachine),
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
    LibelleSection VARCHAR(200) NOT NULL,
    FrequenceLibelle VARCHAR(80)
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
    ValeurNominale FLOAT,
    ToleranceSuperieure FLOAT,
    ToleranceInferieure FLOAT,
    Unite VARCHAR(10),
    LimiteSpecTexte VARCHAR(100),
    Observations NVARCHAR(MAX),
    Instruction NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0
);
GO

-- =================================================================================
-- PARTIE 7 : PLANS D'ASSEMBLAGE (Unifiés)
-- =================================================================================
CREATE TABLE dbo.Plan_Ass_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OperationCode VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code), 
    TypeRobinetCode VARCHAR(10) NULL REFERENCES dbo.TypeRobinet(Code),
    EstModele BIT NOT NULL DEFAULT 1,
    CodeArticleSage VARCHAR(30) NULL,
    Nom VARCHAR(150) NOT NULL,        
    Designation VARCHAR(200),
    Version INT NOT NULL DEFAULT 1,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    NbPiecesReglage INT NOT NULL DEFAULT 5,
    FicheEchantillonnageId UNIQUEIDENTIFIER, 
    DateApplication DATE,
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id), 
    LegendeMoyens NVARCHAR(MAX) NULL, 
    
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    
    CommentaireVersion NVARCHAR(MAX),
    Remarques NVARCHAR(MAX) NULL,
    CONSTRAINT CHK_PlanAss_Modele CHECK ((EstModele = 1 AND CodeArticleSage IS NULL) OR (EstModele = 0 AND CodeArticleSage IS NOT NULL))
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
    LibelleSection VARCHAR(250) NOT NULL,
    NormeReference VARCHAR(40), 
    NqaId INT REFERENCES dbo.NQA(Id),
    Notes NVARCHAR(MAX)
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
    ValeurNominale FLOAT,
    ToleranceSuperieure FLOAT,
    ToleranceInferieure FLOAT,
    Unite VARCHAR(10),
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
-- PARTIE 8 : PLAN PRODUIT FINI (Normatif)
-- =================================================================================
CREATE TABLE dbo.Plan_PF_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TypeRobinetCode VARCHAR(10) NOT NULL REFERENCES dbo.TypeRobinet(Code),
    CodeArticleSage VARCHAR(30) NULL,
    Designation VARCHAR(200),
    Version INT NOT NULL DEFAULT 1,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    VisibleOperateur BIT NOT NULL DEFAULT 0 CHECK (VisibleOperateur = 0),
    DateApplication DATE,
    
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
    TypeSectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.TypeSection(Id),
    LibelleSection VARCHAR(250) NOT NULL,
    NormeReference VARCHAR(40),
    NqaId INT REFERENCES dbo.NQA(Id),
    NbEchantillons INT,
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
    ValeurNominale FLOAT,
    ToleranceSuperieure FLOAT,
    ToleranceInferieure FLOAT,
    Unite VARCHAR(10),
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
    Version INT DEFAULT 1,
    Statut VARCHAR(20) DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME DEFAULT GETDATE(),
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
    Version INT DEFAULT 1,
    Statut VARCHAR(20) DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE')),
    AfficheConformite BIT DEFAULT 1,
    AfficheMoyenDetectionRisques BIT DEFAULT 1,
    AfficheFamilles BIT DEFAULT 0,    
    AfficheFuiteEtalon BIT DEFAULT 0, 
    
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
    OrdreAffiche INT NOT NULL DEFAULT 0, 
    UNIQUE (PlanLigneId, PeriodiciteId, RefMoyenDetectionId)
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
-- PARTIE 11 : CONTRAINTES DE CLÉS ÉTRANGÈRES RETARDÉES
-- =================================================================================
ALTER TABLE dbo.Plan_Ass_Entete
    ADD CONSTRAINT fk_planass_echan
    FOREIGN KEY (FicheEchantillonnageId) REFERENCES dbo.Plan_Echantillonnage_Entete(Id);
GO

-- =================================================================================
-- PARTIE 12 : INDEX DE CONTRAINTES AVANCÉS (VERSIONING & UNICITÉ)
-- =================================================================================
CREATE UNIQUE NONCLUSTERED INDEX UQ_ModeleFab_Version ON dbo.Modele_Fab_Entete(TypeRobinetCode, NatureComposantCode, OperationCode, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanFab_Version ON dbo.Plan_Fab_Entete(CodeArticleSage, OperationCode, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanAss_Modele_Version ON dbo.Plan_Ass_Entete(OperationCode, TypeRobinetCode, Version) WHERE EstModele = 1 AND Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanAss_Instance_Version ON dbo.Plan_Ass_Entete(OperationCode, TypeRobinetCode, CodeArticleSage, Version) WHERE EstModele = 0 AND Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanPF_Version ON dbo.Plan_PF_Entete(TypeRobinetCode, CodeArticleSage, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');

-- ✅ NOUVEAUX INDEX POUR LE PLAN D'ÉCHANTILLONNAGE GLOBAL (V6.6)
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanEchan_Global_Version ON dbo.Plan_Echantillonnage_Entete(Version);
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanEchan_Global_Actif ON dbo.Plan_Echantillonnage_Entete(Statut) WHERE Statut = 'ACTIF';

CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanNC_Version ON dbo.Plan_NC_Entete(PosteCode, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanVerif_Version ON dbo.Plan_VerifMachine_Entete(MachineCode, Version) WHERE Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE');

CREATE UNIQUE NONCLUSTERED INDEX UX_ModeleFab_Actif ON dbo.Modele_Fab_Entete(TypeRobinetCode, NatureComposantCode, OperationCode) WHERE Statut = 'ACTIF';
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanFab_Actif ON dbo.Plan_Fab_Entete(CodeArticleSage, OperationCode) WHERE Statut = 'ACTIF';
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanAss_Maitre_Actif ON dbo.Plan_Ass_Entete(OperationCode, TypeRobinetCode) WHERE EstModele = 1 AND Statut = 'ACTIF';
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanAss_Exception_Actif ON dbo.Plan_Ass_Entete(OperationCode, TypeRobinetCode, CodeArticleSage) WHERE EstModele = 0 AND Statut = 'ACTIF';
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanPF_Actif ON dbo.Plan_PF_Entete(TypeRobinetCode, CodeArticleSage) WHERE Statut = 'ACTIF';
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanNC_Actif ON dbo.Plan_NC_Entete(PosteCode) WHERE Statut = 'ACTIF';
CREATE UNIQUE NONCLUSTERED INDEX UX_PlanVerif_Actif ON dbo.Plan_VerifMachine_Entete(MachineCode) WHERE Statut = 'ACTIF';
GO

-- =================================================================================
-- PARTIE 13 : SEED DATA (LES VRAIES DONNÉES SOPAL COMPLETES)
-- =================================================================================

-- 1. RÉFÉRENTIELS DE BASE
INSERT INTO dbo.Operation (Code, Libelle) VALUES 
('ASS', 'Assemblage'), 
('TRONC', 'Tronçonnage'), 
('ESTOMP', 'Estompage'), 
('USINAG', 'Usinage');

INSERT INTO dbo.TypeRobinet (Code, Libelle) VALUES 
('MAN', 'Manuelle'), 
('AUTO', 'Automatique'), 
('SOP', 'Automatique avec soupape');

INSERT INTO dbo.NatureComposant (Code, Libelle) VALUES 
('CORPS', 'Corps'), 
('VOLANT', 'Volant'), 
('PISTON', 'Piston'), 
('PF', 'Produit Fini');

INSERT INTO dbo.PosteTravail (CodePoste, Libelle) VALUES 
('PAS71', 'Poste 71'), 
('PAS72', 'Poste 72'), 
('PAS78', 'Poste 78');

-- 2. MACHINES ET POSTES
INSERT INTO dbo.Machine (CodeMachine, Libelle, OperationCode, RoleMachine) VALUES 
('MAS19', 'Machine 19', 'ASS', 'MAS_ASS'), 
('MAS22', 'Machine 22', 'ASS', 'MAS_ASS'), 
('MAS26', 'Machine 26', 'ASS', 'MAS_ASS'),
('BEE22', 'Banc 22', 'ASS', 'BEE'), 
('BEE46', 'Banc 46', 'ASS', 'BEE'), 
('BEE47', 'Banc 47', 'ASS', 'BEE'),
('SER05', 'Marquage 05', 'ASS', 'MARQUAGE');

INSERT INTO dbo.PosteTravail_Machine (CodePoste, CodeMachine) VALUES 
('PAS72', 'MAS26'), ('PAS72', 'BEE46'),
('PAS71', 'MAS22'), ('PAS71', 'MAS19'), ('PAS71', 'BEE47'),
('PAS78', 'SER05');

-- 3. ARTICLES SAGE (ITMMASTER)
INSERT INTO dbo.ITMMASTER (CodeArticle, Designation, FamilleProduit, Statut) VALUES 
('25A8B01-1', 'Boite à clapet', 'ROB', 'ACTIF'),
('25AKA01-1', 'R. de GAZ Boite GAZ', 'ROB', 'ACTIF'),
('25B0A01-1', 'R de gaz avec soupape', 'ROB', 'ACTIF'),
('25S2A01-1', 'R. de gaz Asil', 'ROB', 'ACTIF'),
('25S5A01-1', 'R de gaz H cuisiniere', 'ROB', 'ACTIF');

-- 4. DICTIONNAIRE DES RISQUES (Pour Plan NC)
INSERT INTO dbo.RisqueDefaut (CodeDefaut, LibelleDefaut) VALUES 
('R_30B_F', 'ESSAI DE 30 BAR (ROBINET FERME)'), ('R_01B_F', 'ESSAI DE 0,1 BAR (ROBINET FERME)'),
('R_30B_O', 'ESSAI DE 30 BAR (ROBINET OUVERT)'), ('R_01B_O', 'ESSAI DE 0,1 BAR (ROBINET OUVERT)'),
('R_GOUP', 'ABSENCE / MAUVAIS MONTAGE DE LA GOUPILLE'), ('R_PTDUR', 'PRESENCE DU POINT DURE'),
('R_J_AUTO', 'ABSENCE DU JOINT AUTOSERREUR'), ('R_J_ANTI', 'ABSENCE/ MAUVAIS MONTAGE JOINT ANTI-FUITE');

-- 5. DICTIONNAIRE DES FAMILLES (Pour Matrice)
INSERT INTO dbo.Ref_FamilleCorps (Code, Designation) VALUES 
('F_30_35', 'Famille Corps (30, 35)'), ('F_23', 'Famille Corps (23)'), 
('FAM_40_43_44', 'Famille Corps (40, 43, 44)'), ('FAM_49', 'Famille Corps (49)'),
('C_25B0A01', 'Corps (25B0A01)'), ('C_25AXA01', 'Corps (25AXA01)'),
('C_25AWA01', 'Corps (25AWA01)'), ('C_25UA01', 'Corps (25UA01)'),
('C_25A8A01', 'Corps (25A8A01)'), ('C_2588A01', 'Corps (2588A01)'),
('C_2576A01', 'Corps (2576A01)'), ('C_2519A01', 'Corps (2519A01)'),
('C_56', 'Corps (56)'), ('FAM_54_53', 'Famille Corps (54,53)'), 
('FAM_28_52', 'Famille Corps (28,52)'), ('C_25AUA01', 'Corps (25AUA01)');

-- 6. DICTIONNAIRE DES MOYENS DE DÉTECTION
INSERT INTO dbo.Ref_MoyenDetection (Code, Designation) VALUES 
('PRC', 'PRC'), ('PRNC', 'PRNC'), 
('PRC_FEC', 'PRC + FEC'), ('PRC_FENC', 'PRC + FENC'), 
('PRC_LS', 'PRC LS'), ('PRC_LI', 'PRC LI'), 
('PRNC_LS', 'PRNC LS'), ('PRNC_LI', 'PRNC LI');

-- 7. PIÈCES DE RÉFÉRENCE (Globale)
INSERT INTO dbo.PieceReference (Code, TypePiece, Designation, MachineCode) VALUES 
('RCF01', 'PRC', 'Conforme F30', 'MAS22'), ('RCF02', 'PRC', 'Conforme F23', 'MAS22'), 
('RCF03', 'PRC', 'Conforme F40', 'MAS22'), ('RCF04', 'PRC', 'Conforme F49', 'MAS22'),
('RCF05', 'PRC', 'Pièce conforme SER05', 'SER05'), ('RCF06', 'PRC', 'Pièce conforme SER05', 'SER05'), ('RCF07', 'PRC', 'Pièce conforme SER05', 'SER05'),
('RCN04', 'PRNC', 'Pièce bouchée SER05', 'SER05'), ('RCN05', 'PRNC', 'Pièce bouchée SER05', 'SER05'), ('RCN06', 'PRNC', 'Pièce bouchée SER05', 'SER05'),
('BCF01', 'PRC', 'Conforme BEE46 (1)', 'BEE46'), ('BCF02', 'PRC', 'Conforme BEE46 (2)', 'BEE46'),
('BCF03', 'PRC', 'Conforme BEE46 (3)', 'BEE46'), ('BCF04', 'PRC', 'Conforme BEE46 (4)', 'BEE46'),
('RBG01', 'PRC', 'Conforme BEE22', 'BEE22'),
('RBG03', 'PRC', 'Pièce référence RBG03', NULL), ('RBG04', 'PRC', 'Pièce référence RBG04', NULL),
('RBG07', 'PRNC', 'PRNC Limite Supérieur', 'MAS19'), ('RBG08', 'PRC', 'PRC Limite Inférieur', 'MAS19'), 
('RBG09', 'PRC', 'PRC Limite Supérieur', 'MAS19'), ('RBG10', 'PRNC', 'PRNC Limite Inférieur', 'MAS19'),
('BSJ01', 'PRNC', 'Sans joint siège', 'MAS26'), ('BSB01', 'PRNC', 'Sans bille', 'MAS26'),
('BSF01', 'PRNC', 'Sans joint antifuite', 'MAS26'), ('BJR01', 'PRNC', 'Joint antifuite NC', 'MAS26'),
('BCL01', 'PRNC', 'Défaut sertissage 1', 'MAS26'), ('BCC01', 'PRNC', 'Défaut sertissage 2', 'MAS26'),
('BCN01', 'PRNC', 'Passage gaz 1', 'MAS26'), ('BCO01', 'PRNC', 'Passage gaz 2', 'MAS26'),
('TMI01', 'PRNC', 'Nb tours < 2,8', 'MAS22'), ('TMX01', 'PRNC', 'Nb tours > 3,8', 'MAS22'),
('RSG01', 'PRNC', 'Sans goupille', 'MAS22'), ('RSJ01', 'PRNC', 'Manque joint auto-serreur', 'MAS22'),
('FUI24', 'FEC', 'Fuite Etalon C', NULL), ('FUI25', 'FENC', 'Fuite Etalon NC', NULL);

-- 8. PÉRIODICITÉS
INSERT INTO dbo.Periodicite (Code, Libelle) VALUES ('DEM', 'Au démarrage de la machine'), ('PAU', 'Après la pause'), ('FIN', 'A la fin du poste');

-- 9. NQA ET TYPES
INSERT INTO dbo.NQA (ValeurNQA) VALUES (0.65),(1.0),(1.5),(2.5),(4.0),(6.5);
INSERT INTO dbo.TypeCaracteristique (Code, Libelle, EstNumerique) VALUES ('VISUEL', 'Contrôle visuel', 0), ('DIAMETRE', 'Diamètre', 1), ('COUPLE', 'Couple serrage', 1);
INSERT INTO dbo.TypeControle (Code, Libelle) VALUES ('VISUEL', 'Visuel'), ('MESURE', 'Mesure'), ('ATTRIBUT', 'Attribut');
INSERT INTO dbo.MoyenControle (Code, Libelle) VALUES ('DEFAUTHEQUE', 'Défauthèque'), ('PAC', 'Pied à coulisse'), ('BANC_ESSAI', 'Banc d''essai'), ('CLE_DYN', 'Clé dynamométrique');
INSERT INTO dbo.TypeSection (Code, Libelle) VALUES ('REGLAGE', 'Aux réglages'), ('EN_COURS', 'En cours de production'), ('ECHANTILLONNAGE', 'par échantillonnage');
GO

PRINT '=======================================================';
PRINT ' LA BASE SOPALTRACE V6.6 A ÉTÉ GÉNÉRÉE AVEC SUCCÈS !';
PRINT '=======================================================';