-- =================================================================================
-- SopalTraceDB — Données Sage de DÉMONSTRATION pour les documents externes (jamais en production)
--
-- En production, ces copies sont alimentées depuis Sage X3. Ce script remplit des exemples pour tester les dépôts et
-- la traçabilité par OF :
--   1. Commandes fournisseurs (PORDER) : numéros à saisir pour les rapports de contrôle, certificats et BC fournisseur.
--   2. Commandes clients ouvertes (SORDER) avec la référence de commande du client, comme dans Sage.
--   3. BL (SDELIVERY) rattachés à leur commande ouverte (une commande peut avoir plusieurs BL).
--   4. Pour la traçabilité de l'OF OF2607ASS02 : n° de rapport sur les étiquettes déjà scannées et expédition sur un BL.
--
-- À exécuter APRÈS les scripts 01 à 04. Rejouable : rien n'est écrasé.
-- =================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
USE SopalTraceDB;
GO

IF OBJECT_ID('dbo.PORDER') IS NULL OR OBJECT_ID('dbo.SORDER') IS NULL OR COL_LENGTH('dbo.SDELIVERY', 'NumeroCommande') IS NULL
BEGIN
    RAISERROR('Exécuter d''abord les scripts 01 à 04.', 16, 1);
    SET NOEXEC ON;
END
GO

BEGIN TRANSACTION;

-- ─────────────────────────── 1. Commandes fournisseurs ───────────────────────────
INSERT INTO dbo.PORDER (NumeroCommande, CodeFournisseur, DateCommande)
SELECT c.NumeroCommande, c.CodeFournisseur, c.DateCommande FROM (VALUES
    ('CF2607001', 'FRS-LAITON',   DATEADD(DAY, -40, CAST(GETDATE() AS DATE))),
    ('CF2607002', 'FRS-JOINTS',   DATEADD(DAY, -30, CAST(GETDATE() AS DATE))),
    ('CF2607003', 'FRS-RESSORTS', DATEADD(DAY, -25, CAST(GETDATE() AS DATE))),
    ('CF2607004', 'FRS-VISSERIE', DATEADD(DAY, -20, CAST(GETDATE() AS DATE)))
) c(NumeroCommande, CodeFournisseur, DateCommande)
WHERE NOT EXISTS (SELECT 1 FROM dbo.PORDER p WHERE p.NumeroCommande = c.NumeroCommande);

-- ─────────────────────────── 2. Commandes clients ouvertes (Sage) ───────────────────────────
-- Référence de commande du client telle que saisie dans Sage (texte libre : espaces, « / », accents possibles).
INSERT INTO dbo.SORDER (NumeroCommande, CodeClient, DateCommande, ReferenceCommandeClient)
SELECT c.NumeroCommande, c.CodeClient, c.DateCommande, c.Reference FROM (VALUES
    ('SOPCOV-2607001', 'CLI-STEG', DATEADD(DAY, -20, CAST(GETDATE() AS DATE)), '9400108469'),
    ('SOPCOV-2607002', 'CLI-SNDP', DATEADD(DAY, -15, CAST(GETDATE() AS DATE)), 'OA 044803'),
    ('SOPCOV-2607003', 'CLI-STEG', DATEADD(DAY, -5,  CAST(GETDATE() AS DATE)), 'marché 11/2025'),
    ('SOPCOV-2607004', 'CLI-SNDP', DATEADD(DAY, -2,  CAST(GETDATE() AS DATE)), NULL)
) c(NumeroCommande, CodeClient, DateCommande, Reference)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SORDER s WHERE s.NumeroCommande = c.NumeroCommande);

-- ─────────────────────────── 3. BL et leur commande ───────────────────────────
INSERT INTO dbo.SDELIVERY (NumeroBL, CodeClient, DateExpedition, StatutBL, NumeroCommande)
SELECT b.NumeroBL, b.CodeClient, CAST(GETDATE() AS DATE), 'OUVERT', b.NumeroCommande FROM (VALUES
    ('BL2607001', 'CLI-STEG', 'SOPCOV-2607001'),
    ('BL2607002', 'CLI-SNDP', 'SOPCOV-2607002'),
    ('BL2607003', 'CLI-STEG', 'SOPCOV-2607001'),
    ('BL2607004', 'CLI-SNDP', 'SOPCOV-2607004')
) b(NumeroBL, CodeClient, NumeroCommande)
WHERE NOT EXISTS (SELECT 1 FROM dbo.SDELIVERY d WHERE d.NumeroBL = b.NumeroBL);

-- BL déjà présents sans commande : on leur donne la leur.
UPDATE d SET NumeroCommande = b.NumeroCommande
FROM dbo.SDELIVERY d
JOIN (VALUES ('BL2607001', 'SOPCOV-2607001'), ('BL2607002', 'SOPCOV-2607002'),
             ('BL2607003', 'SOPCOV-2607001'), ('BL2607004', 'SOPCOV-2607004')) b(NumeroBL, NumeroCommande) ON b.NumeroBL = d.NumeroBL
WHERE d.NumeroCommande IS NULL;

-- ─────────────────────────── 4. Traçabilité de l'OF OF2607ASS02 ───────────────────────────
-- N° du rapport de contrôle sur les étiquettes déjà scannées par le magasinier (vides seulement).
UPDATE l SET QualityControlReport = r.Rapport
FROM dbo.Mag_PreparationOF_Lot l
JOIN dbo.Mag_PreparationOF p ON p.Id = l.PreparationOFId AND p.NumeroOF = 'OF2607ASS02'
JOIN (VALUES ('ECROU-001', 'RC2607001'), ('JOINT-ANTI-SERREUR', 'RC2607002'), ('PISTON-001', 'RC2607003')) r(CodeArticle, Rapport)
  ON r.CodeArticle = l.CodeArticle
WHERE l.QualityControlReport IS NULL;

-- Expédition de l'OF sur le BL2607001 (comme un scan du magasinier : BL puis OF).
IF NOT EXISTS (SELECT 1 FROM dbo.Mag_ExpeditionBL_ScanOF WHERE NumeroOFScanne = 'OF2607ASS02')
   AND EXISTS (SELECT 1 FROM dbo.AUTILIS WHERE USR_0 = '33333')
BEGIN
    DECLARE @expedition UNIQUEIDENTIFIER = NEWID();
    INSERT INTO dbo.Mag_ExpeditionBL (Id, NumeroBL, MatriculeMagasinier, Statut, DateFin)
    VALUES (@expedition, 'BL2607001', '33333', 'TERMINE', SYSDATETIME());
    INSERT INTO dbo.Mag_ExpeditionBL_ScanOF (ExpeditionBLId, NumeroOFScanne) VALUES (@expedition, 'OF2607ASS02');
END

COMMIT TRANSACTION;

SELECT 'PORDER' AS Copie, COUNT(*) AS Lignes FROM dbo.PORDER
UNION ALL SELECT 'SORDER', COUNT(*) FROM dbo.SORDER
UNION ALL SELECT 'SDELIVERY avec commande', COUNT(*) FROM dbo.SDELIVERY WHERE NumeroCommande IS NOT NULL;
PRINT 'Données Sage de démonstration prêtes.';
GO

SET NOEXEC OFF;
GO
