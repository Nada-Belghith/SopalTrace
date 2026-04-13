USE SopalTraceDB;
GO
-- =================================================================================
-- PARTIE 3 : MODULE PLANS DE FABRICATION (Usinage, Tronçonnage, Estompage)
-- =================================================================================

CREATE TABLE Modele_Fab_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code VARCHAR(60) NOT NULL,
    Libelle VARCHAR(150) NOT NULL,
    TypeRobinetCode VARCHAR(20) NOT NULL REFERENCES ERP_TypeRobinet(Code),
    NatureComposantCode VARCHAR(20) NOT NULL REFERENCES ERP_NatureComposant(Code),
    OperationCode VARCHAR(20) NOT NULL REFERENCES ERP_Operation(Code),
    Version INT NOT NULL DEFAULT 1,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE')),
    Notes NVARCHAR(MAX),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ArchiveLe DATETIME,
    ArchivePar VARCHAR(20),
    UNIQUE (TypeRobinetCode, NatureComposantCode, OperationCode, Version)
);
GO
CREATE TRIGGER trg_no_del_ModFab ON Modele_Fab_Entete INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'Modele_Fab_Entete';
GO

CREATE TABLE Modele_Fab_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES Modele_Fab_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    LibelleSection VARCHAR(200) NOT NULL,
    FrequenceLibelle VARCHAR(80),
    UNIQUE (ModeleEnteteId, OrdreAffiche)
);
GO

CREATE TABLE Modele_Fab_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES Modele_Fab_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES Modele_Fab_Section(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    OutilSourceId UNIQUEIDENTIFIER REFERENCES OutilControle(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(200),
    TypeControleId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeControle(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES MoyenControle(Id),
    GroupeInstrumentId UNIQUEIDENTIFIER REFERENCES GroupeInstrument(Id),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES Periodicite(Id),
    Instruction NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0,
    CHECK (OutilSourceId IS NOT NULL OR LibelleAffiche IS NOT NULL),
    UNIQUE (ModeleEnteteId, SectionId, OrdreAffiche)
);
GO

CREATE TABLE Plan_Fab_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ModeleSourceId UNIQUEIDENTIFIER NOT NULL REFERENCES Modele_Fab_Entete(Id),
    CodeArticleSage VARCHAR(30) NOT NULL,
    Designation VARCHAR(200),
    Nom VARCHAR(150) NOT NULL,
    Version INT NOT NULL DEFAULT 1,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    DateApplication DATE,
    MachineDefautCode VARCHAR(30) REFERENCES ERP_Machine(CodeMachine),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    CommentaireVersion NVARCHAR(MAX),
    UNIQUE (CodeArticleSage, ModeleSourceId, Version)
);
GO
CREATE TRIGGER trg_no_del_PlanFab ON Plan_Fab_Entete INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'Plan_Fab_Entete';
GO

CREATE TABLE Plan_Fab_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_Fab_Entete(Id) ON DELETE CASCADE,
    ModeleSectionId UNIQUEIDENTIFIER REFERENCES Modele_Fab_Section(Id),
    OrdreAffiche INT NOT NULL DEFAULT 0,
    LibelleSection VARCHAR(200) NOT NULL,
    FrequenceLibelle VARCHAR(80),
    UNIQUE (PlanEnteteId, OrdreAffiche)
);
GO

CREATE TABLE Plan_Fab_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_Fab_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_Fab_Section(Id) ON DELETE CASCADE,
    ModeleLigneSourceId UNIQUEIDENTIFIER REFERENCES Modele_Fab_Ligne(Id),
    OrdreAffiche INT NOT NULL DEFAULT 0,
    OutilSourceId UNIQUEIDENTIFIER REFERENCES OutilControle(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(200),
    TypeControleId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeControle(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES MoyenControle(Id),
    GroupeInstrumentId UNIQUEIDENTIFIER REFERENCES GroupeInstrument(Id),
    InstrumentCode VARCHAR(40) REFERENCES ERP_Instrument(CodeInstrument),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES Periodicite(Id),
    ValeurNominale FLOAT,
    ToleranceSuperieure FLOAT,
    ToleranceInferieure FLOAT,
    Unite VARCHAR(10),
    LimiteSpecTexte VARCHAR(100),
    Observations NVARCHAR(MAX),
    Instruction NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0,
    CHECK (OutilSourceId IS NOT NULL OR LibelleAffiche IS NOT NULL),
    UNIQUE (PlanEnteteId, SectionId, OrdreAffiche)
);
GO

-- =================================================================================
-- PARTIE 4 : MODULE PLANS D'ASSEMBLAGE (UNIFIÉ)
-- =================================================================================

CREATE TABLE Plan_Ass_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TypeRobinetCode VARCHAR(20) NOT NULL REFERENCES ERP_TypeRobinet(Code),
    CodeArticleSage VARCHAR(30) NULL,
    Designation VARCHAR(200),
    Version INT NOT NULL DEFAULT 1,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE','OBSOLETE')),
    NbPiecesReglage INT NOT NULL DEFAULT 5,
    FicheEchantillonnageId UNIQUEIDENTIFIER, -- FK créée plus bas
    DateApplication DATE,
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    CommentaireVersion NVARCHAR(MAX)
);
GO
CREATE TRIGGER trg_no_del_PlanAss ON Plan_Ass_Entete INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'Plan_Ass_Entete';
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanAss_Article 
    ON Plan_Ass_Entete(TypeRobinetCode, CodeArticleSage, Version) WHERE CodeArticleSage IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanAss_Generic 
    ON Plan_Ass_Entete(TypeRobinetCode, Version) WHERE CodeArticleSage IS NULL;
