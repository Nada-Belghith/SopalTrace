using SopalTrace.Application.DTOs.QualityPlans.PlanVerifMachines;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;

namespace SopalTrace.Application.Mappers;

public static class PlanVerifMachineMapper
{
    public static PlanVerifMachineEntete ToEntity(CreatePlanVerifMachineRequestDto dto, string user, Guid? formulaireId = null)
    {
        var entete = new PlanVerifMachineEntete
        {
            Id = Guid.NewGuid(),
            MachineCode = dto.MachineCode,
            Nom = dto.Nom,
            Version = dto.VersionInitiale ?? 1,
            Statut = "BROUILLON",
            CreePar = user,
            CreeLe = DateTime.UtcNow,
            Remarques = dto.Remarques,
            LegendeMoyens = dto.LegendeMoyens,
            FormulaireId = formulaireId
        };

        var oldFamilleIdToNewIdMap = new Dictionary<Guid, Guid>();

        foreach (var familleDto in dto.Familles)
        {
            var newFamilleId = Guid.NewGuid();
            oldFamilleIdToNewIdMap[familleDto.Id] = newFamilleId;

            entete.PlanVerifMachineFamilles.Add(new PlanVerifMachineFamille
            {
                Id = newFamilleId,
                PlanEnteteId = entete.Id,
                RefFamilleCorpsId = familleDto.RefFamilleCorpsId,
                OrdreAffiche = familleDto.OrdreAffiche
            });
        }

        var allLignes = dto.LignesConformite.Concat(dto.LignesRisques);

        foreach (var ligneDto in allLignes)
        {
            var ligne = new PlanVerifMachineLigne
            {
                Id = Guid.NewGuid(),
                PlanEnteteId = entete.Id,
                OrdreAffiche = ligneDto.OrdreAffiche,
                TypeLigne = ligneDto.TypeLigne,
                LibelleRisque = ligneDto.LibelleRisque,
                LibelleMethode = ligneDto.LibelleMethode
            };

            foreach (var extraDto in ligneDto.ExtraColonnes)
            {
                ligne.PlanVerifMachineLigneExtraColonnes.Add(new PlanVerifMachineLigneExtraColonne
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
                var ech = new PlanVerifMachineEcheance
                {
                    Id = Guid.NewGuid(),
                    PlanLigneId = ligne.Id,
                    OrdreAffiche = echDto.OrdreAffiche,
                    PeriodiciteId = echDto.PeriodiciteId,
                    RefMoyenDetectionId = echDto.RefMoyenDetectionId
                };

                foreach (var matDto in echDto.MatricePieces)
                {
                    ech.PlanVerifMachineMatricePieces.Add(new PlanVerifMachineMatricePiece
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
                
                ligne.PlanVerifMachineEcheances.Add(ech);
            }

            entete.PlanVerifMachineLignes.Add(ligne);
        }

        return entete;
    }

    public static PlanVerifMachineEnteteDto ToDto(PlanVerifMachineEntete entity)
    {
        return new PlanVerifMachineEnteteDto
        {
            Id = entity.Id,
            MachineCode = entity.MachineCode,
            Nom = entity.Nom,
            Version = entity.Version,
            Statut = entity.Statut,
            CreePar = entity.CreePar,
            CreeLe = entity.CreeLe,
            FormulaireId = entity.FormulaireId,
            Remarques = entity.Remarques,
            LegendeMoyens = entity.LegendeMoyens,
            AfficheConformite = entity.PlanVerifMachineLignes.Any(l => l.TypeLigne == "CONFORMITE"),
            AfficheFamilles = entity.PlanVerifMachineFamilles.Any(),
            AfficheMoyenDetectionRisques = entity.PlanVerifMachineLignes
                .Where(l => l.TypeLigne == "RISQUE")
                .SelectMany(l => l.PlanVerifMachineEcheances)
                .Any(e => e.RefMoyenDetectionId.HasValue),
            AfficheFuiteEtalon = entity.PlanVerifMachineLignes
                .SelectMany(l => l.PlanVerifMachineEcheances)
                .SelectMany(e => e.PlanVerifMachineMatricePieces)
                .Any(m => m.RoleVerif == "FEC" || m.RoleVerif == "FENC"),
            ConfigurationColonnesJson = null,
            Familles = entity.PlanVerifMachineFamilles.Select(f => new PlanVerifMachineFamilleDto
            {
                Id = f.Id,
                RefFamilleCorpsId = f.RefFamilleCorpsId,
                OrdreAffiche = f.OrdreAffiche
            }).ToList(),
            Lignes = entity.PlanVerifMachineLignes.Select(l => new PlanVerifMachineLigneDto
            {
                Id = l.Id,
                OrdreAffiche = l.OrdreAffiche,
                TypeLigne = l.TypeLigne,
                LibelleRisque = l.LibelleRisque,
                LibelleMethode = l.LibelleMethode,
                ExtraColonnes = l.PlanVerifMachineLigneExtraColonnes.Select(ec => new PlanVerifMachineExtraColonneDto
                {
                    Id = ec.Id,
                    CleColonne = ec.CleColonne,
                    ValeurColonne = ec.ValeurColonne,
                    OrdreAffiche = ec.OrdreAffiche
                }).ToList(),
                Echeances = l.PlanVerifMachineEcheances.Select(e => new PlanVerifMachineEcheanceDto
                {
                    Id = e.Id,
                    OrdreAffiche = e.OrdreAffiche,
                    PeriodiciteId = e.PeriodiciteId,
                    RefMoyenDetectionId = e.RefMoyenDetectionId,
                    MatricePieces = e.PlanVerifMachineMatricePieces.Select(m => new PlanVerifMachineMatricePieceDto
                    {
                        Id = m.Id,
                        FamilleId = m.FamilleId,
                        RoleVerif = m.RoleVerif,
                        PieceRefId = m.PieceRefId
                    }).ToList()
                }).ToList()
            }).ToList()
        };
    }
}
