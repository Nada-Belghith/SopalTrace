-- =================================================================================
-- SopalTraceDB — Copie locale des tables Sage X3 (développement et recette uniquement)
-- Articles, nomenclatures, OF, besoins, annuaire, BL. En production, ces tables sont alimentées depuis Sage X3.
-- =================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- requis pour écrire dans les tables à index filtrés
GO
USE SopalTraceDB;
GO

-- =================================================================================
-- 14. INSERTION DES ARTICLES (PRODUIT_FINI ET COMPOSANT)
-- =================================================================================

-- TABLE DE BASE : Article (Avec NatureArticleCode incluse)
INSERT INTO dbo.Article (CodeArticle, Designation, NatureArticleCode, TypeArticle) VALUES
-- Produits Finis
('25A8B01-1', 'Boite à clapet', 'PF', 'PF'),
('25AKA01-1', 'R. de GAZ Boite GAZ', 'PF', 'PF'),
('25B0A01-1', 'R de gaz avec soupape', 'PF', 'PF'),
('2588A01-1-A', 'R de gaz auto', 'PF', 'PF'),
('2511A01-1', 'Produit Assemblage 2511', 'PF', 'PF'),
('2569A01-1', 'Produit Test 2569', 'PF', 'PF'),
('25AAA01-1-9', 'R GAZ SANS MARQUAGE', 'PF', 'PF'),
('25ACA01-1-8', 'ROBINET DE GAZ COURT WINXO', 'PF', 'PF'),
('25AFA01-1-8', 'ROBINET DE GAZ SOMAGAZ', 'PF', 'PF'),
('25AKA01-1-6', 'ROBINET DE GAZ BUTAGAZ', 'PF', 'PF'),
('25A0A01-1-9', 'ROBINET VC6 ADAPTATEUR', 'PF', 'PF'),
('25A8A01-1-A', 'BOITE A CLAPET TETE HEXA', 'PF', 'PF'),
('2540B01-1-B', 'BOITE A CLAPET TOTAL', 'PF', 'PF'),
('2556A01-1-8', 'BOITE A CLAPET CAROTTE LONGUE', 'PF', 'PF'),
('2582A01-1-D', 'BOITE A CLAPET TETE HEXAGONALE H7', 'PF', 'PF'),
('25B2A01-1-1', 'Robinet gaz auto (soupape 26 bar)', 'PF', 'PF'),
('25B7A01-1-1', 'Robinet gaz auto G3/4"', 'PF', 'PF'),
('25B0A01-1-2', 'Robinet gaz auto M23.2', 'PF', 'PF'),
('2552A01-1-E', 'Robinet de gaz M26.2 x2 con10% "SNDP" Agilgaz', 'PF', 'PF'),
('2552A01-1', 'Robinet de gaz manuelle 2552A01-1', 'PF', 'PF'),

-- Semi-finis (Physiquement CORPS, VOLANT, PISTON, mais en prod ce sont des SF)
('2903201-1', 'Article Semi-Fini 2903', 'CORPS', 'SF'),
('2903202-1', 'Corps Semi-Fini Soupape', 'CORPS', 'SF'),
('3028S01-1', 'Article Semi-Fini 3028', 'VOLANT', 'SF'),
('P-25B0A01', 'Piston laiton Asil', 'PISTON', 'SF'),
('31B5201-1', 'VOLANT ROBINET DE GAZ PETROM', 'VOLANT', 'SF'),

