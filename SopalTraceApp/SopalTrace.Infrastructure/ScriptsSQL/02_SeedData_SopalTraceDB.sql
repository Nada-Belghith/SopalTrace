-- =================================================================================
-- SCRIPT MASTER V7.0.0 - JEU DE DONNÉES (SEED DATA) V3.0
-- 100% Adapté à la base sans table 'Composant' et avec 'SF' inclus.
-- =================================================================================

USE SopalTraceDB;
GO

-- =================================================================================
-- 1. RÉFÉRENTIELS DE BASE
-- =================================================================================

INSERT INTO dbo.Operation (Code, Libelle) VALUES 
('ASS', 'Assemblage'), 
('TRONC', 'Tronçonnage'), 
('ESTOMP', 'Estompage'), 
('USINAG', 'Usinage');

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

-- NOUVELLE GESTION : NatureArticle ET Origine (Make vs Buy)
INSERT INTO dbo.NatureArticle (Code, Libelle, Origine) VALUES 
('PF', 'Produit Fini', 'FABRIQUE'),
('CORPS', 'Corps', 'FABRIQUE'),
('VOLANT', 'Volant', 'FABRIQUE'),
('PISTON', 'Piston', 'FABRIQUE'),
('GOUPILLE', 'Goupille', 'ACHETE'),
('JOINT', 'Joint (Torique, Auto-serrure, etc.)', 'ACHETE'),
('LIM_DEBIT', 'Limiteur de débit', 'ACHETE'),
('GRAISSE', 'Graisse industrielle', 'ACHETE'),
('CARTON', 'Carton d''emballage', 'ACHETE'),
('NOTICE', 'Notice / Séparation', 'ACHETE'),
('MATIERE', 'Matière Première', 'ACHETE');

-- 3. LIAISON GAMMES
INSERT INTO dbo.NatureArticle_Operation (NatureArticleCode, OperationCode, OrdreGamme, EstObligatoire) VALUES 
('CORPS', 'USINAG', 3, 1),
('CORPS', 'TRONC', 1, 1),
('CORPS', 'ESTOMP', 2, 1),
('VOLANT', 'TRONC', 1, 1),
('VOLANT', 'ESTOMP', 2, 1),
('VOLANT', 'USINAG', 3, 1),
('PF', 'ASS', 1, 1),
('PISTON', 'ASS', 1, 1);

-- 4. POSTES DE TRAVAIL
INSERT INTO dbo.PosteTravail (CodePoste, Libelle) VALUES 
('PAS71', 'Poste 71'), 
('PAS72', 'Poste 72'), 
('PAS78', 'Poste 78'),
('PAS79', 'Assemblage Sopal Gaz');

-- 5. MACHINES
INSERT INTO dbo.Machine (CodeMachine, Libelle, OperationCode, RoleMachine) VALUES 
('MAS19', 'Machine 19', 'ASS', 'MAS_ASS'), 
('MAS20', 'Machine 20', 'ASS', 'MAS_ASS'), 
('MAS22', 'Machine 22', 'ASS', 'MAS_ASS'), 
('MAS26', 'Machine 26', 'ASS', 'MAS_ASS'),
('BEE22', 'Banc 22', 'ASS', 'BEE'), 
('BEE46', 'Banc 46', 'ASS', 'BEE'), 
('BEE47', 'Banc 47', 'ASS', 'BEE'),
('SER05', 'Marquage 05', 'ASS', 'MARQUAGE');

