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
--   - Documents générés (PDF) depuis les modèles Excel de Sopal : PARTIE 17
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

-- Rôle d'un formulaire : le type de document qu'il désigne et les critères qui forment son champ d'application. Un
-- nouveau type de document s'ajoute ici (avec son écran de saisie et, s'il est mis en forme, son modèle Excel).
-- La nature d'article et le type de robinet ne désignent un formulaire que sans famille (la famille les porte déjà).
-- Nature du type : DOCUMENT (document gérable : le superviseur qualité en définit le contenu, ses versions suivent les
-- documents) ou RESPONSE_SHEET (fiche de réponse : simple gabarit rempli par les réponses des opérateurs, aucun document
-- à gérer). Tous ont un code, une version et un modèle Excel dédié.
CREATE TABLE dbo.FormRole (
    Code               VARCHAR(50)   NOT NULL PRIMARY KEY,
    Label              NVARCHAR(100) NOT NULL,
    DisplayOrder       INT           NOT NULL DEFAULT 0,
    Nature             VARCHAR(20)   NOT NULL DEFAULT 'DOCUMENT'
        CONSTRAINT CK_FormRole_Nature CHECK (Nature IN ('DOCUMENT', 'RESPONSE_SHEET')),
    ScopeByFamily      BIT           NOT NULL DEFAULT 0,
    ScopeByWorkstation BIT           NOT NULL DEFAULT 0,
    ScopeByMachine     BIT           NOT NULL DEFAULT 0,
    ScopeByNature      BIT           NOT NULL DEFAULT 0,
    ScopeByValveType   BIT           NOT NULL DEFAULT 0,
    Active             BIT           NOT NULL DEFAULT 1
);
GO

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
    -- RESULTAT_CONTROLE_ASSEMBLAGE : résultats du contrôle en cours d'assemblage remplis par les opérateurs.
    Role                      VARCHAR(50) REFERENCES dbo.FormRole(Code),
    -- Champ d'application : avec le rôle, il désigne le formulaire d'un document, sans jamais déduire quoi que ce soit
    -- du code (le code, choisi par le superviseur qualité, est un simple libellé). Seuls les critères qui existent sont
    -- renseignés : famille et poste d'un plan d'assemblage (nature PISTON pour le piston, sans famille), famille d'un plan
    -- produit fini, poste et famille d'un contrôle au poste, machine d'une vérification machine, type de robinet ou
    -- poste et famille des résultats d'assemblage. Un document créé sur ce formulaire en reprend le poste et la famille.
    PosteCode                 VARCHAR(30) NULL REFERENCES dbo.PosteTravail(CodePoste),
    FamilleProduitFiniCode    VARCHAR(30) NULL REFERENCES dbo.FamilleProduitFini(Code),
    TypeRobinetCode           VARCHAR(10) NULL REFERENCES dbo.TypeRobinet(Code),
    MachineCode               VARCHAR(30) NULL REFERENCES dbo.Machine(CodeMachine),
    NatureArticleCode         VARCHAR(20) NULL REFERENCES dbo.NatureArticle(Code),
    -- Identifiant commun à toutes les lignes d'un même formulaire (ses versions et ses changements de code) : il relie
    -- FEMANU.0, FEMANU.1 et DEMANU.1 (modèle hérité, numéro de la prochaine version).
    VersionGroupId            UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RefFormulaire_VersionGroupId DEFAULT NEWID(),
    -- Ordre de création dans la séquence des documents (DF_RefFormulaire_CreationOrder) : situe DEMANU.1 après FEMANU.1.
    CreationOrder             BIGINT       NOT NULL,
    -- Une ligne = une identité figée (code + version) dès qu'un document ou une fiche l'utilise
    -- (TR_RefFormulaire_Identity) : changer de code crée une nouvelle ligne, même version.
    UNIQUE (CodeReference, Version)
);
GO

-- Journal des changements de code : la ligne remplacée (archivée, avec ses documents) et celle qui la remplace (même
-- version, nouveau code). Une ligne n'est remplacée qu'une fois. Jamais modifié ni supprimé.
CREATE TABLE dbo.FormCodeChange (
    Id             BIGINT IDENTITY(1,1) PRIMARY KEY,
    PreviousFormId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_Formulaire(Id),
    NewFormId      UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_Formulaire(Id),
    ChangedBy      VARCHAR(20)      NULL REFERENCES dbo.AUTILIS(USR_0),
    ChangedAt      DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT UX_FormCodeChange_PreviousForm UNIQUE (PreviousFormId),
    CONSTRAINT UX_FormCodeChange_NewForm UNIQUE (NewFormId),
    CONSTRAINT CK_FormCodeChange_DifferentForms CHECK (PreviousFormId <> NewFormId)
);
GO

-- Un seul formulaire en cours (brouillon ou actif) par rôle et champ d'application, et par code : un document trouve
-- toujours un formulaire unique, et un code ne désigne jamais deux formulaires différents.
CREATE UNIQUE INDEX UX_RefFormulaire_ChampApplication ON dbo.Ref_Formulaire
    (Role, FamilleProduitFiniCode, PosteCode, TypeRobinetCode, MachineCode, NatureArticleCode) WHERE Statut IN ('BROUILLON', 'ACTIF');
CREATE UNIQUE INDEX UX_RefFormulaire_CodeEnCours ON dbo.Ref_Formulaire (CodeReference) WHERE Statut IN ('BROUILLON', 'ACTIF');
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

-- Recopie la structure d'un formulaire (colonnes propres, valeurs proposées, équipes) sur une nouvelle ligne de
-- formulaire sans structure (changement de code, nouvelle version de même structure).
CREATE PROCEDURE dbo.CopyFormStructure
    @SourceFormId UNIQUEIDENTIFIER,
    @TargetFormId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF EXISTS (SELECT 1 FROM dbo.Ref_Formulaire_ColonneDef WHERE FormulaireId = @TargetFormId)
       OR EXISTS (SELECT 1 FROM dbo.Ref_Formulaire_Equipe WHERE FormulaireId = @TargetFormId)
        THROW 50004, N'Le formulaire cible a déjà une structure.', 1;

    INSERT INTO dbo.Ref_Formulaire_ColonneDef (FormulaireId, CleColonne, LabelAffiche, TypeValeur, InsertAfter, TargetTable, Actif)
    SELECT @TargetFormId, CleColonne, LabelAffiche, TypeValeur, InsertAfter, TargetTable, Actif
    FROM dbo.Ref_Formulaire_ColonneDef WHERE FormulaireId = @SourceFormId;

    INSERT INTO dbo.ColumnAllowedValue (ColumnDefinitionId, Value, DisplayOrder, Active)
    SELECT cible.Id, v.Value, v.DisplayOrder, v.Active
    FROM dbo.ColumnAllowedValue v
    JOIN dbo.Ref_Formulaire_ColonneDef source ON source.Id = v.ColumnDefinitionId AND source.FormulaireId = @SourceFormId
    JOIN dbo.Ref_Formulaire_ColonneDef cible ON cible.FormulaireId = @TargetFormId AND cible.CleColonne = source.CleColonne;

    INSERT INTO dbo.Ref_Formulaire_Equipe (FormulaireId, TeamCode, NomEquipe, HeureDebut, HeureFin, OrdreAffiche, Actif)
    SELECT @TargetFormId, TeamCode, NomEquipe, HeureDebut, HeureFin, OrdreAffiche, Actif
    FROM dbo.Ref_Formulaire_Equipe WHERE FormulaireId = @SourceFormId;
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

