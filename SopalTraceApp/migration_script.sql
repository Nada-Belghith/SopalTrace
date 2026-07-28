-- Migration Script: Migrate CodeReference to FormulaireId for ColonneDef and Equipe

BEGIN TRANSACTION;

-- 1. Add FormulaireId columns (nullable initially)
ALTER TABLE [Ref_Formulaire_ColonneDef] ADD [FormulaireId] UNIQUEIDENTIFIER NULL;
ALTER TABLE [Ref_Formulaire_Equipe] ADD [FormulaireId] UNIQUEIDENTIFIER NULL;

-- 2. Migrate data: Link to the ACTIF version of the Formulaire for each CodeReference
-- (Older versions will lose their columns, but since they were sharing them previously, it's consistent)
UPDATE c
SET c.[FormulaireId] = f.[Id]
FROM [Ref_Formulaire_ColonneDef] c
INNER JOIN [Ref_Formulaire] f ON c.[CodeReference] = f.[CodeReference]
WHERE f.[Statut] = 'ACTIF';

UPDATE e
SET e.[FormulaireId] = f.[Id]
FROM [Ref_Formulaire_Equipe] e
INNER JOIN [Ref_Formulaire] f ON e.[CodeReference] = f.[CodeReference]
WHERE f.[Statut] = 'ACTIF';

-- 3. Delete orphaned rows (where FormulaireId is still NULL, i.e., no ACTIF form found)
-- Or you can choose to keep them and fix manually, but EF Core requires it to be non-null.
DELETE FROM [Ref_Formulaire_ColonneDef] WHERE [FormulaireId] IS NULL;
DELETE FROM [Ref_Formulaire_Equipe] WHERE [FormulaireId] IS NULL;

-- 4. Alter columns to be NOT NULL
ALTER TABLE [Ref_Formulaire_ColonneDef] ALTER COLUMN [FormulaireId] UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE [Ref_Formulaire_Equipe] ALTER COLUMN [FormulaireId] UNIQUEIDENTIFIER NOT NULL;

-- 5. Drop old CodeReference columns
ALTER TABLE [Ref_Formulaire_ColonneDef] DROP COLUMN [CodeReference];
ALTER TABLE [Ref_Formulaire_Equipe] DROP COLUMN [CodeReference];

-- 6. Add Foreign Key Constraints
ALTER TABLE [Ref_Formulaire_ColonneDef]  WITH CHECK ADD  CONSTRAINT [FK_Ref_Formulaire_ColonneDef_Ref_Formulaire_FormulaireId] FOREIGN KEY([FormulaireId])
REFERENCES [Ref_Formulaire] ([Id])
ON DELETE CASCADE;

ALTER TABLE [Ref_Formulaire_ColonneDef] CHECK CONSTRAINT [FK_Ref_Formulaire_ColonneDef_Ref_Formulaire_FormulaireId];

ALTER TABLE [Ref_Formulaire_Equipe]  WITH CHECK ADD  CONSTRAINT [FK_Ref_Formulaire_Equipe_Ref_Formulaire_FormulaireId] FOREIGN KEY([FormulaireId])
REFERENCES [Ref_Formulaire] ([Id])
ON DELETE CASCADE;

ALTER TABLE [Ref_Formulaire_Equipe] CHECK CONSTRAINT [FK_Ref_Formulaire_Equipe_Ref_Formulaire_FormulaireId];

COMMIT TRANSACTION;