-- 6. LIAISON : POSTES <-> MACHINES
INSERT INTO dbo.PosteTravail_Machine (CodePoste, CodeMachine) VALUES 
('PAS72', 'MAS26'), 
('PAS72', 'BEE46'),
('PAS71', 'MAS22'), 
('PAS71', 'MAS19'), 
('PAS71', 'BEE47'),
('PAS78', 'SER05');

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
('C_25UA01', 'Corps (25UA01)');

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
INSERT INTO dbo.Instrument (CodeInstrument, Designation, Categorie, Statut) VALUES 
('BAN59', 'BEE LAB : (BAN59)', 'BANC ESSAI', 'ACTIF'),
('BEE32', 'BEE LAB : (BEE32)', 'BANC ESSAI', 'ACTIF'),
('BAN47', 'BEE LAB : (BAN47)', 'BANC ESSAI', 'ACTIF'),
('PAC', 'PAC (Pied à coulisse)', 'DIMENSIONNEL', 'ACTIF'),
('JPC', 'JPC (Jauge de Profondeur)', 'DIMENSIONNEL', 'ACTIF'),
('CAN', 'CAN (Comparateur / Alésomètre)', 'DIMENSIONNEL', 'ACTIF'),
('CLD', 'CLD (Clé Dynamométrique)', 'MECANIQUE', 'ACTIF');

-- 11. TYPES DE SECTION
INSERT INTO dbo.TypeSection (Code, Libelle) VALUES 
('REGLAGE', 'aux réglages'),
('REGLAGE_PROD', 'au réglage et en cours de production'), 
('EN_COURS', 'par échantillonnage en cours de production'), 
('ECHANT_NQA', 'par échantillonnage '),
('OF', ' au niveau du lot (OF) ');

-- 12. NQA & PÉRIODICITÉS
INSERT INTO dbo.NQA (ValeurNQA) VALUES (0.65),(1.0),(1.5),(2.5),(4.0),(6.5);

INSERT INTO dbo.Periodicite (Code, Libelle, FrequenceNum, FrequenceUnite, OrdreAffichage) VALUES 
('DEM', 'Au démarrage', NULL, NULL, 0), 
('PAU', 'Après la pause', NULL, NULL, 0), 
('FIN', 'A la fin du poste', NULL, NULL, 0),
('SERIE_5P', 'une série de 5 pièces', 5, 'SERIE', 5),
('4P_1H', '4 pièces / heure', 4, '1_HEURE', 5),
('1P_1H', '1 pièce / heure', 1, '1_HEURE', 5),
('100PCT_1H', '100% des pièces/h', 100, 'PCT_HEURE', 5),
('1P_4H', '1 pièce / 4 heures', 1, '4_HEURE', 5);

INSERT INTO dbo.Ref_RegleEchantillonnage (Code, Libelle) VALUES 
('REG-9F43B', 'selon ISO 2859-1 : Tableau 2-A, avec un NQA de 0,65'),
('REG-43BD2', 'Selon FE0591 : Effectif de l’échantillon /poste (A/B) (p/h)'),
('REG-7A57N', 'la première et la dernière pièce et 0,01% du lot'),
('REG-7A57C', 'selon ISO 3951-1 : Tableau 2, avec un NQA de 0,65');

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
-- 14. INSERTION DES ARTICLES (PRODUIT_FINI ET COMPOSANT)
-- =================================================================================

