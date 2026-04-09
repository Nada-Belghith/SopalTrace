-- =================================================================================
-- SCRIPT 05 : SEED DATA (Données de référence initiales)
-- Cible : Microsoft SQL Server (T-SQL)
-- Base : SopalTraceDB
--
-- Prérequis : À exécuter en DERNIER, après avoir créé toutes les tables (Scripts 01 à 04)
-- =================================================================================

USE SopalTraceDB;
GO

-- =================================================================================
-- 1. MASTER DATA (Simulant les données provenant de l'ERP Sage X3)
-- =================================================================================

INSERT INTO ERP_TypeRobinet (Code, Libelle) VALUES
    ('MAN',  'Manuelle'),
    ('AUTO', 'Automatique'),
    ('SOP',  'Avec soupape');
GO

INSERT INTO ERP_NatureComposant (Code, Libelle, TypeLotAttendu) VALUES
    ('CORPS',  'Corps', 'NUM_OF'),
    ('VOLANT', 'Volant', 'NUM_OF'),
    ('PISTON', 'Piston', 'NUM_OF'),
    ('ACC',    'Accessoire', 'NUM_RECEPTION');
GO

INSERT INTO ERP_Operation (Code, Libelle, OrdreProcess) VALUES
    ('TRONC',    'Tronçonnage',             1),
    ('ESTOMP',   'Estompage',               2),
    ('USINAG',   'Usinage',                 3),
    ('ASS_MAN',  'Assemblage Manuel',       4),
    ('ASS_AUTO', 'Assemblage Automatique',  5),
    ('ASS_PF',   'Assemblage Produit Fini', 6);
GO

INSERT INTO ERP_Machine (CodeMachine, Libelle, TypeRobinetCode, OperationCode) VALUES
    ('BEE46', 'Banc d''essai étanchéité automatique 46', 'AUTO', 'ASS_PF'),
    ('BEE47', 'Banc d''essai étanchéité automatique 47', 'MAN', 'ASS_PF'),
    ('MAS22', 'Machine assemblage manuelle 22', 'MAN', 'ASS_MAN'),
    ('MAS26', 'Machine assemblage automatique 26', 'AUTO', 'ASS_AUTO'),
    ('SER05', 'Machine de marquage 05', NULL, 'ASS_PF'),
    ('PAS71', 'Poste assemblage PAS 71', 'SOP', 'ASS_MAN');
GO

-- (Note : Normalement ERP_Instrument est aussi alimenté par l'ERP, on le laisse vide ici pour l'exemple ou on peut l'alimenter via l'UI)

-- =================================================================================
-- 2. DICTIONNAIRES QUALITÉ (Propres à SopalTrace)
-- =================================================================================

INSERT INTO TypeCaracteristique (Code, Libelle, UniteDefaut, EstNumerique) VALUES
    ('DIAMETRE',    'Diamètre',                  'mm',   1),
    ('LONGUEUR',    'Longueur',                  'mm',   1),
    ('PROFONDEUR',  'Profondeur',                'mm',   1),
    ('FILETAGE',    'Filetage',                  NULL,   0),
    ('RAINURE',     'Rainure / gorge',           'mm',   1),
    ('ANGULAIRE',   'Angle / conicité',          '°',    1),
    ('ETANCHEITE',  'Étanchéité',                'bar',  1),
    ('VISUEL',      'Contrôle visuel',           NULL,   0),
    ('FONCTIONNEL', 'Contrôle fonctionnel',      NULL,   0),
    ('PRESENCE',    'Vérification de présence',  NULL,   0),
    ('RUGOS',       'Rugosité de surface',       'µm',   1),
    ('COUPLE',      'Couple de serrage',         'N.m',  1),
    ('PERCUSSION',  'Cote de percussion',        'mm',   1),
    ('SERTISSAGE',  'Sertissage',                'kg',   1),
    ('MARQUAGE',    'Marquage / gravure',        NULL,   0);
GO

INSERT INTO TypeControle (Code, Libelle) VALUES
    ('MESURE',      'Mesure dimensionnelle'),
    ('ATTRIBUT',    'Contrôle par attribut'),
    ('VISUEL',      'Contrôle visuel'),
    ('FONCTIONNEL', 'Test fonctionnel'),
    ('COMPTAGE_NC', 'Comptage non-conformités');
GO

INSERT INTO MoyenControle (Code, Libelle) VALUES
    ('BAGUE_FIL_PN',  'Bague filetée P-NP'),
    ('BAGUE_FIL_CON', 'Bague filetée conique'),
    ('BAGUE_LISSE',   'Bague lisse conique'),
    ('TAMPON_LISSE',  'Tampon lisse'),
    ('TAMPON_FILE',   'Tampon fileté'),
    ('CAL_POS',       'Calibre de position'),
    ('CALIBRE',       'Calibre'),
    ('PAC',           'Pied à coulisse'),
    ('CAN',           'Comparateur à numérique'),
    ('JDE_P',         'Jauge de profondeur'),
    ('COMPARATEUR',   'Comparateur'),
    ('RUGO',          'Rugosimètre'),
    ('BANC_ESSAI',    'Banc d''essai'),
    ('CLE_DYN',       'Clé dynamométrique'),
    ('MANUEL',        'Contrôle manuel'),
    ('DEFAUTHEQUE',   'Défauthèque'),
    ('PLAN_PRODUIT',  'Suivant plan du produit');
GO

INSERT INTO Periodicite (Code, Libelle, FrequenceNum, FrequenceUnite, OrdreAffichage) VALUES
    ('4P_PAR_H',      '4 pièces / heure',                      4,    'PIECE_PAR_H', 1),
    ('1P_PAR_H',      '1 pièce / heure',                       1,    'PIECE_PAR_H', 2),
    ('1P_2H',         '1 pièce / 2 heures',                    1,    '2H',          3),
    ('REGLAGE',       'Au réglage',                            NULL, 'REGLAGE',     4),
    ('1P_H_REGL',     '1 pièce/heure au réglage',              1,    'PIECE_PAR_H', 5),
    ('FIN_SERIE',     'En fin de série',                       NULL, 'FIN_SERIE',   6),
    ('LOT_1_DERN',    '1ère et dernière pièce du lot',         NULL, 'LOT',         7),
    ('LOT_001_PCT',   '0,01% du lot',                          NULL, 'LOT',         8),
    ('SYSTEMATIQUE',  '100% des pièces',                       NULL, '100_PCT',     9),
    ('SELON_ECHAN',   'Selon fiche d''échantillonnage',        NULL, 'ECHAN',       10),
    ('DEMARRAGE',     'Au démarrage de la machine',            NULL, 'MACHINE',     11),
    ('APRES_PAUSE',   'Après la pause',                        NULL, 'MACHINE',     12),
    ('FIN_POSTE',     'En fin de poste',                       NULL, 'MACHINE',     13);
GO

INSERT INTO TypeSection (Code, Libelle) VALUES
    ('REGLAGE', 'Réglages initiaux'),
    ('100_POURCENT', '100% de la production'),
    ('ECHANTILLONNAGE', 'Par échantillonnage'),
    ('LOT', 'Au niveau du lot'),
    ('DEMARRAGE', 'Au démarrage'),
    ('PAUSE', 'Après la pause'),
    ('FIN', 'En fin de poste');
GO

INSERT INTO Defautheque (Code, Description) VALUES
    ('FA0774', 'Défaut d''aspect général'),
    ('FA0755', 'Défaut de marquage / lisibilité');
GO

INSERT INTO NQA (ValeurNQA) VALUES (0.65),(1.0),(1.5),(2.5),(4.0),(6.5);
GO

-- =================================================================================
-- 3. ÉTALONS DE MACHINES (Liés par le CodeMachine SAGE)
-- =================================================================================

INSERT INTO PieceReference (Code, TypePiece, Designation, MachineCode) VALUES
    ('BCF01',  'PRC',  'Pièce référence conforme', 'BEE46'),
    ('BSJ01',  'PRNC', 'Pièce sans joint siège',   'MAS26'),
    ('BSB01',  'PRNC', 'Pièce sans bille',         'MAS26'),
    ('FUI15',  'FEC',  'Fuite étalon conforme',    'MAS22'),
    ('F2117A', 'FENC', 'Fuite étalon non conforme','BEE46'),
    ('RCF01',  'PRC',  'Corps référence',          'MAS22'),
    ('RCF02',  'PRC',  'Corps référence 23',       'MAS22'),
    ('RCF03',  'PRC',  'Corps référence 40,43',    'MAS22'),
    ('RCF04',  'PRC',  'Corps référence 49',       'MAS22');
GO

-- =================================================================================
-- 4. GROUPES D'INSTRUMENTS (Les Alias)
-- =================================================================================

INSERT INTO GroupeInstrument (CodeAlias, Libelle) VALUES
    ('PAC*',   'Pieds à coulisse PAC508 / PAC512 / PAC526 / PAC527'),
    ('PAC***', 'Pied à coulisse PAC725'),
    ('CAN*',   'Comparateurs CAN12 / CAN16'),
    ('JPC**',  'Jauge de profondeur JPC15'),
    ('BAN47',  'Banc d''essai BEE47'),
    ('BAN46',  'Banc d''essai BEE46');
GO

-- (Note : les INSERT dans GroupeInstrumentDetail nécessitent d'abord d'avoir des 
-- instruments physiques dans la table ERP_Instrument. Vous le ferez via l'UI).

-- =================================================================================
-- 5. LA BOÎTE À OUTILS (Facilitateurs de création)
-- =================================================================================

INSERT INTO OutilControle (Code, Libelle, TypeControleId, TypeCaracteristiqueId, MoyenControleId, GroupeInstrumentId, PeriodiciteDefautId)
SELECT 
    o.Code, o.Libelle, tc.Id, tcar.Id, mc.Id, gi.Id, per.Id
FROM (
    VALUES
    ('CTL-DIA-EXT',       'Contrôle diamètre extérieur',         'MESURE',      'DIAMETRE',   'PAC',          'PAC*',  '4P_PAR_H'),
    ('CTL-DIA-INT',       'Contrôle diamètre intérieur',         'MESURE',      'DIAMETRE',   'TAMPON_LISSE', 'CAN*',  '4P_PAR_H'),
    ('CTL-LONG',          'Contrôle longueur',                   'MESURE',      'LONGUEUR',   'PAC',          'PAC*',  '4P_PAR_H'),
    ('CTL-PROF',          'Contrôle profondeur',                 'MESURE',      'PROFONDEUR', 'JDE_P',        'JPC**', '1P_H_REGL'),
    ('CTL-RAINURE',       'Contrôle rainure / gorge',            'MESURE',      'RAINURE',    'PAC',          'PAC*',  '4P_PAR_H'),
    ('CTL-FIL-EXT-PN',    'Contrôle filetage ext. bague P-NP',   'ATTRIBUT',    'FILETAGE',   'BAGUE_FIL_PN', NULL,    '4P_PAR_H'),
    ('CTL-FIL-EXT-CON',   'Contrôle filetage ext. bague coniq.', 'ATTRIBUT',    'FILETAGE',   'BAGUE_FIL_CON',NULL,    '4P_PAR_H'),
    ('CTL-ETANCH',        'Étanchéité banc d''essai',            'FONCTIONNEL', 'ETANCHEITE', 'BANC_ESSAI',   NULL,    'SYSTEMATIQUE'),
    ('CTL-VISUEL',        'Contrôle visuel / aspect',            'VISUEL',      'VISUEL',     'DEFAUTHEQUE',  NULL,    'SYSTEMATIQUE'),
    ('CTL-PRESENTE',      'Vérification de présence',            'ATTRIBUT',    'PRESENCE',   'MANUEL',       NULL,    'SYSTEMATIQUE'),
    ('CTL-RUGOSITE',      'Contrôle rugosité Ra',                'MESURE',      'RUGOS',      'RUGO',         NULL,    '1P_H_REGL'),
    ('CTL-MARQUAGE',      'Contrôle marquage / gravure',         'VISUEL',      'MARQUAGE',   'MANUEL',       NULL,    'SYSTEMATIQUE'),
    ('CTL-SERTISSAGE',    'Contrôle sertissage',                 'MESURE',      'SERTISSAGE', 'COMPARATEUR',  'CAN*',  '4P_PAR_H'),
    ('CTL-PERCUSSION',    'Contrôle cote de percussion',         'MESURE',      'PERCUSSION', 'PAC',          'PAC*',  '4P_PAR_H'),
    ('CTL-COUPLE',        'Contrôle couple de serrage',          'MESURE',      'COUPLE',     'CLE_DYN',      NULL,    '4P_PAR_H')
) AS o(Code, Libelle, TypeControle, TypeCar, MoyenCtrl, GrpInstr, Periodo)
INNER JOIN TypeControle         tc   ON tc.Code  = o.TypeControle
INNER JOIN TypeCaracteristique  tcar ON tcar.Code = o.TypeCar
INNER JOIN MoyenControle        mc   ON mc.Code  = o.MoyenCtrl
LEFT JOIN GroupeInstrument gi   ON gi.CodeAlias = o.GrpInstr
LEFT JOIN Periodicite      per  ON per.Code = o.Periodo;
GO