GO

CREATE TABLE Plan_Ass_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_Ass_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeSectionId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeSection(Id),
    LibelleSection VARCHAR(250) NOT NULL,
    Notes NVARCHAR(MAX),
    UNIQUE (PlanEnteteId, TypeSectionId)
);
GO

CREATE TABLE Plan_Ass_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_Ass_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_Ass_Section(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    OutilSourceId UNIQUEIDENTIFIER REFERENCES OutilControle(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(250),
    TypeControleId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeControle(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES MoyenControle(Id),
    GroupeInstrumentId UNIQUEIDENTIFIER REFERENCES GroupeInstrument(Id),
    InstrumentCode VARCHAR(40) REFERENCES ERP_Instrument(CodeInstrument),
    PeriodiciteId UNIQUEIDENTIFIER REFERENCES Periodicite(Id),
    ValeurNominale FLOAT,
    ToleranceSuperieure FLOAT,
    ToleranceInferieure FLOAT,
    Unite VARCHAR(10),
    LimiteSpecTexte VARCHAR(100),
    EstVerifPresence BIT NOT NULL DEFAULT 0,
    DefauthequeId UNIQUEIDENTIFIER REFERENCES Defautheque(Id),
    RefPlanProduit VARCHAR(60),
    Instruction NVARCHAR(MAX),
    Observations NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0,
    CHECK (OutilSourceId IS NOT NULL OR LibelleAffiche IS NOT NULL),
    UNIQUE (PlanEnteteId, SectionId, OrdreAffiche)
);
GO

-- =================================================================================
-- PARTIE 5 : PLAN PRODUIT FINI (Normatif)
-- =================================================================================

CREATE TABLE Plan_PF_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TypeRobinetCode VARCHAR(20) NOT NULL REFERENCES ERP_TypeRobinet(Code),
    CodeArticleSage VARCHAR(30) NULL,
    Designation VARCHAR(200),
    Version INT NOT NULL DEFAULT 1,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE')),
    VisibleOperateur BIT NOT NULL DEFAULT 0 CHECK (VisibleOperateur = 0),
    DateApplication DATE,
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    CommentaireVersion NVARCHAR(MAX)
);
GO
CREATE TRIGGER trg_no_del_PlanPF ON Plan_PF_Entete INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'Plan_PF_Entete';
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanPF_Article 
    ON Plan_PF_Entete(TypeRobinetCode, CodeArticleSage, Version) WHERE CodeArticleSage IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanPF_Generic 
    ON Plan_PF_Entete(TypeRobinetCode, Version) WHERE CodeArticleSage IS NULL;
GO

CREATE TABLE Plan_PF_Section (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_PF_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    TypeSectionId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeSection(Id),
    LibelleSection VARCHAR(250) NOT NULL,
    NormeReference VARCHAR(40),
    NqaId INT REFERENCES NQA(Id),
    NbEchantillons INT,
    Notes NVARCHAR(MAX),
    UNIQUE (PlanEnteteId, OrdreAffiche)
);
GO

CREATE TABLE Plan_PF_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_PF_Entete(Id),
    SectionId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_PF_Section(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    OutilSourceId UNIQUEIDENTIFIER REFERENCES OutilControle(Id),
    TypeCaracteristiqueId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeCaracteristique(Id),
    LibelleAffiche VARCHAR(250),
    TypeControleId UNIQUEIDENTIFIER NOT NULL REFERENCES TypeControle(Id),
    MoyenControleId UNIQUEIDENTIFIER REFERENCES MoyenControle(Id),
    GroupeInstrumentId UNIQUEIDENTIFIER REFERENCES GroupeInstrument(Id),
    InstrumentCode VARCHAR(40) REFERENCES ERP_Instrument(CodeInstrument),
    ValeurNominale FLOAT,
    ToleranceSuperieure FLOAT,
    ToleranceInferieure FLOAT,
    Unite VARCHAR(10),
    LimiteSpecTexte VARCHAR(100),
    DefauthequeId UNIQUEIDENTIFIER REFERENCES Defautheque(Id),
    Instruction NVARCHAR(MAX),
    Observations NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0,
    CHECK (OutilSourceId IS NOT NULL OR LibelleAffiche IS NOT NULL),
    UNIQUE (PlanEnteteId, SectionId, OrdreAffiche)
);
GO

-- =================================================================================
-- PARTIE 6 : PLANS DE VÉRIFICATION MACHINE
-- =================================================================================

CREATE TABLE Plan_VerifMachine_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    MachineCode VARCHAR(30) NOT NULL REFERENCES ERP_Machine(CodeMachine),
    TypeRobinetCode VARCHAR(20) REFERENCES ERP_TypeRobinet(Code) NULL,
    TypeRapport VARCHAR(20) NOT NULL CHECK (TypeRapport IN ('BEE','MAS_ASS','MARQUAGE')),
    CodeFormulaire VARCHAR(20),
    Nom VARCHAR(150) NOT NULL,
    Version INT NOT NULL DEFAULT 1,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE')),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiePar VARCHAR(20),
    ModifieLe DATETIME,
    CommentaireVersion NVARCHAR(MAX)
);
GO
CREATE TRIGGER trg_no_del_PlanVerif ON Plan_VerifMachine_Entete INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'Plan_VerifMachine_Entete';
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanVerif_Type 
    ON Plan_VerifMachine_Entete(MachineCode, TypeRapport, TypeRobinetCode, Version) WHERE TypeRobinetCode IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanVerif_Generic 
    ON Plan_VerifMachine_Entete(MachineCode, TypeRapport, Version) WHERE TypeRobinetCode IS NULL;
