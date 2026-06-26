-- Drop UNIQUE constraint on Modele_Fabrication_Entete.Code
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_ModeleFabricationEntete_Code' AND object_id = OBJECT_ID('Modele_Fabrication_Entete'))
BEGIN
    ALTER TABLE Modele_Fabrication_Entete DROP CONSTRAINT UQ_ModeleFabricationEntete_Code;
    -- Or if it's an index: DROP INDEX UQ_ModeleFabricationEntete_Code ON Modele_Fabrication_Entete;
END
GO

-- Drop UNIQUE constraint on Plan_Fabrication_Entete.CodeArticleSageVersionne (if exists)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_PlanFabricationEntete_Code' AND object_id = OBJECT_ID('Plan_Fabrication_Entete'))
BEGIN
    ALTER TABLE Plan_Fabrication_Entete DROP CONSTRAINT UQ_PlanFabricationEntete_Code;
END
GO
