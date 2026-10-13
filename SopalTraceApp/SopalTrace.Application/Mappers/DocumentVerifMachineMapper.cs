using SopalTrace.Application.DTOs.QualityPlans.DocumentVerifMachines;
using SopalTrace.Domain.Constants;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class DocumentVerifMachineMapper
{
    public static DocumentEntete ToEntity(CreateDocumentVerifMachineRequestDto dto, string user, Guid? formulaireId = null)
    {
        var entete = new DocumentEntete
        {
            Id = Guid.NewGuid(),
            TypeDocumentCode = TypesDocument.VerifMachine,
            MachineCode = dto.MachineCode,
            Nom = dto.Nom,
            Version = dto.VersionInitiale ?? 1,
            Statut = DocumentStatuts.Brouillon,
            CreePar = user,
            CreeLe = DateTime.Now,
            Remarques = dto.Remarques,
            LegendeMoyens = dto.LegendeMoyens,
            FormulaireId = formulaireId
        };

        var oldFamilleIdToNewIdMap = new Dictionary<Guid, Guid>();

        foreach (var familleDto in dto.Familles)
        {
            var newFamilleId = Guid.NewGuid();
            oldFamilleIdToNewIdMap[familleDto.Id] = newFamilleId;

            entete.DocumentVerifMachineFamilles.Add(new DocumentVerifMachineFamille
            {
                Id = newFamilleId,
                EnteteId = entete.Id,
                RefFamilleCorpsId = familleDto.RefFamilleCorpsId,
                OrdreAffiche = familleDto.OrdreAffiche
            });
        }

        var allLignes = dto.LignesConformite.Concat(dto.LignesRisques);

        foreach (var ligneDto in allLignes)
        {
            var ligne = new DocumentLigne
            {
                Id = Guid.NewGuid(),
                EnteteId = entete.Id,
                OrdreAffiche = ligneDto.OrdreAffiche,
                TypeLigne = ligneDto.TypeLigne,
                LibelleAffiche = ligneDto.LibelleRisque,
                Instruction = ligneDto.LibelleMethode
            };

            foreach (var extraDto in ligneDto.ExtraColonnes)
            {
                ligne.DocumentLigneExtraColonnes.Add(new DocumentLigneExtraColonne
                {
                    Id = Guid.NewGuid(),
                    LigneId = ligne.Id,
                    CleColonne = extraDto.CleColonne,
                    ValeurColonne = extraDto.ValeurColonne,
                    OrdreAffiche = extraDto.OrdreAffiche
                });
            }

            foreach (var echDto in ligneDto.Echeances)
            {
                var ech = new DocumentVerifMachineEcheance
                {
                    Id = Guid.NewGuid(),
                    LigneId = ligne.Id,
                    OrdreAffiche = echDto.OrdreAffiche,
                    PeriodiciteMachineId = echDto.PeriodiciteMachineId,
                    RefMoyenDetectionId = echDto.RefMoyenDetectionId
                };

                foreach (var matDto in echDto.MatricePieces)
                {
                    ech.DocumentVerifMachineMatricePieces.Add(new DocumentVerifMachineMatricePiece
                    {
                        Id = Guid.NewGuid(),
                        EcheanceId = ech.Id,
                        FamilleId = matDto.FamilleId.HasValue && oldFamilleIdToNewIdMap.ContainsKey(matDto.FamilleId.Value) 
                                        ? oldFamilleIdToNewIdMap[matDto.FamilleId.Value] 
                                        : (Guid?)null,
                        RoleVerif = matDto.RoleVerif,
                        PieceRefId = matDto.PieceRefId
                    });
                }
                
                ligne.DocumentVerifMachineEcheances.Add(ech);
            }

            entete.DocumentLignes.Add(ligne);
        }

        return entete;
    }

    public static DocumentVerifMachineEnteteDto ToDto(DocumentEntete entity)
    {
        return new DocumentVerifMachineEnteteDto
        {
            Id = entity.Id,
            RowVersion = VersionsDocument.EnTexte(entity.RowVersion),
            MachineCode = entity.MachineCode ?? string.Empty,
            Nom = entity.Nom,
            Version = entity.Version,
            Statut = entity.Statut,
            CreePar = entity.CreePar,
            CreeLe = entity.CreeLe,
            FormulaireId = entity.FormulaireId,
            Remarques = entity.Remarques,
            LegendeMoyens = entity.LegendeMoyens,
            AfficheConformite = entity.DocumentLignes.Any(l => l.TypeLigne == TypesLigneVerifMachine.Conformite),
            AfficheFamilles = entity.DocumentVerifMachineFamilles.Any(),
            AfficheMoyenDetectionRisques = entity.DocumentLignes
                .Where(l => l.TypeLigne == TypesLigneVerifMachine.Risque)
                .SelectMany(l => l.DocumentVerifMachineEcheances)
                .Any(e => e.RefMoyenDetectionId.HasValue),
            AfficheFuiteEtalon = entity.DocumentLignes
                .SelectMany(l => l.DocumentVerifMachineEcheances)
                .SelectMany(e => e.DocumentVerifMachineMatricePieces)
                .Any(m => m.RoleVerif == "FEC" || m.RoleVerif == "FENC"),
            ConfigurationColonnesJson = null,
            Familles = entity.DocumentVerifMachineFamilles.Select(f => new DocumentVerifMachineFamilleDto
            {
                Id = f.Id,
                RefFamilleCorpsId = f.RefFamilleCorpsId,
                OrdreAffiche = f.OrdreAffiche
            }).ToList(),
            Lignes = entity.DocumentLignes.Select(l => new DocumentVerifMachineLigneDto
            {
                Id = l.Id,
                OrdreAffiche = l.OrdreAffiche,
                TypeLigne = l.TypeLigne ?? string.Empty,
                LibelleRisque = l.LibelleAffiche ?? string.Empty,
                LibelleMethode = l.Instruction,
                ExtraColonnes = l.DocumentLigneExtraColonnes.Select(ec => new DocumentVerifMachineExtraColonneDto
                {
                    Id = ec.Id,
                    CleColonne = ec.CleColonne,
                    ValeurColonne = ec.ValeurColonne,
                    OrdreAffiche = ec.OrdreAffiche
                }).ToList(),
                Echeances = l.DocumentVerifMachineEcheances.Select(e => new DocumentVerifMachineEcheanceDto
                {
                    Id = e.Id,
                    OrdreAffiche = e.OrdreAffiche,
                    PeriodiciteMachineId = e.PeriodiciteMachineId,
                    RefMoyenDetectionId = e.RefMoyenDetectionId,
                    MatricePieces = e.DocumentVerifMachineMatricePieces.Select(m => new DocumentVerifMachineMatricePieceDto
                    {
                        Id = m.Id,
                        FamilleId = m.FamilleId,
                        RoleVerif = m.RoleVerif ?? string.Empty,
                        PieceRefId = m.PieceRefId
                    }).ToList()
                }).ToList()
            }).ToList()
        };
    }
}