-- Ordre de création : une seule séquence de la base numérote, dans l'ordre réel de leur enregistrement, les documents
-- qualité, les documents d'exécution et les affectations de modèles de rapport. « Le modèle en vigueur à la création
-- d'un document » se lit sur cet ordre, sans comparer d'horloges (l'API et la base peuvent ne pas être à la même heure).
CREATE SEQUENCE dbo.CreationOrderSequence AS BIGINT START WITH 1 INCREMENT BY 1;
GO

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
    -- Attribué par la base à l'enregistrement (DF_DocumentEntete_CreationOrder), jamais modifié (TR_DocumentEntete_CreationOrder).
    CreationOrder          BIGINT       NOT NULL,
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
CREATE UNIQUE INDEX UX_Document_Entete_CreationOrder ON dbo.Document_Entete(CreationOrder);
-- Ordre de création des formulaires, dans la même séquence que les documents (déclaré ici : la séquence est créée avec
-- la table des documents).
ALTER TABLE dbo.Ref_Formulaire ADD CONSTRAINT DF_RefFormulaire_CreationOrder DEFAULT (NEXT VALUE FOR dbo.CreationOrderSequence) FOR CreationOrder;
CREATE INDEX IX_RefFormulaire_VersionGroup ON dbo.Ref_Formulaire (VersionGroupId, CreationOrder);
-- Un document générique actif par formulaire (code + version) ; les modèles de fabrication partagent le formulaire de
-- leur rôle, les documents spécifiques celui de leur document générique.
CREATE UNIQUE INDEX UX_DocumentEntete_GenericActiveForm ON dbo.Document_Entete (FormulaireId)
    WHERE Statut = 'ACTIF' AND Portee = 'GENERIQUE' AND TypeDocumentCode <> 'MODELE_FAB' AND FormulaireId IS NOT NULL;
-- Hors du CREATE TABLE : SQL Server refuse NEXT VALUE FOR après une contrainte CHECK de colonne dans la même instruction.
ALTER TABLE dbo.Document_Entete ADD CONSTRAINT DF_DocumentEntete_CreationOrder DEFAULT (NEXT VALUE FOR dbo.CreationOrderSequence) FOR CreationOrder;
GO

CREATE TRIGGER TR_FormCodeChange_Immutable ON dbo.FormCodeChange
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 50002, N'L''historique des codes de formulaire ne peut être ni modifié ni supprimé (ISO 9001).', 1;
END;
GO

-- Identité d'un formulaire : une fois utilisée par un document ou une fiche, la ligne garde son code, sa version, son
-- rôle, son champ d'application et sa lignée. Un formulaire n'est archivé qu'après ses documents génériques actifs.
CREATE TRIGGER TR_RefFormulaire_Identity ON dbo.Ref_Formulaire
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE (i.CodeReference <> d.CodeReference OR i.Version <> d.Version OR i.VersionGroupId <> d.VersionGroupId
                      OR i.CreationOrder <> d.CreationOrder
                      OR ISNULL(i.Role, '') <> ISNULL(d.Role, '')
                      OR ISNULL(i.PosteCode, '') <> ISNULL(d.PosteCode, '')
                      OR ISNULL(i.FamilleProduitFiniCode, '') <> ISNULL(d.FamilleProduitFiniCode, '')
                      OR ISNULL(i.TypeRobinetCode, '') <> ISNULL(d.TypeRobinetCode, '')
                      OR ISNULL(i.MachineCode, '') <> ISNULL(d.MachineCode, '')
                      OR ISNULL(i.NatureArticleCode, '') <> ISNULL(d.NatureArticleCode, ''))
                 AND (EXISTS (SELECT 1 FROM dbo.Document_Entete x WHERE x.FormulaireId = i.Id)
                      OR EXISTS (SELECT 1 FROM dbo.Exec_ControleDocumentStatut s WHERE s.FormId = i.Id)))
        THROW 50003, N'Ce formulaire est déjà utilisé : son code, sa version et son champ d''application ne changent plus. Changer de code crée un nouveau formulaire.', 1;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.Document_Entete x ON x.FormulaireId = i.Id
               WHERE i.Statut = 'ARCHIVE' AND x.Statut = 'ACTIF' AND x.Portee = 'GENERIQUE')
        THROW 50003, N'Un formulaire n''est archivé qu''après ses documents génériques actifs.', 1;
END;
GO

-- Champ d'application d'un formulaire : seulement les critères de son rôle ; nature et type de robinet seulement sans
-- famille ; avec une famille, le type de robinet est toujours celui de la famille (jamais saisi à part).
CREATE TRIGGER TR_RefFormulaire_Scope ON dbo.Ref_Formulaire
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.FamilleProduitFini f ON f.Code = i.FamilleProduitFiniCode
               WHERE ISNULL(i.TypeRobinetCode, '') <> f.TypeRobinetCode)
        THROW 50003, N'Le type de robinet d''un formulaire avec famille est celui de sa famille.', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.FormRole r ON r.Code = i.Role
               WHERE (i.FamilleProduitFiniCode IS NOT NULL AND r.ScopeByFamily = 0)
                  OR (i.PosteCode IS NOT NULL AND r.ScopeByWorkstation = 0)
                  OR (i.MachineCode IS NOT NULL AND r.ScopeByMachine = 0)
                  OR (i.NatureArticleCode IS NOT NULL AND (r.ScopeByNature = 0 OR i.FamilleProduitFiniCode IS NOT NULL))
                  OR (i.TypeRobinetCode IS NOT NULL AND i.FamilleProduitFiniCode IS NULL AND r.ScopeByValveType = 0))
        THROW 50003, N'Ce champ d''application utilise un critère que le rôle du formulaire ne prévoit pas.', 1;
