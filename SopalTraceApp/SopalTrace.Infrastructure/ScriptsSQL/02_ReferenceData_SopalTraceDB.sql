-- =================================================================================
-- SopalTraceDB — Données de RÉFÉRENCE (à exécuter en production)
-- Rôles, types, opérations, machines et leur configuration, familles, natures, sections, périodicités, formulaires, ISO 2859.
-- =================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requis pour écrire dans les tables à index filtrés
GO
USE SopalTraceDB;
GO

-- =================================================================================
-- SCRIPT MASTER V7.0.0 - JEU DE DONNÉES (SEED DATA) V3.0
-- 100% Adapté à la base sans table 'Composant' et avec 'SF' inclus.
-- =================================================================================


-- =================================================================================
-- 0. RÉFÉRENTIELS DE VALEURS
-- =================================================================================

INSERT INTO dbo.AppRole (Code, Label) VALUES
('ADMIN', 'Administrateur'),
('RESPONSABLE_DI', 'Responsable développement industriel'),
('RESPONSABLE_QUALITE', 'Responsable qualité'),
('SUPERVISEUR_QUALITE', 'Superviseur qualité'),
('OPERATEUR', 'Opérateur'),
('MAGASINIER', 'Magasinier');

INSERT INTO dbo.Team (Code, Label) VALUES
('E1', 'Équipe 1'),
('E2', 'Équipe 2'),
('E3', 'Équipe 3');

INSERT INTO dbo.ColumnValueType (Code, Label) VALUES
('TEXT', 'Texte'), ('NUMBER', 'Nombre'), ('DATE', 'Date'), ('BOOLEAN', 'Oui / Non'), ('LIST', 'Liste de valeurs');

INSERT INTO dbo.InspectionLevel (Code, Label, DisplayOrder) VALUES
('S-1', 'Spécial S-1', 1), ('S-2', 'Spécial S-2', 2), ('S-3', 'Spécial S-3', 3), ('S-4', 'Spécial S-4', 4),
('I', 'Général I', 5), ('II', 'Général II', 6), ('III', 'Général III', 7);

INSERT INTO dbo.ReferencePieceRole (Code, Label) VALUES
('PRC', 'Pièce de référence conforme'),
('PRNC', 'Pièce de référence non conforme'),
('FEC', 'Fuite étalon conforme'),
('FENC', 'Fuite étalon non conforme');

-- =================================================================================
-- 1. RÉFÉRENTIELS DE BASE
-- =================================================================================

INSERT INTO dbo.Operation (Code, Libelle, AvecMachine, AvecCotesTronconnage) VALUES
('ASS', 'Assemblage', 0, 0),
('TRONC', 'Tronçonnage', 1, 1),
('ESTOMP', 'Estompage', 1, 0),
('USINAG', 'Usinage', 1, 0);

INSERT INTO dbo.TypeRobinet (Code, Libelle) VALUES
('MAN', 'Manuelle'),
('AUTO', 'Automatique'),
('SOUP', 'Automatique avec soupape');

-- 2. FAMILLES D'ARTICLES
INSERT INTO dbo.FamilleProduitFini (Code, Designation, TypeRobinetCode) VALUES
('RBGFA-BAC-01', 'Robinet bouteille de gaz à fermeture automatique type boite à clapet à filetage droit', 'AUTO'),
('RBGFA-BAC-02', 'Robinet bouteille de gaz à fermeture automatique type boite à clapet à filetage conique', 'AUTO'),
('RBGFA', 'Robinet bouteille de gaz à fermeture automatique', 'AUTO'),
('RBGFM', 'Robinet bouteille de gaz à fermeture manuelle', 'MAN'),
('RBGFA_SOUPAPE', 'Robinet bouteille de gaz à fermeture automatique avec soupape', 'SOUP');

-- Familles dont les plans d'assemblage sont propres à chaque poste.
UPDATE dbo.FamilleProduitFini SET PlanParPoste = 1 WHERE Code = 'RBGFA_SOUPAPE';