GO

CREATE TABLE Plan_VerifMachine_Ligne (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_VerifMachine_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche INT NOT NULL DEFAULT 0,
    LibelleRisque VARCHAR(250) NOT NULL,
    RisqueDefautId UNIQUEIDENTIFIER REFERENCES RisqueDefaut(Id),
    LibelleMethode VARCHAR(250),
    Periodicite VARCHAR(80) NOT NULL,
    TypeSaisie VARCHAR(15) NOT NULL DEFAULT 'CONFORMITE' CHECK (TypeSaisie IN ('CONFORMITE','MESURE','TEXTE')),
    ValeurNominale FLOAT,
    ToleranceSup FLOAT,
    ToleranceInf FLOAT,
    Unite VARCHAR(10),
    Instruction NVARCHAR(MAX),
    EstCritique BIT NOT NULL DEFAULT 0,
    UNIQUE (PlanEnteteId, OrdreAffiche)
);
GO

CREATE TABLE Plan_VerifMachine_PieceRef (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PlanLigneId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_VerifMachine_Ligne(Id) ON DELETE CASCADE,
    PieceRefId UNIQUEIDENTIFIER NOT NULL REFERENCES PieceReference(Id),
    RoleVerif VARCHAR(10) NOT NULL CHECK (RoleVerif IN ('PRC','PRNC','FEC','FENC')),
    ResultatAttendu VARCHAR(10) NOT NULL DEFAULT 'C' CHECK (ResultatAttendu IN ('C','NC','BLOQUE')),
    Notes NVARCHAR(MAX),
    UNIQUE (PlanLigneId, PieceRefId)
);
GO

-- =================================================================================
-- PARTIE 7 : FICHE D'ÉCHANTILLONNAGE
-- =================================================================================

CREATE TABLE Plan_Echantillonnage_Entete (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CodeReference VARCHAR(20) NOT NULL UNIQUE,
    CodeArticleSage VARCHAR(30) NULL,
    MachineCode VARCHAR(30) REFERENCES ERP_Machine(CodeMachine),
    NiveauControle VARCHAR(5) NOT NULL CHECK (NiveauControle IN ('I','II','III')),
    TypePlan VARCHAR(10) NOT NULL CHECK (TypePlan IN ('SIMPLE','DOUBLE')),
    ModeControle VARCHAR(15) NOT NULL CHECK (ModeControle IN ('NORMAL','REDUIT','RENFORCE')),
    NqaId INT NOT NULL REFERENCES NQA(Id),
    Version INT NOT NULL DEFAULT 1,
    Statut VARCHAR(20) NOT NULL DEFAULT 'BROUILLON' CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE')),
    CreePar VARCHAR(20) NOT NULL,
    CreeLe DATETIME NOT NULL DEFAULT GETDATE(),
    CommentaireVersion NVARCHAR(MAX)
);
GO
CREATE TRIGGER trg_no_del_PlanEchan ON Plan_Echantillonnage_Entete INSTEAD OF DELETE AS EXEC sp_RaiseDeleteError 'Plan_Echantillonnage_Entete';
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanEchan_Article 
    ON Plan_Echantillonnage_Entete(CodeReference, CodeArticleSage, Version) WHERE CodeArticleSage IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UQ_PlanEchan_Generic 
    ON Plan_Echantillonnage_Entete(CodeReference, Version) WHERE CodeArticleSage IS NULL;
GO

CREATE TABLE Plan_Echantillonnage_Regle (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FicheEnteteId UNIQUEIDENTIFIER NOT NULL REFERENCES Plan_Echantillonnage_Entete(Id) ON DELETE CASCADE,
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

-- FK retardée
ALTER TABLE Plan_Ass_Entete
    ADD CONSTRAINT fk_planass_echan
    FOREIGN KEY (FicheEchantillonnageId) REFERENCES Plan_Echantillonnage_Entete(Id);
GO