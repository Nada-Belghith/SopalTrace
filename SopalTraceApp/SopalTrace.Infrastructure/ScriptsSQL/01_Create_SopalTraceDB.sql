-- ================================================================================
-- SOPAL TRACE DB - SCHEMA NORMALISÉ V3.0 (ULTIME)
-- Architecture avancée : NatureArticle & Origine (Make vs Buy)
-- ================================================================================

USE master;
GO

-- Backup et suppression de l'ancienne base
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

-- ================================================================================
-- PARTIE 0: TABLES DE RÉFÉRENCE (Non supprimables - ISO 9001)
-- ================================================================================

CREATE TABLE dbo.TypeRobinet (
    Code VARCHAR(10) PRIMARY KEY,
    Libelle VARCHAR(60) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.FamilleProduitFini (
    Code VARCHAR(30) PRIMARY KEY,
    Designation VARCHAR(250) NOT NULL,
    TypeRobinetCode VARCHAR(10) REFERENCES dbo.TypeRobinet(Code),
    Actif BIT NOT NULL DEFAULT 1
);
GO

-- NOUVELLE TABLE UNIFIÉE (Remplace NatureComposant)
CREATE TABLE dbo.NatureArticle (
    Code VARCHAR(20) PRIMARY KEY,
    Libelle VARCHAR(100) NOT NULL,
    Origine VARCHAR(10) NOT NULL CHECK (Origine IN ('FABRIQUE', 'ACHETE')),
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Ref_FamilleCorps (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(50) NOT NULL UNIQUE,
    Designation VARCHAR(150) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Operation (
    Code VARCHAR(20) PRIMARY KEY,
    Libelle VARCHAR(80) NOT NULL,
    OrdreProcess INT NOT NULL DEFAULT 0,
    Actif BIT NOT NULL DEFAULT 1
);
GO

-- MISE A JOUR : Pointe vers NatureArticle
CREATE TABLE dbo.NatureArticle_Operation (
    NatureArticleCode VARCHAR(20) NOT NULL REFERENCES dbo.NatureArticle(Code) ON DELETE CASCADE,
    OperationCode VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code) ON DELETE CASCADE,
    OrdreGamme INT NOT NULL DEFAULT 1,
    EstObligatoire BIT NOT NULL DEFAULT 1,
    PRIMARY KEY (NatureArticleCode, OperationCode)
);
GO

CREATE TABLE dbo.PosteTravail (
    CodePoste VARCHAR(30) PRIMARY KEY,
    Libelle VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.TypeCaracteristique (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(80) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.TypeControle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(80) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.MoyenControle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(40) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Periodicite (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(200) NOT NULL,
    FrequenceNum INT,
    FrequenceUnite VARCHAR(100),
    OrdreAffichage INT NOT NULL DEFAULT 0,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Ref_RegleEchantillonnage (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(250) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.TypeSection (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(300) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Defautheque (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Description VARCHAR(200),
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.RisqueDefaut (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeDefaut VARCHAR(30) NOT NULL UNIQUE,
    LibelleDefaut VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.NQA (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ValeurNQA FLOAT NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.Ref_MoyenDetection (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(50) NOT NULL UNIQUE,
    Designation VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Instrument (
    CodeInstrument VARCHAR(40) PRIMARY KEY,
    Designation VARCHAR(100) NOT NULL,
    Categorie VARCHAR(40),
    PrecisionLecture FLOAT,
    Unite VARCHAR(10),
    DateEtalonnage DATE,
    DateProchaineVerif DATE,
    Statut VARCHAR(20) NOT NULL DEFAULT 'ACTIF',
    Actif BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.PieceReference (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    TypePiece VARCHAR(10) NOT NULL CHECK (TypePiece IN ('PRC','PRNC','FEC','FENC')),
    Designation VARCHAR(150),
    FamilleDesc VARCHAR(60),
    FamilleCorpsId UNIQUEIDENTIFIER REFERENCES dbo.Ref_FamilleCorps(Id),
    Actif BIT NOT NULL DEFAULT 1
);
GO

-- ================================================================================
-- PARTIE 1: AUTHENTIFICATION ET SÉCURITÉ
-- ================================================================================

CREATE TABLE dbo.UtilisateursApp (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Matricule VARCHAR(20) NOT NULL UNIQUE,
    NomComplet VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    MotDePasseHash VARCHAR(255) NOT NULL,
    RoleApp VARCHAR(50) NOT NULL,
    IntituleMetier VARCHAR(100),
    CodeRecuperation VARCHAR(6),
    DateExpirationCode DATETIME,
    DateCreation DATETIME DEFAULT GETDATE(),
    DateDerniereConnexion DATETIME,
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
    Details VARCHAR(255),
    DateAction DATETIME DEFAULT GETDATE()
);
GO

-- ================================================================================
-- PARTIE 2: ARTICLES (AVEC NATURE INTEGREE)
-- ================================================================================

-- TABLE DE BASE: Article
CREATE TABLE dbo.Article (
    CodeArticle VARCHAR(30) PRIMARY KEY,
    Designation VARCHAR(150) NOT NULL,
    Designation2 VARCHAR(150),
    NatureArticleCode VARCHAR(20) NOT NULL REFERENCES dbo.NatureArticle(Code), -- NOUVEAU: La nature est gérée ici !
    TypeArticle VARCHAR(20) NOT NULL CHECK (TypeArticle IN ('PF', 'SF', 'COMPOSANT')),
    Statut VARCHAR(10) NOT NULL DEFAULT 'ACTIF',
    DateCreation DATETIME DEFAULT GETDATE(),
    DateModification DATETIME,
    Actif BIT NOT NULL DEFAULT 1
);
GO

-- SPÉCIALISATION: Produit Fini
CREATE TABLE dbo.ProduitFini (
    CodeArticle VARCHAR(30) PRIMARY KEY REFERENCES dbo.Article(CodeArticle) ON DELETE CASCADE,
    FamilleProduitFiniCode VARCHAR(30) NOT NULL REFERENCES dbo.FamilleProduitFini(Code),
    TypeRobinetCode VARCHAR(10) NOT NULL REFERENCES dbo.TypeRobinet(Code)
);
GO


-- Nomenclature
CREATE TABLE dbo.BOMD_Nomenclature (
    ArticleParent VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle), 
    CodeComposant VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    CodeAlternative INT NOT NULL,
    QuantiteRequise FLOAT NOT NULL,
    PRIMARY KEY (ArticleParent, CodeComposant, CodeAlternative)
);
GO

-- ================================================================================
-- PARTIE 3: ERP - SAGE X3 CACHE
-- ================================================================================

CREATE TABLE dbo.AUTILIS (
    USR_0 VARCHAR(5) PRIMARY KEY,
    INTUSR_0 VARCHAR(100) NOT NULL,
    ENAFLG_0 INT NOT NULL DEFAULT 1,
    CODMET_0 VARCHAR(20) NOT NULL,
    ADDEML_0 VARCHAR(150)
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

-- ================================================================================
-- PARTIE 4: ORDRES DE FABRICATION ET MATIÈRES
-- ================================================================================

CREATE TABLE dbo.MFGHEAD_OrdreFabrication (
    NumeroOF VARCHAR(30) PRIMARY KEY,
    CodeArticle VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    QuantitePrevue FLOAT NOT NULL,
    QuantiteLancee FLOAT NOT NULL DEFAULT 0,
    QuantiteReelle FLOAT NOT NULL DEFAULT 0,
    StatutOF VARCHAR(20) NOT NULL,
    DateDebut DATETIME,
    DateFin DATETIME,
    CONSTRAINT CK_OF_OFStatusValid CHECK (StatutOF IN ('EN_COURS', 'PLANIFIE', 'TERMINE', 'ANNULE'))
);
GO

CREATE TABLE dbo.MFGMAT_BesoinOF (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NumeroOF VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF) ON DELETE CASCADE,
    CodeArticle VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
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

-- ================================================================================
-- PARTIE 5: MACHINES ET POSTES DE TRAVAIL
-- ================================================================================

CREATE TABLE dbo.Machine (
    CodeMachine VARCHAR(30) PRIMARY KEY,
    Libelle VARCHAR(100) NOT NULL,
    OperationCode VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code),
    TypeAffectation VARCHAR(15) NOT NULL DEFAULT 'INDEPENDANTE' 
        CHECK (TypeAffectation IN ('INDEPENDANTE', 'POSTE')),
    RoleMachine VARCHAR(20) CHECK (RoleMachine IN ('BEE', 'MAS_ASS', 'MARQUAGE', 'USINAG', 'TRONC', 'ESTOMP')),
    Actif BIT NOT NULL DEFAULT 1
);
GO

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

-- ================================================================================
-- PARTIE 6: FORMULAIRES DE CONTRÔLE
-- ================================================================================

CREATE TABLE dbo.Ref_Formulaire (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeReference VARCHAR(30) NOT NULL,
    Designation VARCHAR(150) NOT NULL,
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'ACTIF',
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    Role VARCHAR(50) NULL,
    ConfigurationStructureJson NVARCHAR(MAX) NULL,
    UNIQUE (CodeReference, Version)
);
GO
 



CREATE TABLE dbo.OutilControle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(40) NOT NULL UNIQUE,
    Libelle VARCHAR(150) NOT NULL,
    TypeControleId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeControle(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER REFERENCES dbo.TypeCaracteristique(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    PeriodiciteDefautId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    LimiteSpecTexteDefaut VARCHAR(100),
    InstructionDefaut NVARCHAR(MAX),
    Actif BIT NOT NULL DEFAULT 1
);
GO

-- ================================================================================
-- PARTIE 7: PLAN D'ÉCHANTILLONNAGE
-- ================================================================================

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
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    CommentaireVersion NVARCHAR(MAX),
    Remarques NVARCHAR(MAX),
    LegendeMoyens NVARCHAR(MAX)
);
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

-- ================================================================================
-- PARTIE 8: MODÈLES DE FABRICATION (USINAGE - BASE)
-- ================================================================================

CREATE TABLE dbo.Modele_Fabrication_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(60) NOT NULL,
    Libelle VARCHAR(150) NOT NULL,
    NatureArticleCode VARCHAR(20) NOT NULL REFERENCES dbo.NatureArticle(Code),
    OperationCode VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code),
    FamilleProduitFiniCode VARCHAR(30) REFERENCES dbo.FamilleProduitFini(Code),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' 
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    Notes NVARCHAR(MAX),
    LegendeMoyens NVARCHAR(MAX),




    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    UNIQUE (Code, Libelle, Version)
);
GO

CREATE TABLE dbo.Modele_Fabrication_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Modele_Fabrication_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    LibelleSection VARCHAR(200) NOT NULL,
    TypeSectionId UNIQUEIDENTIFIER REFERENCES dbo.TypeSection(Id),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    RegleEchantillonnageId UNIQUEIDENTIFIER REFERENCES dbo.Ref_RegleEchantillonnage(Id)
);
GO

CREATE TABLE dbo.Modele_Fabrication_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Modele_Fabrication_Section(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeCaracteristiqueId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(200),
    LimiteSpecTexte VARCHAR(100),
    TypeControleId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeControle(Id),
    InstrumentCode VARCHAR(40) REFERENCES dbo.Instrument(CodeInstrument),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    Instruction NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0,
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    MoyenTexteLibre VARCHAR(100),
    Observations NVARCHAR(MAX) NULL,
    ColonnesSupplementaires NVARCHAR(MAX) NULL,
    ImageBase64 VARCHAR(MAX) NULL
);
GO

ALTER TABLE dbo.Modele_Fabrication_Ligne
ADD CONSTRAINT CK_ModeleFab_Ligne_Moyen_XOR CHECK (
    (MoyenControleId IS NOT NULL AND MoyenTexteLibre IS NULL)
    OR
    (MoyenControleId IS NULL AND MoyenTexteLibre IS NOT NULL)
);
GO

-- ================================================================================
-- PARTIE 9: PLANS DE FABRICATION (USINAGE - INSTANCES)
-- ================================================================================

CREATE TABLE dbo.Plan_Fabrication_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleSourceId UNIQUEIDENTIFIER REFERENCES dbo.Modele_Fabrication_Entete(Id),
    CodeArticleSage VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    Designation VARCHAR(200),
    Nom VARCHAR(150) NOT NULL,
    Version INT NOT NULL DEFAULT 0,
    OperationCode VARCHAR(20) REFERENCES dbo.Operation(Code),
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' 
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    
    MachineDefautCode VARCHAR(30) REFERENCES dbo.Machine(CodeMachine),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    LegendeMoyens NVARCHAR(MAX),




    Remarques NVARCHAR(MAX),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME
);
GO

CREATE TABLE dbo.Plan_Fabrication_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Fabrication_Entete(Id) ON DELETE CASCADE,
    ModeleSectionId UNIQUEIDENTIFIER REFERENCES dbo.Modele_Fabrication_Section(Id),
    OrdreAffiche INT NOT NULL DEFAULT 0,
    LibelleSection VARCHAR(300) NOT NULL,
    TypeSectionId UNIQUEIDENTIFIER REFERENCES dbo.TypeSection(Id),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    RegleEchantillonnageId UNIQUEIDENTIFIER REFERENCES dbo.Ref_RegleEchantillonnage(Id)
);
GO

CREATE TABLE dbo.Plan_Fabrication_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Fabrication_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Fabrication_Section(Id) ON DELETE CASCADE,
    ModeleLigneSourceId UNIQUEIDENTIFIER REFERENCES dbo.Modele_Fabrication_Ligne(Id),
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeCaracteristiqueId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(200),
    TypeControleId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeControle(Id),
    InstrumentCode VARCHAR(40) REFERENCES dbo.Instrument(CodeInstrument),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    LimiteSpecTexte VARCHAR(100),
    Observations NVARCHAR(MAX),
    Instruction NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0,
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    MoyenTexteLibre VARCHAR(100),
    ColonnesSupplementaires NVARCHAR(MAX) NULL,
    ImageBase64 VARCHAR(MAX) NULL
);
GO

ALTER TABLE dbo.Plan_Fabrication_Ligne
ADD CONSTRAINT CK_PlanFab_Ligne_Moyen_XOR CHECK (
    (MoyenControleId IS NOT NULL AND MoyenTexteLibre IS NULL)
    OR
    (MoyenControleId IS NULL AND MoyenTexteLibre IS NOT NULL)
);
GO

-- ================================================================================
-- PARTIE 10: PLANS D'ASSEMBLAGE (AVEC FAMILLE)
-- ================================================================================

CREATE TABLE dbo.Plan_Assemblage_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OperationCode VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code),
    FamilleProduitFiniCode VARCHAR(30) REFERENCES dbo.FamilleProduitFini(Code),
    NatureArticleCode VARCHAR(20) REFERENCES dbo.NatureArticle(Code),
    PosteCode VARCHAR(30) REFERENCES dbo.PosteTravail(CodePoste),
    Designation VARCHAR(200),
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' 
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    LegendeMoyens NVARCHAR(MAX),




    Remarques NVARCHAR(MAX),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME
);
GO

CREATE TABLE dbo.Plan_Assemblage_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Assemblage_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeSectionId UNIQUEIDENTIFIER REFERENCES dbo.TypeSection(Id),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    LibelleSection VARCHAR(300) NOT NULL,
    NormeReference VARCHAR(40),
    NqaId INT REFERENCES dbo.NQA(Id),
    Notes NVARCHAR(MAX),
    RegleEchantillonnageId UNIQUEIDENTIFIER REFERENCES dbo.Ref_RegleEchantillonnage(Id)
);
GO

CREATE TABLE dbo.Plan_Assemblage_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Assemblage_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Assemblage_Section(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeCaracteristiqueId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(250),
    TypeControleId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeControle(Id),
    MachineCode VARCHAR(30) REFERENCES dbo.Machine(CodeMachine),
    InstrumentCode VARCHAR(40) REFERENCES dbo.Instrument(CodeInstrument),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    LimiteSpecTexte VARCHAR(100),
    EstVerifPresence BIT NOT NULL DEFAULT 0,
    DefauthequeId UNIQUEIDENTIFIER REFERENCES dbo.Defautheque(Id),
    RefPlanProduit VARCHAR(60),
    Instruction NVARCHAR(MAX),
    Observations NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0,
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    MoyenTexteLibre VARCHAR(100),
    ColonnesSupplementaires NVARCHAR(MAX) NULL,
    ImageBase64 VARCHAR(MAX) NULL
);
GO

ALTER TABLE dbo.Plan_Assemblage_Ligne
ADD CONSTRAINT CK_PlanAss_Ligne_Moyen_XOR CHECK (
    (MoyenControleId IS NOT NULL AND MoyenTexteLibre IS NULL)
    OR
    (MoyenControleId IS NULL AND MoyenTexteLibre IS NOT NULL)
);
GO

-- ================================================================================
-- PARTIE 11: PLAN PRODUIT FINI
-- ================================================================================

CREATE TABLE dbo.Plan_ProduitFini_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FamilleProduitFiniCode VARCHAR(30) NOT NULL REFERENCES dbo.FamilleProduitFini(Code),
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' 
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    LegendeMoyens NVARCHAR(MAX),




    Remarques NVARCHAR(MAX),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    UNIQUE (FamilleProduitFiniCode, Version)
);
GO

CREATE TABLE dbo.Plan_ProduitFini_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_ProduitFini_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeSectionId UNIQUEIDENTIFIER REFERENCES dbo.TypeSection(Id),
    LibelleSection VARCHAR(300) NOT NULL,
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    RegleEchantillonnageId UNIQUEIDENTIFIER REFERENCES dbo.Ref_RegleEchantillonnage(Id),
    Notes NVARCHAR(MAX)
);
GO