-- Natures d'articles (l'origine fabriqué / acheté est portée par chaque article : Article.Origin)
INSERT INTO dbo.NatureArticle (Code, Libelle) VALUES
('PF', 'Produit Fini'),
('CORPS', 'Corps'),
('VOLANT', 'Volant'),
('PISTON', 'Piston'),
('ECROU', 'Écrou'),
('GOUPILLE', 'Goupille'),
('JOINT', 'Joint (Torique, Auto-serrure, etc.)'),
('LIM_DEBIT', 'Limiteur de débit'),
('GRAISSE', 'Graisse industrielle'),
('CARTON', 'Carton d''emballage'),
('NOTICE', 'Notice / Séparation'),
('MATIERE', 'Matière Première');

-- Natures contrôlées avec un plan générique unique.
UPDATE dbo.NatureArticle SET PlanGenerique = 1 WHERE Code = 'MATIERE';

-- 3. LIAISON GAMMES
INSERT INTO dbo.NatureArticle_Operation (NatureArticleCode, OperationCode, OrdreGamme) VALUES
('CORPS', 'USINAG', 3),
('CORPS', 'TRONC', 1),
('CORPS', 'ESTOMP', 2),
('VOLANT', 'TRONC', 1),
('VOLANT', 'ESTOMP', 2),
('VOLANT', 'USINAG', 3),
('PF', 'ASS', 1),
('PISTON', 'ASS', 1);

-- 4. POSTES DE TRAVAIL
INSERT INTO dbo.PosteTravail (CodePoste, Libelle) VALUES
('PAS71', 'Poste 71'),
('PAS72', 'Poste 72'),
('PAS78', 'Poste 78');

-- 5. MACHINES
INSERT INTO dbo.Machine (CodeMachine, Libelle, OperationCode) VALUES
('MAS19', 'Machine 19', 'ASS'),
('MAS20', 'Machine 20', 'ASS'),
('MAS22', 'Machine 22', 'ASS'),
('MAS26', 'Machine 26', 'ASS'),
('BEE22', 'Banc 22', 'ASS'),
('BEE46', 'Banc 46', 'ASS'),
('BEE47', 'Banc 47', 'ASS'),
('SER05', 'Marquage 05', 'ASS');

-- 6. LIAISON : POSTES <-> MACHINES
INSERT INTO dbo.PosteTravail_Machine (CodePoste, CodeMachine) VALUES
('PAS72', 'MAS26'),
('PAS72', 'BEE46'),
('PAS71', 'MAS22'),
('PAS71', 'MAS19'),
('PAS71', 'BEE47'),
('PAS71', 'SER05');

-- 7. DICTIONNAIRE DES RISQUES
INSERT INTO dbo.RisqueDefaut (CodeDefaut, LibelleDefaut) VALUES
('R_30B_F', 'ESSAI DE 30 BAR (ROBINET FERME)'),
('R_01B_F', 'ESSAI DE 0,1 BAR (ROBINET FERME)'),
('R_30B_O', 'ESSAI DE 30 BAR (ROBINET OUVERT)'),
('R_01B_O', 'ESSAI DE 0,1 BAR (ROBINET OUVERT)'),
('R_GOUP', 'ABSENCE / MAUVAIS MONTAGE DE LA GOUPILLE'),
('R_PTDUR', 'PRESENCE DU POINT DURE'),
('R_J_AUTO', 'ABSENCE DU JOINT AUTOSERREUR'),
('R_J_ANTI', 'ABSENCE/ MAUVAIS MONTAGE JOINT ANTI-FUITE');

-- 8. MATRICE 3D - FAMILLES & MOYENS
INSERT INTO dbo.Ref_FamilleCorps (Code, Designation) VALUES
('F_30_35', 'Famille Corps (30, 35)'),
('F_23', 'Famille Corps (23)'),
('FAM_40_43_44', 'Famille Corps (40, 43, 44)'),
('FAM_49', 'Famille Corps (49)'),
('C_25B0A01', 'Corps (25B0A01)'),
('C_25AXA01', 'Corps (25AXA01)'),
('C_25AWA01', 'Corps (25AWA01)'),
('C_25UA01', 'Corps (25UA01)'),
('C_25A8A01', 'Corps (25A8A01)'),
('C_2588A01', 'Corps (2588A01)'),
('C_2576A01', 'Corps (2576A01)'),
('C_2519A01', 'Corps (2519A01)');

