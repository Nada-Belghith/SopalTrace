-- =================================================================================
-- CREATION DE LA BASE DE DONNEES UNIQUE : SopalTraceDB
-- =================================================================================
CREATE DATABASE SopalTraceDB;
GO

USE SopalTraceDB;
GO

-- =================================================================================
-- PARTIE 1 : TABLES PROPRES A L'APPLICATION SOPALTRACE
-- =================================================================================

-- 1. Table principale des Utilisateurs
CREATE TABLE UtilisateursApp (
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
CREATE INDEX IX_UtilisateursApp_Matricule ON UtilisateursApp(Matricule);
CREATE INDEX IX_UtilisateursApp_Email ON UtilisateursApp(Email);
GO

-- 2. Table de gestion des Sessions (Refresh Tokens)
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UtilisateurId UNIQUEIDENTIFIER NOT NULL,
    Token VARCHAR(255) NOT NULL UNIQUE, 
    JwtId VARCHAR(100) NOT NULL,        
    DateCreation DATETIME DEFAULT GETDATE(),
    DateExpiration DATETIME NOT NULL,
    EstRevoque BIT DEFAULT 0,           
    CONSTRAINT FK_RefreshTokens_Utilisateurs FOREIGN KEY (UtilisateurId) REFERENCES UtilisateursApp(Id) ON DELETE CASCADE
);
CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
GO

-- 3. Table de Journalisation des accès (Logs / Sécurité)
CREATE TABLE JournalConnexions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Matricule VARCHAR(20) NOT NULL,        
    Action VARCHAR(50) NOT NULL,           
    Details VARCHAR(255) NULL,             
    DateAction DATETIME DEFAULT GETDATE()
);
CREATE INDEX IX_JournalConnexions_Matricule ON JournalConnexions(Matricule);
CREATE INDEX IX_JournalConnexions_Date ON JournalConnexions(DateAction);
GO

-- =================================================================================
-- PARTIE 2 : TABLES DE SYNCHRONISATION (REPLIQUES ERP SAGE)
-- Ces tables serviront de cache local, mis à jour périodiquement depuis le vrai ERP
-- =================================================================================

-- 4. Table des utilisateurs (GESAUS - Copie locale)
CREATE TABLE AUTILIS (
    USR_0 VARCHAR(5) PRIMARY KEY,
    INTUSR_0 VARCHAR(100) NOT NULL,
    ENAFLG_0 INT NOT NULL DEFAULT 1,
    CODMET_0 VARCHAR(20) NOT NULL,
    ADDEML_0 VARCHAR(150) NULL
);
GO

-- 5. Table des traductions des métiers (ATEXTRA - Copie locale)
CREATE TABLE ATEXTRA (
    CODFIC_0 VARCHAR(50) NOT NULL,
    ZONE_0 VARCHAR(50) NOT NULL,
    IDENT1_0 VARCHAR(50) NOT NULL,
    LANGUE_0 VARCHAR(3) NOT NULL,
    TEXTE_0 VARCHAR(255) NOT NULL,
    PRIMARY KEY (CODFIC_0, ZONE_0, IDENT1_0, LANGUE_0)
);
GO

-- =================================================================================
-- PARTIE 3 : JEU DE DONNEES DE TEST (MOCK)
-- =================================================================================

INSERT INTO AUTILIS (USR_0, INTUSR_0, ENAFLG_0, CODMET_0) VALUES
('54321', 'Amira Ben Ali', 1, 'PROD_GAZ'),
('12345', 'Ahmed Ben Salah', 1, 'QUAL'),
('99999', 'Karim Trabelsi', 1, 'MAG');

INSERT INTO ATEXTRA (CODFIC_0, ZONE_0, IDENT1_0, LANGUE_0, TEXTE_0) VALUES
('APROFMET', 'INTPRF', 'PROD_GAZ', 'FRA', 'Operateur Sopal Gaz'),
('APROFMET', 'INTPRF', 'QUAL', 'FRA', 'Administrateur Qualite'),
('APROFMET', 'INTPRF', 'MAG', 'FRA', 'Magasinier PF');
GO