CREATE TABLE dbo.Plan_ProduitFini_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_ProduitFini_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_ProduitFini_Section(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeCaracteristiqueId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(250),
    TypeControleId UNIQUEIDENTIFIER NULL REFERENCES dbo.TypeControle(Id),
    InstrumentCode VARCHAR(40) REFERENCES dbo.Instrument(CodeInstrument),
    LimiteSpecTexte VARCHAR(100) NULL,
    DefauthequeId UNIQUEIDENTIFIER REFERENCES dbo.Defautheque(Id),
    Instruction NVARCHAR(MAX),
    Observations NVARCHAR(MAX),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    MoyenTexteLibre VARCHAR(100),
    ColonnesSupplementaires NVARCHAR(MAX) NULL,
    ImageBase64 VARCHAR(MAX) NULL
);
GO

ALTER TABLE dbo.Plan_ProduitFini_Ligne
ADD CONSTRAINT CK_PlanPF_Ligne_Moyen_XOR CHECK (
    (MoyenControleId IS NOT NULL AND MoyenTexteLibre IS NULL)
    OR
    (MoyenControleId IS NULL AND MoyenTexteLibre IS NOT NULL)
);
GO

-- ================================================================================
-- PARTIE 12: PLANS DE NON-CONFORMITÉ (TALLY)
-- ================================================================================