-- Composants / Matières / Consommables (TypeArticle = COMPOSANT)
('C-25B0A01', 'Corps laiton brut (Soupape)', 'CORPS', 'COMPOSANT'),
('2623201-1-7', 'Corps robinet de gaz carotte M26.2', 'CORPS', 'COMPOSANT'),
('2903S01-1-1', 'Piston de gaz complet', 'PISTON', 'COMPOSANT'),
('m-25B0A01', 'Corps laiton brut (Soupape)', 'CORPS', 'COMPOSANT'),
('n-25B0A01', 'Corps laiton brut (Soupape)', 'CORPS', 'COMPOSANT'),
('b-25B0A01', 'Corps laiton brut (Soupape)', 'CORPS', 'COMPOSANT'),
('V-25A8B01', 'Volant plastique noir', 'VOLANT', 'COMPOSANT'),
('3113201-1-5', 'Volant', 'VOLANT', 'COMPOSANT'),
('E1BU000', 'CARTON GAZ 410 X 205 X 104', 'CARTON', 'COMPOSANT'),
('E452000', 'NOTICE ROBINET DE GAZ', 'NOTICE', 'COMPOSANT'),
('C118000', 'GRAISSE RETINAX PISTON ROBINET', 'GRAISSE', 'COMPOSANT'),
('E256000', 'SEPARATION 204 X 103', 'NOTICE', 'COMPOSANT'),
('E257000', 'SEPARATION 405 X 103', 'NOTICE', 'COMPOSANT'),
('4112000-1-1', 'Goupille carrée 2.5 x 2.5', 'GOUPILLE', 'COMPOSANT'),
('3825000-1-1', 'Limiteur de débit (Type D)', 'LIM_DEBIT', 'COMPOSANT'),
('3834000-1-1', 'Joint auto-serrure Ø13.5 x Ø8 x 7.5', 'JOINT', 'COMPOSANT'),
('M1150RP', 'Barre Laiton Brut Rond Ø25 L3000', 'MATIERE', 'COMPOSANT'),
('M405S00', 'Bobine de Fil Laiton Ø3mm', 'MATIERE', 'COMPOSANT'),
('M3000L0', 'Laiton brut en lingots (Fonderie)', 'MATIERE', 'COMPOSANT'),
('2610201-1', 'Laiton brut d''extrusion', 'MATIERE', 'COMPOSANT'),
('2906201-1', 'Lingot de Laiton Allié', 'MATIERE', 'COMPOSANT'),
('3738000-1', 'Barre Laiton Hexagonale H17', 'MATIERE', 'COMPOSANT'),
('3137201-1', 'Barre Laiton Rectangulaire 20x5', 'MATIERE', 'COMPOSANT'),
-- SOPAL GAZ - Article de l'OF et sa nomenclature (Image "Bon de Sortie")
('13NMA01-1', 'SOPAL GAZ 1594547 P/A/M/000', 'PF', 'PF'),
('3801007-1', 'JOINT FIBRE ROUGE 24x16x2', 'JOINT', 'COMPOSANT'),
('1501013-1', 'BAGUE EN ACETAL 25', 'JOINT', 'COMPOSANT'),
('3261501-1', 'ECROU 3/4 RACCORD TC P5 SONEDE', 'MATIERE', 'COMPOSANT'),
('3580000-1', 'ANNEAU EXPANSIF TYPE A 22 A2 DIN7993', 'GOUPILLE', 'COMPOSANT'),
('E105000', 'CARTON 295 X 210 X 145', 'CARTON', 'COMPOSANT'),
('E329000', 'SACHET EN PLASTIQUE 13x18 IMP (12/100)', 'NOTICE', 'COMPOSANT'),
('2630201.1.9', 'Corps r-gaz moyenne carotte goupille carrée', 'CORPS', 'SF'),
('M1180RP', 'Laiton Ø 18 (suivant EN12165 CW617N) LONGUEUR', 'MATIERE', 'COMPOSANT');

-- SPÉCIALISATION : Produit Fini (famille obligatoire, qui donne le type de robinet)
INSERT INTO dbo.ProduitFini (CodeArticle, FamilleProduitFiniCode) VALUES
('25A8B01-1', 'RBGFA-BAC-01'),
('25AKA01-1', 'RBGFM'),
('25B0A01-1', 'RBGFA_SOUPAPE'),
('2588A01-1-A', 'RBGFA-BAC-01'),
('2511A01-1', 'RBGFM'),
('2569A01-1', 'RBGFM'),
('25AAA01-1-9', 'RBGFM'),
('25ACA01-1-8', 'RBGFM'),
('25AFA01-1-8', 'RBGFM'),
('25AKA01-1-6', 'RBGFM'),
('25A0A01-1-9', 'RBGFM'),
('25A8A01-1-A', 'RBGFA-BAC-01'),
('2540B01-1-B', 'RBGFA-BAC-01'),
('2556A01-1-8', 'RBGFA-BAC-01'),
('2582A01-1-D', 'RBGFA-BAC-02'),
('25B2A01-1-1', 'RBGFA_SOUPAPE'),
('25B7A01-1-1', 'RBGFA_SOUPAPE'),
('25B0A01-1-2', 'RBGFA_SOUPAPE'),
('2552A01-1-E', 'RBGFA-BAC-01'),
('2552A01-1', 'RBGFM'),
('13NMA01-1', 'RBGFA');

-- REMARQUE : Plus d'insertion dans dbo.Composant car la table n'existe plus !