-- TABLE DE BASE : Article (Avec NatureArticleCode incluse)
INSERT INTO dbo.Article (CodeArticle, Designation, NatureArticleCode, Statut, TypeArticle) VALUES 
-- Produits Finis
('25A8B01-1', 'Boite à clapet', 'PF', 'ACTIF', 'PF'),
('25AKA01-1', 'R. de GAZ Boite GAZ', 'PF', 'ACTIF', 'PF'),
('25B0A01-1', 'R de gaz avec soupape', 'PF', 'ACTIF', 'PF'),
('2588A01-1-A', 'R de gaz auto', 'PF', 'ACTIF', 'PF'),
('2511A01-1', 'Produit Assemblage 2511', 'PF', 'ACTIF', 'PF'),
('2569A01-1', 'Produit Test 2569', 'PF', 'ACTIF', 'PF'),
('25AAA01-1-9', 'R GAZ SANS MARQUAGE', 'PF', 'ACTIF', 'PF'),
('25ACA01-1-8', 'ROBINET DE GAZ COURT WINXO', 'PF', 'ACTIF', 'PF'),
('25AFA01-1-8', 'ROBINET DE GAZ SOMAGAZ', 'PF', 'ACTIF', 'PF'),
('25AKA01-1-6', 'ROBINET DE GAZ BUTAGAZ', 'PF', 'ACTIF', 'PF'),
('25A0A01-1-9', 'ROBINET VC6 ADAPTATEUR', 'PF', 'ACTIF', 'PF'),
('25A8A01-1-A', 'BOITE A CLAPET TETE HEXA', 'PF', 'ACTIF', 'PF'),
('2540B01-1-B', 'BOITE A CLAPET TOTAL', 'PF', 'ACTIF', 'PF'),
('2556A01-1-8', 'BOITE A CLAPET CAROTTE LONGUE', 'PF', 'ACTIF', 'PF'),
('2582A01-1-D', 'BOITE A CLAPET TETE HEXAGONALE H7', 'PF', 'ACTIF', 'PF'),
('25B2A01-1-1', 'Robinet gaz auto (soupape 26 bar)', 'PF', 'ACTIF', 'PF'),
('25B7A01-1-1', 'Robinet gaz auto G3/4"', 'PF', 'ACTIF', 'PF'),
('25B0A01-1-2', 'Robinet gaz auto M23.2', 'PF', 'ACTIF', 'PF'),
('2552A01-1-E', 'Robinet de gaz M26.2 x2 con10% "SNDP" Agilgaz', 'PF', 'ACTIF', 'PF'),

-- Semi-finis (Physiquement CORPS, VOLANT, PISTON, mais en prod ce sont des SF)
('2903201-1', 'Article Semi-Fini 2903', 'CORPS', 'ACTIF', 'SF'),
('2903202-1', 'Corps Semi-Fini Soupape', 'CORPS', 'ACTIF', 'SF'),
('3028S01-1', 'Article Semi-Fini 3028', 'VOLANT', 'ACTIF', 'SF'),
('P-25B0A01', 'Piston laiton Asil', 'PISTON', 'ACTIF', 'SF'),