CREATE TABLE dbo.Plan_ControlePoste_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PosteCode VARCHAR(30) NOT NULL REFERENCES dbo.PosteTravail(CodePoste),
    Nom VARCHAR(150) NOT NULL,
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' 
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    ConfigurationColonnesJson NVARCHAR(MAX),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    Remarques NVARCHAR(MAX),
    LegendeMoyens NVARCHAR(MAX)
    -- UNIQUE (PosteCode, Version)
);
GO

CREATE TABLE dbo.Plan_ControlePoste_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ControlePosteEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_ControlePoste_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL,
    MachineCode VARCHAR(30) NOT NULL REFERENCES dbo.Machine(CodeMachine),
    RisqueDefautId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.RisqueDefaut(Id),
    UNIQUE (ControlePosteEnteteId, OrdreAffiche)
);
GO

-- ================================================================================
-- PARTIE 13: PLANS DE VÉRIFICATION MACHINE (MATRICE 3D)
-- ================================================================================

CREATE TABLE dbo.Plan_VerifMachine_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    MachineCode VARCHAR(30) NOT NULL REFERENCES dbo.Machine(CodeMachine),
    Nom VARCHAR(150) NOT NULL,
    Version INT DEFAULT 0,
    Statut VARCHAR(20) DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE')),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    ConfigurationColonnesJson NVARCHAR(MAX),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    Remarques NVARCHAR(MAX),
    LegendeMoyens NVARCHAR(MAX),




    -- UNIQUE (MachineCode, Version)
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
    LibelleMethode VARCHAR(250),
    ColonnesSupplementaires NVARCHAR(MAX)
);
GO

