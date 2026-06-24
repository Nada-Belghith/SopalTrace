-- ================================================================================
-- SOPAL TRACE DB - SCHEMA V4.0 FINAL
--
-- ARCHITECTURE :
--   Plan_Fabrication_*   → inchangé
--   Plan_VerifMachine_*  → inchangé
--   Tous les autres      → Document_Entete / Document_Section / Document_Ligne
--
-- PRINCIPES :
--   - Zéro stored procedure, zéro trigger métier, zéro colonne calculée
--   - Ref_Caracteristique : LibelleNormalise fourni par le backend C# (pas de PERSISTED)
--   - JSON supprimé : remplacé par ExtraColonne (EAV) + Libre1..5
--   - Triggers = uniquement protection ISO 9001 (interdiction suppression physique)
--   - Nouveau type de document = INSERT INTO TypeDocument, zéro DDL
-- ================================================================================

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

-- ================================================================================
-- PARTIE 0 : TABLES DE RÉFÉRENCE (ISO 9001 – suppression interdite par trigger)
-- ================================================================================

CREATE TABLE dbo.TypeRobinet (
    Code    VARCHAR(10)  PRIMARY KEY,
    Libelle VARCHAR(60)  NOT NULL,
    Actif   BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.FamilleProduitFini (
    Code            VARCHAR(30)  PRIMARY KEY,
    Designation     VARCHAR(250) NOT NULL,
    TypeRobinetCode VARCHAR(10)  REFERENCES dbo.TypeRobinet(Code),
    Actif           BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.NatureArticle (
    Code    VARCHAR(20)  PRIMARY KEY,
    Libelle VARCHAR(100) NOT NULL,
    Origine VARCHAR(10)  NOT NULL CHECK (Origine IN ('FABRIQUE','ACHETE')),
    Actif   BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Ref_FamilleCorps (
    Id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code        VARCHAR(50)  NOT NULL UNIQUE,
    Designation VARCHAR(150) NOT NULL,
    Actif       BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Operation (
    Code         VARCHAR(20) PRIMARY KEY,
    Libelle      VARCHAR(80) NOT NULL,
    OrdreProcess INT         NOT NULL DEFAULT 0,
    Actif        BIT         NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.NatureArticle_Operation (
    NatureArticleCode VARCHAR(20) NOT NULL REFERENCES dbo.NatureArticle(Code) ON DELETE CASCADE,
    OperationCode     VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code)     ON DELETE CASCADE,
    OrdreGamme        INT         NOT NULL DEFAULT 1,
    EstObligatoire    BIT         NOT NULL DEFAULT 1,
    PRIMARY KEY (NatureArticleCode, OperationCode)
);
GO

CREATE TABLE dbo.PosteTravail (
    CodePoste VARCHAR(30)  PRIMARY KEY,
    Libelle   VARCHAR(100) NOT NULL,
    Actif     BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.TypeCaracteristique (
    Id      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code    VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(80) NOT NULL,
    Actif   BIT         NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.TypeControle (
    Id      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code    VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(80) NOT NULL,
    Actif   BIT         NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.MoyenControle (
    Id      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code    VARCHAR(40) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif   BIT         NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Periodicite (
    Id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code           VARCHAR(30)  NOT NULL UNIQUE,
    Libelle        VARCHAR(200) NOT NULL,
    FrequenceNum   INT,
    FrequenceUnite VARCHAR(100),
    OrdreAffichage INT          NOT NULL DEFAULT 0,
    Actif          BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Ref_RegleEchantillonnage (
    Id      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code    VARCHAR(30)  NOT NULL UNIQUE,
    Libelle VARCHAR(250) NOT NULL,
    Actif   BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.TypeSection (
    Id      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code    VARCHAR(300) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif   BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Defautheque (
    Id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code        VARCHAR(30)  NOT NULL UNIQUE,
    Description VARCHAR(200),
    Actif       BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.RisqueDefaut (
    Id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeDefaut    VARCHAR(30)  NOT NULL UNIQUE,
    LibelleDefaut VARCHAR(100) NOT NULL,
    Actif         BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.NQA (
    Id        INT   IDENTITY(1,1) PRIMARY KEY,
    ValeurNQA FLOAT NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.Ref_MoyenDetection (
    Id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code        VARCHAR(50)  NOT NULL UNIQUE,
    Designation VARCHAR(100) NOT NULL,
    Actif       BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Instrument (
    CodeInstrument     VARCHAR(40)  PRIMARY KEY,
    Designation        VARCHAR(100) NOT NULL,
    Categorie          VARCHAR(40),
    PrecisionLecture   FLOAT,
    Unite              VARCHAR(10),
    DateEtalonnage     DATE,
    DateProchaineVerif DATE,
    Statut             VARCHAR(20)  NOT NULL DEFAULT 'ACTIF',
    Actif              BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.PieceReference (
    Id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code           VARCHAR(30)  NOT NULL UNIQUE,
    TypePiece      VARCHAR(10)  NOT NULL CHECK (TypePiece IN ('PRC','PRNC','FEC','FENC')),
    Designation    VARCHAR(150),
    FamilleDesc    VARCHAR(60),
    FamilleCorpsId UNIQUEIDENTIFIER REFERENCES dbo.Ref_FamilleCorps(Id),
    Actif          BIT          NOT NULL DEFAULT 1
);
GO

-- ================================================================================
-- PARTIE 1 : CATALOGUE CARACTÉRISTIQUES
--
-- Le responsable saisit librement.
-- Le backend C# normalise (trim + suppression accents + uppercase) AVANT insert.
-- LibelleNormalise est fourni par le backend, pas calculé en base.
-- La contrainte UNIQUE sur LibelleNormalise est un filet de sécurité uniquement.
-- ================================================================================

CREATE TABLE dbo.Ref_Caracteristique (
    Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Libelle               VARCHAR(250) NOT NULL,         -- tel que saisi (1ère fois)
    LibelleNormalise      VARCHAR(250) NOT NULL UNIQUE,  -- fourni par le backend C#
    TypeCaracteristiqueId UNIQUEIDENTIFIER REFERENCES dbo.TypeCaracteristique(Id),
    LimiteSpecTexteDefaut VARCHAR(100),
    InstructionDefaut     NVARCHAR(MAX),
    Actif                 BIT          NOT NULL DEFAULT 1
);
GO

CREATE INDEX IX_Ref_Caracteristique_Normalise ON dbo.Ref_Caracteristique(LibelleNormalise);
GO

-- ================================================================================
-- PARTIE 2 : TYPE DOCUMENT (référentiel extensible)
--
-- Pour ajouter un nouveau type de plan : INSERT ici, zéro DDL.
-- ================================================================================

CREATE TABLE dbo.TypeDocument (
    Code              VARCHAR(30)  PRIMARY KEY,
    Libelle           VARCHAR(100) NOT NULL,
    DescriptionChamps NVARCHAR(MAX) NULL,   -- documentation métier uniquement
    Actif             BIT          NOT NULL DEFAULT 1
);
GO



-- ================================================================================
-- PARTIE 3 : AUTHENTIFICATION ET SÉCURITÉ
-- ================================================================================

CREATE TABLE dbo.UtilisateursApp (
    Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Matricule             VARCHAR(20)  NOT NULL UNIQUE,
    NomComplet            VARCHAR(100) NOT NULL,
    Email                 VARCHAR(150) NOT NULL UNIQUE,
    MotDePasseHash        VARCHAR(255) NOT NULL,
    RoleApp               VARCHAR(50)  NOT NULL,
    IntituleMetier        VARCHAR(100),
    CodeRecuperation      VARCHAR(6),
    DateExpirationCode    DATETIME,
    DateCreation          DATETIME     DEFAULT GETDATE(),
    DateDerniereConnexion DATETIME,
    EstActif              BIT          DEFAULT 1
);
GO

CREATE INDEX IX_UtilisateursApp_Matricule ON dbo.UtilisateursApp(Matricule);
GO

CREATE TABLE dbo.RefreshTokens (
    Id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UtilisateurId  UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.UtilisateursApp(Id) ON DELETE CASCADE,
    Token          VARCHAR(255) NOT NULL UNIQUE,
    JwtId          VARCHAR(100) NOT NULL,
    DateCreation   DATETIME     DEFAULT GETDATE(),
    DateExpiration DATETIME     NOT NULL,
    EstRevoque     BIT          DEFAULT 0
);
GO

CREATE TABLE dbo.JournalConnexions (
    Id         UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Matricule  VARCHAR(20)  NOT NULL,
    Action     VARCHAR(50)  NOT NULL,
    Details    VARCHAR(255),
    DateAction DATETIME     DEFAULT GETDATE()
);
GO

-- ================================================================================
-- PARTIE 4 : ARTICLES
-- ================================================================================

CREATE TABLE dbo.Article (
    CodeArticle       VARCHAR(30)  PRIMARY KEY,
    Designation       VARCHAR(150) NOT NULL,
    Designation2      VARCHAR(150),
    NatureArticleCode VARCHAR(20)  NOT NULL REFERENCES dbo.NatureArticle(Code),
    TypeArticle       VARCHAR(20)  NOT NULL CHECK (TypeArticle IN ('PF','SF','COMPOSANT')),
    Statut            VARCHAR(10)  NOT NULL DEFAULT 'ACTIF',
    DateCreation      DATETIME     DEFAULT GETDATE(),
    DateModification  DATETIME,
    Actif             BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.ProduitFini (
    CodeArticle            VARCHAR(30) PRIMARY KEY REFERENCES dbo.Article(CodeArticle) ON DELETE CASCADE,
    FamilleProduitFiniCode VARCHAR(30) NOT NULL REFERENCES dbo.FamilleProduitFini(Code),
    TypeRobinetCode        VARCHAR(10) NOT NULL REFERENCES dbo.TypeRobinet(Code)
);
GO

CREATE TABLE dbo.BOMD_Nomenclature (
    ArticleParent   VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    CodeComposant   VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    CodeAlternative INT         NOT NULL,
    QuantiteRequise FLOAT       NOT NULL,
    PRIMARY KEY (ArticleParent, CodeComposant, CodeAlternative)
);
GO

-- ================================================================================
-- PARTIE 5 : ERP – SAGE X3 CACHE
-- ================================================================================

CREATE TABLE dbo.AUTILIS (
    USR_0    VARCHAR(5)   PRIMARY KEY,
    INTUSR_0 VARCHAR(100) NOT NULL,
    ENAFLG_0 INT          NOT NULL DEFAULT 1,
    CODMET_0 VARCHAR(20)  NOT NULL,
    ADDEML_0 VARCHAR(150)
);
GO

CREATE TABLE dbo.ATEXTRA (
    CODFIC_0 VARCHAR(50)  NOT NULL,
    ZONE_0   VARCHAR(50)  NOT NULL,
    IDENT1_0 VARCHAR(50)  NOT NULL,
    LANGUE_0 VARCHAR(3)   NOT NULL,
    TEXTE_0  VARCHAR(255) NOT NULL,
    PRIMARY KEY (CODFIC_0, ZONE_0, IDENT1_0, LANGUE_0)
);
GO

-- ================================================================================
-- PARTIE 6 : ORDRES DE FABRICATION
-- ================================================================================

CREATE TABLE dbo.MFGHEAD_OrdreFabrication (
    NumeroOF       VARCHAR(30) PRIMARY KEY,
    CodeArticle    VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    QuantitePrevue FLOAT       NOT NULL,
    QuantiteLancee FLOAT       NOT NULL DEFAULT 0,
    QuantiteReelle FLOAT       NOT NULL DEFAULT 0,
    StatutOF       VARCHAR(20) NOT NULL CHECK (StatutOF IN ('EN_COURS','PLANIFIE','TERMINE','ANNULE')),
    DateDebut      DATETIME,
    DateFin        DATETIME
);
GO

CREATE TABLE dbo.MFGMAT_BesoinOF (
    Id              INT  IDENTITY(1,1) PRIMARY KEY,
    NumeroOF        VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF) ON DELETE CASCADE,
    CodeArticle     VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    QuantiteRequise FLOAT       NOT NULL,
    QuantiteSortie  FLOAT       NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.SDELIVERY (
    NumeroBL       VARCHAR(30) PRIMARY KEY,
    CodeClient     VARCHAR(30) NOT NULL,
    DateExpedition DATE        NOT NULL,
    StatutBL       VARCHAR(10) NOT NULL
);
GO

-- ================================================================================
-- PARTIE 7 : MACHINES ET POSTES DE TRAVAIL
-- ================================================================================

CREATE TABLE dbo.Machine (
    CodeMachine     VARCHAR(30)  PRIMARY KEY,
    Libelle         VARCHAR(100) NOT NULL,
    OperationCode   VARCHAR(20)  NOT NULL REFERENCES dbo.Operation(Code),
    TypeAffectation VARCHAR(15)  NOT NULL DEFAULT 'INDEPENDANTE'
        CHECK (TypeAffectation IN ('INDEPENDANTE','POSTE')),
    RoleMachine     VARCHAR(20)  CHECK (RoleMachine IN ('BEE','MAS_ASS','MARQUAGE','USINAG','TRONC','ESTOMP')),
    Actif           BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Machine_FamilleCorps (
    MachineCode       VARCHAR(30)      NOT NULL REFERENCES dbo.Machine(CodeMachine)  ON DELETE CASCADE,
    RefFamilleCorpsId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_FamilleCorps(Id) ON DELETE CASCADE,
    PRIMARY KEY (MachineCode, RefFamilleCorpsId)
);
GO

CREATE TABLE dbo.PosteTravail_Machine (
    CodePoste   VARCHAR(30) NOT NULL REFERENCES dbo.PosteTravail(CodePoste),
    CodeMachine VARCHAR(30) NOT NULL REFERENCES dbo.Machine(CodeMachine),
    PRIMARY KEY (CodePoste, CodeMachine)
);
GO

-- ================================================================================
-- PARTIE 8 : FORMULAIRES DE CONTRÔLE
-- ================================================================================

CREATE TABLE dbo.Ref_Formulaire (
    Id                        UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeReference             VARCHAR(30)  NOT NULL,
    Designation               VARCHAR(150) NOT NULL,
    Version                   INT          NOT NULL DEFAULT 0,
    Statut                    VARCHAR(20)  NOT NULL DEFAULT 'ACTIF',
    CreeLe                    DATETIME     NOT NULL DEFAULT GETDATE(),
    CreePar                   VARCHAR(20),
    ModifiePar                VARCHAR(20),
    ModifieLe                 DATETIME,
    Role                      VARCHAR(50),
    UNIQUE (CodeReference, Version)
);
GO

CREATE TABLE dbo.Ref_Formulaire_ColonneDef (
    Id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeReference    NVARCHAR(250) NOT NULL,
    CleColonne       VARCHAR(60)  NOT NULL,
    LabelAffiche     VARCHAR(100) NOT NULL,
    TypeValeur       VARCHAR(50)  NOT NULL DEFAULT 'TEXT',
    InsertAfter      VARCHAR(60)  NULL,
    TargetTable      VARCHAR(50)  NULL,
    Actif            BIT          NOT NULL DEFAULT 1,
    UNIQUE (CodeReference, CleColonne)
);
GO

CREATE TRIGGER trg_no_del_Ref_Formulaire_ColonneDef
ON dbo.Ref_Formulaire_ColonneDef
INSTEAD OF DELETE
AS BEGIN
    RAISERROR ('Suppression physique interdite sur Ref_Formulaire_ColonneDef (Exigence ISO 9001).', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO

CREATE TABLE dbo.Ref_Formulaire_Equipe (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeReference   NVARCHAR(250) NOT NULL,
    NomEquipe       VARCHAR(60)   NOT NULL,
    HeureDebut      INT           NOT NULL,
    HeureFin        INT           NOT NULL,
    OrdreAffiche    INT           NOT NULL DEFAULT 0,
    Actif           BIT           NOT NULL DEFAULT 1,
    UNIQUE (CodeReference, NomEquipe)
);
GO

CREATE TRIGGER trg_no_del_Ref_Formulaire_Equipe
ON dbo.Ref_Formulaire_Equipe
INSTEAD OF DELETE
AS BEGIN
    RAISERROR ('Suppression physique interdite sur Ref_Formulaire_Equipe (ISO 9001).', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO


CREATE TABLE dbo.OutilControle (
    Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code                  VARCHAR(40)  NOT NULL UNIQUE,
    Libelle               VARCHAR(150) NOT NULL,
    TypeControleId        UNIQUEIDENTIFIER REFERENCES dbo.TypeControle(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER REFERENCES dbo.TypeCaracteristique(Id),
    MoyenControleId       UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    PeriodiciteDefautId   UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    LimiteSpecTexteDefaut VARCHAR(100),
    InstructionDefaut     NVARCHAR(MAX),
    Actif                 BIT          NOT NULL DEFAULT 1
);
GO

-- ================================================================================
-- PARTIE 9 : PLAN D'ÉCHANTILLONNAGE
-- ================================================================================

CREATE TABLE dbo.Plan_Echantillonnage_Entete (
    Id                 UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NiveauControle     VARCHAR(5)  NOT NULL CHECK (NiveauControle IN ('I','II','III')),
    TypePlan           VARCHAR(10) NOT NULL CHECK (TypePlan IN ('SIMPLE','DOUBLE')),
    ModeControle       VARCHAR(15) NOT NULL CHECK (ModeControle IN ('NORMAL','REDUIT','RENFORCE')),
    NqaId              INT         NOT NULL REFERENCES dbo.NQA(Id),
    FormulaireId       UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    Version            INT         NOT NULL DEFAULT 0,
    Statut             VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE')),
    CreePar            VARCHAR(20) NOT NULL,
    CreeLe             DATETIME    NOT NULL DEFAULT GETDATE(),
    ModifiePar         VARCHAR(20),
    ModifieLe          DATETIME,
    CommentaireVersion NVARCHAR(MAX),
    Remarques          NVARCHAR(MAX),
    LegendeMoyens      NVARCHAR(MAX)
);
GO

CREATE TABLE dbo.Plan_Echantillonnage_Regle (
    Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FicheEnteteId         UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Echantillonnage_Entete(Id) ON DELETE CASCADE,
    TailleMinLot          INT,
    TailleMaxLot          INT,
    LettreCode            VARCHAR(5)  NOT NULL,
    EffectifEchantillon_A INT         NOT NULL,
    NbPostes_B            INT         NOT NULL DEFAULT 1,
    EffectifParPoste_AB   INT,
    CritereAcceptation_Ac INT         NOT NULL,
    CritereRejet_Re       INT         NOT NULL,
    UNIQUE (FicheEnteteId, LettreCode)
);
GO

-- ================================================================================
-- PARTIE 10 : MODÈLES DE FABRICATION (inchangés)
-- ================================================================================

CREATE TABLE dbo.Modele_Fabrication_Entete (
    Id                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code                   VARCHAR(60)  NOT NULL,
    Libelle                VARCHAR(150) NOT NULL,
    NatureArticleCode      VARCHAR(20)  NOT NULL REFERENCES dbo.NatureArticle(Code),
    OperationCode          VARCHAR(20)  NOT NULL REFERENCES dbo.Operation(Code),
    FamilleProduitFiniCode VARCHAR(30)  REFERENCES dbo.FamilleProduitFini(Code),
    FormulaireId           UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    Version                INT          NOT NULL DEFAULT 0,
    Statut                 VARCHAR(20)  NOT NULL DEFAULT 'BROUILLON'
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    Notes                  NVARCHAR(MAX),
    LegendeMoyens          NVARCHAR(MAX),
    CreePar                VARCHAR(20)  NOT NULL,
    CreeLe                 DATETIME     NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.Modele_Fabrication_Section (
    Id                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleEnteteId         UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Modele_Fabrication_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche           INT              NOT NULL DEFAULT 0,
    LibelleSection         VARCHAR(200)     NOT NULL,
    TypeSectionId          UNIQUEIDENTIFIER REFERENCES dbo.TypeSection(Id),
    PeriodiciteId          UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    RegleEchantillonnageId UNIQUEIDENTIFIER REFERENCES dbo.Ref_RegleEchantillonnage(Id)
);
GO

CREATE TABLE dbo.Modele_Fabrication_Ligne (
    Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SectionId             UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Modele_Fabrication_Section(Id) ON DELETE CASCADE,
    OrdreAffiche          INT              NOT NULL DEFAULT 0,
    CaracteristiqueId     UNIQUEIDENTIFIER REFERENCES dbo.Ref_Caracteristique(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche        VARCHAR(200),
    LimiteSpecTexte       VARCHAR(100),
    TypeControleId        UNIQUEIDENTIFIER REFERENCES dbo.TypeControle(Id),
    InstrumentCode        VARCHAR(40)      REFERENCES dbo.Instrument(CodeInstrument),
    PeriodiciteId         UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    Instruction           NVARCHAR(MAX),
    EstCritique           BIT              NOT NULL DEFAULT 0,
    MoyenControleId       UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    MoyenTexteLibre       VARCHAR(100),
    Observations          NVARCHAR(MAX),
    ImageBase64           VARCHAR(MAX),
    Libre1                NVARCHAR(255),
    Libre2                NVARCHAR(255),
    Libre3                NVARCHAR(255),
    Libre4                NVARCHAR(255),
    Libre5                NVARCHAR(255),
    CONSTRAINT CK_ModFab_Ligne_Moyen_XOR CHECK (
        NOT (MoyenControleId IS NOT NULL AND MoyenTexteLibre IS NOT NULL)
    )
);
GO

CREATE TABLE dbo.Modele_Fabrication_Ligne_ExtraColonne (
    Id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    LigneId       UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Modele_Fabrication_Ligne(Id) ON DELETE CASCADE,
    CleColonne    VARCHAR(60)   NOT NULL,
    ValeurColonne NVARCHAR(500),
    OrdreAffiche  INT           NOT NULL DEFAULT 0,
    UNIQUE (LigneId, CleColonne)
);
GO

-- ================================================================================
-- PARTIE 11 : PLANS DE FABRICATION (inchangés)
-- ================================================================================

CREATE TABLE dbo.Plan_Fabrication_Entete (
    Id                       UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleSourceId           UNIQUEIDENTIFIER REFERENCES dbo.Modele_Fabrication_Entete(Id),
    CodeArticleSageVersionne VARCHAR(30)  NOT NULL,
    Designation              VARCHAR(200),
    Nom                      VARCHAR(150) NOT NULL,
    Version                  INT          NOT NULL DEFAULT 0,
    OperationCode            VARCHAR(20)  REFERENCES dbo.Operation(Code),
    Statut                   VARCHAR(20)  NOT NULL DEFAULT 'BROUILLON'
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    MachineDefautCode        VARCHAR(30)  REFERENCES dbo.Machine(CodeMachine),
    FormulaireId             UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    LegendeMoyens            NVARCHAR(MAX),
    Remarques                NVARCHAR(MAX),
    CreePar                  VARCHAR(20)  NOT NULL,
    CreeLe                   DATETIME     NOT NULL DEFAULT GETDATE(),
    ModifiePar               VARCHAR(20),
    ModifieLe                DATETIME
);
GO

CREATE TABLE dbo.Plan_Fabrication_Section (
    Id                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId           UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Fabrication_Entete(Id) ON DELETE CASCADE,
    ModeleSectionId        UNIQUEIDENTIFIER REFERENCES dbo.Modele_Fabrication_Section(Id),
    OrdreAffiche           INT              NOT NULL DEFAULT 0,
    LibelleSection         VARCHAR(300)     NOT NULL,
    TypeSectionId          UNIQUEIDENTIFIER REFERENCES dbo.TypeSection(Id),
    PeriodiciteId          UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    RegleEchantillonnageId UNIQUEIDENTIFIER REFERENCES dbo.Ref_RegleEchantillonnage(Id)
);
GO

CREATE TABLE dbo.Plan_Fabrication_Ligne (
    Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId          UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Fabrication_Entete(Id),
    SectionId             UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Fabrication_Section(Id) ON DELETE CASCADE,
    ModeleLigneSourceId   UNIQUEIDENTIFIER REFERENCES dbo.Modele_Fabrication_Ligne(Id),
    OrdreAffiche          INT              NOT NULL DEFAULT 0,
    CaracteristiqueId     UNIQUEIDENTIFIER REFERENCES dbo.Ref_Caracteristique(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER REFERENCES dbo.TypeCaracteristique(Id),
    LibelleAffiche        VARCHAR(200),
    TypeControleId        UNIQUEIDENTIFIER REFERENCES dbo.TypeControle(Id),
    InstrumentCode        VARCHAR(40)      REFERENCES dbo.Instrument(CodeInstrument),
    PeriodiciteId         UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    LimiteSpecTexte       VARCHAR(100),
    Observations          NVARCHAR(MAX),
    Instruction           NVARCHAR(MAX),
    EstCritique           BIT              NOT NULL DEFAULT 0,
    MoyenControleId       UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    MoyenTexteLibre       VARCHAR(100),
    ImageBase64           VARCHAR(MAX),
    Libre1                NVARCHAR(255),
    Libre2                NVARCHAR(255),
    Libre3                NVARCHAR(255),
    Libre4                NVARCHAR(255),
    Libre5                NVARCHAR(255),
    CONSTRAINT CK_PlanFab_Ligne_Moyen_XOR CHECK (
        NOT (MoyenControleId IS NOT NULL AND MoyenTexteLibre IS NOT NULL)
    )
);
GO

CREATE TABLE dbo.Plan_Fabrication_Ligne_ExtraColonne (
    Id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    LigneId       UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_Fabrication_Ligne(Id) ON DELETE CASCADE,
    CleColonne    VARCHAR(60)   NOT NULL,
    ValeurColonne NVARCHAR(500),
    OrdreAffiche  INT           NOT NULL DEFAULT 0,
    UNIQUE (LigneId, CleColonne)
);
GO

-- ================================================================================
-- PARTIE 12 : DOCUMENTS UNIFIÉS
--
-- Remplace : Plan_Assemblage_*, Plan_ProduitFini_*,
--            Plan_ControlePoste_*, Plan_ResultatControleCF_*
--
-- TypeDocumentCode discrimine le type.
-- Champs spécifiques à un type = NULL pour les autres.
-- ================================================================================

CREATE TABLE dbo.Document_Entete (
    Id                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TypeDocumentCode       VARCHAR(30)  NOT NULL REFERENCES dbo.TypeDocument(Code),
    Nom                    VARCHAR(150) NOT NULL,
    Designation            VARCHAR(200),
    Version                INT          NOT NULL DEFAULT 0,
    Statut                 VARCHAR(20)  NOT NULL DEFAULT 'BROUILLON'
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    OperationCode          VARCHAR(20)  REFERENCES dbo.Operation(Code),
    FormulaireId           UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    LegendeMoyens          NVARCHAR(MAX),
    Remarques              NVARCHAR(MAX),
    CreePar                VARCHAR(20)  NOT NULL,
    CreeLe                 DATETIME     NOT NULL DEFAULT GETDATE(),
    ModifiePar             VARCHAR(20),
    ModifieLe              DATETIME,
    -- Spécifique ASSEMBLAGE / PRODUIT_FINI
    NatureArticleCode      VARCHAR(20)  REFERENCES dbo.NatureArticle(Code),
    FamilleProduitFiniCode VARCHAR(30)  REFERENCES dbo.FamilleProduitFini(Code),
    -- Spécifique ASSEMBLAGE / CTRL_POSTE / RESULTAT_CF
    PosteCode              VARCHAR(30)  REFERENCES dbo.PosteTravail(CodePoste),
    -- Champs libres pour extensions futures
    Libre1                 NVARCHAR(255),
    Libre2                 NVARCHAR(255),
    Libre3                 NVARCHAR(255)
);
GO

CREATE INDEX IX_Document_Entete_Type   ON dbo.Document_Entete(TypeDocumentCode);
CREATE INDEX IX_Document_Entete_Statut ON dbo.Document_Entete(Statut);
GO


CREATE TABLE dbo.Document_Section (
    Id                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EnteteId               UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche           INT              NOT NULL DEFAULT 0,
    LibelleSection         VARCHAR(300)     NOT NULL,
    TypeSectionId          UNIQUEIDENTIFIER REFERENCES dbo.TypeSection(Id),
    PeriodiciteId          UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    RegleEchantillonnageId UNIQUEIDENTIFIER REFERENCES dbo.Ref_RegleEchantillonnage(Id),
    Notes                  NVARCHAR(MAX),
    -- Spécifique PRODUIT_FINI / ASSEMBLAGE
    NormeReference         VARCHAR(40),
    NqaId                  INT REFERENCES dbo.NQA(Id)
);
GO

CREATE TABLE dbo.Document_Ligne (
    Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EnteteId              UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Entete(Id),
    SectionId             UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Section(Id) ON DELETE CASCADE,
    OrdreAffiche          INT              NOT NULL DEFAULT 0,
    -- Caractéristique (catalogue partagé, dédup géré par le backend)
    CaracteristiqueId     UNIQUEIDENTIFIER REFERENCES dbo.Ref_Caracteristique(Id),
    LibelleAffiche        VARCHAR(250),    -- surcharge du libellé catalogue si besoin
    -- Contrôle commun
    TypeCaracteristiqueId UNIQUEIDENTIFIER REFERENCES dbo.TypeCaracteristique(Id),
    TypeControleId        UNIQUEIDENTIFIER REFERENCES dbo.TypeControle(Id),
    MoyenControleId       UNIQUEIDENTIFIER REFERENCES dbo.MoyenControle(Id),
    MoyenTexteLibre       VARCHAR(100),
    InstrumentCode        VARCHAR(40)      REFERENCES dbo.Instrument(CodeInstrument),
    PeriodiciteId         UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    LimiteSpecTexte       VARCHAR(100),
    EstCritique           BIT              NOT NULL DEFAULT 0,
    Instruction           NVARCHAR(MAX),
    Observations          NVARCHAR(MAX),
    ImageBase64           VARCHAR(MAX),
    -- Spécifique ASSEMBLAGE
    MachineCode           VARCHAR(30)      REFERENCES dbo.Machine(CodeMachine),
    EstVerifPresence      BIT              NOT NULL DEFAULT 0,
    DefauthequeId         UNIQUEIDENTIFIER REFERENCES dbo.Defautheque(Id),
    RefPlanProduit        VARCHAR(60),
    -- Spécifique CTRL_POSTE
    MachineCodeCtrlPoste  VARCHAR(30)      REFERENCES dbo.Machine(CodeMachine),
    RisqueDefautId        UNIQUEIDENTIFIER REFERENCES dbo.RisqueDefaut(Id),
    -- Champs libres ERP-style (remplacent ColonnesSupplementaires JSON)
    Libre1                NVARCHAR(255),
    Libre2                NVARCHAR(255),
    Libre3                NVARCHAR(255),
    Libre4                NVARCHAR(255),
    Libre5                NVARCHAR(255),
    -- Contrainte assouplie : les deux peuvent être NULL (ligne de brouillon/vide)
    -- Seule la combinaison des deux renseignés simultanément est interdite
    CONSTRAINT CK_Doc_Ligne_Moyen_XOR CHECK (
        NOT (MoyenControleId IS NOT NULL AND MoyenTexteLibre IS NOT NULL)
    )
);
GO

CREATE INDEX IX_Document_Ligne_Entete        ON dbo.Document_Ligne(EnteteId);
CREATE INDEX IX_Document_Ligne_Section       ON dbo.Document_Ligne(SectionId);
CREATE INDEX IX_Document_Ligne_Caracteristique ON dbo.Document_Ligne(CaracteristiqueId);
GO

-- Extra-colonnes ligne (remplace ColonnesSupplementaires JSON)
CREATE TABLE dbo.Document_Ligne_ExtraColonne (
    Id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    LigneId       UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Ligne(Id) ON DELETE CASCADE,
    CleColonne    VARCHAR(60)   NOT NULL,
    ValeurColonne NVARCHAR(500),
    OrdreAffiche  INT           NOT NULL DEFAULT 0,
    UNIQUE (LigneId, CleColonne)
);
GO

-- ================================================================================
-- PARTIE 13 : PLANS DE VÉRIFICATION MACHINE (inchangés)
-- ================================================================================

CREATE TABLE dbo.Plan_VerifMachine_Entete (
    Id                        UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    MachineCode               VARCHAR(30)  NOT NULL REFERENCES dbo.Machine(CodeMachine),
    Nom                       VARCHAR(150) NOT NULL,
    Version                   INT          DEFAULT 0,
    Statut                    VARCHAR(20)  DEFAULT 'BROUILLON'
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE')),
    CreePar                   VARCHAR(20)  NOT NULL,
    CreeLe                    DATETIME     DEFAULT GETDATE(),
    ModifiePar                VARCHAR(20),
    ModifieLe                 DATETIME,
    FormulaireId              UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    Remarques                 NVARCHAR(MAX),
    LegendeMoyens             NVARCHAR(MAX)
);
GO

CREATE TABLE dbo.Plan_VerifMachine_Famille (
    Id                UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId      UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche      INT              NOT NULL,
    RefFamilleCorpsId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_FamilleCorps(Id),
    UNIQUE (PlanEnteteId, RefFamilleCorpsId)
);
GO

CREATE TABLE dbo.Plan_VerifMachine_Ligne (
    Id                      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId            UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche            INT              NOT NULL,
    TypeLigne               VARCHAR(20)      DEFAULT 'RISQUE'
        CHECK (TypeLigne IN ('CONFORMITE','RISQUE')),
    LibelleRisque           VARCHAR(250)     NOT NULL,
    LibelleMethode          VARCHAR(250)
);
GO

-- Extra-colonnes pour Plan_VerifMachine_Ligne
CREATE TABLE dbo.Plan_VerifMachine_Ligne_ExtraColonne (
    Id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    LigneId       UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Ligne(Id) ON DELETE CASCADE,
    CleColonne    VARCHAR(60)   NOT NULL,
    ValeurColonne NVARCHAR(500),
    OrdreAffiche  INT           NOT NULL DEFAULT 0,
    UNIQUE (LigneId, CleColonne)
);
GO

CREATE TABLE dbo.Plan_VerifMachine_Echeance (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanLigneId         UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Ligne(Id) ON DELETE CASCADE,
    PeriodiciteId       UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Periodicite(Id),
    RefMoyenDetectionId UNIQUEIDENTIFIER REFERENCES dbo.Ref_MoyenDetection(Id),
    OrdreAffiche        INT              NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.Plan_VerifMachine_MatricePiece (
    Id         UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EcheanceId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Plan_VerifMachine_Echeance(Id) ON DELETE CASCADE,
    FamilleId  UNIQUEIDENTIFIER REFERENCES dbo.Plan_VerifMachine_Famille(Id),
    RoleVerif  VARCHAR(10) CHECK (RoleVerif IN ('PRC','PRNC','FEC','FENC')),
    PieceRefId UNIQUEIDENTIFIER REFERENCES dbo.PieceReference(Id),
    UNIQUE (EcheanceId, FamilleId, RoleVerif, PieceRefId)
);
GO

-- ================================================================================
-- PARTIE 14 : MODULE MAGASIN
-- ================================================================================

CREATE TABLE dbo.Mag_PreparationOF (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NumeroOF            VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    MatriculeMagasinier VARCHAR(20) NOT NULL,
    Statut              VARCHAR(20) NOT NULL DEFAULT 'EN_COURS'
        CHECK (Statut IN ('EN_COURS','PLANIFIE','TERMINE','ANNULE')),
    DateDebut           DATETIME    DEFAULT GETDATE(),
    DateFin             DATETIME
);
GO

CREATE TABLE dbo.Mag_PreparationOF_Lot (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PreparationOFId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Mag_PreparationOF(Id) ON DELETE CASCADE,
    CodeArticle     VARCHAR(30)  NOT NULL REFERENCES dbo.Article(CodeArticle),
    NumeroLotScanne VARCHAR(100) NOT NULL,
    Quantite        FLOAT        NOT NULL,
    DateScan        DATETIME     DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.Mag_QuickControl_Rapport (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NumeroOF        VARCHAR(30)  NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    CodeArticle     VARCHAR(30)  NOT NULL REFERENCES dbo.Article(CodeArticle),
    NumeroRapportQC VARCHAR(100) NOT NULL,
    DateScan        DATETIME     DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.Mag_ExpeditionBL (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NumeroBL            VARCHAR(30) NOT NULL REFERENCES dbo.SDELIVERY(NumeroBL),
    MatriculeMagasinier VARCHAR(20) NOT NULL,
    Statut              VARCHAR(20) NOT NULL DEFAULT 'EN_COURS'
        CHECK (Statut IN ('EN_COURS','TERMINE')),
    DateDebut           DATETIME    DEFAULT GETDATE(),
    DateFin             DATETIME
);
GO

CREATE TABLE dbo.Mag_ExpeditionBL_ScanOF (
    Id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExpeditionBLId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Mag_ExpeditionBL(Id) ON DELETE CASCADE,
    NumeroOFScanne VARCHAR(30)  NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    DateScan       DATETIME     DEFAULT GETDATE()
);
GO

-- ================================================================================
-- PARTIE 15 : EXÉCUTION DES CONTRÔLES
-- ================================================================================

CREATE TABLE dbo.Exec_ControleOF (
    Id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    NumeroOF         VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    OperationCode    VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code),
    MachineCodePrevu VARCHAR(30) REFERENCES dbo.Machine(CodeMachine),
    PosteCodePrevu   VARCHAR(30) REFERENCES dbo.PosteTravail(CodePoste),
    MachineCode      VARCHAR(30) REFERENCES dbo.Machine(CodeMachine),
    PosteCode        VARCHAR(30) REFERENCES dbo.PosteTravail(CodePoste),
    NumEquipe        INT         NOT NULL DEFAULT 1,
    PlanSourceId     UNIQUEIDENTIFIER NOT NULL,
    TypePlan         VARCHAR(10) NOT NULL CHECK (TypePlan IN ('FAB','DOC')),
    Statut           VARCHAR(20) NOT NULL DEFAULT 'EN_COURS'
        CHECK (Statut IN ('EN_COURS','CLOTURE')),
    DateDebut        DATETIME    NOT NULL DEFAULT GETDATE(),
    DateFin          DATETIME
);
GO

CREATE TABLE dbo.Exec_PieceType (
    Id                 UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExecControleOFId   UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id) ON DELETE CASCADE,
    HeureValidation    DATETIME         NOT NULL DEFAULT GETDATE(),
    Resultat           VARCHAR(2)       NOT NULL CHECK (Resultat IN ('C','NC')),
    Remarque           VARCHAR(500),
    MatriculeOperateur VARCHAR(20)      NOT NULL
);
GO

CREATE TABLE dbo.Exec_ControleTranche (
    Id                   UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExecControleOFId     UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id) ON DELETE CASCADE,
    TrancheHoraire       VARCHAR(20)  NOT NULL,
    HeureDebut           DATETIME     NOT NULL,
    HeureFin             DATETIME     NOT NULL,
    ResultatFinal        VARCHAR(2)   CHECK (ResultatFinal IN ('C','NC')),
    DetailsNC            VARCHAR(500),
    ActionsCorrection    VARCHAR(500),
    MatriculeApprobateur VARCHAR(20)
);
GO

CREATE TABLE dbo.Exec_Prelevement (
    Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ExecControleTrancheId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleTranche(Id) ON DELETE CASCADE,
    HeurePrevue           DATETIME    NOT NULL,
    HeureSaisie           DATETIME,
    ResultatGlobal        VARCHAR(2)  CHECK (ResultatGlobal IN ('C','NC')),
    MatriculeOperateur    VARCHAR(20)
);
GO

CREATE TABLE dbo.Exec_Prelevement_Ligne (
    Id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PrelevementId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_Prelevement(Id) ON DELETE CASCADE,
    LignePlanId   UNIQUEIDENTIFIER NOT NULL,
    Resultat      VARCHAR(2)   CHECK (Resultat IN ('C','NC')),
    ValeurMesuree FLOAT,
    Remarque      VARCHAR(500)
);
GO

-- ================================================================================
-- PARTIE 16 : TRIGGERS ISO 9001
--
-- Rôle unique : interdire DELETE physique sur les tables de référence.
-- Aucune logique métier ici. Tout le métier est dans le backend C#.
-- Pour "supprimer" : mettre Actif = 0 (soft delete).
-- ================================================================================

CREATE OR ALTER PROCEDURE dbo.sp_RaiseDeleteError (@TableName VARCHAR(100))
AS BEGIN
    RAISERROR('Suppression physique interdite (ISO 9001) sur la table : %s — utiliser Actif = 0', 16, 1, @TableName);
END;
GO

CREATE TRIGGER trg_no_del_TypeRobinet             ON dbo.TypeRobinet             INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeRobinet'; END;
GO
CREATE TRIGGER trg_no_del_FamilleProduitFini       ON dbo.FamilleProduitFini       INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'FamilleProduitFini'; END;
GO
CREATE TRIGGER trg_no_del_NatureArticle            ON dbo.NatureArticle            INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'NatureArticle'; END;
GO
CREATE TRIGGER trg_no_del_Ref_FamilleCorps         ON dbo.Ref_FamilleCorps         INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_FamilleCorps'; END;
GO
CREATE TRIGGER trg_no_del_Operation                ON dbo.Operation                INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Operation'; END;
GO
CREATE TRIGGER trg_no_del_PosteTravail             ON dbo.PosteTravail             INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'PosteTravail'; END;
GO
CREATE TRIGGER trg_no_del_TypeCaracteristique      ON dbo.TypeCaracteristique      INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeCaracteristique'; END;
GO
CREATE TRIGGER trg_no_del_TypeControle             ON dbo.TypeControle             INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeControle'; END;
GO
CREATE TRIGGER trg_no_del_MoyenControle            ON dbo.MoyenControle            INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'MoyenControle'; END;
GO
CREATE TRIGGER trg_no_del_Periodicite              ON dbo.Periodicite              INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Periodicite'; END;
GO
CREATE TRIGGER trg_no_del_Ref_RegleEchantillonnage ON dbo.Ref_RegleEchantillonnage INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_RegleEchantillonnage'; END;
GO
CREATE TRIGGER trg_no_del_TypeSection              ON dbo.TypeSection              INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeSection'; END;
GO
CREATE TRIGGER trg_no_del_Defautheque              ON dbo.Defautheque              INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Defautheque'; END;
GO
CREATE TRIGGER trg_no_del_RisqueDefaut             ON dbo.RisqueDefaut             INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'RisqueDefaut'; END;
GO
CREATE TRIGGER trg_no_del_NQA                      ON dbo.NQA                      INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'NQA'; END;
GO
CREATE TRIGGER trg_no_del_Ref_MoyenDetection       ON dbo.Ref_MoyenDetection       INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_MoyenDetection'; END;
GO
CREATE TRIGGER trg_no_del_Instrument               ON dbo.Instrument               INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Instrument'; END;
GO
CREATE TRIGGER trg_no_del_PieceReference           ON dbo.PieceReference           INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'PieceReference'; END;
GO
CREATE TRIGGER trg_no_del_TypeDocument             ON dbo.TypeDocument             INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'TypeDocument'; END;
GO
CREATE TRIGGER trg_no_del_Ref_Caracteristique      ON dbo.Ref_Caracteristique      INSTEAD OF DELETE AS BEGIN EXEC dbo.sp_RaiseDeleteError 'Ref_Caracteristique'; END;
GO

-- ================================================================================
-- INDEXES PERFORMANCE
-- ================================================================================

CREATE INDEX IX_Article_TypeArticle          ON dbo.Article(TypeArticle);
CREATE INDEX IX_Article_NatureArticle        ON dbo.Article(NatureArticleCode);
CREATE INDEX IX_ProduitFini_FamilleCode      ON dbo.ProduitFini(FamilleProduitFiniCode);
CREATE INDEX IX_BOMD_Parent                  ON dbo.BOMD_Nomenclature(ArticleParent);
CREATE INDEX IX_BOMD_Composant               ON dbo.BOMD_Nomenclature(CodeComposant);
CREATE INDEX IX_MFGHEAD_CodeArticle          ON dbo.MFGHEAD_OrdreFabrication(CodeArticle);
CREATE INDEX IX_PlanFab_CodeArticle          ON dbo.Plan_Fabrication_Entete(CodeArticleSageVersionne);
CREATE INDEX IX_PlanFab_Statut               ON dbo.Plan_Fabrication_Entete(Statut);
CREATE INDEX IX_Machine_Operation            ON dbo.Machine(OperationCode);
GO

PRINT '✅ SopalTraceDB V4.0 FINAL créée avec succès !';
PRINT '';
PRINT '  Plan_Fabrication_*    → inchangé';
PRINT '  Plan_VerifMachine_*   → inchangé';
PRINT '  Document_*            → unifié (Assemblage + PF + CtrlPoste + ResultatCF)';
PRINT '  Ref_Caracteristique   → LibelleNormalise fourni par le backend C# (pas de colonne calculée)';
PRINT '  JSON supprimé         → ExtraColonne (EAV) + Libre1..5';
PRINT '  Triggers              → ISO 9001 uniquement (interdit DELETE physique)';
PRINT '  Logique métier        → 100% backend C#';