-- Composants / Matières / Consommables (TypeArticle = COMPOSANT)
('C-25B0A01', 'Corps laiton brut (Soupape)', 'CORPS', 'ACTIF', 'COMPOSANT'),
('2623201-1-7', 'Corps robinet de gaz carotte M26.2', 'CORPS', 'ACTIF', 'COMPOSANT'),
('2903S01-1-1', 'Piston de gaz complet', 'PISTON', 'ACTIF', 'COMPOSANT'),
('m-25B0A01', 'Corps laiton brut (Soupape)', 'CORPS', 'ACTIF', 'COMPOSANT'),
('n-25B0A01', 'Corps laiton brut (Soupape)', 'CORPS', 'ACTIF', 'COMPOSANT'),
('b-25B0A01', 'Corps laiton brut (Soupape)', 'CORPS', 'ACTIF', 'COMPOSANT'),
('V-25A8B01', 'Volant plastique noir', 'VOLANT', 'ACTIF', 'COMPOSANT'),
('3113201-1-5', 'Volant', 'VOLANT', 'ACTIF', 'COMPOSANT'),
('E1BU000', 'CARTON GAZ 410 X 205 X 104', 'CARTON', 'ACTIF', 'COMPOSANT'),
('E452000', 'NOTICE ROBINET DE GAZ', 'NOTICE', 'ACTIF', 'COMPOSANT'),
('C118000', 'GRAISSE RETINAX PISTON ROBINET', 'GRAISSE', 'ACTIF', 'COMPOSANT'),
('E256000', 'SEPARATION 204 X 103', 'NOTICE', 'ACTIF', 'COMPOSANT'),
('E257000', 'SEPARATION 405 X 103', 'NOTICE', 'ACTIF', 'COMPOSANT'),
('4112000-1-1', 'Goupille carrée 2.5 x 2.5', 'GOUPILLE', 'ACTIF', 'COMPOSANT'),
('3825000-1-1', 'Limiteur de débit (Type D)', 'LIM_DEBIT', 'ACTIF', 'COMPOSANT'),
('3834000-1-1', 'Joint auto-serrure Ø13.5 x Ø8 x 7.5', 'JOINT', 'ACTIF', 'COMPOSANT'),
('M1150RP', 'Barre Laiton Brut Rond Ø25 L3000', 'MATIERE', 'ACTIF', 'COMPOSANT'),
('M405S00', 'Bobine de Fil Laiton Ø3mm', 'MATIERE', 'ACTIF', 'COMPOSANT'),
('M3000L0', 'Laiton brut en lingots (Fonderie)', 'MATIERE', 'ACTIF', 'COMPOSANT'),
('2610201-1', 'Laiton brut d''extrusion', 'MATIERE', 'ACTIF', 'COMPOSANT'),
('2906201-1', 'Lingot de Laiton Allié', 'MATIERE', 'ACTIF', 'COMPOSANT'),
('3738000-1', 'Barre Laiton Hexagonale H17', 'MATIERE', 'ACTIF', 'COMPOSANT'),
('3137201-1', 'Barre Laiton Rectangulaire 20x5', 'MATIERE', 'ACTIF', 'COMPOSANT'),
-- SOPAL GAZ - Article de l'OF et sa nomenclature (Image "Bon de Sortie")
('13NMA01-1', 'SOPAL GAZ 1594547 P/A/M/000', 'PF', 'ACTIF', 'PF'),
('3801007-1', 'JOINT FIBRE ROUGE 24x16x2', 'JOINT', 'ACTIF', 'COMPOSANT'),
('1501013-1', 'BAGUE EN ACETAL 25', 'JOINT', 'ACTIF', 'COMPOSANT'),
('3261501-1', 'ECROU 3/4 RACCORD TC P5 SONEDE', 'MATIERE', 'ACTIF', 'COMPOSANT'),
('3580000-1', 'ANNEAU EXPANSIF TYPE A 22 A2 DIN7993', 'GOUPILLE', 'ACTIF', 'COMPOSANT'),
('E105000', 'CARTON 295 X 210 X 145', 'CARTON', 'ACTIF', 'COMPOSANT'),
('E329000', 'SACHET EN PLASTIQUE 13x18 IMP (12/100)', 'NOTICE', 'ACTIF', 'COMPOSANT');

-- SPÉCIALISATION : Produit Fini (Famille et TypeRobinet obligatoires)
INSERT INTO dbo.ProduitFini (CodeArticle, FamilleProduitFiniCode, TypeRobinetCode) VALUES 
('25A8B01-1', 'RBGFA-BAC-01', 'AUTO'),
('25AKA01-1', 'RBGFM', 'MAN'),
('25B0A01-1', 'RBGFA_SOUPAPE', 'SOUP'),
('2588A01-1-A', 'RBGFA-BAC-01', 'AUTO'),
('2511A01-1', 'RBGFM', 'MAN'),
('2569A01-1', 'RBGFM', 'MAN'),
('25AAA01-1-9', 'RBGFM', 'MAN'),
('25ACA01-1-8', 'RBGFM', 'MAN'),
('25AFA01-1-8', 'RBGFM', 'MAN'),
('25AKA01-1-6', 'RBGFM', 'MAN'),
('25A0A01-1-9', 'RBGFM', 'MAN'),
('25A8A01-1-A', 'RBGFA-BAC-01', 'AUTO'),
('2540B01-1-B', 'RBGFA-BAC-01', 'AUTO'),
('2556A01-1-8', 'RBGFA-BAC-01', 'AUTO'),
('2582A01-1-D', 'RBGFA-BAC-02', 'AUTO'),
('25B2A01-1-1', 'RBGFA_SOUPAPE', 'SOUP'),
('25B7A01-1-1', 'RBGFA_SOUPAPE', 'SOUP'),
('25B0A01-1-2', 'RBGFA_SOUPAPE', 'SOUP'),
('2552A01-1-E', 'RBGFA-BAC-01', 'AUTO'),
('13NMA01-1', 'RBGFA', 'AUTO');