CREATE TABLE dbo.Plan_VerifMachine_Echeance (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanLigneId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Ligne(Id) ON DELETE CASCADE,
    PeriodiciteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Periodicite(Id),
    RefMoyenDetectionId UNIQUEIDENTIFIER REFERENCES dbo.Ref_MoyenDetection(Id),
    OrdreAffiche INT NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.Plan_VerifMachine_MatricePiece (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EcheanceId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Echeance(Id) ON DELETE CASCADE,
    FamilleId UNIQUEIDENTIFIER REFERENCES dbo.Plan_VerifMachine_Famille(Id),
    RoleVerif VARCHAR(10) CHECK (RoleVerif IN ('PRC', 'PRNC', 'FEC', 'FENC')),
    PieceRefId UNIQUEIDENTIFIER REFERENCES dbo.PieceReference(Id),
    UNIQUE (EcheanceId, FamilleId, RoleVerif)
);
GO

-- ================================================================================
-- PARTIE 14: MODULE MAGASIN (PRÉPARATION & EXPÉDITION)
-- ================================================================================

CREATE TABLE dbo.Mag_PreparationOF (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NumeroOF VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    MatriculeMagasinier VARCHAR(20) NOT NULL,
    Statut VARCHAR(20) NOT NULL DEFAULT 'EN_COURS' CHECK (Statut IN ('EN_COURS', 'PLANIFIE', 'TERMINE', 'ANNULE')),
    DateDebut DATETIME DEFAULT GETDATE(),
    DateFin DATETIME
);
GO

CREATE TABLE dbo.Mag_PreparationOF_Lot (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PreparationOFId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Mag_PreparationOF(Id) ON DELETE CASCADE,
    CodeArticle VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    NumeroLotScanne VARCHAR(100) NOT NULL,
    Quantite FLOAT NOT NULL,
    DateScan DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.Mag_QuickControl_Rapport (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NumeroOF VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    CodeArticle VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    NumeroRapportQC VARCHAR(100) NOT NULL,
    DateScan DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.Mag_ExpeditionBL (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NumeroBL VARCHAR(30) NOT NULL REFERENCES dbo.SDELIVERY(NumeroBL),
    MatriculeMagasinier VARCHAR(20) NOT NULL,
    Statut VARCHAR(20) NOT NULL DEFAULT 'EN_COURS' CHECK (Statut IN ('EN_COURS', 'TERMINE')),
    DateDebut DATETIME DEFAULT GETDATE(),
    DateFin DATETIME
);
GO

CREATE TABLE dbo.Mag_ExpeditionBL_ScanOF (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExpeditionBLId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Mag_ExpeditionBL(Id) ON DELETE CASCADE,
    NumeroOFScanne VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    DateScan DATETIME DEFAULT GETDATE()
);
GO

-- ================================================================================
-- PARTIE 15: EXÉCUTION DES CONTRÔLES (OPÉRATEUR)
-- ================================================================================

CREATE TABLE dbo.Exec_ControleOF (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NumeroOF VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    OperationCode VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code),
    MachineCodePrevu VARCHAR(30) REFERENCES dbo.Machine(CodeMachine), -- Poste prévu
    PosteCodePrevu VARCHAR(30) REFERENCES dbo.PosteTravail(CodePoste),  -- Poste prévu (si type POSTE)
    MachineCode VARCHAR(30) REFERENCES dbo.Machine(CodeMachine),    -- Poste réel choisi
    PosteCode VARCHAR(30) REFERENCES dbo.PosteTravail(CodePoste),   -- Poste réel (si type POSTE)
    NumEquipe INT NOT NULL DEFAULT 1,                               -- Eq (Equipe 1, 2...)
    PlanSourceId UNIQUEIDENTIFIER NOT NULL,
    TypePlan VARCHAR(10) NOT NULL CHECK (TypePlan IN ('FAB', 'ASS')),
    Statut VARCHAR(20) NOT NULL DEFAULT 'EN_COURS' CHECK (Statut IN ('EN_COURS', 'CLOTURE')),
    DateDebut DATETIME NOT NULL DEFAULT GETDATE(),
    DateFin DATETIME
);
GO

CREATE TABLE dbo.Exec_PieceType (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExecControleOFId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id) ON DELETE CASCADE,
    HeureValidation DATETIME NOT NULL DEFAULT GETDATE(),
    Resultat VARCHAR(2) NOT NULL CHECK (Resultat IN ('C', 'NC')),
    Remarque VARCHAR(500),
    MatriculeOperateur VARCHAR(20) NOT NULL
);
GO

CREATE TABLE dbo.Exec_ControleTranche (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExecControleOFId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id) ON DELETE CASCADE,
    TrancheHoraire VARCHAR(20) NOT NULL,
    HeureDebut DATETIME NOT NULL,
    HeureFin DATETIME NOT NULL,
    ResultatFinal VARCHAR(2) CHECK (ResultatFinal IN ('C', 'NC')),
    DetailsNC VARCHAR(500),
    ActionsCorrection VARCHAR(500),
    MatriculeApprobateur VARCHAR(20)
);
GO

