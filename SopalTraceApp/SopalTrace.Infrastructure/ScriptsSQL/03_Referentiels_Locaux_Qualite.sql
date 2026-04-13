USE SopalTraceDB;
GO
-- =================================================================================
-- PARTIE 2 : RÉFÉRENTIELS LOCAUX (PROPRES À LA QUALITÉ SOPALTRACE)
-- =================================================================================
CREATE TABLE TypeCaracteristique (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(80) NOT NULL,
    UniteDefaut VARCHAR(10),
    EstNumerique BIT NOT NULL DEFAULT 1,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE TypeControle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(80) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE MoyenControle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(40) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE Periodicite (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    FrequenceNum INT,
    FrequenceUnite VARCHAR(20),
    OrdreAffichage INT NOT NULL DEFAULT 0,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE TypeSection (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE Defautheque (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(20) NOT NULL UNIQUE,
    Description VARCHAR(200),
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE GroupeInstrument (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeAlias VARCHAR(20) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE GroupeInstrumentDetail (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    GroupeId UNIQUEIDENTIFIER NOT NULL REFERENCES GroupeInstrument(Id) ON DELETE CASCADE,
    CodeInstrument VARCHAR(40) NOT NULL REFERENCES ERP_Instrument(CodeInstrument),
    UNIQUE (GroupeId, CodeInstrument)
);

CREATE TABLE PieceReference (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    TypePiece VARCHAR(10) NOT NULL CHECK (TypePiece IN ('PRC','PRNC','FEC','FENC')),
    Designation VARCHAR(150),
    FamilleDesc VARCHAR(60),
    MachineCode VARCHAR(30) REFERENCES ERP_Machine(CodeMachine),
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE RisqueDefaut (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeDefaut VARCHAR(30) NOT NULL UNIQUE,
    LibelleDefaut VARCHAR(100) NOT NULL,
    TypeControleId UNIQUEIDENTIFIER REFERENCES TypeControle(Id),
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE NQA (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ValeurNQA FLOAT NOT NULL UNIQUE
);

CREATE TABLE OutilControle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(40) NOT NULL UNIQUE,
    Libelle VARCHAR(150) NOT NULL,
    TypeControleId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeControle(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER REFERENCES TypeCaracteristique(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES MoyenControle(Id),
    GroupeInstrumentId UNIQUEIDENTIFIER REFERENCES GroupeInstrument(Id),
    PeriodiciteDefautId UNIQUEIDENTIFIER REFERENCES Periodicite(Id),
    LimiteSpecTexteDefaut VARCHAR(100),
    InstructionDefaut NVARCHAR(MAX),
    Actif BIT NOT NULL DEFAULT 1
);
GO
CREATE TABLE TypeCaracteristique (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(80) NOT NULL,
    UniteDefaut VARCHAR(10),
    EstNumerique BIT NOT NULL DEFAULT 1,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE TypeControle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(80) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE MoyenControle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(40) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE Periodicite (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    FrequenceNum INT,
    FrequenceUnite VARCHAR(20),
    OrdreAffichage INT NOT NULL DEFAULT 0,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE TypeSection (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE Defautheque (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(20) NOT NULL UNIQUE,
    Description VARCHAR(200),
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE GroupeInstrument (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeAlias VARCHAR(20) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE GroupeInstrumentDetail (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    GroupeId UNIQUEIDENTIFIER NOT NULL REFERENCES GroupeInstrument(Id) ON DELETE CASCADE,
    CodeInstrument VARCHAR(40) NOT NULL REFERENCES ERP_Instrument(CodeInstrument),
    UNIQUE (GroupeId, CodeInstrument)
);

CREATE TABLE PieceReference (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(30) NOT NULL UNIQUE,
    TypePiece VARCHAR(10) NOT NULL CHECK (TypePiece IN ('PRC','PRNC','FEC','FENC')),
    Designation VARCHAR(150),
    FamilleDesc VARCHAR(60),
    MachineCode VARCHAR(30) REFERENCES ERP_Machine(CodeMachine),
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE RisqueDefaut (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeDefaut VARCHAR(30) NOT NULL UNIQUE,
    LibelleDefaut VARCHAR(100) NOT NULL,
    TypeControleId UNIQUEIDENTIFIER REFERENCES TypeControle(Id),
    Actif BIT NOT NULL DEFAULT 1
);

CREATE TABLE NQA (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ValeurNQA FLOAT NOT NULL UNIQUE
);

CREATE TABLE OutilControle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(40) NOT NULL UNIQUE,
    Libelle VARCHAR(150) NOT NULL,
    TypeControleId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeControle(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER REFERENCES TypeCaracteristique(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES MoyenControle(Id),
    GroupeInstrumentId UNIQUEIDENTIFIER REFERENCES GroupeInstrument(Id),
    PeriodiciteDefautId UNIQUEIDENTIFIER REFERENCES Periodicite(Id),
    LimiteSpecTexteDefaut VARCHAR(100),
    InstructionDefaut NVARCHAR(MAX),
    Actif BIT NOT NULL DEFAULT 1
);
GO
-- Procédure générique pour Soft Delete
CREATE OR ALTER PROCEDURE sp_RaiseDeleteError (@TableName VARCHAR(100))
AS
BEGIN
    DECLARE @Msg VARCHAR(200) = 'Suppression physique interdite sur ' + @TableName + '. Utilisez Actif = 0 ou Statut = ''ARCHIVE''.';
    RAISERROR(@Msg, 16, 1);
END;
GO

-- Triggers de sécurité
CREATE TRIGGER trg_no_del_TypeCar ON TypeCaracteristique INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'TypeCaracteristique';
GO
CREATE TRIGGER trg_no_del_TypeCtrl ON TypeControle INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'TypeControle';
GO
CREATE TRIGGER trg_no_del_Moyen ON MoyenControle INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'MoyenControle';
GO
CREATE TRIGGER trg_no_del_Perio ON Periodicite INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'Periodicite';
GO
CREATE TRIGGER trg_no_del_GrpInstr ON GroupeInstrument INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'GroupeInstrument';
GO
CREATE TRIGGER trg_no_del_PieceRef ON PieceReference INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'PieceReference';
GO
CREATE TRIGGER trg_no_del_Risque ON RisqueDefaut INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'RisqueDefaut';
GO
CREATE TRIGGER trg_no_del_Outil ON OutilControle INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'OutilControle';
GO
CREATE TRIGGER trg_no_del_TypeSec ON TypeSection INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'TypeSection';
GO
CREATE TRIGGER trg_no_del_Defaut ON Defautheque INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'Defautheque';
GO