END;
GO

-- Un document générique actif s'appuie toujours sur un formulaire en cours (brouillon ou actif).
CREATE TRIGGER TR_DocumentEntete_FormInProgress ON dbo.Document_Entete
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.Ref_Formulaire f ON f.Id = i.FormulaireId
               WHERE i.Statut = 'ACTIF' AND i.Portee = 'GENERIQUE' AND f.Statut = 'ARCHIVE')
        THROW 50003, N'Un document générique actif s''appuie sur un formulaire en cours, jamais sur un formulaire archivé.', 1;
END;
GO

CREATE TRIGGER TR_DocumentEntete_CreationOrder ON dbo.Document_Entete
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(CreationOrder) AND EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id WHERE i.CreationOrder <> d.CreationOrder)
        THROW 50003, N'L''ordre de création d''un document ne change jamais.', 1;
END;
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

-- Recrée un document à l'identique sur une autre ligne de formulaire (changement de code, nouveau logo) : en-tête
-- (même nom, même version que le formulaire cible), sections, lignes, valeurs des colonnes propres, croquis, paramètres
-- d'échantillonnage, familles, échéances et matrice de vérification machine. Le document source n'est pas modifié (le
-- service l'archive avant). À compléter si une table de structure de document est ajoutée.
CREATE PROCEDURE dbo.CopyDocumentToForm
    @SourceDocumentId UNIQUEIDENTIFIER,
    @FormId           UNIQUEIDENTIFIER,
    @Status           VARCHAR(20),
    @CreatedBy        VARCHAR(20),
    @VersionComment   NVARCHAR(MAX),
    @NewDocumentId    UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Document_Entete WHERE Id = @SourceDocumentId)
        THROW 50004, N'Document à recréer introuvable.', 1;

    SET @NewDocumentId = NEWID();
    INSERT INTO dbo.Document_Entete (Id, TypeDocumentCode, Portee, CodeArticleSageVersionne, DocumentSourceId, Nom, Designation,
        Version, Statut, OperationCode, FormulaireId, LegendeMoyens, Remarques, CommentaireVersion, CreePar,
        NatureArticleCode, FamilleProduitFiniCode, PosteCode, MachineCode)
    SELECT @NewDocumentId, d.TypeDocumentCode, d.Portee, d.CodeArticleSageVersionne, d.DocumentSourceId, d.Nom, d.Designation,
        f.Version, @Status, d.OperationCode, f.Id, d.LegendeMoyens, d.Remarques, @VersionComment, @CreatedBy,
        d.NatureArticleCode, d.FamilleProduitFiniCode, d.PosteCode, d.MachineCode
    FROM dbo.Document_Entete d CROSS JOIN dbo.Ref_Formulaire f
    WHERE d.Id = @SourceDocumentId AND f.Id = @FormId;

    DECLARE @Sections TABLE (OldId UNIQUEIDENTIFIER PRIMARY KEY, NewId UNIQUEIDENTIFIER NOT NULL);
    INSERT INTO @Sections SELECT Id, NEWID() FROM dbo.Document_Section WHERE EnteteId = @SourceDocumentId;
    INSERT INTO dbo.Document_Section (Id, EnteteId, OrdreAffiche, LibelleSection, TypeSectionId, PeriodiciteId, Notes, NqaId)
    SELECT m.NewId, @NewDocumentId, s.OrdreAffiche, s.LibelleSection, s.TypeSectionId, s.PeriodiciteId, s.Notes, s.NqaId
    FROM dbo.Document_Section s JOIN @Sections m ON m.OldId = s.Id;

    DECLARE @Lines TABLE (OldId UNIQUEIDENTIFIER PRIMARY KEY, NewId UNIQUEIDENTIFIER NOT NULL);
    INSERT INTO @Lines SELECT Id, NEWID() FROM dbo.Document_Ligne WHERE EnteteId = @SourceDocumentId;
    INSERT INTO dbo.Document_Ligne (Id, EnteteId, SectionId, OrdreAffiche, CaracteristiqueId, LibelleAffiche, TypeCaracteristiqueId,
        TypeControleId, MoyenControleId, MoyenTexteLibre, InstrumentCode, PeriodiciteId, LimiteSpecTexte, EstCritique,
        Instruction, Observations, MachineCode, RisqueDefautId, TypeLigne)
    SELECT m.NewId, @NewDocumentId, ms.NewId, l.OrdreAffiche, l.CaracteristiqueId, l.LibelleAffiche, l.TypeCaracteristiqueId,
        l.TypeControleId, l.MoyenControleId, l.MoyenTexteLibre, l.InstrumentCode, l.PeriodiciteId, l.LimiteSpecTexte, l.EstCritique,
        l.Instruction, l.Observations, l.MachineCode, l.RisqueDefautId, l.TypeLigne
    FROM dbo.Document_Ligne l JOIN @Lines m ON m.OldId = l.Id LEFT JOIN @Sections ms ON ms.OldId = l.SectionId;

    INSERT INTO dbo.Document_Ligne_ExtraColonne (LigneId, CleColonne, ValeurColonne, OrdreAffiche)
    SELECT m.NewId, v.CleColonne, v.ValeurColonne, v.OrdreAffiche
    FROM dbo.Document_Ligne_ExtraColonne v JOIN @Lines m ON m.OldId = v.LigneId;

    INSERT INTO dbo.DocumentLineImage (LineId, ContentType, Content)
    SELECT m.NewId, i.ContentType, i.Content FROM dbo.DocumentLineImage i JOIN @Lines m ON m.OldId = i.LineId;

    INSERT INTO dbo.SamplingPlan (DocumentId, InspectionLevelCode, SamplingType, InspectionSeverity, AcceptableQualityLevelId,
        AcceptanceNumber, RejectionNumber)
    SELECT @NewDocumentId, InspectionLevelCode, SamplingType, InspectionSeverity, AcceptableQualityLevelId, AcceptanceNumber, RejectionNumber
    FROM dbo.SamplingPlan WHERE DocumentId = @SourceDocumentId;

    DECLARE @Families TABLE (OldId UNIQUEIDENTIFIER PRIMARY KEY, NewId UNIQUEIDENTIFIER NOT NULL);
    INSERT INTO @Families SELECT Id, NEWID() FROM dbo.Document_VerifMachine_Famille WHERE EnteteId = @SourceDocumentId;
    INSERT INTO dbo.Document_VerifMachine_Famille (Id, EnteteId, OrdreAffiche, RefFamilleCorpsId)
    SELECT m.NewId, @NewDocumentId, f.OrdreAffiche, f.RefFamilleCorpsId
    FROM dbo.Document_VerifMachine_Famille f JOIN @Families m ON m.OldId = f.Id;

    DECLARE @Deadlines TABLE (OldId UNIQUEIDENTIFIER PRIMARY KEY, NewId UNIQUEIDENTIFIER NOT NULL);
    INSERT INTO @Deadlines SELECT e.Id, NEWID() FROM dbo.Document_VerifMachine_Echeance e JOIN @Lines m ON m.OldId = e.LigneId;
    INSERT INTO dbo.Document_VerifMachine_Echeance (Id, LigneId, PeriodiciteMachineId, RefMoyenDetectionId, OrdreAffiche)
    SELECT me.NewId, ml.NewId, e.PeriodiciteMachineId, e.RefMoyenDetectionId, e.OrdreAffiche
    FROM dbo.Document_VerifMachine_Echeance e JOIN @Deadlines me ON me.OldId = e.Id JOIN @Lines ml ON ml.OldId = e.LigneId;

    INSERT INTO dbo.Document_VerifMachine_MatricePiece (EcheanceId, FamilleId, RoleVerif, PieceRefId)
    SELECT me.NewId, mf.NewId, p.RoleVerif, p.PieceRefId
    FROM dbo.Document_VerifMachine_MatricePiece p JOIN @Deadlines me ON me.OldId = p.EcheanceId
    LEFT JOIN @Families mf ON mf.OldId = p.FamilleId;