CREATE TABLE dbo.Exec_Prelevement (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExecControleTrancheId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleTranche(Id) ON DELETE CASCADE,
    HeurePrevue DATETIME NOT NULL,
    HeureSaisie DATETIME,
    ResultatGlobal VARCHAR(2) CHECK (ResultatGlobal IN ('C', 'NC')),
    MatriculeOperateur VARCHAR(20)
);
GO

CREATE TABLE dbo.Exec_Prelevement_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PrelevementId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_Prelevement(Id) ON DELETE CASCADE,
    LignePlanId UNIQUEIDENTIFIER NOT NULL,
    Resultat VARCHAR(2) CHECK (Resultat IN ('C', 'NC')),
    ValeurMesuree FLOAT,
    Remarque VARCHAR(500)
);
GO

-- ================================================================================
-- PARTIE 16: TRIGGERS ISO 9001 (PROTECTION CONTRE SUPPRESSIONS)
-- ================================================================================

CREATE OR ALTER PROCEDURE dbo.sp_RaiseDeleteError (@TableName VARCHAR(100))
AS
BEGIN
    DECLARE @Msg VARCHAR(200) = 'La suppression physique est interdite (ISO 9001) sur ' + @TableName;
    RAISERROR(@Msg, 16, 1);
END;
GO