INSERT INTO dbo.Ref_MoyenDetection (Code, Designation) VALUES
('PRC', 'PRC'),
('PRNC', 'PRNC'),
('PRC_FEC', 'PRC + FEC'),
('PRC_FENC', 'PRC + FENC'),
('PRC_LS', 'PRC LS'),
('PRC_LI', 'PRC LI');

-- 9. MOYENS DE CONTRÔLE
INSERT INTO dbo.MoyenControle (Code, Libelle) VALUES
('VISUEL', 'Visuel'),
('BANC_ESSAI', 'Banc d''essai'),
('PAC', 'P à C (Pied à coulisse)'),
('JAP', 'J à P (Jauge de profondeur)'),
('COMP', 'Comparateur'),
('CLD', 'Clé dynamométrique'),
('BAGUE_F', 'Bague filetée P-NP'),
('TAMPON_F', 'Tampon fileté P-NP'),
('DEFAUTHEQUE', 'Défauthèque');

-- 10. INSTRUMENTS DE CONTRÔLE
INSERT INTO dbo.Instrument (CodeInstrument, Designation) VALUES
('BAN59', 'BEE LAB : (BAN59)'),
('BEE32', 'BEE LAB : (BEE32)'),
('BAN47', 'BEE LAB : (BAN47)'),
('PAC', 'PAC (Pied à coulisse)'),
('JPC', 'JPC (Jauge de Profondeur)'),
('CAN', 'CAN (Comparateur / Alésomètre)'),
('CLD', 'CLD (Clé Dynamométrique)');

-- 11. TYPES DE SECTION
-- TypeNormalise = identifiant unique + source de vérité pour DetecterTypeSection() et visibleSections.
-- Libelle = texte fixe affiché à l'opérateur.
-- Périodicité facultative pour tous les types : elle se choisit sur la section du document.
-- NQA : 0,65 pour les échantillonnages selon norme (valeur Sopal, fixe pour le moment).
INSERT INTO dbo.TypeSection (TypeNormalise, Libelle) VALUES
('REGLAGE',              'Contrôle aux réglages'),
('REGLAGE_PROD',         'Contrôle au réglage et en cours de production'),
('ECHANTILLONNAGE_3789', 'Contrôle par échantillonnage en cours de production'),
('ECHANTILLONNAGE_2859', 'Contrôle par échantillonnage (ISO 2859-1, NQA 0,65)'),
('ECHANTILLONNAGE_3951', 'Contrôle par échantillonnage par mesures (ISO 3951-1, NQA 0,65)'),
('LOT_OF',               'Contrôle lot OF (1ère et dernière pièce)'),
('LOT_POSTE',            'Contrôle par poste (1ère et dernière pièce)'),
('TOUS_PIECES',          'Contrôle 100% des pièces');

-- 12. NQA & PÉRIODICITÉS
INSERT INTO dbo.NQA (ValeurNQA) VALUES (0.01),(0.015),(0.025),(0.04),(0.065),(0.1),(0.15),(0.25),(0.4),(0.65),(1.0),(1.5),(2.5),(4.0),(6.5),(10.0);

INSERT INTO dbo.Periodicite (NbrPieces, Unite, NbrHeures) VALUES
(5, 'SERIE', NULL),
(4, 'HEURE', 1),
(1, 'HEURE', 1),
(1, 'HEURE', 4);

INSERT INTO dbo.PeriodiciteMachine (Code, Libelle, OrdreAffichage, ControlMoment) VALUES
('DEM', 'Au démarrage', 0, 'DEMARRAGE'),
('PAU', 'Après la pause', 0, 'APRES_PAUSE'),
('FIN', 'A la fin du poste', 0, 'FIN_POSTE');


-- 13. TYPES CARACTÉRISTIQUES ET CONTRÔLE
INSERT INTO dbo.TypeCaracteristique (Code, Libelle) VALUES
('VISUEL', 'Contrôle visuel'),
('DIM', 'Diamètre / Dimension'),
('ETANCH', 'Étanchéité'),
('Serrage', 'Couple de serrage');

INSERT INTO dbo.TypeControle (Code, Libelle) VALUES
('VISUEL', 'Visuel'),
('MESURE', 'Mesure'),
('MANUEL', 'Manuel'),
('ATTRIBUT', 'Attribut');

-- =================================================================================
-- 18. SEED DATA - UTILISATEURS (CACHE ERP SAGE X3 ET APPLICATION)
-- =================================================================================

