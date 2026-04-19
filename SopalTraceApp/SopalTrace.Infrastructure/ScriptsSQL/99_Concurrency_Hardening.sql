-- MODELE FAB: un seul ACTIF par (Type, Nature, Op)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ModeleFab_Actif')
BEGIN
    CREATE UNIQUE INDEX UX_ModeleFab_Actif
    ON dbo.Modele_Fab_Entete(TypeRobinetCode, NatureComposantCode, OperationCode)
    WHERE Statut = 'ACTIF';
END
GO

-- PLAN ASS: un seul ACTIF maître
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanAss_Maitre_Actif')
BEGIN
    CREATE UNIQUE INDEX UX_PlanAss_Maitre_Actif
    ON dbo.Plan_Ass_Entete(OperationCode, TypeRobinetCode)
    WHERE EstModele = 1 AND Statut = 'ACTIF';
END
GO

-- PLAN ASS: un seul ACTIF exception
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanAss_Exception_Actif')
BEGIN
    CREATE UNIQUE INDEX UX_PlanAss_Exception_Actif
    ON dbo.Plan_Ass_Entete(OperationCode, TypeRobinetCode, CodeArticleSage)
    WHERE EstModele = 0 AND Statut = 'ACTIF';
END
GO

-- PLAN FAB: un seul ACTIF par article
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanFab_Actif')
BEGIN
    CREATE UNIQUE INDEX UX_PlanFab_Actif
    ON dbo.Plan_Fab_Entete(CodeArticleSage)
    WHERE Statut = 'ACTIF';
END
GO

-- PLAN VERIF MACHINE: un seul ACTIF avec type
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanVerif_Actif_AvecType')
BEGIN
    CREATE UNIQUE INDEX UX_PlanVerif_Actif_AvecType
    ON dbo.Plan_VerifMachine_Entete(MachineCode, TypeRapport, TypeRobinetCode)
    WHERE TypeRobinetCode IS NOT NULL AND Statut = 'ACTIF';
END
GO

-- PLAN VERIF MACHINE: un seul ACTIF sans type
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanVerif_Actif_SansType')
BEGIN
    CREATE UNIQUE INDEX UX_PlanVerif_Actif_SansType
    ON dbo.Plan_VerifMachine_Entete(MachineCode, TypeRapport)
    WHERE TypeRobinetCode IS NULL AND Statut = 'ACTIF';
END
GO

-- PLAN ECHANTILLONNAGE: un seul ACTIF générique
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanEchan_Actif_Generic')
BEGIN
    CREATE UNIQUE INDEX UX_PlanEchan_Actif_Generic
    ON dbo.Plan_Echantillonnage_Entete(CodeReference)
    WHERE CodeArticleSage IS NULL AND Statut = 'ACTIF';
END
GO

-- PLAN ECHANTILLONNAGE: un seul ACTIF article
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_PlanEchan_Actif_Article')
BEGIN
    CREATE UNIQUE INDEX UX_PlanEchan_Actif_Article
    ON dbo.Plan_Echantillonnage_Entete(CodeReference, CodeArticleSage)
    WHERE CodeArticleSage IS NOT NULL AND Statut = 'ACTIF';
END
GO