-- Apply triggers to all reference tables
CREATE TRIGGER trg_no_del_TypeRobinet ON dbo.TypeRobinet INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeRobinet'; END;
GO
CREATE TRIGGER trg_no_del_FamilleProduitFini ON dbo.FamilleProduitFini INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'FamilleProduitFini'; END;
GO
CREATE TRIGGER trg_no_del_NatureArticle ON dbo.NatureArticle INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'NatureArticle'; END;
GO
CREATE TRIGGER trg_no_del_Ref_FamilleCorps ON dbo.Ref_FamilleCorps INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_FamilleCorps'; END;
GO
CREATE TRIGGER trg_no_del_Operation ON dbo.Operation INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Operation'; END;
GO
CREATE TRIGGER trg_no_del_PosteTravail ON dbo.PosteTravail INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'PosteTravail'; END;
GO
CREATE TRIGGER trg_no_del_TypeCaracteristique ON dbo.TypeCaracteristique INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeCaracteristique'; END;
GO
CREATE TRIGGER trg_no_del_TypeControle ON dbo.TypeControle INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeControle'; END;
GO
CREATE TRIGGER trg_no_del_MoyenControle ON dbo.MoyenControle INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'MoyenControle'; END;
GO
CREATE TRIGGER trg_no_del_Periodicite ON dbo.Periodicite INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Periodicite'; END;
GO
CREATE TRIGGER trg_no_del_Ref_RegleEchantillonnage ON dbo.Ref_RegleEchantillonnage INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_RegleEchantillonnage'; END;
GO
CREATE TRIGGER trg_no_del_TypeSection ON dbo.TypeSection INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeSection'; END;
GO
CREATE TRIGGER trg_no_del_Defautheque ON dbo.Defautheque INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Defautheque'; END;
GO
CREATE TRIGGER trg_no_del_RisqueDefaut ON dbo.RisqueDefaut INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'RisqueDefaut'; END;
GO
CREATE TRIGGER trg_no_del_NQA ON dbo.NQA INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'NQA'; END;
GO
CREATE TRIGGER trg_no_del_Ref_MoyenDetection ON dbo.Ref_MoyenDetection INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_MoyenDetection'; END;
GO
CREATE TRIGGER trg_no_del_Instrument ON dbo.Instrument INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Instrument'; END;
GO
CREATE TRIGGER trg_no_del_PieceReference ON dbo.PieceReference INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'PieceReference'; END;
GO

