-- ================================================================================
-- SOPAL TRACE DB - SCHEMA V4.0 FINAL
--
-- ARCHITECTURE :
--   Documents qualité          → Document_Entete / Document_Section / Document_Ligne
--                                 (portée GENERIQUE ou SPECIFIQUE)
--   Document_VerifMachine_*    → extensions des plans de vérification machine
--                                 (familles, échéances, matrice des pièces)
--
-- PRINCIPES :
--   - Logique métier dans le backend C# ; la base garantit l'intégrité
--   - Triggers limités à l'intégrité : interdiction de suppression (ISO 9001),
--     unicités conditionnelles (voir PARTIE 16)
--   - Tous les matricules référencent l'annuaire des employés (AUTILIS)
--   - Dates en DATETIME2(3), heure locale du serveur (Africa/Tunis)
--   - Concurrence optimiste : colonne RowVersion sur les documents et l'exécution
--   - Ref_Caracteristique : LibelleNormalise fourni par le backend C# (pas de PERSISTED)
--   - Colonnes propres à un formulaire : tables *_ExtraColonne (définies par Ref_Formulaire_ColonneDef)
--   - Nouveau type de document = INSERT INTO TypeDocument, zéro DDL
-- ================================================================================

-- Requis pour les index filtrés (une seule session active par OF, un seul assemblage actif par OF).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE master;
GO

-- Sécurité : ce script ne supprime jamais une base existante.
-- Pour réinitialiser un environnement de développement, supprimer la base manuellement au préalable.
IF DB_ID('SopalTraceDB') IS NOT NULL
BEGIN
    RAISERROR('La base SopalTraceDB existe déjà : script interrompu (aucune donnée supprimée).', 16, 1);
    SET NOEXEC ON;
END
GO

CREATE DATABASE SopalTraceDB;
GO
USE SopalTraceDB;
GO

-- ================================================================================
-- PARTIE 0 : TABLES DE RÉFÉRENCE (ISO 9001 – suppression interdite par trigger)
-- ================================================================================

-- Référentiels de valeurs : le métier peut ajouter une valeur sans modifier la base.
-- Les valeurs qui pilotent une logique du code restent des contraintes CHECK + constantes C#.