END;
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
    -- Clôture de l'OF demandée par l'équipe (assemblage) ; NULL sinon. La fin de poste se demande poste par
    -- poste (Exec_SessionPost.ClosingRequestedAt). Tenue par le serveur : chaque tablette affiche la même étape.
    ClosingRequest VARCHAR(10) NULL CONSTRAINT CK_ExecControleOfSession_ClosingRequest CHECK (ClosingRequest = 'FIN_OF'),
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

-- Plusieurs équipes peuvent travailler en même temps sur l'OF, chacune sur ses postes (une seule équipe à la
-- fois par poste : UX_ExecSessionPost_OneTeamPerPoste). Une équipe a au plus une session ouverte (en production
-- ou en pause) par OF ; elle peut en ouvrir autant qu'elle veut, l'une après l'autre. (DateFin est nulle pour
-- toute session ouverte — CK_ExecControleOfSession_DateFin — et figure dans la clé pour que la relation
-- OF → sessions reste « plusieurs » côté EF.)
CREATE UNIQUE INDEX UX_ExecControleOfSession_OneOpenSessionPerTeam
    ON dbo.Exec_ControleOf_Session (ExecControleOfId, Equipe, DateFin) WHERE Statut IN ('EN_COURS', 'EN_PAUSE');
GO

-- Poste tenu par une équipe dans sa session (choisi à la prise de poste ou « Démarrer ce poste ») : il suit son
-- propre parcours (échantillonnage, démarrage, production, fin de poste), indépendamment des autres postes de
-- l'OF. Le premier démarrage d'un poste sur l'OF porte aussi le contrôle « début d'OF » (LOT_OF) de ce poste.
-- La fin de poste se demande (ClosingRequestedAt) puis se termine (EndedAt) poste par poste ; tant qu'un poste
-- n'est pas terminé, aucune autre équipe ne peut le prendre.
CREATE TABLE dbo.Exec_SessionPost (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    SessionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOf_Session(Id),
    -- OF de la session (cohérence : TR_ExecSessionPost_Consistency), pour l'unicité par poste de l'OF.
    ExecControleOfId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Exec_ControleOF(Id),
    PosteCode VARCHAR(30) NOT NULL REFERENCES dbo.PosteTravail(CodePoste),
    StartedAt DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
    StartedBy VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
    -- Fin de poste demandée par l'équipe sur ce poste : ses contrôles de fin deviennent dus.
    ClosingRequestedAt DATETIME2(3) NULL,
    -- Poste terminé par l'équipe (contrôles de fin faits) : une autre équipe peut le prendre.
    EndedAt DATETIME2(3) NULL,
    EndedBy VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
    CONSTRAINT UQ_ExecSessionPost UNIQUE (SessionId, PosteCode),
    CONSTRAINT CK_ExecSessionPost_Dates CHECK (
        (ClosingRequestedAt IS NULL OR ClosingRequestedAt >= StartedAt) AND (EndedAt IS NULL OR EndedAt >= StartedAt)),
    CONSTRAINT CK_ExecSessionPost_EndedBy CHECK (EndedBy IS NULL OR EndedAt IS NOT NULL)
);
GO

-- Une seule équipe à la fois sur un poste de l'OF : la suivante le prend après que la précédente l'a terminé.
CREATE UNIQUE INDEX UX_ExecSessionPost_OneTeamPerPoste
    ON dbo.Exec_SessionPost (ExecControleOfId, PosteCode) WHERE EndedAt IS NULL;
GO

-- Un poste appartient à l'OF de sa session, et une session terminée n'a plus de poste en cours.
CREATE TRIGGER TR_ExecSessionPost_Consistency ON dbo.Exec_SessionPost
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Exec_ControleOf_Session s ON s.Id = i.SessionId
               WHERE s.ExecControleOfId <> i.ExecControleOfId)
        THROW 50111, 'Le poste n''appartient pas à l''OF de la session.', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Exec_ControleOf_Session s ON s.Id = i.SessionId
               WHERE i.EndedAt IS NULL AND s.Statut = 'TERMINE')
        THROW 50112, 'Session d''équipe terminée : aucun poste ne peut y rester en cours.', 1;
END;
GO

-- Session terminée : tous ses postes le sont. Clôture de l'OF demandée : l'équipe est seule sur l'OF (les autres
-- ont terminé leurs postes) et aucune autre ne commence avant la clôture.
CREATE TRIGGER TR_ExecControleOfSession_Consistency ON dbo.Exec_ControleOf_Session
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.Exec_SessionPost p ON p.SessionId = i.Id
               WHERE i.Statut = 'TERMINE' AND p.EndedAt IS NULL)
        THROW 50112, 'Session d''équipe terminée : aucun poste ne peut y rester en cours.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Exec_ControleOf_Session s
               JOIN (SELECT DISTINCT ExecControleOfId FROM inserted) i ON i.ExecControleOfId = s.ExecControleOfId
               WHERE s.Statut IN ('EN_COURS', 'EN_PAUSE')
               GROUP BY s.ExecControleOfId
               HAVING COUNT(*) > 1 AND MAX(CASE WHEN s.ClosingRequest = 'FIN_OF' THEN 1 ELSE 0 END) = 1)
        THROW 50110, 'Clôture de l''OF en cours : une seule équipe peut être ouverte sur l''OF.', 1;