-- ================================================================================
-- PARTIE 17: RESULTATS DU CONTROLE EN COURS DE FABRICATION
-- ================================================================================

CREATE TABLE dbo.Plan_ResultatControleCF_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PosteCode VARCHAR(30) NOT NULL REFERENCES dbo.PosteTravail(CodePoste),
    FormulaireId UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    Nom VARCHAR(150) NOT NULL,
    Version INT NOT NULL DEFAULT 0,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON', 'ACTIF', 'ARCHIVE')),
    ConfigurationJson NVARCHAR(MAX),
    Remarques NVARCHAR(MAX),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME
);
GO

CREATE TABLE dbo.Plan_ResultatControleCF_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanRCCFEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_ResultatControleCF_Entete(Id) ON DELETE CASCADE,
    SectionType VARCHAR(50) NOT NULL,
    LibelleAffiche VARCHAR(200) NOT NULL,
    OrdreAffiche INT NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.Plan_ResultatControleCF_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_ResultatControleCF_Section(Id) ON DELETE CASCADE,
    Caracteristique VARCHAR(300) NOT NULL,
    LimiteSpecTexte NVARCHAR(MAX),
    TypeControleId UNIQUEIDENTIFIER REFERENCES dbo.TypeControle(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    InstrumentCode VARCHAR(40) REFERENCES dbo.Instrument(CodeInstrument),
    Observations NVARCHAR(MAX),
    OrdreAffiche INT NOT NULL DEFAULT 0
);
GO

-- ================================================================================
-- INDEXES POUR PERFORMANCE
-- ================================================================================

CREATE INDEX IX_Article_TypeArticle ON dbo.Article(TypeArticle);
CREATE INDEX IX_Article_NatureArticle ON dbo.Article(NatureArticleCode);
CREATE INDEX IX_ProduitFini_FamilleCode ON dbo.ProduitFini(FamilleProduitFiniCode);
CREATE INDEX IX_BOMD_Parent ON dbo.BOMD_Nomenclature(ArticleParent);
CREATE INDEX IX_BOMD_Composant ON dbo.BOMD_Nomenclature(CodeComposant);
CREATE INDEX IX_MFGHEAD_CodeArticle ON dbo.MFGHEAD_OrdreFabrication(CodeArticle);
CREATE INDEX IX_PlanFab_CodeArticle ON dbo.Plan_Fabrication_Entete(CodeArticleSage);
CREATE INDEX IX_PlanFab_Statut ON dbo.Plan_Fabrication_Entete(Statut);
CREATE INDEX IX_Machine_Operation ON dbo.Machine(OperationCode);
GO

PRINT '✅ Base normalisée créée avec succès (V3.0)!';

