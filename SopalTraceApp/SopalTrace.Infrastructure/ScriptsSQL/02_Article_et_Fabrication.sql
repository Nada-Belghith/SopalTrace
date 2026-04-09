USE SopalTraceDB;
GO

-- =================================================================================
-- PARTIE 1 : TABLES DE SYNCHRONISATION ERP SAGE (CACHE LOCAL)
-- Ces données sont "Read-Only" pour l'application SopalTrace, elles viennent de l'ERP.
-- =================================================================================

-- 1.1 Articles, OF et Logistique
CREATE TABLE ITMMASTER (
    CodeArticle VARCHAR(30) PRIMARY KEY,
    Designation VARCHAR(100),
    Designation2 VARCHAR(100),
    FamilleProduit VARCHAR(30),
    Statut VARCHAR(10)
);

CREATE TABLE MFGHEAD (
    NumeroOF VARCHAR(30) PRIMARY KEY,
    CodeArticle VARCHAR(30),
    QuantitePrevue FLOAT,
    StatutOF VARCHAR(10)
);

CREATE TABLE MFGMAT (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NumeroOF VARCHAR(30) NOT NULL,
    CodeArticle VARCHAR(30) NOT NULL,
    QuantiteRequise FLOAT NOT NULL,
    QuantiteSortie FLOAT NOT NULL DEFAULT 0
);
CREATE INDEX IX_MFGMAT_NumeroOF ON MFGMAT(NumeroOF);

CREATE TABLE SDELIVERY (
    NumeroBL VARCHAR(30) PRIMARY KEY,
    CodeClient VARCHAR(30) NOT NULL, 
    DateExpedition DATE NOT NULL,        
    StatutBL VARCHAR(10) NOT NULL  
);

-- 1.2 Nomenclatures et Master Data Industriels (Remontés depuis les référentiels)
CREATE TABLE TypeRobinet (
    Code VARCHAR(20) PRIMARY KEY, -- Ex: 'MAN', 'AUTO'
    Libelle VARCHAR(60) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE NatureComposant (
    Code VARCHAR(20) PRIMARY KEY, -- Ex: 'CORPS'
    Libelle VARCHAR(60) NOT NULL,
    TypeLotAttendu VARCHAR(30) NULL, -- Ex: 'NUM_OF' (pour Corps/Volant) ou 'NUM_RECEPTION' (pour Accessoires)
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE Operation (
    Code VARCHAR(20) PRIMARY KEY, -- Ex: 'USINAG'
    Libelle VARCHAR(80) NOT NULL,
    OrdreProcess INT NOT NULL DEFAULT 0,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE Instrument (
    CodeInstrument VARCHAR(40) PRIMARY KEY, -- Ex: 'PAC508'
    Designation VARCHAR(100) NOT NULL,
    Categorie VARCHAR(40),
    PrecisionLecture FLOAT,
    Unite VARCHAR(10),
    DateEtalonnage DATE,
    DateProchaineVerif DATE,
    Statut VARCHAR(20) NOT NULL DEFAULT 'ACTIF',
    Localisation VARCHAR(60),
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE Machine (
    CodeMachine VARCHAR(30) PRIMARY KEY, -- Ex: 'MAS22', 'BEE46'
    Libelle VARCHAR(100) NOT NULL,
    TypeRobinetCode VARCHAR(20) REFERENCES ERP_TypeRobinet(Code),
    OperationCode VARCHAR(20) REFERENCES ERP_Operation(Code),
    Localisation VARCHAR(60),
    Actif BIT NOT NULL DEFAULT 1
);
GO