END;
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
    -- Formulaire (code + version) d'une fiche de réponse, fixé à sa création (TR_ExecControleDocumentStatut_Form) :
    -- résultats du contrôle en cours d'assemblage ou de fabrication, registre de traçabilité. Les autres documents
    -- d'exécution ont le formulaire du document appliqué (DocId).
    FormId UNIQUEIDENTIFIER NULL REFERENCES dbo.Ref_Formulaire(Id),
    -- Attribué par la base à l'enregistrement, jamais modifié : fixe le modèle de rapport en vigueur pour ce document.
    CreationOrder BIGINT NOT NULL,
    RowVersion ROWVERSION NOT NULL
);
GO

CREATE UNIQUE INDEX UX_ExecControleDocumentStatut_CreationOrder ON dbo.Exec_ControleDocumentStatut(CreationOrder);
ALTER TABLE dbo.Exec_ControleDocumentStatut ADD CONSTRAINT DF_ExecControleDocumentStatut_CreationOrder
    DEFAULT (NEXT VALUE FOR dbo.CreationOrderSequence) FOR CreationOrder;
GO

-- Formulaire d'une fiche de réponse : seulement sur une fiche de son type (résultats d'assemblage, résultats en cours de
-- fabrication, registre de traçabilité), et jamais changé une fois fixé (le modèle du document en dépend).
CREATE TRIGGER TR_ExecControleDocumentStatut_Form ON dbo.Exec_ControleDocumentStatut
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i LEFT JOIN dbo.Ref_Formulaire f ON f.Id = i.FormId
               WHERE i.FormId IS NOT NULL
                 AND NOT ((i.TypeDocument = 'PLAN_ASS' AND f.Role = 'RESULTAT_CONTROLE_ASSEMBLAGE')
                       OR (i.TypeDocument = 'RESULTAT_CONTROLE_CF' AND f.Role = 'RESULTAT_CONTROLE_CF')
                       OR (i.TypeDocument = 'REGISTRE_TRACA' AND f.Role = 'REGISTRE_TRACABILITE')))
        THROW 50003, N'Une fiche ne reçoit qu''un formulaire de son type (résultats d''assemblage, résultats en cours de fabrication, registre).', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE d.FormId IS NOT NULL AND (i.FormId IS NULL OR i.FormId <> d.FormId))
        THROW 50003, N'Le formulaire d''un document d''exécution ne change jamais.', 1;
END;
GO

CREATE TRIGGER TR_ExecControleDocumentStatut_CreationOrder ON dbo.Exec_ControleDocumentStatut
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(CreationOrder) AND EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id WHERE i.CreationOrder <> d.CreationOrder)
        THROW 50003, N'L''ordre de création d''un document d''exécution ne change jamais.', 1;
END;
GO

CREATE INDEX IX_ExecControleDocumentStatut_ExecSession
    ON dbo.Exec_ControleDocumentStatut (ExecControleOfId, SessionId) INCLUDE (TypeDocument, PosteCode, MachineCode);
-- Une fiche d'échantillonnage par poste de l'OF (chaque poste vit comme si l'OF n'en avait qu'un).
CREATE UNIQUE INDEX UX_ExecControleDocumentStatut_SamplingPerPoste
    ON dbo.Exec_ControleDocumentStatut (ExecControleOfId, PosteCode) WHERE TypeDocument = 'ECHANTILLONNAGE' AND PosteCode IS NOT NULL;
-- Confirmation des lots d'une équipe : toujours sur le registre d'un poste.
ALTER TABLE dbo.Exec_ControleDocumentStatut ADD CONSTRAINT CK_ExecControleDocumentStatut_RegistrePoste
    CHECK (TypeDocument <> 'REGISTRE_TRACA' OR PosteCode IS NOT NULL);
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

-- Registre de traçabilité : un par poste de l'OF (PosteCode). Une ligne par changement de lots sur le poste (corps,
-- volant, composants scannés par le magasinier), avec l'équipe et l'opérateur ; la version est propre au poste.
-- Une ligne erronée est annulée avec son motif, jamais supprimée. Chaque équipe confirme ses lots du poste avant
-- sa fin de poste (Exec_ControleDocumentStatut par session et par poste).
CREATE TABLE dbo.ExecRegistreTracabilite (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    ExecControleOfId UNIQUEIDENTIFIER NOT NULL,
    DateHeure DATETIME2(3) NOT NULL DEFAULT SYSDATETIME(),
    Version INT NOT NULL DEFAULT 1,
    Corps VARCHAR(100) NULL,
    Volant VARCHAR(100) NULL,
    SessionId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleOf_Session(Id),
    Equipe VARCHAR(10) NULL REFERENCES dbo.Team(Code),
    -- Poste dont c'est le registre : un poste affecté à l'OF (FK_ExecRegistreTracabilite_OfPoste).
    PosteCode VARCHAR(30) NOT NULL,
    MatriculeOperateur VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
    CancelledAt DATETIME2(3) NULL,
    CancelledBy VARCHAR(20) NULL REFERENCES dbo.AUTILIS(USR_0),
    CancellationReason VARCHAR(250) NULL,
    CONSTRAINT PK_ExecRegistreTracabilite PRIMARY KEY (Id),
    CONSTRAINT FK_ExecRegistreTracabilite_ExecControleOf FOREIGN KEY (ExecControleOfId) REFERENCES dbo.Exec_ControleOf (Id),
    CONSTRAINT FK_ExecRegistreTracabilite_OfPoste FOREIGN KEY (ExecControleOfId, PosteCode)
        REFERENCES dbo.Exec_ControleOf_Poste (ExecControleOfId, PosteCode),
    CONSTRAINT CK_ExecRegistreTracabilite_Cancellation CHECK (
        CancelledAt IS NULL OR (CancelledBy IS NOT NULL AND CancellationReason IS NOT NULL))
);
GO

CREATE INDEX IX_ExecRegistreTracabilite_Poste ON dbo.ExecRegistreTracabilite (ExecControleOfId, PosteCode, DateHeure);
GO

