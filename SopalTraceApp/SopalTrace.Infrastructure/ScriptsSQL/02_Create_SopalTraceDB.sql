CREATE DATABASE SopalTraceDB;
GO
USE SopalTraceDB;
GO

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
-- Permet de tracer qui s'est connecté, quand, et surtout les tentatives de piratage (mot de passe erroné)
CREATE TABLE JournalConnexions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Matricule VARCHAR(20) NOT NULL,        -- On stocke le matricule même si l'utilisateur n'existe pas
    Action VARCHAR(50) NOT NULL,           -- Ex: 'CONNEXION_SUCCES', 'ECHEC_MDP', 'DECONNEXION', 'MDP_OUBLIE'
    AdresseIP VARCHAR(50) NULL,            -- Pour savoir depuis quelle tablette l'action a été faite
    Details VARCHAR(255) NULL,             -- Ex: 'Mot de passe incorrect' ou 'Compte ERP inactif'
    DateAction DATETIME DEFAULT GETDATE()
);

CREATE INDEX IX_JournalConnexions_Matricule ON JournalConnexions(Matricule);
CREATE INDEX IX_JournalConnexions_Date ON JournalConnexions(DateAction);
GO