CREATE TABLE dbo.AppRole (
    Code   VARCHAR(50)  PRIMARY KEY,
    Label  VARCHAR(100) NOT NULL,
    Active BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.ColumnValueType (
    Code   VARCHAR(20)  PRIMARY KEY,
    Label  VARCHAR(60)  NOT NULL,
    Active BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.ExecutionDocumentType (
    Code   VARCHAR(50)  PRIMARY KEY,
    Label  VARCHAR(100) NOT NULL,
    Active BIT          NOT NULL DEFAULT 1,
    -- Type du document qualité exécuté (NULL = pas de document : clôture, registre).
    -- Clé étrangère vers TypeDocument déclarée après la création de cette table.
    DocumentTypeCode VARCHAR(30) NULL
);
GO

-- Équipes de production (identités, sans horaire : les horaires dépendent du formulaire).
CREATE TABLE dbo.Team (
    Code   VARCHAR(10)  PRIMARY KEY,
    Label  VARCHAR(60)  NOT NULL,
    Active BIT          NOT NULL DEFAULT 1
);
GO

-- Niveaux de contrôle ISO 2859-1.
CREATE TABLE dbo.InspectionLevel (
    Code         VARCHAR(10) PRIMARY KEY,
    Label        VARCHAR(60) NOT NULL,
    DisplayOrder INT         NOT NULL
);
GO

-- Rôle d'une pièce de référence en vérification machine (conforme, non conforme, fuite étalon).
CREATE TABLE dbo.ReferencePieceRole (
    Code   VARCHAR(10)  PRIMARY KEY,
    Label  VARCHAR(100) NOT NULL
);
GO

CREATE TABLE dbo.TypeRobinet (
    Code    VARCHAR(10)  PRIMARY KEY,
    Libelle VARCHAR(60)  NOT NULL,
    Actif   BIT          NOT NULL DEFAULT 1
);
GO

-- Un type de robinet regroupe plusieurs familles (ex. AUTO : RBGFA-BAC-01, RBGFA-BAC-02) ;
-- chaque famille a un seul type, et c'est par elle que le produit fini connaît son type.
CREATE TABLE dbo.FamilleProduitFini (
    Code            VARCHAR(30)  PRIMARY KEY,
    Designation     VARCHAR(250) NOT NULL,
    TypeRobinetCode VARCHAR(10)  NOT NULL REFERENCES dbo.TypeRobinet(Code),
    Actif           BIT          NOT NULL DEFAULT 1,
    -- Les plans d'assemblage de cette famille sont propres à chaque poste (ex. robinets avec soupape)
    PlanParPoste    BIT          NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.NatureArticle (
    Code    VARCHAR(20)  PRIMARY KEY,
    Libelle VARCHAR(100) NOT NULL,
    -- Pas d'origine ici : une même nature peut regrouper des articles fabriqués et achetés
    -- (ex. VOLANT). L'origine est portée par l'article (Article.Origin).
    Actif   BIT          NOT NULL DEFAULT 1,
    -- Un seul plan générique pour toute la nature (pas de plan par article)
    PlanGenerique BIT    NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.Ref_FamilleCorps (
    Id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Code        VARCHAR(50)  NOT NULL UNIQUE,
    Designation VARCHAR(150) NOT NULL,
    Actif       BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Operation (
    Code         VARCHAR(20) PRIMARY KEY,
    Libelle      VARCHAR(80) NOT NULL,
    Actif        BIT         NOT NULL DEFAULT 1,
    -- L'OF s'exécute sur une machine (machine prévue / utilisée enregistrées au démarrage)
    AvecMachine  BIT         NOT NULL DEFAULT 0,
    -- Longueur et diamètre nominaux saisis au démarrage (tronçonnage)
    AvecCotesTronconnage BIT NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.NatureArticle_Operation (
    NatureArticleCode VARCHAR(20) NOT NULL REFERENCES dbo.NatureArticle(Code) ON DELETE CASCADE,
    OperationCode     VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code)     ON DELETE CASCADE,
    OrdreGamme        INT         NOT NULL DEFAULT 1,
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
    Id      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Code    VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(80) NOT NULL,
    Actif   BIT         NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.TypeControle (
    Id      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Code    VARCHAR(30) NOT NULL UNIQUE,
    Libelle VARCHAR(80) NOT NULL,
    Actif   BIT         NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.MoyenControle (
    Id      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Code    VARCHAR(40) NOT NULL UNIQUE,
    Libelle VARCHAR(100) NOT NULL,
    Actif   BIT         NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Periodicite (
    Id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    NbrPieces   INT          NOT NULL,
    Unite       VARCHAR(20)  NOT NULL CHECK (Unite IN ('SERIE', 'ECHANTILLON', 'HEURE')),
    NbrHeures   INT          NULL,
    Actif       BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.PeriodiciteMachine (
    Id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Code           VARCHAR(30)  NOT NULL UNIQUE,
    Libelle        VARCHAR(200) NOT NULL,
    OrdreAffichage INT          NOT NULL DEFAULT 0,
    Actif          BIT          NOT NULL DEFAULT 1,
    -- Moment de la session où les points de cette périodicité se vérifient (assistant d'assemblage).
    -- Donnée explicite : le libellé reste libre et n'est jamais interprété. NULL = sans lien avec ces moments.
    ControlMoment  VARCHAR(20)  NULL CHECK (ControlMoment IN ('DEMARRAGE', 'APRES_PAUSE', 'FIN_POSTE'))
);
GO

CREATE TABLE dbo.TypeSection (
    Id                UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    -- TypeNormalise : identifiant unique et source de vérité pour la logique applicative (C# + Vue.js).
    -- Remplace l'ancien 'Code' (redondant) et les 4 fallbacks fragiles de DetecterTypeSection().
    -- Valeurs possibles : 'REGLAGE' | 'REGLAGE_PROD' | 'ECHANTILLONNAGE_3789' | 'ECHANTILLONNAGE_2859' | 'ECHANTILLONNAGE_3951'
    --                     | 'LOT_POSTE' | 'LOT_OF' | 'TOUS_PIECES'
    -- Le type de section porte seul la règle de contrôle (réglage, échantillonnage selon norme, lot, 100 %) ;
    -- la périodicité, facultative pour tous les types, est choisie sur la section (Document_Section.PeriodiciteId).
    TypeNormalise     VARCHAR(50)  NOT NULL UNIQUE,
    -- Libelle : texte fixe affiché à l'opérateur, récupéré directement depuis cette table.
    Libelle           VARCHAR(100) NOT NULL,
    Actif             BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.RisqueDefaut (
    Id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    CodeDefaut    VARCHAR(30)  NOT NULL UNIQUE,
    LibelleDefaut VARCHAR(100) NOT NULL,
    Actif         BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.NQA (
    Id        INT   IDENTITY(1,1) PRIMARY KEY,
    ValeurNQA DECIMAL(6,3) NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.Ref_MoyenDetection (
    Id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Code        VARCHAR(50)  NOT NULL UNIQUE,
    Designation VARCHAR(100) NOT NULL,
    Actif       BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Instrument (
    CodeInstrument     VARCHAR(40)  PRIMARY KEY,
    Designation        VARCHAR(100) NOT NULL,
    Actif              BIT          NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.PieceReference (
    Id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Code           VARCHAR(30)  NOT NULL UNIQUE,
    TypePiece      VARCHAR(10)  NOT NULL REFERENCES dbo.ReferencePieceRole(Code),
    Designation    VARCHAR(150),
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
    Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Libelle               VARCHAR(250) NOT NULL,         -- tel que saisi (1ère fois)
    LibelleNormalise      VARCHAR(250) NOT NULL UNIQUE,  -- fourni par le backend C#
    TypeCaracteristiqueId UNIQUEIDENTIFIER REFERENCES dbo.TypeCaracteristique(Id),
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
    Actif             BIT          NOT NULL DEFAULT 1
);
GO

ALTER TABLE dbo.ExecutionDocumentType
    ADD CONSTRAINT FK_ExecutionDocumentType_TypeDocument FOREIGN KEY (DocumentTypeCode) REFERENCES dbo.TypeDocument(Code);
GO



-- ================================================================================
-- PARTIE 3 : AUTHENTIFICATION ET SÉCURITÉ
-- ================================================================================

-- Annuaire des employés (miroir Sage X3) : référence de tous les matricules de l'application.
-- La synchronisation désactive un employé (ENAFLG_0) au lieu de le supprimer.
CREATE TABLE dbo.AUTILIS (
    USR_0    VARCHAR(20)  PRIMARY KEY,
    INTUSR_0 VARCHAR(100) NOT NULL,
    ENAFLG_0 INT          NOT NULL DEFAULT 1,
    CODMET_0 VARCHAR(20)  NOT NULL,
    ADDEML_0 VARCHAR(150)
);
GO

CREATE TABLE dbo.UtilisateursApp (
    Id                          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Matricule                   VARCHAR(20)  NOT NULL UNIQUE REFERENCES dbo.AUTILIS(USR_0),
    NomComplet                  VARCHAR(100) NOT NULL,
    Email                       VARCHAR(150) NOT NULL UNIQUE,
    -- NULL tant que l'employé n'a pas défini son mot de passe (demande en attente, invitation envoyée).
    MotDePasseHash              VARCHAR(255) NULL,
    -- NULL possible seulement pour une demande en attente ou refusée : le rôle est choisi par l'administrateur.
    RoleApp                     VARCHAR(50)  NULL REFERENCES dbo.AppRole(Code),
    IntituleMetier              VARCHAR(100),
    -- Cycle de vie du compte (machine à états, transitions contrôlées par l'application) :
    --   PENDING_APPROVAL → INVITED (validation administrateur) ou REJECTED (refus motivé)
    --   INVITED → ACTIVE (mot de passe défini via le lien) ; ACTIVE / INVITED ↔ DISABLED
    -- Le verrouillage après échecs de connexion est temporaire et indépendant (LockoutEndsAt).
    AccountStatus               VARCHAR(20)  NOT NULL DEFAULT 'PENDING_APPROVAL'
        CONSTRAINT CK_UtilisateursApp_AccountStatus CHECK (AccountStatus IN ('PENDING_APPROVAL','INVITED','ACTIVE','REJECTED','DISABLED')),
    CodeRecuperationHash        VARCHAR(255),
    DateExpirationCode          DATETIME2(3),
    NombreTentativesCode        INT          NOT NULL DEFAULT 0,
    DateCreation                DATETIME2(3)     DEFAULT SYSDATETIME(),
    DateDerniereConnexion       DATETIME2(3),
    -- Lien d'activation à usage unique : seule l'empreinte SHA-256 (hexadécimal) du jeton est stockée.
    ActivationTokenHash         CHAR(64)     NULL,
    ActivationTokenExpiresAt    DATETIME2(3) NULL,
    -- Décision de l'administrateur sur la demande d'accès (traçabilité ISO 9001).
    ReviewedBy                  VARCHAR(20)  NULL,
    ReviewedAt                  DATETIME2(3) NULL,
    ReviewComment               VARCHAR(500) NULL,
    -- Échecs de connexion consécutifs ; au-delà du seuil, le compte est verrouillé jusqu'à LockoutEndsAt.
    FailedLoginCount            INT          NOT NULL DEFAULT 0,
    LockoutEndsAt               DATETIME2(3) NULL,
    -- Renouvelé à chaque changement de rôle, désactivation ou nouveau mot de passe :
    -- les jetons d'accès émis avant ce changement sont refusés immédiatement.
    SecurityStamp               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    CONSTRAINT CK_UtilisateursApp_RoleRequired CHECK (RoleApp IS NOT NULL OR AccountStatus IN ('PENDING_APPROVAL','REJECTED')),
    CONSTRAINT CK_UtilisateursApp_PasswordWhenActive CHECK (AccountStatus <> 'ACTIVE' OR MotDePasseHash IS NOT NULL)
);
GO

CREATE UNIQUE INDEX UX_UtilisateursApp_ActivationTokenHash ON dbo.UtilisateursApp(ActivationTokenHash) WHERE ActivationTokenHash IS NOT NULL;
GO

-- Rôle choisi par l'administrateur avant l'inscription de l'employé : à l'inscription, le compte est
-- directement invité avec ce rôle (sans passer par la file des demandes). La ligne est consommée.
CREATE TABLE dbo.PreassignedRole (
    Matricule   VARCHAR(20)  PRIMARY KEY REFERENCES dbo.AUTILIS(USR_0),
    RoleCode    VARCHAR(50)  NOT NULL REFERENCES dbo.AppRole(Code),
    AssignedBy  VARCHAR(20)  NOT NULL,
    AssignedAt  DATETIME2(3) NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE INDEX IX_UtilisateursApp_Matricule ON dbo.UtilisateursApp(Matricule);
GO

CREATE TABLE dbo.RefreshTokens (
    Id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UtilisateurId  UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.UtilisateursApp(Id) ON DELETE CASCADE,
    -- Empreinte SHA-256 (hexadécimal) du jeton : le jeton lui-même n'est jamais stocké.
    TokenHash      CHAR(64)     NOT NULL UNIQUE,
    JwtId          VARCHAR(100) NOT NULL,
    DateCreation   DATETIME2(3)     DEFAULT SYSDATETIME(),
    DateExpiration DATETIME2(3)     NOT NULL,
    EstRevoque     BIT          DEFAULT 0
);
GO

CREATE TABLE dbo.JournalConnexions (
    Id         UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Matricule  VARCHAR(20)  NOT NULL,
    Action     VARCHAR(50)  NOT NULL,
    Details    VARCHAR(255),
    DateAction DATETIME2(3)     DEFAULT SYSDATETIME()
);
GO

-- Journal d'audit (ISO 9001) : chaque création, modification ou suppression sur les documents
-- et l'exécution, avec l'auteur et les valeurs avant/après (JSON). Alimenté par l'API.
CREATE TABLE dbo.AuditLog (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    TableName     VARCHAR(128)  NOT NULL,
    EntityId      VARCHAR(100)  NOT NULL,
    Action        VARCHAR(10)   NOT NULL CHECK (Action IN ('INSERT', 'UPDATE', 'DELETE')),
    Changes       NVARCHAR(MAX) NOT NULL CHECK (ISJSON(Changes) = 1),
    UserMatricule VARCHAR(20)   NULL REFERENCES dbo.AUTILIS(USR_0),
    OccurredAt    DATETIME2(3)  NOT NULL DEFAULT SYSDATETIME()
);
GO

CREATE INDEX IX_AuditLog_Entity ON dbo.AuditLog (TableName, EntityId, OccurredAt);
GO

-- Le journal d'audit est en ajout seul.
CREATE TRIGGER TR_AuditLog_AppendOnly ON dbo.AuditLog
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 50002, 'Le journal d''audit ne peut être ni modifié ni supprimé.', 1;
END;
GO

-- ================================================================================
-- PARTIE 4 : ARTICLES
-- ================================================================================

-- TypeArticle : PF (produit fini) et SF (semi-fini) sont fabriqués, COMPOSANT est acheté.
-- Origin en est déduite (une seule source de vérité, jamais saisie).
CREATE TABLE dbo.Article (
    CodeArticle       VARCHAR(30)  PRIMARY KEY,
    Designation       VARCHAR(150) NOT NULL,
    NatureArticleCode VARCHAR(20)  NOT NULL REFERENCES dbo.NatureArticle(Code),
    TypeArticle       VARCHAR(20)  NOT NULL CHECK (TypeArticle IN ('PF','SF','COMPOSANT')),
    Origin            AS CAST(CASE WHEN TypeArticle = 'COMPOSANT' THEN 'PURCHASED' ELSE 'MANUFACTURED' END AS VARCHAR(12)) PERSISTED NOT NULL,
    DateCreation      DATETIME2(3)     DEFAULT SYSDATETIME(),
    DateModification  DATETIME2(3),
    Actif             BIT          NOT NULL DEFAULT 1,
    -- La nature « PF » est réservée aux produits finis, et un produit fini a toujours cette nature.
    CONSTRAINT CK_Article_FinishedProductNature CHECK (
        (NatureArticleCode = 'PF' AND TypeArticle = 'PF') OR (NatureArticleCode <> 'PF' AND TypeArticle <> 'PF')),
    -- Cibles des clés composites de ProduitFini (seulement un PF) et de la nomenclature (parent fabriqué).
    CONSTRAINT UQ_Article_Type   UNIQUE (CodeArticle, TypeArticle),
    CONSTRAINT UQ_Article_Origin UNIQUE (CodeArticle, Origin)
);
GO

-- Spécialisation d'un article PF : sa famille (obligatoire), qui donne son type de robinet.
-- La clé composite (CodeArticle, ArticleType = 'PF') interdit d'y rattacher un SF ou un composant.
CREATE TABLE dbo.ProduitFini (
    CodeArticle            VARCHAR(30) PRIMARY KEY,
    FamilleProduitFiniCode VARCHAR(30) NOT NULL REFERENCES dbo.FamilleProduitFini(Code),
    ArticleType            AS CAST('PF' AS VARCHAR(20)) PERSISTED NOT NULL,
    CONSTRAINT FK_ProduitFini_FinishedProductArticle FOREIGN KEY (CodeArticle, ArticleType)
        REFERENCES dbo.Article(CodeArticle, TypeArticle) ON DELETE CASCADE
);
GO

-- Composition d'un article fabriqué (PF ou SF) : semi-finis, composants achetés, matière.
-- ParentOrigin = 'MANUFACTURED' + clé composite : un article acheté n'a pas de nomenclature.
-- Les cycles sur plusieurs niveaux (A → B → A) ne peuvent pas être bloqués par une contrainte.
CREATE TABLE dbo.BOMD_Nomenclature (
    ArticleParent   VARCHAR(30) NOT NULL,
    CodeComposant   VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    CodeAlternative INT         NOT NULL,
    QuantiteRequise DECIMAL(18,3) NOT NULL,
    ParentOrigin    AS CAST('MANUFACTURED' AS VARCHAR(12)) PERSISTED NOT NULL,
    PRIMARY KEY (ArticleParent, CodeComposant, CodeAlternative),
    CONSTRAINT FK_BillOfMaterials_ManufacturedParent FOREIGN KEY (ArticleParent, ParentOrigin)
        REFERENCES dbo.Article(CodeArticle, Origin),
    CONSTRAINT CK_BillOfMaterials_NotItsOwnComponent CHECK (ArticleParent <> CodeComposant),
    -- Sage tolère une quantité nulle sur une ligne de nomenclature : seule une quantité négative est refusée.
    CONSTRAINT CK_BillOfMaterials_NonNegativeQuantity CHECK (QuantiteRequise >= 0)
);
GO

-- ================================================================================
-- PARTIE 5 : ERP – SAGE X3 CACHE
-- ================================================================================

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
    QuantitePrevue DECIMAL(18,3) NOT NULL,
    QuantiteLancee DECIMAL(18,3) NOT NULL DEFAULT 0,
    QuantiteReelle DECIMAL(18,3) NOT NULL DEFAULT 0,
    StatutOF       VARCHAR(20) NOT NULL CHECK (StatutOF IN ('EN_COURS','PLANIFIE','TERMINE','ANNULE','EN_PAUSE')),
    DateDebut      DATETIME2(3),
    DateFin        DATETIME2(3)
);
GO

CREATE TABLE dbo.MFGMAT_BesoinOF (
    Id              INT  IDENTITY(1,1) PRIMARY KEY,
    NumeroOF        VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF) ON DELETE CASCADE,
    CodeArticle     VARCHAR(30) NOT NULL REFERENCES dbo.Article(CodeArticle),
    QuantiteRequise DECIMAL(18,3) NOT NULL,
    QuantiteSortie  DECIMAL(18,3) NOT NULL DEFAULT 0
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
    Actif           BIT          NOT NULL DEFAULT 1,
    -- Présentation du formulaire de vérification machine (au lieu de règles codées sur le nom de la machine)
    VerifModele                 VARCHAR(10) NULL CHECK (VerifModele IN ('BEE','MAS','SER')),  -- NULL = générique
    VerifSansConformite         BIT NOT NULL DEFAULT 0,  -- pas de section « conformité »
    VerifFuiteEtalon            BIT NOT NULL DEFAULT 0,  -- colonnes fuite étalon par défaut
    VerifMoyenControleNumerote  BIT NOT NULL DEFAULT 0,  -- libellés « numéro du moyen de contrôle »
    VerifSansDeltaP             BIT NOT NULL DEFAULT 0,  -- pas de colonne ΔP
    VerifSansPression           BIT NOT NULL DEFAULT 0,  -- ni pression ni ΔP
    VerifTablesEtalonSeparees   BIT NOT NULL DEFAULT 0,  -- risques normaux et risques étalon en deux tableaux
    VerifFuiteEnPa              BIT NOT NULL DEFAULT 0,  -- fuite affichée en Pa
    VerifColonnesParTableau     BIT NOT NULL DEFAULT 0   -- colonnes propres à chaque tableau ; moyen de détection fusionné avec la périodicité des risques
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
    Id                        UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    CodeReference             VARCHAR(30)  NOT NULL,
    Designation               VARCHAR(150) NOT NULL,
    Version                   INT          NOT NULL DEFAULT 0,
    Statut                    VARCHAR(20)  NOT NULL DEFAULT 'ACTIF'
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE')),
    CreeLe                    DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
    CreePar                   VARCHAR(20) REFERENCES dbo.AUTILIS(USR_0),
    ModifiePar                VARCHAR(20) REFERENCES dbo.AUTILIS(USR_0),
    ModifieLe                 DATETIME2(3),
    -- Rôle métier du formulaire (constantes RolesFormulaire côté C#).
    Role                      VARCHAR(50)
        CHECK (Role IN ('EN_COURS_DE_FABRICATION','EN_COURS_DE_ASSEMBLAGE','PRODUIT_FINI','RESULTAT_CONTROLE_CF','RESULTAT_CONTROLE_POSTE','VERIF_MACHINE','ECHANTILLONNAGE')),
    -- Champ d'application des documents construits sur ce formulaire (ex. contrôle au poste « PAS71 soupape ») :
    -- poste, et famille de produit fini (NULL = toutes les familles). Un document créé sur ce formulaire reprend
    -- ce poste et, si elle est renseignée, cette famille : aucune déduction depuis le code ou la désignation.
    PosteCode                 VARCHAR(30) NULL REFERENCES dbo.PosteTravail(CodePoste),
    FamilleProduitFiniCode    VARCHAR(30) NULL REFERENCES dbo.FamilleProduitFini(Code),
    UNIQUE (CodeReference, Version)
);
GO

CREATE TABLE dbo.Ref_Formulaire_ColonneDef (
    Id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    FormulaireId     UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_Formulaire(Id),
    CleColonne       VARCHAR(60)  NOT NULL,
    LabelAffiche     VARCHAR(100) NOT NULL,
    TypeValeur       VARCHAR(20)  NOT NULL DEFAULT 'TEXT' REFERENCES dbo.ColumnValueType(Code),
    InsertAfter      VARCHAR(60)  NULL,
    -- Vérification machine : tableau où la colonne apparaît (NULL = sans objet)
    TargetTable      VARCHAR(50)  NULL CHECK (TargetTable IN ('all','conformite','risques')),
    Actif            BIT          NOT NULL DEFAULT 1,
    UNIQUE (FormulaireId, CleColonne)
);
GO

-- Valeurs proposées pour une colonne de type LIST.
CREATE TABLE dbo.ColumnAllowedValue (
    Id                 UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ColumnDefinitionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_Formulaire_ColonneDef(Id),
    Value              NVARCHAR(200)    NOT NULL,
    DisplayOrder       INT              NOT NULL DEFAULT 0,
    Active             BIT              NOT NULL DEFAULT 1,
    UNIQUE (ColumnDefinitionId, Value)
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
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    FormulaireId    UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_Formulaire(Id),
    -- Équipe concernée : la Nième équipe active du formulaire correspond à l'équipe EN.
    TeamCode        VARCHAR(10)   NOT NULL REFERENCES dbo.Team(Code),
    NomEquipe       VARCHAR(60)   NOT NULL,
    HeureDebut      INT           NOT NULL,
    HeureFin        INT           NOT NULL,
    OrdreAffiche    INT           NOT NULL DEFAULT 0,
    Actif           BIT           NOT NULL DEFAULT 1,
    UNIQUE (FormulaireId, NomEquipe)
);
GO

CREATE UNIQUE INDEX UX_RefFormulaireEquipe_Team ON dbo.Ref_Formulaire_Equipe (FormulaireId, TeamCode) WHERE Actif = 1;
GO

CREATE TRIGGER trg_no_del_Ref_Formulaire_Equipe
ON dbo.Ref_Formulaire_Equipe
INSTEAD OF DELETE
AS BEGIN
    RAISERROR ('Suppression physique interdite sur Ref_Formulaire_Equipe (ISO 9001).', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO


-- ================================================================================
-- PARTIE 9 : RÉFÉRENTIEL ISO 2859-1 (tables d'échantillonnage)
-- ================================================================================

CREATE TABLE dbo.Ref_ISO2859_LettresCode (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QteMin INT NOT NULL,
    QteMax INT NOT NULL,
    NiveauControle VARCHAR(10) NOT NULL REFERENCES dbo.InspectionLevel(Code),
    CodeLettre CHAR(1) NOT NULL
);
GO

CREATE TABLE dbo.Ref_ISO2859_Echantillonnage (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CodeLettre CHAR(1) NOT NULL,
    Quantite INT NOT NULL,
    UNIQUE (CodeLettre)
);
GO




-- ================================================================================
-- PARTIE 10 : DOCUMENTS QUALITÉ (famille unique)
--
-- Tous les documents de contrôle partagent la même structure : en-tête, sections,
-- lignes, colonnes supplémentaires. TypeDocumentCode discrimine le type :
--   MODELE_FAB (modèle de fabrication), PLAN_FAB (plan de fabrication d'un article),
--   PLAN_ASS, PLAN_PF, CTRL_POSTE, RESULTAT_CF.
--
-- Portee :
--   GENERIQUE  : s'applique selon des critères (nature, famille, opération, poste...)
--   SPECIFIQUE : s'applique à un code article (CodeArticleSageVersionne obligatoire)
-- Un document générique ne dépend d'aucun autre document. Un document spécifique peut être créé
-- à partir d'un générique (DocumentSourceId renseigné) ou directement (DocumentSourceId NULL).
-- ================================================================================

CREATE TABLE dbo.Document_Entete (
    Id                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TypeDocumentCode       VARCHAR(30)  NOT NULL REFERENCES dbo.TypeDocument(Code),
    Portee                 VARCHAR(12)  NOT NULL DEFAULT 'GENERIQUE'
        CHECK (Portee IN ('GENERIQUE','SPECIFIQUE')),
    -- Article ciblé (code Sage versionné) : renseigné si et seulement si la portée est SPECIFIQUE.
    CodeArticleSageVersionne VARCHAR(30) NULL,
    -- Document générique dont celui-ci est issu (plan de fabrication créé depuis un modèle).
    DocumentSourceId       UNIQUEIDENTIFIER NULL REFERENCES dbo.Document_Entete(Id),
    Nom                    VARCHAR(150) NOT NULL,
    Designation            VARCHAR(200),
    Version                INT          NOT NULL DEFAULT 0,
    Statut                 VARCHAR(20)  NOT NULL DEFAULT 'BROUILLON'
        CHECK (Statut IN ('BROUILLON','ACTIF','ARCHIVE')),
    OperationCode          VARCHAR(20)  REFERENCES dbo.Operation(Code),
    FormulaireId           UNIQUEIDENTIFIER REFERENCES dbo.Ref_Formulaire(Id),
    LegendeMoyens          NVARCHAR(MAX),
    Remarques              NVARCHAR(MAX),
    -- Motif de la création de cette version
    CommentaireVersion     NVARCHAR(MAX),
    CreePar                VARCHAR(20)  NOT NULL REFERENCES dbo.AUTILIS(USR_0),
    CreeLe                 DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
    ModifiePar             VARCHAR(20) REFERENCES dbo.AUTILIS(USR_0),
    ModifieLe              DATETIME2(3),
    -- Spécifique ASSEMBLAGE / PRODUIT_FINI
    NatureArticleCode      VARCHAR(20)  REFERENCES dbo.NatureArticle(Code),
    -- Le type de robinet du document se déduit de la famille (FamilleProduitFini.TypeRobinetCode).
    FamilleProduitFiniCode VARCHAR(30)  REFERENCES dbo.FamilleProduitFini(Code),
    -- Spécifique ASSEMBLAGE / CTRL_POSTE / RESULTAT_CF
    PosteCode              VARCHAR(30)  REFERENCES dbo.PosteTravail(CodePoste),
    -- PLAN_FAB : machine prévue par défaut ; VERIF_MACHINE : machine vérifiée (obligatoire)
    MachineCode            VARCHAR(30)  REFERENCES dbo.Machine(CodeMachine),
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT CK_DocumentEntete_Portee CHECK (
        (Portee = 'SPECIFIQUE' AND CodeArticleSageVersionne IS NOT NULL)
        OR (Portee = 'GENERIQUE' AND CodeArticleSageVersionne IS NULL)),
    CONSTRAINT CK_DocumentEntete_SourceSpecifique CHECK (DocumentSourceId IS NULL OR Portee = 'SPECIFIQUE'),
    CONSTRAINT CK_DocumentEntete_VerifMachine CHECK (
        TypeDocumentCode <> 'VERIF_MACHINE' OR MachineCode IS NOT NULL)
);
GO

CREATE INDEX IX_Document_Entete_TypeStatut ON dbo.Document_Entete(TypeDocumentCode, Statut);
CREATE INDEX IX_Document_Entete_Article    ON dbo.Document_Entete(CodeArticleSageVersionne) WHERE CodeArticleSageVersionne IS NOT NULL;
CREATE INDEX IX_Document_Entete_Machine    ON dbo.Document_Entete(MachineCode) WHERE MachineCode IS NOT NULL;
GO


CREATE TABLE dbo.Document_Section (
    Id                     UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    EnteteId               UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche           INT              NOT NULL DEFAULT 0,
    LibelleSection         VARCHAR(300)     NOT NULL,
    TypeSectionId          UNIQUEIDENTIFIER REFERENCES dbo.TypeSection(Id),
    PeriodiciteId          UNIQUEIDENTIFIER REFERENCES dbo.Periodicite(Id),
    Notes                  NVARCHAR(MAX),
    -- Spécifique PRODUIT_FINI / ASSEMBLAGE
    NqaId                  INT REFERENCES dbo.NQA(Id)
);
GO

CREATE TABLE dbo.Document_Ligne (
    Id                    UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    EnteteId              UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Entete(Id),
    -- NULL pour les documents sans sections (vérification machine : lignes rattachées à l'entête)
    SectionId             UNIQUEIDENTIFIER NULL REFERENCES dbo.Document_Section(Id) ON DELETE CASCADE,
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
    -- Machine contrôlée (assemblage, contrôle au poste)
    MachineCode           VARCHAR(30)      REFERENCES dbo.Machine(CodeMachine),
    -- Défaut surveillé (contrôle au poste)
    RisqueDefautId        UNIQUEIDENTIFIER REFERENCES dbo.RisqueDefaut(Id),
    -- Vérification machine : point de conformité ou risque (libellé = LibelleAffiche, méthode = Instruction)
    TypeLigne             VARCHAR(20)      NULL CHECK (TypeLigne IN ('CONFORMITE','RISQUE')),
    -- Colonnes propres à un formulaire : Document_Ligne_ExtraColonne
    -- Contrainte assouplie : les deux peuvent être NULL (ligne de brouillon/vide)
    -- Seule la combinaison des deux renseignés simultanément est interdite
    CONSTRAINT CK_Doc_Ligne_Moyen_XOR CHECK (
        NOT (MoyenControleId IS NOT NULL AND MoyenTexteLibre IS NOT NULL)
    ),
    -- Une ligne hors section est forcément une ligne de vérification machine libellée.
    CONSTRAINT CK_Doc_Ligne_Section CHECK (
        SectionId IS NOT NULL OR (TypeLigne IS NOT NULL AND LibelleAffiche IS NOT NULL))
);
GO

CREATE INDEX IX_Document_Ligne_Entete        ON dbo.Document_Ligne(EnteteId);
CREATE INDEX IX_Document_Ligne_Section       ON dbo.Document_Ligne(SectionId);
CREATE INDEX IX_Document_Ligne_Caracteristique ON dbo.Document_Ligne(CaracteristiqueId);
GO

-- Valeurs des colonnes propres au formulaire du document. La colonne est identifiée par sa clé
-- dans le formulaire du document (UNIQUE FormulaireId + CleColonne) : contrôlé par
-- TR_DocumentLigneExtraColonne_Validate, qui vérifie aussi la valeur selon le type de la colonne.
CREATE TABLE dbo.Document_Ligne_ExtraColonne (
    Id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    LigneId       UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Ligne(Id) ON DELETE CASCADE,
    CleColonne    VARCHAR(60)   NOT NULL,
    ValeurColonne NVARCHAR(500),
    OrdreAffiche  INT           NOT NULL DEFAULT 0,
    UNIQUE (LigneId, CleColonne)
);
GO

-- Croquis d'une ligne de contrôle, stocké hors de la ligne pour ne pas alourdir les lectures.
CREATE TABLE dbo.DocumentLineImage (
    LineId      UNIQUEIDENTIFIER PRIMARY KEY REFERENCES dbo.Document_Ligne(Id) ON DELETE CASCADE,
    ContentType VARCHAR(20)      NOT NULL CHECK (ContentType IN ('image/png','image/jpeg','image/gif')),
    Content     VARBINARY(MAX)   NOT NULL,
    CONSTRAINT CK_DocumentLineImage_Size CHECK (DATALENGTH(Content) BETWEEN 1 AND 2097152)
);
GO

-- Paramètres ISO 2859-1 d'un plan d'échantillonnage (Document_Entete de type PLAN_ECHANTILLONNAGE).
CREATE TABLE dbo.SamplingPlan (
    DocumentId               UNIQUEIDENTIFIER PRIMARY KEY REFERENCES dbo.Document_Entete(Id) ON DELETE CASCADE,
    InspectionLevelCode      VARCHAR(10) NOT NULL REFERENCES dbo.InspectionLevel(Code),
    SamplingType             VARCHAR(10) NOT NULL CHECK (SamplingType IN ('SIMPLE','DOUBLE')),
    -- Sévérité du contrôle : normal, réduit ou renforcé
    InspectionSeverity       VARCHAR(15) NOT NULL CHECK (InspectionSeverity IN ('NORMAL','REDUIT','RENFORCE')),
    -- Niveau de qualité acceptable (NQA)
    AcceptableQualityLevelId INT         NOT NULL REFERENCES dbo.NQA(Id),
    -- Critères d'acceptation (Ac) et de rejet (Re)
    AcceptanceNumber         INT         NOT NULL,
    -- Pas de contrainte entre Ac et Re : aucune règle métier arrêtée pour le moment.
    RejectionNumber          INT         NOT NULL
);
GO

-- ================================================================================
-- PARTIE 11 : EXTENSIONS VÉRIFICATION MACHINE
-- ================================================================================

-- Un plan de vérification machine est un Document_Entete de type VERIF_MACHINE (MachineCode
-- obligatoire) dont les lignes (Document_Ligne, TypeLigne renseigné) sont sans section.
-- Les tables ci-dessous portent sa structure propre : familles de corps en colonnes,
-- échéances par ligne et matrice des pièces de référence.

CREATE TABLE dbo.Document_VerifMachine_Famille (
    Id                UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    EnteteId          UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Entete(Id) ON DELETE CASCADE,
    OrdreAffiche      INT              NOT NULL,
    RefFamilleCorpsId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_FamilleCorps(Id),
    UNIQUE (EnteteId, RefFamilleCorpsId)
);
GO

CREATE TABLE dbo.Document_VerifMachine_Echeance (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    LigneId             UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Ligne(Id) ON DELETE CASCADE,
    PeriodiciteMachineId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.PeriodiciteMachine(Id),
    RefMoyenDetectionId UNIQUEIDENTIFIER REFERENCES dbo.Ref_MoyenDetection(Id),
    OrdreAffiche        INT              NOT NULL DEFAULT 0
);
GO

CREATE TABLE dbo.Document_VerifMachine_MatricePiece (
    Id         UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    EcheanceId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_VerifMachine_Echeance(Id) ON DELETE CASCADE,
    FamilleId  UNIQUEIDENTIFIER REFERENCES dbo.Document_VerifMachine_Famille(Id),
    RoleVerif  VARCHAR(10) REFERENCES dbo.ReferencePieceRole(Code),
    PieceRefId UNIQUEIDENTIFIER REFERENCES dbo.PieceReference(Id),
    UNIQUE (EcheanceId, FamilleId, RoleVerif, PieceRefId)
);
GO

-- ================================================================================
-- PARTIE 12 : MODULE MAGASIN
-- ================================================================================

-- Préparation des composants d'un OF par le magasinier, scan par scan.
-- EN_COURS : scans en cours ; TERMINE : composants servis, l'OF est disponible pour la production ; ANNULE : abandonnée.
-- (Le statut de production de l'OF se lit dans Exec_ControleOF, pas ici.)
CREATE TABLE dbo.Mag_PreparationOF (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    NumeroOF            VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    MatriculeMagasinier VARCHAR(20) NOT NULL REFERENCES dbo.AUTILIS(USR_0),
    Statut              VARCHAR(20) NOT NULL DEFAULT 'EN_COURS'
        CHECK (Statut IN ('EN_COURS','TERMINE','ANNULE')),
    DateDebut           DATETIME2(3)    DEFAULT SYSDATETIME(),
    DateFin             DATETIME2(3),
    -- Motif saisi quand la préparation est terminée sans que tous les composants aient été servis.
    IncompleteReason    NVARCHAR(300) NULL,
    RowVersion ROWVERSION NOT NULL
);
GO

-- Un scan d'étiquette = une ligne (lot d'un composant servi pour l'OF).
CREATE TABLE dbo.Mag_PreparationOF_Lot (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    PreparationOFId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Mag_PreparationOF(Id) ON DELETE CASCADE,
    CodeArticle     VARCHAR(30)  NOT NULL REFERENCES dbo.Article(CodeArticle),
    NumeroLotScanne VARCHAR(100) NOT NULL,
    -- Quantité portée par l'étiquette ; NULL si l'étiquette n'en indique pas.
    Quantite        DECIMAL(18,3) NULL CHECK (Quantite > 0),
    -- N° du rapport de contrôle rapide (Quick Control) imprimé sur l'étiquette.
    QualityControlReport VARCHAR(100) NULL,
    -- Contenu brut lu par la douchette, conservé pour la traçabilité.
    ScannedContent  NVARCHAR(300) NULL,
    DateScan        DATETIME2(3)     DEFAULT SYSDATETIME()
);
GO

-- Un même lot n'est enregistré qu'une fois par article dans une préparation (double scan → 409).
CREATE UNIQUE INDEX UX_MagPreparationOFLot_Lot ON dbo.Mag_PreparationOF_Lot (PreparationOFId, CodeArticle, NumeroLotScanne);
GO

CREATE TABLE dbo.Mag_ExpeditionBL (
    Id                  UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    NumeroBL            VARCHAR(30) NOT NULL REFERENCES dbo.SDELIVERY(NumeroBL),
    MatriculeMagasinier VARCHAR(20) NOT NULL REFERENCES dbo.AUTILIS(USR_0),
    Statut              VARCHAR(20) NOT NULL DEFAULT 'EN_COURS'
        CHECK (Statut IN ('EN_COURS','TERMINE')),
    DateDebut           DATETIME2(3)    DEFAULT SYSDATETIME(),
    DateFin             DATETIME2(3),
    RowVersion ROWVERSION NOT NULL
);
GO

CREATE TABLE dbo.Mag_ExpeditionBL_ScanOF (
    Id             UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExpeditionBLId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Mag_ExpeditionBL(Id) ON DELETE CASCADE,
    NumeroOFScanne VARCHAR(30)  NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    DateScan       DATETIME2(3)     DEFAULT SYSDATETIME()
);
GO

-- Un OF peut être livré sur plusieurs BL (livraisons partielles), mais une seule fois par BL.
CREATE UNIQUE INDEX UX_MagExpeditionBLScanOF_OF ON dbo.Mag_ExpeditionBL_ScanOF (ExpeditionBLId, NumeroOFScanne);
GO

-- ================================================================================
-- PARTIE 13 : EXÉCUTION DES CONTRÔLES
-- ================================================================================

CREATE TABLE dbo.Exec_ControleOF (
    Id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    NumeroOF         VARCHAR(30) NOT NULL REFERENCES dbo.MFGHEAD_OrdreFabrication(NumeroOF),
    OperationCode    VARCHAR(20) NOT NULL REFERENCES dbo.Operation(Code),
    MachineCodePrevu VARCHAR(30) REFERENCES dbo.Machine(CodeMachine),
    MachineCode      VARCHAR(30) REFERENCES dbo.Machine(CodeMachine),
    -- Plan de fabrication suivi (FAB) ; NULL pour l'assemblage (plans résolus par poste).
    PlanSourceId     UNIQUEIDENTIFIER NULL REFERENCES dbo.Document_Entete(Id),
    TypeOf           VARCHAR(10) NOT NULL CHECK (TypeOf IN ('FAB','ASS')),
    -- Le réglage est un statut à part entière (pas d'indicateur séparé).
    Statut           VARCHAR(20) NOT NULL DEFAULT 'EN_COURS'
        CHECK (Statut IN ('EN_COURS','CLOTURE','EN_PAUSE','REGLAGE')),
    DateDebut        DATETIME2(3)    NOT NULL DEFAULT SYSDATETIME(),
    -- Fin réelle de l'opération (clôture) uniquement.
    DateFin          DATETIME2(3),
    -- Début de la pause ou du réglage en cours ; NULL en production.
    SuspendedAt      DATETIME2(3)    NULL,
    -- Décalage cumulé (pauses, réglages) appliqué au calendrier des contrôles.
    TempsPauseTotalMinutes FLOAT NOT NULL DEFAULT 0,
    -- Tronçonnage : cotes nominales saisies au démarrage (le plan qualité n'est jamais modifié par l'opérateur).
    LongueurNominale DECIMAL(10,3)   NULL CHECK (LongueurNominale > 0),
    DiametreNominal  DECIMAL(10,3)   NULL CHECK (DiametreNominal > 0),
    RowVersion       ROWVERSION      NOT NULL,
    CONSTRAINT CK_ExecControleOF_DateFin CHECK (DateFin IS NULL OR Statut = 'CLOTURE')
);
GO

-- Postes affectés à l'exécution (seule source des postes d'un OF).
CREATE TABLE dbo.Exec_ControleOf_Poste (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExecControleOfId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id),
    PosteCode VARCHAR(30) NOT NULL REFERENCES dbo.PosteTravail(CodePoste)
);
GO

CREATE TABLE dbo.Exec_ControleOf_Session (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExecControleOfId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id),
    Equipe VARCHAR(10) NOT NULL REFERENCES dbo.Team(Code),
    DateDebut DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
    -- Fin de la session (fin de poste ou clôture de l'OF) uniquement.
    DateFin DATETIME2(3) NULL,
    -- Début de la pause en cours ; NULL hors pause.
    SuspendedAt DATETIME2(3) NULL,
    Statut VARCHAR(20) NOT NULL CHECK (Statut IN ('EN_COURS', 'EN_PAUSE', 'TERMINE')),
    TempsPauseTotalMinutes FLOAT NOT NULL DEFAULT 0,
    -- L'équipe a pris sa pause déjeuner : la vérification machine « après pause » devient due à la reprise.
    HadLunchBreak BIT NOT NULL DEFAULT 0,
    -- Fin demandée par l'équipe (assemblage) : FIN_POSTE ou FIN_OF ; NULL en production.
    -- Tenue par le serveur pour que chaque tablette affiche la même étape.
    ClosingRequest VARCHAR(10) NULL CHECK (ClosingRequest IN ('FIN_POSTE', 'FIN_OF')),
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT CK_ExecControleOfSession_DateFin CHECK (DateFin IS NULL OR Statut = 'TERMINE')
);
GO

CREATE INDEX IX_ExecControleOF_NumeroOF ON dbo.Exec_ControleOF (NumeroOF, OperationCode);
CREATE INDEX IX_ExecControleOF_TypeStatut ON dbo.Exec_ControleOF (TypeOf, Statut);
CREATE UNIQUE INDEX UX_ExecControleOfPoste ON dbo.Exec_ControleOf_Poste (ExecControleOfId, PosteCode);
CREATE INDEX IX_ExecControleOfSession_Exec ON dbo.Exec_ControleOf_Session (ExecControleOfId, Statut);
GO

-- Unicités conditionnelles portées par des triggers et non par des index uniques filtrés :
-- en Database First, un index unique sur la seule clé étrangère ferait générer une relation 1-1
-- (une seule session par OF) au lieu d'une collection.
CREATE TRIGGER TR_ExecControleOF_SingleActiveAssembly ON dbo.Exec_ControleOF
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
               FROM dbo.Exec_ControleOF e
               JOIN (SELECT DISTINCT NumeroOF FROM inserted) i ON i.NumeroOF = e.NumeroOF
               WHERE e.TypeOf = 'ASS' AND e.Statut <> 'CLOTURE'
               GROUP BY e.NumeroOF HAVING COUNT(*) > 1)
        THROW 50001, 'L''assemblage de cet OF est déjà démarré.', 1;
END;
GO

-- Plusieurs équipes peuvent avoir une session ouverte sur un OF (l'une produit, les autres sont en pause et
-- reprendront leur étape) : une seule équipe en production à la fois, une seule session ouverte par équipe.
-- (Statut est dans la clé du premier index pour que la relation OF → sessions reste « plusieurs » côté EF.)
CREATE UNIQUE INDEX UX_ExecControleOfSession_OneRunningTeam
    ON dbo.Exec_ControleOf_Session (ExecControleOfId, Statut) WHERE Statut = 'EN_COURS';
CREATE UNIQUE INDEX UX_ExecControleOfSession_OneOpenSessionPerTeam
    ON dbo.Exec_ControleOf_Session (ExecControleOfId, Equipe) WHERE Statut IN ('EN_COURS', 'EN_PAUSE');
GO

CREATE TABLE dbo.Exec_ControleDocumentStatut (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExecControleOfId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id),
    TypeDocument VARCHAR(50) NOT NULL REFERENCES dbo.ExecutionDocumentType(Code),
    PosteCode VARCHAR(30) NULL REFERENCES dbo.PosteTravail(CodePoste),
    MachineCode VARCHAR(30) NULL REFERENCES dbo.Machine(CodeMachine),
    -- Document qualité appliqué (son type doit correspondre au type d'exécution : TR_ExecControleDocumentStatut_TypeDocument)
    DocId UNIQUEIDENTIFIER NULL REFERENCES dbo.Document_Entete(Id),
    SessionId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleOf_Session(Id),
    EstDemarrageTermine BIT NOT NULL DEFAULT 0,
    EstPauseTermine BIT NOT NULL DEFAULT 0,
    EstTermine BIT NOT NULL DEFAULT 0,
    DateTermine DATETIME2(3) NULL,
    Equipe VARCHAR(10) NULL REFERENCES dbo.Team(Code),
    DateExecution DATETIME2(3) NULL,
    MatriculeOperateur VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
    RowVersion ROWVERSION NOT NULL
);
GO

CREATE INDEX IX_ExecControleDocumentStatut_ExecSession
    ON dbo.Exec_ControleDocumentStatut (ExecControleOfId, SessionId) INCLUDE (TypeDocument, PosteCode, MachineCode);
GO

CREATE TABLE dbo.Exec_PieceType (
    Id                 UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExecControleOFId   UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id),
    HeureValidation    DATETIME2(3)         NOT NULL DEFAULT SYSDATETIME(),
    Resultat           VARCHAR(2)       NOT NULL CHECK (Resultat IN ('C','NC')),
    Remarque           VARCHAR(500),
    MatriculeOperateur VARCHAR(20)      NOT NULL REFERENCES dbo.AUTILIS(USR_0)
);
GO

CREATE TABLE dbo.Exec_ControleTranche (
    Id                   UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExecControleOFId     UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id),
    TrancheHoraire       VARCHAR(20)  NOT NULL,
    HeureDebut           DATETIME2(3)     NOT NULL,
    HeureFin             DATETIME2(3)     NOT NULL,
    ResultatFinal        VARCHAR(10)  CHECK (ResultatFinal IN ('C','NC','REGLAGE','IGNORE')),
    DetailsNC            VARCHAR(500),
    ActionsCorrection    VARCHAR(500),
    MatriculeApprobateur VARCHAR(20) REFERENCES dbo.AUTILIS(USR_0),
    Remarques            VARCHAR(MAX),
    SessionId            UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleOf_Session(Id),
    RowVersion           ROWVERSION   NOT NULL
);
GO

CREATE INDEX IX_ExecControleTranche_Exec ON dbo.Exec_ControleTranche (ExecControleOFId, TrancheHoraire);
GO

-- Période de production pendant laquelle le serveur planifie les contrôles périodiques :
--   - assemblage : par équipe (SessionId) et par poste, une pièce toutes les 60 / effectif par poste minutes
--     (fiche d'échantillonnage) ;
--   - fabrication : par section du plan (SectionId), selon sa périodicité horaire.
-- Une pause, un réglage, la fin de poste ou la clôture la ferment ; la reprise en ouvre une nouvelle et le calcul
-- repart de zéro. Une période sans intervalle (StartedAt = EndedAt) marque le début d'un réglage d'assemblage.
CREATE TABLE dbo.Exec_ProductionPeriod (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExecControleOfId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id),
    SessionId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleOf_Session(Id),
    PosteCode VARCHAR(30) NULL REFERENCES dbo.PosteTravail(CodePoste),
    SectionId UNIQUEIDENTIFIER NULL REFERENCES dbo.Document_Section(Id),
    StartedAt DATETIME2(3) NOT NULL,
    IntervalMinutes FLOAT NULL CHECK (IntervalMinutes > 0),
    -- Nombre de contrôles déjà planifiés dans la période (les suivants sont à StartedAt + (n + 1) × intervalle).
    PlannedControlCount INT NOT NULL DEFAULT 0 CHECK (PlannedControlCount >= 0),
    EndedAt DATETIME2(3) NULL,
    EndReason VARCHAR(20) NULL CHECK (EndReason IN ('PAUSE', 'SETTING', 'SETTING_CANCELLED', 'END_OF_SHIFT', 'STOPPED')),
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT CK_ExecProductionPeriod_End CHECK (
        (EndedAt IS NULL AND EndReason IS NULL) OR (EndedAt IS NOT NULL AND EndReason IS NOT NULL AND EndedAt >= StartedAt)),
    -- Assemblage (équipe + poste) ou fabrication (section), jamais les deux.
    CONSTRAINT CK_ExecProductionPeriod_Scope CHECK (
        (SessionId IS NOT NULL AND PosteCode IS NOT NULL AND SectionId IS NULL)
        OR (SessionId IS NULL AND PosteCode IS NULL AND SectionId IS NOT NULL))
);
GO

-- Au plus une période ouverte par équipe et par poste (assemblage), par section (fabrication).
CREATE UNIQUE INDEX UX_ExecProductionPeriod_OneOpenPerPoste
    ON dbo.Exec_ProductionPeriod (SessionId, PosteCode) WHERE EndedAt IS NULL AND SessionId IS NOT NULL;
CREATE UNIQUE INDEX UX_ExecProductionPeriod_OneOpenPerSection
    ON dbo.Exec_ProductionPeriod (ExecControleOfId, SectionId) WHERE EndedAt IS NULL AND SectionId IS NOT NULL;
CREATE INDEX IX_ExecProductionPeriod_Exec ON dbo.Exec_ProductionPeriod (ExecControleOfId, EndedAt);
GO

CREATE TABLE dbo.Exec_Prelevement_Intermediaire (
    Id                UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExecControleOFId  UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id),
    SectionId         UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Document_Section(Id),
    TrancheHoraire    VARCHAR(20)  NOT NULL,
    NumeroOccurrence  INT          NOT NULL,
    HeureNotifPrevue  DATETIME2(3)     NOT NULL,
    HeureReponse      DATETIME2(3)     NULL,
    Resultat          VARCHAR(10)  NULL CHECK (Resultat IN ('C','NC','REGLAGE','IGNORE')),
    EstEnRetard       BIT          NOT NULL DEFAULT 0,
    EstRepondu        BIT          NOT NULL DEFAULT 0,
    CreeLe            DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
    SessionId         UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleOf_Session(Id),
    -- Période de production de l'occurrence ; assemblage : son poste (numérotation par équipe et par poste).
    PosteCode         VARCHAR(30)  NULL REFERENCES dbo.PosteTravail(CodePoste),
    ProductionPeriodId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ProductionPeriod(Id),
    RowVersion        ROWVERSION   NOT NULL,
    CONSTRAINT UQ_ExecPrelevementIntermediaire_Occurrence
        UNIQUE (ExecControleOFId, SectionId, SessionId, PosteCode, TrancheHoraire, NumeroOccurrence)
);
GO

-- Réponse saisie sur une ligne d'un document pendant l'exécution d'un OF : conformité, mesure
-- (plans FAB / ASS) ou nombre de non-conformités par tranche horaire (contrôle au poste).
CREATE TABLE dbo.Exec_ControleLigne_Reponse (
    Id                      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    ExecControleOFId        UNIQUEIDENTIFIER    NOT NULL REFERENCES dbo.Exec_ControleOF(Id),
    -- Exécution du document auquel appartient la ligne (contrôle au poste…)
    ExecControleDocumentStatutId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleDocumentStatut(Id),
    DocumentLigneId         UNIQUEIDENTIFIER    NOT NULL REFERENCES dbo.Document_Ligne(Id),
    -- REGLAGE, LOT, 100PCT (plans) ou CONTROLE_POSTE
    Contexte                VARCHAR(50)         NOT NULL,
    NumeroReglage           VARCHAR(20)         NULL,
    TrancheHoraire          VARCHAR(20)         NULL,
    ResultatType            VARCHAR(20)         NOT NULL
        CHECK (ResultatType IN ('CONFORME', 'NON_CONFORME', 'MESURE', 'COMPTAGE')),
    ValeurMesuree           DECIMAL(10,3)       NULL,
    -- Nombre compté (ResultatType = COMPTAGE)
    Quantite                INT                 NULL CHECK (Quantite >= 0),
    DetailsNC               VARCHAR(500)        NULL,
    ActionsCorrection       VARCHAR(500)        NULL,
    MatriculeOperateur      VARCHAR(20)         NOT NULL REFERENCES dbo.AUTILIS(USR_0),
    Equipe                  VARCHAR(10)         NULL REFERENCES dbo.Team(Code),
    DateSaisie              DATETIME2(3)        NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT CK_NumeroReglageObligatoire CHECK (Contexte <> 'REGLAGE' OR NumeroReglage IS NOT NULL),
    CONSTRAINT CK_ValeurMesureeObligatoire CHECK (ResultatType <> 'MESURE' OR ValeurMesuree IS NOT NULL),
    CONSTRAINT CK_ComptageComplet CHECK (
        ResultatType <> 'COMPTAGE' OR (Quantite IS NOT NULL AND TrancheHoraire IS NOT NULL AND ExecControleDocumentStatutId IS NOT NULL))
);
GO

-- Index pour retrouver rapidement toutes les lignes d'une session de réglage ou d'un lot
CREATE INDEX IX_ControleLigne_Reglage
    ON dbo.Exec_ControleLigne_Reponse (ExecControleOFId, Contexte, NumeroReglage);
-- Un seul comptage par ligne et par tranche horaire dans une exécution de document.
CREATE UNIQUE INDEX UX_ControleLigne_Comptage
    ON dbo.Exec_ControleLigne_Reponse (ExecControleDocumentStatutId, DocumentLigneId, TrancheHoraire)
    WHERE ResultatType = 'COMPTAGE';
GO

-- Index pour retrouver la dernière saisie d'une ligne de plan
CREATE INDEX IX_ControleLigne_Ligne
    ON dbo.Exec_ControleLigne_Reponse (ExecControleOFId, DocumentLigneId, DateSaisie);
GO



-- ================================================================================
-- PARTIE 14 : SYSTÈME D'ALERTES
-- ================================================================================

CREATE TABLE dbo.Alertes (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TypeAlerte VARCHAR(50) NOT NULL,
    CleEntite VARCHAR(150) NOT NULL,
    DonneesContexte NVARCHAR(MAX) NOT NULL,
    Destinataires VARCHAR(500) NOT NULL,
    DateAlerte DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
    EstResolu BIT NOT NULL DEFAULT 0,
    DateResolution DATETIME2(3) NULL
);
GO

-- ================================================================================
-- PARTIE 15 : TRIGGERS ISO 9001
--
-- Rôle unique : interdire DELETE physique sur les tables de référence.
-- Aucune logique métier ici. Tout le métier est dans le backend C#.
-- Pour "supprimer" : mettre Actif = 0 (soft delete).
-- ================================================================================

CREATE OR ALTER PROCEDURE dbo.usp_RaiseDeleteError (@TableName VARCHAR(100))
AS BEGIN
    RAISERROR('Suppression physique interdite (ISO 9001) sur la table : %s — utiliser Actif = 0', 16, 1, @TableName);
END;
GO

CREATE TRIGGER trg_no_del_TypeRobinet             ON dbo.TypeRobinet             INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'TypeRobinet'; END;
GO
CREATE TRIGGER trg_no_del_FamilleProduitFini       ON dbo.FamilleProduitFini       INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'FamilleProduitFini'; END;
GO
CREATE TRIGGER trg_no_del_NatureArticle            ON dbo.NatureArticle            INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'NatureArticle'; END;
GO
CREATE TRIGGER trg_no_del_Ref_FamilleCorps         ON dbo.Ref_FamilleCorps         INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'Ref_FamilleCorps'; END;
GO
CREATE TRIGGER trg_no_del_Operation                ON dbo.Operation                INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'Operation'; END;
GO
CREATE TRIGGER trg_no_del_PosteTravail             ON dbo.PosteTravail             INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'PosteTravail'; END;
GO
CREATE TRIGGER trg_no_del_TypeCaracteristique      ON dbo.TypeCaracteristique      INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'TypeCaracteristique'; END;
GO
CREATE TRIGGER trg_no_del_TypeControle             ON dbo.TypeControle             INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'TypeControle'; END;
GO
CREATE TRIGGER trg_no_del_MoyenControle            ON dbo.MoyenControle            INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'MoyenControle'; END;
GO
CREATE TRIGGER trg_no_del_Periodicite              ON dbo.Periodicite              INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'Periodicite'; END;
GO
GO
CREATE TRIGGER trg_no_del_TypeSection              ON dbo.TypeSection              INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'TypeSection'; END;
GO
CREATE TRIGGER trg_no_del_RisqueDefaut             ON dbo.RisqueDefaut             INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'RisqueDefaut'; END;
GO
CREATE TRIGGER trg_no_del_NQA                      ON dbo.NQA                      INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'NQA'; END;
GO
CREATE TRIGGER trg_no_del_Ref_MoyenDetection       ON dbo.Ref_MoyenDetection       INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'Ref_MoyenDetection'; END;
GO
CREATE TRIGGER trg_no_del_Instrument               ON dbo.Instrument               INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'Instrument'; END;
GO
CREATE TRIGGER trg_no_del_PieceReference           ON dbo.PieceReference           INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'PieceReference'; END;
GO
CREATE TRIGGER trg_no_del_TypeDocument             ON dbo.TypeDocument             INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'TypeDocument'; END;
GO
CREATE TRIGGER trg_no_del_Ref_Caracteristique      ON dbo.Ref_Caracteristique      INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'Ref_Caracteristique'; END;
GO
CREATE TRIGGER trg_no_del_Team ON dbo.Team INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'Team'; END;
GO
CREATE TRIGGER trg_no_del_AppRole ON dbo.AppRole INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'AppRole'; END;
GO
CREATE TRIGGER trg_no_del_ColumnValueType ON dbo.ColumnValueType INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'ColumnValueType'; END;
GO
CREATE TRIGGER trg_no_del_ExecutionDocumentType ON dbo.ExecutionDocumentType INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'ExecutionDocumentType'; END;
GO
CREATE TRIGGER trg_no_del_InspectionLevel ON dbo.InspectionLevel INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'InspectionLevel'; END;
GO
CREATE TRIGGER trg_no_del_ReferencePieceRole ON dbo.ReferencePieceRole INSTEAD OF DELETE AS BEGIN EXEC dbo.usp_RaiseDeleteError 'ReferencePieceRole'; END;
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
CREATE INDEX IX_Machine_Operation            ON dbo.Machine(OperationCode);

GO
CREATE TABLE dbo.Exec_Echantillonnage (
    Id UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID() NOT NULL,
    ExecControleDocumentStatutId UNIQUEIDENTIFIER NOT NULL,
    TailleLot INT NOT NULL,
    NbPostesB INT NOT NULL,
    LettreCode VARCHAR(10) NOT NULL,
    EffectifEchantillonA INT NOT NULL,
    EffectifParPosteAb INT NULL,
    CritereAcceptationAc INT NOT NULL,
    CritereRejetRe INT NOT NULL,
    CONSTRAINT PK_Exec_Echantillonnage PRIMARY KEY (Id),
    CONSTRAINT FK_Exec_Echantillonnage_ExecControleDocumentStatut FOREIGN KEY (ExecControleDocumentStatutId) REFERENCES dbo.Exec_ControleDocumentStatut (Id)
);
GO

CREATE TABLE dbo.Exec_Echantillonnage_Instrument (
    ExecEchantillonnageId UNIQUEIDENTIFIER NOT NULL,
    CodeInstrument VARCHAR(40) NOT NULL,
    CONSTRAINT PK_Exec_Echantillonnage_Instrument PRIMARY KEY (ExecEchantillonnageId, CodeInstrument),
    CONSTRAINT FK_ExecEchantillonnageInstrument_ExecEchantillonnage FOREIGN KEY (ExecEchantillonnageId) REFERENCES dbo.Exec_Echantillonnage (Id) ON DELETE CASCADE,
    CONSTRAINT FK_ExecEchantillonnageInstrument_Instrument FOREIGN KEY (CodeInstrument) REFERENCES dbo.Instrument (CodeInstrument)
);
GO

CREATE TABLE dbo.Exec_VerifMachine_Reponse (
    Id UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID() NOT NULL,
    ExecControleDocumentStatutId UNIQUEIDENTIFIER NOT NULL,
    DocumentVerifMachineEcheanceId UNIQUEIDENTIFIER NOT NULL,
    DateExecution DATETIME2(3) NOT NULL,
    MatriculeOperateur VARCHAR(20) NOT NULL REFERENCES dbo.AUTILIS(USR_0),
    PressionEntree DECIMAL(10,3) NULL,
    FuiteAffichee DECIMAL(10,3) NULL,
    Conforme BIT NULL,
    Observation VARCHAR(500) NULL,
    CONSTRAINT PK_ExecVerifMachineReponse PRIMARY KEY (Id),
    CONSTRAINT FK_ExecVerifMachineReponse_Statut FOREIGN KEY (ExecControleDocumentStatutId) REFERENCES dbo.Exec_ControleDocumentStatut (Id),
    CONSTRAINT FK_ExecVerifMachineReponse_Echeance FOREIGN KEY (DocumentVerifMachineEcheanceId) REFERENCES dbo.Document_VerifMachine_Echeance (Id)
);
GO

CREATE INDEX IX_ExecVerifMachineReponse_Statut ON dbo.Exec_VerifMachine_Reponse (ExecControleDocumentStatutId);
GO


-- Résultat Contrôle Poste
-- Contrôle au poste : un document par jour de production et par poste, commun aux équipes (chacune saisit
-- ses heures, définies dans le document). Les non-conformités par ligne et par heure sont dans
-- Exec_ControleLigne_Reponse (COMPTAGE). Ici, les seules saisies sans ligne : pièces réalisées par tranche
-- et pièces rebutées par équipe. Totaux, pièces testées / conformes et taux de NC sont calculés, jamais stockés.
CREATE TABLE dbo.Exec_RC_Poste_Reponse (
    Id UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID() NOT NULL,
    ExecControleDocumentStatutId UNIQUEIDENTIFIER NOT NULL,
    TrancheHoraire VARCHAR(20) NOT NULL,
    TotalRealiseHeure INT NOT NULL CHECK (TotalRealiseHeure >= 0),
    -- Équipe et opérateur qui ont saisi (la tranche appartient à l'équipe).
    Equipe VARCHAR(10) NULL REFERENCES dbo.Team(Code),
    MatriculeOperateur VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
    DateSaisie DATETIME2(3) NULL,
    CONSTRAINT PK_ExecRcPosteReponse PRIMARY KEY CLUSTERED (Id ASC),
    CONSTRAINT FK_ExecRcPosteReponse_Statut FOREIGN KEY (ExecControleDocumentStatutId) REFERENCES dbo.Exec_ControleDocumentStatut (Id),
    CONSTRAINT UQ_ExecRcPosteReponse_Tranche UNIQUE (ExecControleDocumentStatutId, TrancheHoraire)
);

CREATE TABLE dbo.Exec_RC_Poste_Bilan (
    Id UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID() NOT NULL PRIMARY KEY,
    ExecControleDocumentStatutId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleDocumentStatut (Id),
    -- Pièces rebutées par équipe : le total du document est leur somme.
    Equipe VARCHAR(10) NULL REFERENCES dbo.Team(Code),
    NbPiecesRebutees INT NOT NULL CHECK (NbPiecesRebutees >= 0),
    MatriculeOperateur VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
    DateSaisie DATETIME2(3) NULL,
    CONSTRAINT UQ_ExecRcPosteBilan_Equipe UNIQUE (ExecControleDocumentStatutId, Equipe)
);
GO

-- Registre de traçabilité de l'OF : une ligne par changement de lots (corps, volant, composants scannés par le
-- magasinier), avec l'équipe, le poste et l'opérateur. Une ligne erronée est annulée avec son motif, jamais
-- supprimée. Chaque équipe confirme ses lots avant sa fin de poste (Exec_ControleDocumentStatut par session).
CREATE TABLE dbo.ExecRegistreTracabilite (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    ExecControleOfId UNIQUEIDENTIFIER NOT NULL,
    DateHeure DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
    Version INT NOT NULL DEFAULT 1,
    Corps VARCHAR(100) NULL,
    Volant VARCHAR(100) NULL,
    SessionId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleOf_Session(Id),
    Equipe VARCHAR(10) NULL REFERENCES dbo.Team(Code),
    PosteCode VARCHAR(30) NULL REFERENCES dbo.PosteTravail(CodePoste),
    MatriculeOperateur VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
    CancelledAt DATETIME2(3) NULL,
    CancelledBy VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
    CancellationReason VARCHAR(250) NULL,
    CONSTRAINT PK_ExecRegistreTracabilite PRIMARY KEY (Id),
    CONSTRAINT FK_ExecRegistreTracabilite_ExecControleOf FOREIGN KEY (ExecControleOfId) REFERENCES dbo.Exec_ControleOf (Id),
    CONSTRAINT CK_ExecRegistreTracabilite_Cancellation CHECK (
        CancelledAt IS NULL OR (CancelledBy IS NOT NULL AND CancellationReason IS NOT NULL))
);
GO

CREATE TABLE dbo.ExecRegistreTracabiliteComposant (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    RegistreTracabiliteId UNIQUEIDENTIFIER NOT NULL,
    -- Article du composant (la désignation n'est qu'un libellé d'affichage).
    CodeArticle VARCHAR(30) NULL REFERENCES dbo.Article(CodeArticle),
    DesignationComposant VARCHAR(200) NOT NULL,
    LotSelectionne VARCHAR(100) NULL,
    CONSTRAINT PK_ExecRegistreTracabiliteComposant PRIMARY KEY (Id),
    CONSTRAINT FK_ExecRegistreTracabiliteComposant_ExecRegistreTracabilite FOREIGN KEY (RegistreTracabiliteId) REFERENCES dbo.ExecRegistreTracabilite (Id) ON DELETE CASCADE
);
GO

-- ================================================================================
-- PARTIE 16 : RÈGLES D'INTÉGRITÉ PAR TRIGGERS
--
-- Codes d'erreur : 50001 = conflit d'unicité métier (HTTP 409),
--                  50002 = suppression interdite (HTTP 409),
--                  50003 = donnée refusée par une règle métier (HTTP 400).
-- ================================================================================

-- Une valeur de colonne supplémentaire doit viser une colonne du formulaire de son document
-- et respecter le type de cette colonne (valeur vide toujours acceptée).
CREATE TRIGGER TR_DocumentLigneExtraColonne_Validate ON dbo.Document_Ligne_ExtraColonne
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @cle VARCHAR(60), @valeur NVARCHAR(500), @type VARCHAR(20), @defId UNIQUEIDENTIFIER, @message NVARCHAR(400);

    SELECT TOP 1 @cle = i.CleColonne
    FROM inserted i
    JOIN dbo.Document_Ligne l  ON l.Id = i.LigneId
    JOIN dbo.Document_Entete e ON e.Id = l.EnteteId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Ref_Formulaire_ColonneDef d
                      WHERE d.FormulaireId = e.FormulaireId AND d.CleColonne = i.CleColonne);
    IF @cle IS NOT NULL
    BEGIN
        SET @message = CONCAT(N'La colonne « ', @cle, N' » n''existe pas dans le formulaire de ce document.');
        THROW 50003, @message, 1;
    END;

    SELECT TOP 1 @cle = d.LabelAffiche, @valeur = i.ValeurColonne, @type = d.TypeValeur
    FROM inserted i
    JOIN dbo.Document_Ligne l  ON l.Id = i.LigneId
    JOIN dbo.Document_Entete e ON e.Id = l.EnteteId
    JOIN dbo.Ref_Formulaire_ColonneDef d ON d.FormulaireId = e.FormulaireId AND d.CleColonne = i.CleColonne
    WHERE NULLIF(LTRIM(RTRIM(i.ValeurColonne)), N'') IS NOT NULL
      AND (   (d.TypeValeur = 'NUMBER'  AND TRY_CAST(i.ValeurColonne AS DECIMAL(18,6)) IS NULL)
           OR (d.TypeValeur = 'DATE'    AND (LEN(i.ValeurColonne) <> 10 OR TRY_CONVERT(DATE, i.ValeurColonne, 23) IS NULL))
           OR (d.TypeValeur = 'BOOLEAN' AND i.ValeurColonne NOT IN (N'true', N'false'))
           OR (d.TypeValeur = 'LIST'    AND NOT EXISTS (SELECT 1 FROM dbo.ColumnAllowedValue v
                                                       WHERE v.ColumnDefinitionId = d.Id AND v.Value = i.ValeurColonne)));
    IF @type IS NOT NULL
    BEGIN
        SET @message = CONCAT(N'Valeur « ', @valeur, N' » invalide pour la colonne « ', @cle, N' » (',
            CASE @type WHEN 'NUMBER' THEN N'nombre attendu' WHEN 'DATE' THEN N'date AAAA-MM-JJ attendue'
                       WHEN 'BOOLEAN' THEN N'oui/non attendu' ELSE N'valeur hors de la liste proposée' END, N').');
        THROW 50003, @message, 1;
    END;
END;
GO

-- Le type d'une colonne ne change plus une fois des valeurs saisies (elles deviendraient invalides).
CREATE TRIGGER TR_RefFormulaireColonneDef_TypeLocked ON dbo.Ref_Formulaire_ColonneDef
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(TypeValeur) AND EXISTS (
        SELECT 1
        FROM inserted i
        JOIN deleted o ON o.Id = i.Id AND o.TypeValeur <> i.TypeValeur
        JOIN dbo.Document_Entete e ON e.FormulaireId = i.FormulaireId
        JOIN dbo.Document_Ligne l  ON l.EnteteId = e.Id
        JOIN dbo.Document_Ligne_ExtraColonne v ON v.LigneId = l.Id AND v.CleColonne = i.CleColonne
        WHERE NULLIF(LTRIM(RTRIM(v.ValeurColonne)), N'') IS NOT NULL)
        THROW 50003, N'Le type d''une colonne déjà renseignée ne peut plus être modifié : créez une nouvelle colonne.', 1;
END;
GO

-- Le document lié à une exécution doit être du type prévu pour ce type d'exécution
-- (ex. une vérification machine applique un plan VERIF_MACHINE, jamais un plan d'assemblage).
CREATE TRIGGER TR_ExecControleDocumentStatut_TypeDocument ON dbo.Exec_ControleDocumentStatut
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
               FROM inserted i
               JOIN dbo.ExecutionDocumentType t ON t.Code = i.TypeDocument
               LEFT JOIN dbo.Document_Entete d ON d.Id = i.DocId
               WHERE i.DocId IS NOT NULL
                 AND (t.DocumentTypeCode IS NULL OR d.TypeDocumentCode <> t.DocumentTypeCode))
        THROW 50003, N'Le document lié ne correspond pas au type de contrôle exécuté.', 1;
END;
GO

-- Documents à exemplaire actif unique : un plan de vérification ACTIF par machine,
-- un seul plan d'échantillonnage ACTIF.
CREATE TRIGGER TR_DocumentEntete_SingleActive ON dbo.Document_Entete
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
               FROM dbo.Document_Entete e
               JOIN (SELECT DISTINCT MachineCode FROM inserted WHERE TypeDocumentCode = 'VERIF_MACHINE') i
                 ON i.MachineCode = e.MachineCode
               WHERE e.TypeDocumentCode = 'VERIF_MACHINE' AND e.Statut = 'ACTIF'
               GROUP BY e.MachineCode HAVING COUNT(*) > 1)
        THROW 50001, 'Un plan de vérification est déjà actif pour cette machine.', 1;

    IF EXISTS (SELECT 1 FROM inserted WHERE TypeDocumentCode = 'PLAN_ECHANTILLONNAGE' AND Statut = 'ACTIF')
       AND (SELECT COUNT(*) FROM dbo.Document_Entete
            WHERE TypeDocumentCode = 'PLAN_ECHANTILLONNAGE' AND Statut = 'ACTIF') > 1
        THROW 50001, 'Un plan d''échantillonnage est déjà actif.', 1;
END;
GO

-- Une seule préparation magasin non annulée par OF.
CREATE TRIGGER TR_MagPreparationOF_SingleActive ON dbo.Mag_PreparationOF
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
               FROM dbo.Mag_PreparationOF p
               JOIN (SELECT DISTINCT NumeroOF FROM inserted) i ON i.NumeroOF = p.NumeroOF
               WHERE p.Statut <> 'ANNULE'
               GROUP BY p.NumeroOF HAVING COUNT(*) > 1)
        THROW 50001, 'Cet OF a déjà une préparation magasin en cours.', 1;
END;
GO

-- Un BL n'est expédié qu'une fois.
CREATE TRIGGER TR_MagExpeditionBL_Single ON dbo.Mag_ExpeditionBL
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
               FROM dbo.Mag_ExpeditionBL e
               JOIN (SELECT DISTINCT NumeroBL FROM inserted) i ON i.NumeroBL = e.NumeroBL
               GROUP BY e.NumeroBL HAVING COUNT(*) > 1)
        THROW 50001, 'Ce BL a déjà une expédition.', 1;
END;
GO

-- Une exécution d'OF est une preuve qualité : elle n'est jamais supprimée.
CREATE TRIGGER TR_ExecControleOF_NoDelete ON dbo.Exec_ControleOF
INSTEAD OF DELETE
AS
BEGIN
    THROW 50002, 'Une exécution d''OF ne peut pas être supprimée (ISO 9001).', 1;
END;
GO

-- Une clôture est définitive : un OF clôturé ne peut ni changer de statut ni être réaffecté.
CREATE TRIGGER TR_ExecControleOF_ClosedImmutable ON dbo.Exec_ControleOF
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN inserted i ON i.Id = d.Id
               WHERE d.Statut = 'CLOTURE'
                 AND (i.Statut <> d.Statut OR i.NumeroOF <> d.NumeroOF OR i.OperationCode <> d.OperationCode))
        THROW 50002, 'Cet OF est clôturé : il ne peut plus être modifié (ISO 9001).', 1;
END;
GO

-- Données d'exécution d'un OF clôturé : suppression interdite.
-- Les occurrences jamais répondues restent supprimables (nettoyage à la clôture).
CREATE TRIGGER TR_ExecControleOfPoste_ProtectClosed ON dbo.Exec_ControleOf_Poste
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleOF o ON o.Id = d.ExecControleOfId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecControleOfSession_ProtectClosed ON dbo.Exec_ControleOf_Session
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleOF o ON o.Id = d.ExecControleOfId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecControleDocumentStatut_ProtectClosed ON dbo.Exec_ControleDocumentStatut
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleOF o ON o.Id = d.ExecControleOfId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecPieceType_ProtectClosed ON dbo.Exec_PieceType
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleOF o ON o.Id = d.ExecControleOFId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecControleTranche_ProtectClosed ON dbo.Exec_ControleTranche
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleOF o ON o.Id = d.ExecControleOFId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecPrelevementIntermediaire_ProtectClosed ON dbo.Exec_Prelevement_Intermediaire
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleOF o ON o.Id = d.ExecControleOFId
               WHERE o.Statut = 'CLOTURE' AND d.EstRepondu = 1)
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecControleLigneReponse_ProtectClosed ON dbo.Exec_ControleLigne_Reponse
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleOF o ON o.Id = d.ExecControleOFId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecRegistreTracabilite_ProtectClosed ON dbo.ExecRegistreTracabilite
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleOF o ON o.Id = d.ExecControleOfId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecEchantillonnage_ProtectClosed ON dbo.Exec_Echantillonnage
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleDocumentStatut ds ON ds.Id = d.ExecControleDocumentStatutId
               JOIN dbo.Exec_ControleOF o ON o.Id = ds.ExecControleOfId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecVerifMachineReponse_ProtectClosed ON dbo.Exec_VerifMachine_Reponse
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleDocumentStatut ds ON ds.Id = d.ExecControleDocumentStatutId
               JOIN dbo.Exec_ControleOF o ON o.Id = ds.ExecControleOfId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecRCPosteReponse_ProtectClosed ON dbo.Exec_RC_Poste_Reponse
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleDocumentStatut ds ON ds.Id = d.ExecControleDocumentStatutId
               JOIN dbo.Exec_ControleOF o ON o.Id = ds.ExecControleOfId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO
CREATE TRIGGER TR_ExecRCPosteBilan_ProtectClosed ON dbo.Exec_RC_Poste_Bilan
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted d
               JOIN dbo.Exec_ControleDocumentStatut ds ON ds.Id = d.ExecControleDocumentStatutId
               JOIN dbo.Exec_ControleOF o ON o.Id = ds.ExecControleOfId
               WHERE o.Statut = 'CLOTURE')
        THROW 50002, 'Données d''un OF clôturé : suppression interdite (ISO 9001).', 1;
END;
GO

-- ================================================================================
-- MOINDRE PRIVILÈGE : rôle applicatif
-- L'API ne doit jamais se connecter avec 'sa'. Créer un login dédié (mot de passe
-- fourni par l'exploitation, jamais versionné) puis l'associer à ce rôle :
--   CREATE LOGIN sopaltrace_api WITH PASSWORD = '<secret>', CHECK_POLICY = ON;
--   CREATE USER sopaltrace_api FOR LOGIN sopaltrace_api;
--   ALTER ROLE sopaltrace_app ADD MEMBER sopaltrace_api;
-- ================================================================================
CREATE ROLE sopaltrace_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO sopaltrace_app;
GO

PRINT 'SopalTraceDB créée avec succès.';
GO

SET NOEXEC OFF;
GO