-- Ligne saisie par une équipe : sa session appartient à l'OF et a tenu ce poste (registre du poste).
CREATE TRIGGER TR_ExecRegistreTracabilite_Consistency ON dbo.ExecRegistreTracabilite
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i
               WHERE i.SessionId IS NOT NULL
                 AND NOT EXISTS (SELECT 1 FROM dbo.Exec_SessionPost p
                                 WHERE p.SessionId = i.SessionId AND p.ExecControleOfId = i.ExecControleOfId AND p.PosteCode = i.PosteCode))
        THROW 50114, 'Registre de traçabilité : l''équipe n''a pas tenu ce poste de l''OF.', 1;
END;
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
-- PARTIE 17 : MODÈLES DE RAPPORT ET DOCUMENTS GÉNÉRÉS
--
-- Un modèle de rapport (ReportTemplate) met en forme un type de document qualité (PLAN_ASS…) ou un type de document
-- d'exécution rempli par les opérateurs (fiche d'échantillonnage, contrôle au poste…) à partir d'un fichier Excel de
-- Sopal contenant des champs nommés ({{Date}}, {{Section.Libelle}}, {{Ligne.Caracteristique}}…). Une variante
-- (ex. machine BEE46) peut avoir son propre modèle : il est rattaché au formulaire du document (FE-VM-BEE46).
-- Chaque fichier envoyé est une version (ReportTemplateVersion) : brouillon (vérifié, aperçu possible), puis publiée
-- par une affectation. Le contenu d'une version ne change jamais.
-- Chaque formulaire (Ref_Formulaire) reçoit son modèle dédié : une version de modèle appartient à un seul formulaire
-- (FormGroupId = VersionGroupId de ses lignes : ses versions et ses changements de code), porte un nom propre à ce
-- formulaire et est numérotée dans ce formulaire. Un même fichier Excel est rattaché à nouveau pour chaque formulaire.
-- ReportTemplate est le type de modèle : les champs que le fichier peut utiliser. Les affectations (ReportTemplateAssignment)
-- forment un historique jamais modifié :
--   DEFAULT        : modèle des documents sans formulaire (type de modèle sans FormRole seulement) ;
--   FORM_STRUCTURE : modèle d'une version de formulaire ; il vaut aussi pour ses documents créés avant lui ;
--   FORM_REVISION  : révision d'un modèle pour un formulaire (logo…) : seulement pour les documents créés après.
-- Une nouvelle version de formulaire aux mêmes colonnes garde le modèle de la version précédente ; une version dont les
-- colonnes changent n'en hérite pas : son modèle doit être rattaché (PDF refusé d'ici là).
-- Le modèle d'un document se déduit de son ordre de création (CreationOrder) et de cet historique : un document garde
-- le modèle en vigueur à sa création (règles : ResolutionModeleRapport côté application).
-- Les modèles livrés sont rattachés à l'import aux formulaires listés dans ReportTemplateInitialForm.
-- Un document généré (GeneratedDocument) est produit à la demande (consultation, impression) puis conservé : PDF et
-- Excel rempli, version du modèle utilisée et empreinte des données du document ; il n'est jamais modifié ni
-- supprimé (ISO 9001).
-- Les empreintes SHA-256 sont calculées par la base (colonnes calculées) : elles ne peuvent pas mentir.
-- ================================================================================

CREATE TABLE dbo.ReportTemplate (
    Code                      VARCHAR(30)   PRIMARY KEY,
    Name                      NVARCHAR(150) NOT NULL,
    -- Type mis en forme : document qualité OU document d'exécution.
    DocumentTypeCode          VARCHAR(30)   NULL REFERENCES dbo.TypeDocument(Code),
    ExecutionDocumentTypeCode VARCHAR(50)   NULL REFERENCES dbo.ExecutionDocumentType(Code),
    -- Rôle des formulaires dont ce modèle met en forme les documents ; NULL : documents sans formulaire (registre).
    FormRole                  VARCHAR(50)   NULL REFERENCES dbo.FormRole(Code),
    CONSTRAINT CK_ReportTemplate_Source CHECK (
        (DocumentTypeCode IS NOT NULL AND ExecutionDocumentTypeCode IS NULL) OR (DocumentTypeCode IS NULL AND ExecutionDocumentTypeCode IS NOT NULL))
);
GO

-- Formulaires qui reçoivent la version livrée d'un modèle à son import (rattachements de départ) : un formulaire n'est
-- listé qu'une fois. Les rattachements suivants se font depuis l'écran des modèles.
CREATE TABLE dbo.ReportTemplateInitialForm (
    FormCodeReference  VARCHAR(30) PRIMARY KEY,
    ReportTemplateCode VARCHAR(30) NOT NULL REFERENCES dbo.ReportTemplate(Code)
);
GO