-- Compte technique : auteur des données importées par script. ENAFLG_0 = 0 : aucune inscription possible.
INSERT INTO dbo.AUTILIS (USR_0, INTUSR_0, ENAFLG_0, CODMET_0, ADDEML_0) VALUES
('SYSTEM', 'Compte technique (imports et traitements automatiques)', 0, 'SYSTEME', NULL);

-- =================================================================================
-- 19. LIAISON MACHINES <-> FAMILLES DE CORPS
-- =================================================================================

INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'SER05', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('F_30_35', 'F_23', 'FAM_40_43_44', 'FAM_49');

INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'MAS22', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('F_30_35', 'F_23', 'FAM_40_43_44', 'FAM_49');

INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'BEE46', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('C_25A8A01', 'C_2588A01', 'C_2576A01', 'C_2519A01');

INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'BEE47', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('F_30_35', 'F_23', 'FAM_40_43_44', 'FAM_49');

INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'BEE22', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('C_25B0A01', 'C_25AXA01', 'C_25AWA01', 'C_25UA01');

INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'MAS19', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('C_25B0A01', 'C_25AXA01', 'C_25AWA01', 'C_25UA01');

-- =================================================================================
-- 20. TEST DE VOTRE NOUVELLE LOGIQUE MAGASINIER / QUICK CONTROL
-- =================================================================================



-- =================================================================================
-- 21. OPÉRATIONS DE FABRICATION RÉELLES (Source : Fiche Suiveuse)
-- =================================================================================
-- La fiche suiveuse (image) montre pour VOLANT ROBINET DE GAZ PETROM :
--   Op  5 → Poste prévu : TRN26 (Tournage)
--   Op 10 → Poste prévu : PRS04 (Pressage)
--   Op 15 → Poste prévu : GRN08 (Grenaillage)
--   Op 20 → Poste prévu : TRF14 (Traitement de surface)
--   Op 25 → Poste prévu : LAV08 (Lavage)
-- Le "Poste réel" sera enregistré dans Exec_ControleOF.MachineCode lors du démarrage.


-- Mise à jour de la gamme VOLANT : remplacement par la gamme réelle
DELETE FROM dbo.NatureArticle_Operation WHERE NatureArticleCode = 'VOLANT';

INSERT INTO dbo.NatureArticle_Operation (NatureArticleCode, OperationCode, OrdreGamme) VALUES
('VOLANT', 'TRONC', 5),   -- Op  5 : Tronçonnage  (obligatoire)
('VOLANT', 'ESTOMP', 10),   -- Op 10 : Estampage    (obligatoire)
('VOLANT', 'USINAG', 15);   -- Op 15 : Usinage      (obligatoire)

-- Mise à jour de la gamme CORPS : opérations granulaires
DELETE FROM dbo.NatureArticle_Operation WHERE NatureArticleCode = 'CORPS';

INSERT INTO dbo.NatureArticle_Operation (NatureArticleCode, OperationCode, OrdreGamme) VALUES
('CORPS', 'TRONC', 5),
('CORPS', 'ESTOMP', 10),
('CORPS', 'USINAG', 15);

-- Machines = Postes de travail prévus (un poste par machine INDEPENDANTE)
-- Chaque machine est affectée à une opération → c'est le "Poste prévu" de la gamme
INSERT INTO dbo.Machine (CodeMachine, Libelle, OperationCode, Actif) VALUES
-- Tronçonnage
('TRN26', 'TRN26','TRONC', 1),
-- Estampage
('PRS04', 'PRS04','ESTOMP', 1),
-- Usinage
('TRF14', 'TRF14','USINAG', 1);

-- Présentation du formulaire de vérification de chaque machine (reprend les règles historiques du front).
UPDATE dbo.Machine SET VerifModele = 'BEE', VerifSansConformite = 1, VerifFuiteEtalon = 1 WHERE CodeMachine IN ('BEE22','BEE46','BEE47');

UPDATE dbo.Machine SET VerifFuiteEnPa = 1 WHERE CodeMachine = 'BEE47';

UPDATE dbo.Machine SET VerifModele = 'MAS' WHERE CodeMachine IN ('MAS19','MAS20','MAS22','MAS26');