-- =================================================================================
-- 15. INSERTION DES NOMENCLATURES STANDARDS (Image 1 : BOMD)
-- =================================================================================
INSERT INTO dbo.BOMD_Nomenclature (ArticleParent, CodeComposant, CodeAlternative, QuantiteRequise) VALUES
-- Nomenclature pour 2569A01-1
('2569A01-1', 'M1150RP', 31, 0.016949),
('2569A01-1', 'M405S00', 31, 0.007739),
-- Nomenclature pour 2511A01-1
('2511A01-1', '2610201-1', 4, 1),
('2511A01-1', '2906201-1', 4, 1),
('2511A01-1', '3738000-1', 4, 2),
('2511A01-1', '3137201-1', 4, 1),
-- Corps et volant semi-finis du 2511A01-1 : l'opérateur choisit leur OF d'usinage dans le registre de traçabilité.
('2511A01-1', '2903201-1', 4, 1),
('2511A01-1', '3028S01-1', 4, 1),
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
('25B2A01-1-1', '31B5201-1', 1, 1), -- Volant du robinet soupape (même famille que son autre parent 25B0A01-1)
('25A8B01-1', '3028S01-1', 1, 1),
('25B0A01-1', '31B5201-1', 1, 1), -- Lie le Volant 31B5201-1 au parent Produit Fini pour hériter de sa famille de robinet
('25B0A01-1', 'C-25B0A01', 1, 1),
('25B0A01-1', 'b-25B0A01', 1, 1),
('25B0A01-1', 'n-25B0A01', 1, 1),
('25B0A01-1', 'm-25B0A01', 1, 1),
('2552A01-1', '2630201.1.9', 1, 1), -- Lie le Corps 2630201.1.9 au parent Produit Fini Manuelle
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
INSERT INTO dbo.MFGHEAD_OrdreFabrication --statut : EN_COURS, PLANIFIE, TERMINE, ANNULE
(NumeroOF, CodeArticle, QuantitePrevue, QuantiteLancee, QuantiteReelle, StatutOF, DateDebut, DateFin)
VALUES
('OF26080157', '2903201-1', 100000, 100000, 99850, 'EN_COURS', '2026-07-30', '2026-08-28'),
('OF26050439', '3028S01-1', 4400, 4400, 0, 'PLANIFIE', '2026-06-01', '2026-06-01'),
('OF26041131', '2511A01-1', 3000, 3000, 3000, 'EN_COURS', '2026-04-06', '2026-04-06'),
('OF25120735', '2903201-1', 200000, 180000, 0, 'EN_COURS', '2026-04-30', '2026-05-29'),
('OF26011015', '13NMA01-1', 27720, 27720, 0, 'EN_COURS', '2026-03-02', '2026-03-02'),
('OF26022552', '2552A01-1-E', 5000, 5000, 0, 'EN_COURS', '2026-05-18', '2026-05-18'),
('OF24120004', '2630201.1.9', 1000, 1000, 0, 'EN_COURS', '2024-12-09', '2024-12-09');

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
('OF24120004', 'M1180RP', 1000, 0),
('OF26050439', 'M405S00', 440, 0);

-- =================================================================================
-- 18. SEED DATA - UTILISATEURS (CACHE ERP SAGE X3 ET APPLICATION)
-- =================================================================================

INSERT INTO dbo.AUTILIS (USR_0, INTUSR_0, ENAFLG_0, CODMET_0, ADDEML_0) VALUES
('11111', 'Nada Belghith', 2, 'RESPONSABLE_DI', NULL),
('77777', 'Nada Belghith2', 2, 'RESPONSABLE_DI', 'nada.belghith@enis.u-sfax.tn'),
('22222', 'Res Qualite Test', 2, 'RESPONSABLE_QUALITE', 'resqualite@s.com'),
('55555', 'Operateur Test', 2, 'OPERATEUR', 'belghithnada@gmail.com'),
('33333', 'Magasinier Test', 2, 'MAGASINIER', 'magasinier@s.com'),
('44444', 'Superviseur Test', 2, 'SUPERVISEUR_QUALITE', 'imen.belghith@enis.tn'),
('66666', 'Admin Doc', 2, 'ADMIN', 'nada.belghith@enis.tn'),
('11112', 'Superviseur Prod 1', 2, 'SUPERVISEUR_PROD', NULL),
('22223', 'Manager Prod 1', 2, 'MANAGER_PROD', NULL),
('88881', 'Achats Test', 2, 'SERVICE_ACHATS', 'achats@s.com'),
('88882', 'Commercial Test', 2, 'SERVICE_COMMERCIAL', 'commercial@s.com'),
('88883', 'Qualite Reception Test', 2, 'SERVICE_QUALITE', 'qualite.reception@s.com'),
('88884', 'Developpement Industriel Test', 2, 'SERVICE_DI', 'di@s.com');