-- REMARQUE : Plus d'insertion dans dbo.Composant car la table n'existe plus !

-- =================================================================================
-- 15. INSERTION DES NOMENCLATURES STANDARDS (Image 1 : BOMD)
-- =================================================================================
INSERT INTO dbo.BOMD_Nomenclature (ArticleParent, CodeComposant, CodeAlternative, QuantiteRequise) VALUES 
-- Nomenclature pour 2569A01-1
('2569A01-1', 'M1150RP', 31, 0.016949),
('2569A01-1', 'M405S00', 31, 0.007739),
-- Nomenclature pour 2610201-1
('2610201-1', 'M3000L0', 21, 0),
-- Nomenclature pour 2511A01-1
('2511A01-1', '2610201-1', 4, 1),
('2511A01-1', '2906201-1', 4, 1),
('2511A01-1', '3738000-1', 4, 2),
('2511A01-1', '3137201-1', 4, 1),
-- Nomenclature détaillée (Dessin Sopal du Robinet M26.2 Agilgaz)
('2552A01-1-E', 'E1BU000', 1, 0.02),
('2552A01-1-E', 'E452000', 1, 0.02),
('2552A01-1-E', 'E257000', 1, 0.08),
('2552A01-1-E', 'E256000', 1, 0.18),
('2552A01-1-E', 'C118000', 1, 0.001),
('2552A01-1-E', '4112000-1-1', 1, 1),
('2552A01-1-E', '3825000-1-1', 1, 1),
('2552A01-1-E', '3834000-1-1', 1, 1),
('2552A01-1-E', '3113201-1-5', 1, 1),
('2552A01-1-E', '2903S01-1-1', 1, 1),
('2552A01-1-E', '2623201-1-7', 1, 1),
-- Nomenclature pour les articles semi-finis (SF) pour hériter de la famille de robinet
('25B0A01-1', '2903201-1', 1, 1),
('25B2A01-1-1', '2903202-1', 1, 1),
('25A8B01-1', '3028S01-1', 1, 1),
-- Nomenclature de l'exemple "Bon de Sortie"
('13NMA01-1', '3801007-1', 1, 27.720),
('13NMA01-1', '1501013-1', 1, 27.720),
('13NMA01-1', '3261501-1', 1, 27.720),
('13NMA01-1', '3580000-1', 1, 27.720),
('13NMA01-1', 'E105000', 1, 555),
('13NMA01-1', 'E329000', 1, 555);

-- =================================================================================
-- 16. INSERTION DES ORDRES DE FABRICATION (Image 2 : MFGHEAD)
-- =================================================================================
INSERT INTO dbo.MFGHEAD_OrdreFabrication --statut : 1 = En cours, 2 = Planifié, 3 = Terminé, 4 = Annulé
(NumeroOF, CodeArticle, QuantitePrevue, QuantiteLancee, QuantiteReelle, StatutOF, DateDebut, DateFin) 
VALUES 
('OF26080157', '2903201-1', 100000, 100000, 99850, 1, '2026-07-30', '2026-08-28'),
('OF26050439', '3028S01-1', 4400, 4400, 0, 2, '2026-06-01', '2026-06-01'),
('OF26041131', '2511A01-1', 3000, 3000, 3000, 1, '2026-04-06', '2026-04-06'),
('OF25120735', '2903201-1', 200000, 180000, 0, 1, '2026-04-30', '2026-05-29'),
('OF26011015', '13NMA01-1', 27720, 27720, 0, 1, '2026-03-02', '2026-03-02'),
('OF26022552', '2552A01-1-E', 5000, 5000, 0, 1, '2026-05-18', '2026-05-18');