UPDATE dbo.Machine SET VerifSansConformite = 1 WHERE CodeMachine IN ('MAS19','MAS20');

UPDATE dbo.Machine SET VerifMoyenControleNumerote = 1, VerifSansDeltaP = 1 WHERE CodeMachine = 'MAS19';

UPDATE dbo.Machine SET VerifFuiteEtalon = 1, VerifTablesEtalonSeparees = 1 WHERE CodeMachine = 'MAS22';

UPDATE dbo.Machine SET VerifSansPression = 1, VerifColonnesParTableau = 1 WHERE CodeMachine = 'MAS26';

UPDATE dbo.Machine SET VerifModele = 'SER', VerifSansConformite = 1, VerifSansPression = 1 WHERE CodeMachine = 'SER05';

INSERT INTO dbo.TypeDocument (Code, Libelle) VALUES
    ('MODELE_FAB',   'Modèle de fabrication'),
    ('PLAN_FAB',     'Plan de fabrication'),
    ('VERIF_MACHINE','Plan de vérification machine'),
    ('PLAN_ECHANTILLONNAGE','Plan d''échantillonnage'),
    ('CTRL_POSTE',   'Plan de contrôle de Poste (Tally)'),
    ('RESULTAT_CF',  'Résultats Contrôle en cours de Fabrication'),
    ('PLAN_ASS',     'Plan Assemblage'),
    ('PLAN_PF',      'Plan Produit Fini');

-- Types d'exécution et type de document qualité appliqué (NULL = pas de document).
INSERT INTO dbo.ExecutionDocumentType (Code, Label, DocumentTypeCode) VALUES
('VERIF_MACHINE', 'Vérification machine', 'VERIF_MACHINE'),
('CLOTURE_VM_POSTE', 'Clôture des vérifications machine du poste', NULL),
('PLAN_ASS', 'Plan d''assemblage', 'PLAN_ASS'),
('RESULTAT_CONTROLE_POSTE', 'Résultat contrôle au poste', 'CTRL_POSTE'),
('CLOTURE_RC_POSTE', 'Clôture du résultat contrôle au poste', NULL),
('RESULTAT_CONTROLE_CF', 'Résultat contrôle en cours de fabrication (ENCF)', 'RESULTAT_CF'),
('ECHANTILLONNAGE', 'Fiche d''échantillonnage', 'PLAN_ECHANTILLONNAGE'),
('REGISTRE_TRACA', 'Registre de traçabilité', NULL);