INSERT INTO dbo.ATEXTRA (CODFIC_0, ZONE_0, IDENT1_0, LANGUE_0, TEXTE_0) VALUES
('AUTILIS', 'INTUSR', '11111', 'FRA', 'Nada Belghith'),
('AUTILIS', 'INTUSR', '22222', 'FRA', 'Res Qualite Test'),
('AUTILIS', 'INTUSR', '55555', 'FRA', 'Operateur Test'),
('AUTILIS', 'INTUSR', '33333', 'FRA', 'Magasinier Test'),
('AUTILIS', 'INTUSR', '44444', 'FRA', 'Superviseur Test'),
('AUTILIS', 'INTUSR', '66666', 'FRA', 'Admin Doc'),
('AUTILIS', 'INTUSR', '11112', 'FRA', 'Superviseur Prod 1'),
('AUTILIS', 'INTUSR', '22223', 'FRA', 'Manager Prod 1');

-- =================================================================================
-- 22. ARTICLE ET OF DE LA FICHE SUIVEUSE (Test d'intégration complet)
-- =================================================================================

-- OF26021488 tel que visible sur la fiche (StatutOF = 1 = En Cours)
INSERT INTO dbo.MFGHEAD_OrdreFabrication
    (NumeroOF, CodeArticle, QuantitePrevue, QuantiteLancee, QuantiteReelle, StatutOF, DateDebut, DateFin)
VALUES
    ('OF26021488', '31B5201-1', 20006, 20006, 0, 'EN_COURS', '2026-02-03', '2026-02-26');

-- NOUVEL OF POUR CORPS : OF26029030 pour Corps Semi-Fini (2903201-1)
INSERT INTO dbo.MFGHEAD_OrdreFabrication
    (NumeroOF, CodeArticle, QuantitePrevue, QuantiteLancee, QuantiteReelle, StatutOF, DateDebut, DateFin)
VALUES
    ('OF26029030', '2903201-1', 15000, 15000, 0, 'EN_COURS', '2026-05-19', '2026-05-19');

-- =================================================================================
-- TEST SCENARIO : OF n-25B0A01 COMPLET (Magasin -> Opérateur)
-- =================================================================================
-- 1. Ordre de fabrication (Sage ERP)
INSERT INTO dbo.MFGHEAD_OrdreFabrication
    (NumeroOF, CodeArticle, QuantitePrevue, QuantiteLancee, QuantiteReelle, StatutOF, DateDebut, DateFin)
VALUES
    ('OF2606N25B0', 'n-25B0A01', 2000, 2000, 0, 'EN_COURS', '2026-06-29', '2026-06-30');

-- 2. Besoins OF (Matière Première)
INSERT INTO dbo.MFGMAT_BesoinOF (NumeroOF, CodeArticle, QuantiteRequise, QuantiteSortie)
VALUES ('OF2606N25B0', 'M1180RP', 2000, 0);

-- =================================================================================
-- EXEMPLES D'OF POUR PRODUIT FINI (ASSEMBLAGE)
-- =================================================================================
-- OF 1: Assemblage 2511
INSERT INTO dbo.MFGHEAD_OrdreFabrication
    (NumeroOF, CodeArticle, QuantitePrevue, QuantiteLancee, QuantiteReelle, StatutOF, DateDebut, DateFin)
VALUES
    ('OF2607ASS01', '2511A01-1', 1000, 1000, 0, 'EN_COURS', GETDATE(), GETDATE());

-- OF 2: Robinet gaz auto (PF)
INSERT INTO dbo.MFGHEAD_OrdreFabrication
    (NumeroOF, CodeArticle, QuantitePrevue, QuantiteLancee, QuantiteReelle, StatutOF, DateDebut, DateFin)
VALUES
    ('OF2607ASS02', '25B2A01-1-1', 500, 500, 0, 'EN_COURS', GETDATE(), GETDATE());

-- =================================================================================
-- COMPOSANTS D'ASSEMBLAGE (ECROUS, VOLANTS, ETC.) POUR LE MAGASINIER
-- =================================================================================
INSERT INTO dbo.Article (CodeArticle, Designation, NatureArticleCode, TypeArticle) VALUES
('ECROU-001', 'Ecrous', 'ECROU', 'COMPOSANT'),
('VOLANT-001', 'Volants', 'VOLANT', 'COMPOSANT'),
('LIMITEUR-001', 'limiteur', 'LIM_DEBIT', 'COMPOSANT'),
('PISTON-001', 'Piston', 'PISTON', 'COMPOSANT'),
('GOUPILLE-001', 'Goupille', 'GOUPILLE', 'COMPOSANT'),
('JOINT-ANTI-SERREUR', 'joint', 'JOINT', 'COMPOSANT');

-- Composants achetés des produits d'assemblage (nomenclature Sage, après la création de ces articles).
INSERT INTO dbo.BOMD_Nomenclature (ArticleParent, CodeComposant, CodeAlternative, QuantiteRequise) VALUES
('2511A01-1', 'ECROU-001', 4, 1),
('2511A01-1', 'GOUPILLE-001', 4, 1),
('2511A01-1', 'JOINT-ANTI-SERREUR', 4, 1),
('25B2A01-1-1', 'ECROU-001', 1, 1),
('25B2A01-1-1', 'LIMITEUR-001', 1, 1),
('25B2A01-1-1', 'PISTON-001', 1, 1),
('25B2A01-1-1', 'GOUPILLE-001', 1, 2),
('25B2A01-1-1', 'JOINT-ANTI-SERREUR', 1, 1);

-- Besoins des OF d'assemblage : nomenclature de l'article × quantité de l'OF, comme Sage les crée au lancement.
-- Le magasinier sert les composants achetés (et le piston) ; corps et volant semi-finis viennent de leur atelier.
INSERT INTO dbo.MFGMAT_BesoinOF (NumeroOF, CodeArticle, QuantiteRequise, QuantiteSortie) VALUES
('OF2607ASS01', '2903201-1', 1000, 0),
('OF2607ASS01', '3028S01-1', 1000, 0),
('OF2607ASS01', 'ECROU-001', 1000, 0),
('OF2607ASS01', 'GOUPILLE-001', 1000, 0),
('OF2607ASS01', 'JOINT-ANTI-SERREUR', 1000, 0),
('OF2607ASS02', '2903202-1', 500, 0),
('OF2607ASS02', '31B5201-1', 500, 0),
('OF2607ASS02', 'ECROU-001', 500, 0),
('OF2607ASS02', 'LIMITEUR-001', 500, 0),
('OF2607ASS02', 'PISTON-001', 500, 0),
('OF2607ASS02', 'GOUPILLE-001', 1000, 0),
('OF2607ASS02', 'JOINT-ANTI-SERREUR', 500, 0);

-- =================================================================================
-- Bons de livraison (copie de la table Sage SDELIVERY) pour l'expédition magasin
-- =================================================================================
-- Commandes clients (copie de la table Sage SORDER) : commandes ouvertes créées par le commercial, avec la référence
-- de commande du client (saisie libre dans Sage). Une commande peut être livrée sur plusieurs BL.
INSERT INTO dbo.SORDER (NumeroCommande, CodeClient, DateCommande, ReferenceCommandeClient) VALUES
('SOPCOV-2607001', 'CLI-STEG', DATEADD(DAY, -20, CAST(GETDATE() AS DATE)), '9400108469'),
('SOPCOV-2607002', 'CLI-SNDP', DATEADD(DAY, -15, CAST(GETDATE() AS DATE)), 'OA 044803'),
('SOPCOV-2607003', 'CLI-STEG', DATEADD(DAY, -5, CAST(GETDATE() AS DATE)), 'marché 11/2025'),
('SOPCOV-2607004', 'CLI-SNDP', DATEADD(DAY, -2, CAST(GETDATE() AS DATE)), NULL);

INSERT INTO dbo.SDELIVERY (NumeroBL, CodeClient, DateExpedition, StatutBL, NumeroCommande) VALUES
('BL2607001', 'CLI-STEG', CAST(GETDATE() AS DATE), 'OUVERT', 'SOPCOV-2607001'),
('BL2607002', 'CLI-SNDP', CAST(GETDATE() AS DATE), 'OUVERT', 'SOPCOV-2607002'),
('BL2607003', 'CLI-STEG', CAST(GETDATE() AS DATE), 'OUVERT', 'SOPCOV-2607001'),
('BL2607004', 'CLI-SNDP', CAST(GETDATE() AS DATE), 'OUVERT', 'SOPCOV-2607004');

-- Commandes fournisseurs (copie de la table Sage PORDER) : composants et matières reçus.
INSERT INTO dbo.PORDER (NumeroCommande, CodeFournisseur, DateCommande) VALUES
('CF2607001', 'FRS-LAITON', DATEADD(DAY, -40, CAST(GETDATE() AS DATE))),
('CF2607002', 'FRS-JOINTS', DATEADD(DAY, -30, CAST(GETDATE() AS DATE))),
('CF2607003', 'FRS-RESSORTS', DATEADD(DAY, -25, CAST(GETDATE() AS DATE)));
GO