CREATE TABLE dbo.ReportTemplateVersion (
    Id                 UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ReportTemplateCode VARCHAR(30)    NOT NULL REFERENCES dbo.ReportTemplate(Code),
    -- Formulaire propriétaire (VersionGroupId de ses lignes) ; NULL : modèle par défaut des documents sans formulaire.
    FormGroupId        UNIQUEIDENTIFIER NULL,
    -- Nom du modèle, propre à son formulaire (ex. « Plan de contrôle d'assemblage (FE0594) — FE-ASS-MANU »).
    Name               NVARCHAR(200)  NOT NULL,
    -- Numéro dans son formulaire (dans son type de modèle pour un modèle par défaut).
    VersionNumber      INT            NOT NULL CHECK (VersionNumber > 0),
    FileName           NVARCHAR(200)  NOT NULL,
    Content            VARBINARY(MAX) NOT NULL,
    ContentSha256      AS CONVERT(CHAR(64), HASHBYTES('SHA2_256', Content), 2) PERSISTED,
    Status             VARCHAR(20)    NOT NULL DEFAULT 'DRAFT'
        CONSTRAINT CK_ReportTemplateVersion_Status CHECK (Status IN ('DRAFT', 'PUBLISHED')),
    -- Avertissements de la vérification du fichier (JSON), acceptés à la publication.
    ValidationWarnings NVARCHAR(MAX)  NULL,
    Comment            NVARCHAR(500)  NULL,
    -- NULL : modèle livré avec l'application.
    UploadedBy         VARCHAR(20)    NULL REFERENCES dbo.AUTILIS(USR_0),
    UploadedAt         DATETIME2(3)   NOT NULL DEFAULT SYSDATETIME(),
    PublishedBy        VARCHAR(20)    NULL REFERENCES dbo.AUTILIS(USR_0),
    PublishedAt        DATETIME2(3)   NULL,
    CONSTRAINT CK_ReportTemplateVersion_Published CHECK (
        (Status = 'DRAFT' AND PublishedAt IS NULL) OR (Status = 'PUBLISHED' AND PublishedAt IS NOT NULL))
);
GO

CREATE UNIQUE INDEX UX_ReportTemplateVersion_FormNumber ON dbo.ReportTemplateVersion(FormGroupId, VersionNumber) WHERE FormGroupId IS NOT NULL;
CREATE UNIQUE INDEX UX_ReportTemplateVersion_DefaultNumber ON dbo.ReportTemplateVersion(ReportTemplateCode, VersionNumber) WHERE FormGroupId IS NULL;
GO

CREATE TABLE dbo.ReportTemplateAssignment (
    Id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
    ReportTemplateVersionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.ReportTemplateVersion(Id),
    Kind                    VARCHAR(20)      NOT NULL CONSTRAINT CK_ReportTemplateAssignment_Kind
        CHECK (Kind IN ('DEFAULT', 'FORM_STRUCTURE', 'FORM_REVISION')),
    -- Version de formulaire concernée (NULL pour DEFAULT).
    FormId                  UNIQUEIDENTIFIER NULL REFERENCES dbo.Ref_Formulaire(Id),
    CreationOrder           BIGINT           NOT NULL,
    Comment                 NVARCHAR(500)    NULL,
    -- NULL : modèle livré avec l'application.
    AssignedBy              VARCHAR(20)      NULL REFERENCES dbo.AUTILIS(USR_0),
    AssignedAt              DATETIME2(3)     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT CK_ReportTemplateAssignment_Form CHECK (
        (Kind = 'DEFAULT' AND FormId IS NULL) OR (Kind <> 'DEFAULT' AND FormId IS NOT NULL))
);
GO

CREATE UNIQUE INDEX UX_ReportTemplateAssignment_CreationOrder ON dbo.ReportTemplateAssignment(CreationOrder);
ALTER TABLE dbo.ReportTemplateAssignment ADD CONSTRAINT DF_ReportTemplateAssignment_CreationOrder
    DEFAULT (NEXT VALUE FOR dbo.CreationOrderSequence) FOR CreationOrder;
CREATE INDEX IX_ReportTemplateAssignment_Form ON dbo.ReportTemplateAssignment(FormId) WHERE FormId IS NOT NULL;
CREATE INDEX IX_ReportTemplateAssignment_Version ON dbo.ReportTemplateAssignment(ReportTemplateVersionId);
GO

CREATE TABLE dbo.GeneratedDocument (
    Id                      UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    -- Source : un document qualité OU un document d'exécution (réponses des opérateurs).
    DocumentEnteteId        UNIQUEIDENTIFIER NULL REFERENCES dbo.Document_Entete(Id),
    ExecControleDocumentStatutId UNIQUEIDENTIFIER NULL REFERENCES dbo.Exec_ControleDocumentStatut(Id),
    ReportTemplateVersionId UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.ReportTemplateVersion(Id),
    -- Empreinte des données du document mises en forme : une même donnée n'est générée qu'une fois par version du modèle.
    SourceDataSha256        CHAR(64)       NOT NULL,
    FileName                NVARCHAR(200)  NOT NULL,
    PdfContent              VARBINARY(MAX) NOT NULL,
    PdfSha256               AS CONVERT(CHAR(64), HASHBYTES('SHA2_256', PdfContent), 2) PERSISTED,
    WorkbookContent         VARBINARY(MAX) NOT NULL,
    GeneratedAt             DATETIME2(3)   NOT NULL DEFAULT SYSDATETIME(),
    -- Utilisateur qui a consulté ou imprimé le document (NULL : génération par l'application).
    RequestedBy             VARCHAR(20)    NULL REFERENCES dbo.AUTILIS(USR_0),
    CONSTRAINT CK_GeneratedDocument_Source CHECK (
        (DocumentEnteteId IS NOT NULL AND ExecControleDocumentStatutId IS NULL) OR (DocumentEnteteId IS NULL AND ExecControleDocumentStatutId IS NOT NULL))
);
GO

-- Une même donnée n'est générée qu'une fois par version du modèle, pour chaque source.
CREATE UNIQUE INDEX UX_GeneratedDocument_Document ON dbo.GeneratedDocument(DocumentEnteteId, ReportTemplateVersionId, SourceDataSha256) WHERE DocumentEnteteId IS NOT NULL;
CREATE UNIQUE INDEX UX_GeneratedDocument_Execution ON dbo.GeneratedDocument(ExecControleDocumentStatutId, ReportTemplateVersionId, SourceDataSha256) WHERE ExecControleDocumentStatutId IS NOT NULL;
CREATE INDEX IX_GeneratedDocument_Document ON dbo.GeneratedDocument(DocumentEnteteId, GeneratedAt DESC);
CREATE INDEX IX_GeneratedDocument_Execution ON dbo.GeneratedDocument(ExecControleDocumentStatutId, GeneratedAt DESC);
GO

-- Demandes de modèle : ce que l'administrateur doit rattacher, et pourquoi. Une demande naît d'un nouveau formulaire
-- (NEW_FORM), d'une nouvelle version dont la structure change (NEW_STRUCTURE) ou du superviseur qualité (SUPERVISOR,
-- avec son choix : même version ou version suivante). Une seule demande en attente par formulaire ; une demande traitée
-- garde l'affectation qui l'a satisfaite (traçabilité ISO 9001).
CREATE TABLE dbo.ReportTemplateRequest (
    Id            BIGINT IDENTITY(1,1) PRIMARY KEY,
    FormId        UNIQUEIDENTIFIER NOT NULL REFERENCES dbo.Ref_Formulaire(Id),
    Origin        VARCHAR(20)    NOT NULL
        CONSTRAINT CK_ReportTemplateRequest_Origin CHECK (Origin IN ('NEW_FORM', 'NEW_STRUCTURE', 'SUPERVISOR')),
    Reason        NVARCHAR(500)  NULL,
    -- Choix du superviseur (demande SUPERVISOR seulement) : appliqué tel quel au rattachement.
    VersionChange VARCHAR(20)    NULL
        CONSTRAINT CK_ReportTemplateRequest_VersionChange CHECK (VersionChange IN ('SAME_VERSION', 'NEXT_VERSION')),
    Status        VARCHAR(20)    NOT NULL DEFAULT 'PENDING'
        CONSTRAINT CK_ReportTemplateRequest_Status CHECK (Status IN ('PENDING', 'DONE', 'CANCELLED')),
    RequestedBy   VARCHAR(20)    NULL REFERENCES dbo.AUTILIS(USR_0),
    RequestedAt   DATETIME2(3)   NOT NULL DEFAULT SYSDATETIME(),
    HandledBy     VARCHAR(20)    NULL REFERENCES dbo.AUTILIS(USR_0),
    HandledAt     DATETIME2(3)   NULL,
    AssignmentId  BIGINT         NULL REFERENCES dbo.ReportTemplateAssignment(Id),
    CONSTRAINT CK_ReportTemplateRequest_VersionChoice CHECK (
        (Origin = 'SUPERVISOR' AND VersionChange IS NOT NULL) OR (Origin <> 'SUPERVISOR' AND VersionChange IS NULL)),
    CONSTRAINT CK_ReportTemplateRequest_Handled CHECK (
        (Status = 'PENDING' AND HandledAt IS NULL AND AssignmentId IS NULL)
        OR (Status = 'DONE' AND HandledAt IS NOT NULL AND AssignmentId IS NOT NULL)
        OR (Status = 'CANCELLED' AND HandledAt IS NOT NULL))
);
GO

CREATE UNIQUE INDEX UX_ReportTemplateRequest_Pending ON dbo.ReportTemplateRequest(FormId) WHERE Status = 'PENDING';
GO

-- Le fichier d'une version ne change jamais ; une version n'est jamais supprimée et ne redevient pas brouillon.
CREATE TRIGGER TR_ReportTemplateVersion_Immutable ON dbo.ReportTemplateVersion
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
        THROW 50002, N'Une version de modèle de rapport ne peut pas être supprimée (ISO 9001).', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE i.ReportTemplateCode <> d.ReportTemplateCode OR i.VersionNumber <> d.VersionNumber
                  OR ISNULL(i.FormGroupId, '00000000-0000-0000-0000-000000000000') <> ISNULL(d.FormGroupId, '00000000-0000-0000-0000-000000000000')
                  OR i.Name <> d.Name
                  OR i.FileName <> d.FileName OR i.ContentSha256 <> d.ContentSha256
                  OR ISNULL(i.UploadedBy, '') <> ISNULL(d.UploadedBy, '') OR i.UploadedAt <> d.UploadedAt)
        THROW 50003, N'Le fichier d''une version de modèle ne peut pas être modifié : envoyez une nouvelle version.', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id = i.Id
               WHERE d.Status = 'PUBLISHED' AND (i.Status <> 'PUBLISHED' OR i.PublishedAt <> d.PublishedAt
                                                 OR ISNULL(i.PublishedBy, '') <> ISNULL(d.PublishedBy, '')))
        THROW 50003, N'Une version de modèle publiée ne change plus.', 1;