-- =================================================================================
-- 23. SEED DATA - FORMULAIRES DE REFERENCE
-- =================================================================================
-- Formulaire commun pour Semi-Finis (Corps, Volant, Piston)
INSERT INTO dbo.Ref_Formulaire (Id, CodeReference, Designation, Version, Statut, CreeLe, Role) VALUES
('D3C2A4F4-1B0A-4F9E-B4FF-67E97A135593', 'PRC', 'Formulaire PRC', 0, 'ACTIF', GETDATE(), 'EN_COURS_DE_FABRICATION'),
('A1B2C3D4-1111-4444-5555-666677778888', 'FE-ASS-PISTON', 'Plan Ass Piston', 0, 'BROUILLON', GETDATE(), 'EN_COURS_DE_ASSEMBLAGE'),
('B2C3D4E5-2222-4444-5555-666677778888', 'FE-ASS-MANU', 'Plan Ass PF Manu', 0, 'BROUILLON', GETDATE(), 'EN_COURS_DE_ASSEMBLAGE'),
(NEWID(), 'FE-ASS-RBGFA-BAC-01', 'Plan ASS RBGFA-BAC-01', 0, 'BROUILLON', GETDATE(), 'EN_COURS_DE_ASSEMBLAGE'),
(NEWID(), 'FE-ASS-RBGFA-BAC-02', 'Plan ASS RBGFA-BAC-02', 0, 'BROUILLON', GETDATE(), 'EN_COURS_DE_ASSEMBLAGE'),
(NEWID(), 'FE-PF-RBGFA_SOUPAPE_PAS71', 'Plan PF RBGFA_SOUPAPE PAS71 ', 0, 'BROUILLON', GETDATE(), 'EN_COURS_DE_ASSEMBLAGE'),
(NEWID(), 'FE-PF-RBGFA_SOUPAPE_PAS72', 'Plan PF RBGFA_SOUPAPE PAS72 ', 0, 'BROUILLON', GETDATE(), 'EN_COURS_DE_ASSEMBLAGE'),
(NEWID(), 'FE-PF-RBGFA_SOUPAPE_PAS78', 'Plan PF RBGFA_SOUPAPE PAS78 ', 0, 'BROUILLON', GETDATE(), 'EN_COURS_DE_ASSEMBLAGE'),
(NEWID(), 'FE-PF-RBGFA_SOUPAPE', 'Plan PF RBGFA_SOUPAPE', 0, 'BROUILLON', GETDATE(), 'PRODUIT_FINI'),
(NEWID(), 'FE-PF-RBGFA', 'Plan PF RBGFA', 0, 'BROUILLON', GETDATE(), 'PRODUIT_FINI'),
(NEWID(), 'FE-PF-RBGFA-BAC-01', 'Plan PF RBGFA-BAC-01', 0, 'BROUILLON', GETDATE(), 'PRODUIT_FINI'),
(NEWID(), 'FE-PF-RBGFA-BAC-02', 'Plan PF RBGFA-BAC-02', 0, 'BROUILLON', GETDATE(), 'PRODUIT_FINI'),
(NEWID(), 'FE-PF-RBGFM', 'Plan PF RBGFM', 0, 'BROUILLON', GETDATE(), 'PRODUIT_FINI'),
(NEWID(), 'FE-VM-MAS19', 'Formulaire Verif Machine MAS19', 0, 'BROUILLON', GETDATE(), 'VERIF_MACHINE'),
(NEWID(), 'FE-VM-MAS20', 'Formulaire Verif Machine MAS20', 0, 'BROUILLON', GETDATE(), 'VERIF_MACHINE'),
(NEWID(), 'FE-VM-MAS22', 'Formulaire Verif Machine MAS22', 0, 'BROUILLON', GETDATE(), 'VERIF_MACHINE'),
(NEWID(), 'FE-VM-MAS26', 'Formulaire Verif Machine MAS26', 0, 'BROUILLON', GETDATE(), 'VERIF_MACHINE'),
(NEWID(), 'FE-VM-BEE22', 'Formulaire Verif Machine BEE22', 0, 'BROUILLON', GETDATE(), 'VERIF_MACHINE'),
(NEWID(), 'FE-VM-BEE46', 'Formulaire Verif Machine BEE46', 0, 'BROUILLON', GETDATE(), 'VERIF_MACHINE'),
(NEWID(), 'FE-VM-BEE47', 'Formulaire Verif Machine BEE47', 0, 'BROUILLON', GETDATE(), 'VERIF_MACHINE'),
(NEWID(), 'FE-ECHAN-01', 'Formulaire Echantillonnage', 0, 'BROUILLON', GETDATE(), 'ECHANTILLONNAGE'),
(NEWID(), 'FE-VM-SER05', 'Formulaire Verif Machine SER05', 0, 'BROUILLON', GETDATE(), 'VERIF_MACHINE'),
(NEWID(), 'FE-RC-PAS71', 'Formulaire Résultat Contrôle PAS71', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_POSTE'),
(NEWID(), 'FE-RC-PAS72', 'Formulaire Résultat Contrôle PAS72', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_POSTE'),
(NEWID(), 'FE-RC-PAS78', 'Formulaire Résultat Contrôle PAS78', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_POSTE'),
(NEWID(), 'FE-RC-PAS71_SOUPAPE', 'Formulaire Résultat Contrôle PAS71 Soupape', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_POSTE'),
(NEWID(), 'FE-RC-PAS72_SOUPAPE', 'Formulaire Résultat Contrôle PAS72 Soupape', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_POSTE'),
(NEWID(), 'FE-RC-PAS78_SOUPAPE', 'Formulaire Résultat Contrôle PAS78 Soupape', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_POSTE'),
(NEWID(), 'FE-RC-PAS79', 'Formulaire Résultat Contrôle PAS79', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_POSTE'),
(NEWID(), 'FE-RC-ENCF', 'Résultats du contrôle en cours de fabrication (Usi/Esp/Trn)', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_CF'),
(NEWID(), 'FE-RC-ENCF-ASS', 'Résultats du contrôle en cours de fabrication Assemblage', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_CF'),
(NEWID(), 'FE-RC-ENCF-ASS-PAS71_SOUPAPE', 'Résultats du CF Assemblage articles avec soupape PAS71', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_CF'),
(NEWID(), 'FE-RC-ENCF-ASS-PAS72_SOUPAPE', 'Résultats du CF Assemblage articles avec soupape PAS72', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_CF'),
(NEWID(), 'FE-RC-ENCF-ASS-PAS78_SOUPAPE', 'Résultats du CF Assemblage articles avec soupape PAS78', 0, 'BROUILLON', GETDATE(), 'RESULTAT_CONTROLE_CF');

-- Champ d'application des formulaires de contrôle au poste : chaque poste a son formulaire, et un autre pour
-- les robinets avec soupape (famille RBGFA_SOUPAPE).
UPDATE f SET PosteCode = v.PosteCode, FamilleProduitFiniCode = v.FamilleProduitFiniCode
FROM dbo.Ref_Formulaire f
JOIN (VALUES
    ('FE-RC-PAS71', 'PAS71', NULL),
    ('FE-RC-PAS72', 'PAS72', NULL),
    ('FE-RC-PAS78', 'PAS78', NULL),
    ('FE-RC-PAS71_SOUPAPE', 'PAS71', 'RBGFA_SOUPAPE'),
    ('FE-RC-PAS72_SOUPAPE', 'PAS72', 'RBGFA_SOUPAPE'),
    ('FE-RC-PAS78_SOUPAPE', 'PAS78', 'RBGFA_SOUPAPE')
) v (CodeReference, PosteCode, FamilleProduitFiniCode) ON v.CodeReference = f.CodeReference;

-- Équipes et horaires des formulaires de contrôle au poste (modifiables dans le document par le superviseur).
INSERT INTO dbo.Ref_Formulaire_Equipe (FormulaireId, TeamCode, NomEquipe, HeureDebut, HeureFin, OrdreAffiche)
SELECT f.Id, e.TeamCode, e.NomEquipe, e.HeureDebut, e.HeureFin, e.OrdreAffiche
FROM dbo.Ref_Formulaire f
CROSS JOIN (VALUES ('E1', 'Équipe 1', 6, 14, 1), ('E2', 'Équipe 2', 14, 22, 2), ('E3', 'Équipe 3', 22, 6, 3))
    e (TeamCode, NomEquipe, HeureDebut, HeureFin, OrdreAffiche)
WHERE f.Role = 'RESULTAT_CONTROLE_POSTE'
  AND NOT EXISTS (SELECT 1 FROM dbo.Ref_Formulaire_Equipe x WHERE x.FormulaireId = f.Id);

PRINT '=================================================================';
PRINT '  LA BASE SOPALTRACE V7.1 (MODULE EXÉCUTION OPÉRATEUR) EST OK !  ';
PRINT '=================================================================';

-- 14. SEED ISO 2859 DATA
DELETE FROM dbo.Ref_ISO2859_LettresCode;

DELETE FROM dbo.Ref_ISO2859_Echantillonnage;

INSERT INTO dbo.Ref_ISO2859_LettresCode (QteMin, QteMax, NiveauControle, CodeLettre) VALUES
(2, 8, 'S-1', 'B'),
(2, 8, 'S-2', 'B'),
(2, 8, 'S-3', 'B'),
(2, 8, 'S-4', 'B'),
(2, 8, 'I', 'B'),
(2, 8, 'II', 'B'),
(2, 8, 'III', 'B'),
(9, 15, 'S-1', 'B'),
(9, 15, 'S-2', 'B'),
(9, 15, 'S-3', 'B'),
(9, 15, 'S-4', 'B'),
(9, 15, 'I', 'B'),
(9, 15, 'II', 'B'),
(9, 15, 'III', 'C'),
(16, 25, 'S-1', 'B'),
(16, 25, 'S-2', 'B'),
(16, 25, 'S-3', 'B'),
(16, 25, 'S-4', 'B'),
(16, 25, 'I', 'B'),
(16, 25, 'II', 'C'),
(16, 25, 'III', 'D'),
(26, 50, 'S-1', 'B'),
(26, 50, 'S-2', 'B'),
(26, 50, 'S-3', 'B'),
(26, 50, 'S-4', 'C'),
(26, 50, 'I', 'C'),
(26, 50, 'II', 'D'),
(26, 50, 'III', 'E'),
(51, 90, 'S-1', 'B'),
(51, 90, 'S-2', 'B'),
(51, 90, 'S-3', 'C'),
(51, 90, 'S-4', 'C'),
(51, 90, 'I', 'C'),
(51, 90, 'II', 'E'),
(51, 90, 'III', 'F'),
(91, 150, 'S-1', 'B'),
(91, 150, 'S-2', 'B'),
(91, 150, 'S-3', 'C'),
(91, 150, 'S-4', 'D'),
(91, 150, 'I', 'D'),
(91, 150, 'II', 'F'),
(91, 150, 'III', 'G'),
(151, 280, 'S-1', 'B'),
(151, 280, 'S-2', 'C'),
(151, 280, 'S-3', 'D'),
(151, 280, 'S-4', 'E'),
(151, 280, 'I', 'E'),
(151, 280, 'II', 'G'),
(151, 280, 'III', 'H'),
(281, 500, 'S-1', 'B'),
(281, 500, 'S-2', 'C'),
(281, 500, 'S-3', 'D'),
(281, 500, 'S-4', 'E'),
(281, 500, 'I', 'F'),
(281, 500, 'II', 'H'),
(281, 500, 'III', 'J'),
(501, 1200, 'S-1', 'C'),
(501, 1200, 'S-2', 'C'),
(501, 1200, 'S-3', 'E'),
(501, 1200, 'S-4', 'F'),
(501, 1200, 'I', 'G'),
(501, 1200, 'II', 'J'),
(501, 1200, 'III', 'K'),
(1201, 3200, 'S-1', 'C'),
(1201, 3200, 'S-2', 'D'),
(1201, 3200, 'S-3', 'E'),
(1201, 3200, 'S-4', 'G'),
(1201, 3200, 'I', 'H'),
(1201, 3200, 'II', 'K'),
(1201, 3200, 'III', 'L'),
(3201, 10000, 'S-1', 'C'),
(3201, 10000, 'S-2', 'D'),
(3201, 10000, 'S-3', 'F'),
(3201, 10000, 'S-4', 'G'),
(3201, 10000, 'I', 'J'),
(3201, 10000, 'II', 'L'),
(3201, 10000, 'III', 'M'),
(10001, 35000, 'S-1', 'C'),
(10001, 35000, 'S-2', 'D'),
(10001, 35000, 'S-3', 'F'),
(10001, 35000, 'S-4', 'H'),
(10001, 35000, 'I', 'K'),
(10001, 35000, 'II', 'M'),
(10001, 35000, 'III', 'N'),
(35001, 150000, 'S-1', 'D'),
(35001, 150000, 'S-2', 'E'),
(35001, 150000, 'S-3', 'G'),
(35001, 150000, 'S-4', 'J'),
(35001, 150000, 'I', 'L'),
(35001, 150000, 'II', 'N'),
(35001, 150000, 'III', 'P'),
(150001, 500000, 'S-1', 'D'),
(150001, 500000, 'S-2', 'E'),
(150001, 500000, 'S-3', 'G'),
(150001, 500000, 'S-4', 'J'),
(150001, 500000, 'I', 'M'),
(150001, 500000, 'II', 'P'),
(150001, 500000, 'III', 'Q'),
(500001, 500000000, 'S-1', 'D'),
(500001, 500000000, 'S-2', 'E'),
(500001, 500000000, 'S-3', 'H'),
(500001, 500000000, 'S-4', 'K'),
(500001, 500000000, 'I', 'N'),
(500001, 500000000, 'II', 'Q'),
(500001, 500000000, 'III', 'R');

-- Ref_ISO2859_Echantillonnage : Table 2-A ISO 2859-1 — une ligne par lettre de code
INSERT INTO dbo.Ref_ISO2859_Echantillonnage (CodeLettre, Quantite) VALUES
('A', 2),
('B', 3),
('C', 5),
('D', 8),
('E', 13),
('F', 20),
('G', 32),
('H', 50),
('J', 80),
('K', 125),
('L', 200),
('M', 315),
('N', 500),
('P', 800),
('Q', 1250),
('R', 2000);
GO