-- =================================================================================
-- 17. INSERTION DES BESOINS SPÉCIFIQUES DES OF (MFGMAT)
-- =================================================================================
INSERT INTO dbo.MFGMAT_BesoinOF (NumeroOF, CodeArticle, QuantiteRequise, QuantiteSortie) VALUES 
('OF26041131', '2610201-1', 3000, 0), 
('OF26041131', '2906201-1', 3000, 0), 
('OF26041131', '3738000-1', 6000, 0), 
('OF26041131', '3137201-1', 3000, 0),
-- Besoins réels pour l'OF de test (Bon de Sortie)
('OF26011015', '3801007-1', 27.720, 0),
('OF26011015', '1501013-1', 27.720, 0),
('OF26011015', '3261501-1', 27.720, 0),
('OF26011015', '3580000-1', 27.720, 0),
('OF26011015', 'E105000', 555, 0),
('OF26011015', 'E329000', 555, 0),
-- Besoins pour l'OF du Robinet M26.2 Agilgaz (Dessin Sopal)
('OF26022552', '2623201-1-7', 5000, 0),
('OF26022552', '2903S01-1-1', 5000, 0),
('OF26022552', '3113201-1-5', 5000, 0),
('OF26022552', '3834000-1-1', 5000, 0),
('OF26022552', '3825000-1-1', 5000, 0),
('OF26022552', '4112000-1-1', 5000, 0),
('OF26022552', 'C118000', 5, 0),
('OF26022552', 'E256000', 900, 0),
('OF26022552', 'E257000', 400, 0),
('OF26022552', 'E452000', 100, 0),
('OF26022552', 'E1BU000', 100, 0),
-- Matières premières requises pour les OF de Semi-Finis (Corps, Volant, Piston)
('OF26080157', 'M1150RP', 2500, 0),
('OF25120735', 'M1150RP', 5000, 0),
('OF26050439', 'M405S00', 440, 0); 

-- =================================================================================
-- 18. SEED DATA - UTILISATEURS (CACHE ERP SAGE X3 ET APPLICATION)
-- =================================================================================

INSERT INTO dbo.AUTILIS (USR_0, INTUSR_0, ENAFLG_0, CODMET_0, ADDEML_0) VALUES 
('12345', 'Nada Belghith', 2, 'ADMIN', 'nada.belghith@enis.tn'),
('OP001', 'Operateur Test', 2, 'OPERATEUR', 'operateur@sopal.com'),
('QA001', 'Qualite Test', 2, 'QUALITE', 'qualite@sopal.com'),
('23456', 'Magasinier Test', 2, 'MAGASINIER', 'magasinier@sopal.com');

INSERT INTO dbo.ATEXTRA (CODFIC_0, ZONE_0, IDENT1_0, LANGUE_0, TEXTE_0) VALUES 
('AUTILIS', 'INTUSR', '12345', 'FRA', 'Nada Belghith'),
('AUTILIS', 'INTUSR', 'OP001', 'FRA', 'Operateur Test'),
('AUTILIS', 'INTUSR', 'QA001', 'FRA', 'Qualite Test'),
('AUTILIS', 'INTUSR', 'MAG01', 'FRA', 'Magasinier Test');



-- =================================================================================
-- 19. LIAISON MACHINES <-> FAMILLES DE CORPS
-- =================================================================================

INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'SER05', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('F_30_35', 'F_23', 'FAM_40_43_44', 'FAM_49');

INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'MAS22', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('F_30_35', 'F_23', 'FAM_40_43_44', 'FAM_49');

INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'BEE46', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('C_25B0A01', 'C_25AXA01', 'C_25AWA01', 'C_25UA01');

INSERT INTO dbo.Machine_FamilleCorps (MachineCode, RefFamilleCorpsId)
SELECT 'BEE22', Id FROM dbo.Ref_FamilleCorps WHERE Code IN ('C_25B0A01', 'C_25AXA01', 'C_25AWA01', 'C_25UA01');

-- =================================================================================
-- 20. TEST DE VOTRE NOUVELLE LOGIQUE MAGASINIER / QUICK CONTROL
-- =================================================================================


GO
PRINT '=================================================================';
PRINT ' LA BASE SOPALTRACE V7.0 (AVEC MODULE MAGASINIER) EST GÉNÉRÉE !  ';
PRINT '=================================================================';