END;
GO

-- Une version de modèle appartient à un formulaire du rôle que son type met en forme ; un type sans formulaire
-- (modèle par défaut) n'a pas de formulaire propriétaire.
CREATE TRIGGER TR_ReportTemplateVersion_Owner ON dbo.ReportTemplateVersion
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.ReportTemplate t ON t.Code = i.ReportTemplateCode
               WHERE (t.FormRole IS NULL AND i.FormGroupId IS NOT NULL)
                  OR (i.FormGroupId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ref_Formulaire f
                                                               WHERE f.VersionGroupId = i.FormGroupId AND f.Role = t.FormRole)))
        THROW 50003, N'Un modèle appartient à un formulaire dont son type met en forme les documents (même rôle).', 1;
END;
GO

-- Historique des affectations : jamais modifié ni supprimé. Une affectation utilise une version publiée ; une
-- affectation à un formulaire vise un modèle de document qualité et une version de formulaire non archivée.
CREATE TRIGGER TR_ReportTemplateAssignment_Rules ON dbo.ReportTemplateAssignment
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 50002, N'L''historique des affectations de modèles ne peut être ni modifié ni supprimé (ISO 9001).', 1;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               WHERE v.Status <> 'PUBLISHED')
        THROW 50003, N'Seule une version de modèle publiée peut être affectée.', 1;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               JOIN dbo.ReportTemplate t ON t.Code = v.ReportTemplateCode
               JOIN dbo.Ref_Formulaire f ON f.Id = i.FormId
               WHERE f.Statut = 'ARCHIVE' OR t.FormRole IS NULL OR f.Role IS NULL OR f.Role <> t.FormRole)
        THROW 50003, N'Un modèle n''est rattaché qu''à une version de formulaire non archivée dont il met en forme les documents (même rôle).', 1;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               JOIN dbo.ReportTemplate t ON t.Code = v.ReportTemplateCode
               WHERE i.Kind = 'DEFAULT' AND t.FormRole IS NOT NULL)
        THROW 50003, N'Un modèle des documents d''un formulaire se rattache au formulaire, pas comme modèle par défaut.', 1;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               LEFT JOIN dbo.Ref_Formulaire f ON f.Id = i.FormId
               WHERE (i.Kind = 'DEFAULT' AND v.FormGroupId IS NOT NULL)
                  OR (i.Kind <> 'DEFAULT' AND ISNULL(v.FormGroupId, '00000000-0000-0000-0000-000000000000') <> f.VersionGroupId))
        THROW 50003, N'Un modèle n''est rattaché qu''au formulaire auquel il est dédié.', 1;
END;
GO

-- Un document généré est figé ; il est produit par une version publiée du modèle de son type de document.
CREATE TRIGGER TR_GeneratedDocument_Immutable ON dbo.GeneratedDocument
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 50002, N'Un document généré ne peut être ni modifié ni supprimé (ISO 9001).', 1;
END;
GO

CREATE TRIGGER TR_GeneratedDocument_Template ON dbo.GeneratedDocument
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i
               JOIN dbo.ReportTemplateVersion v ON v.Id = i.ReportTemplateVersionId
               JOIN dbo.ReportTemplate t ON t.Code = v.ReportTemplateCode
               LEFT JOIN dbo.Document_Entete e ON e.Id = i.DocumentEnteteId
               LEFT JOIN dbo.Exec_ControleDocumentStatut s ON s.Id = i.ExecControleDocumentStatutId
               WHERE v.Status <> 'PUBLISHED'
                  OR (i.DocumentEnteteId IS NOT NULL AND ISNULL(t.DocumentTypeCode, '') <> e.TypeDocumentCode)
                  OR (i.ExecControleDocumentStatutId IS NOT NULL AND ISNULL(t.ExecutionDocumentTypeCode, '') <> s.TypeDocument))
        THROW 50003, N'Un document généré utilise une version publiée du modèle de son type de document.', 1;
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
