using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Domain.Entities;
using System.Linq;

namespace SopalTrace.Application.Mappers.Execution
{
    public static class ExecEncfMapper
    {
        public static ExecEncfDto? ToDto(ExecControleOf? entity)
        {
            if (entity == null) return null;

            return new ExecEncfDto
            {
                Id = entity.Id,
                NumeroOf = entity.NumeroOf,
                OperationCode = entity.OperationCode,
                PosteCode = entity.PosteCode,
                MachineCode = entity.MachineCode,
                NumEquipe = entity.NumEquipe,
                PlanSourceId = entity.PlanSourceId,
                TypePlan = entity.TypePlan,
                Statut = entity.Statut,
                DateDebut = entity.DateDebut,
                DateFin = entity.DateFin,
                
                // Info OF if available
                CodeArticle = entity.NumeroOfNavigation?.CodeArticle,
                DesignationArticle = entity.NumeroOfNavigation?.CodeArticleNavigation?.Designation,
                Atelier = entity.PosteCodeNavigation?.Libelle,

                PiecesTypes = entity.ExecPieceTypes?.Select(p => new ExecPieceTypeDto
                {
                    Id = p.Id,
                    ExecControleOFId = p.ExecControleOfid,
                    HeureValidation = p.HeureValidation,
                    Resultat = p.Resultat,
                    Remarque = p.Remarque,
                    MatriculeOperateur = p.MatriculeOperateur
                }).ToList() ?? new System.Collections.Generic.List<ExecPieceTypeDto>(),

                Tranches = entity.ExecControleTranches?.Select(t => new ExecControleTrancheDto
                {
                    Id = t.Id,
                    ExecControleOFId = t.ExecControleOfid,
                    TrancheHoraire = t.TrancheHoraire,
                    HeureDebut = t.HeureDebut,
                    HeureFin = t.HeureFin,
                    ResultatFinal = t.ResultatFinal,
                    DetailsNC = t.DetailsNc,
                    ActionsCorrection = t.ActionsCorrection,
                    MatriculeApprobateur = t.MatriculeApprobateur
                }).OrderBy(t => t.HeureDebut).ToList() ?? new System.Collections.Generic.List<ExecControleTrancheDto>()
            };
        }

        public static ExecControleOf? ToEntity(ExecEncfDto? dto)
        {
            if (dto == null) return null;

            return new ExecControleOf
            {
                Id = dto.Id ?? System.Guid.NewGuid(),
                NumeroOf = dto.NumeroOf ?? string.Empty,
                OperationCode = dto.OperationCode ?? string.Empty,
                PosteCode = dto.PosteCode ?? string.Empty,
                MachineCode = dto.MachineCode,
                NumEquipe = dto.NumEquipe,
                PlanSourceId = dto.PlanSourceId,
                TypePlan = dto.TypePlan ?? "FAB",
                Statut = dto.Statut ?? "EN_COURS",
                DateDebut = dto.DateDebut != default ? dto.DateDebut : System.DateTime.Now,
                DateFin = dto.DateFin
            };
        }

        public static ExecPieceType? ToEntity(ExecPieceTypeDto? dto)
        {
            if (dto == null) return null;

            return new ExecPieceType
            {
                Id = dto.Id ?? System.Guid.NewGuid(),
                ExecControleOfid = dto.ExecControleOFId,
                HeureValidation = dto.HeureValidation,
                Resultat = dto.Resultat ?? string.Empty,
                Remarque = dto.Remarque,
                MatriculeOperateur = dto.MatriculeOperateur ?? string.Empty
            };
        }

        public static ExecControleTranche? ToEntity(ExecControleTrancheDto? dto)
        {
            if (dto == null) return null;

            return new ExecControleTranche
            {
                Id = dto.Id ?? System.Guid.NewGuid(),
                ExecControleOfid = dto.ExecControleOFId,
                TrancheHoraire = dto.TrancheHoraire ?? string.Empty,
                HeureDebut = dto.HeureDebut,
                HeureFin = dto.HeureFin,
                ResultatFinal = dto.ResultatFinal ?? string.Empty,
                DetailsNc = dto.DetailsNC,
                ActionsCorrection = dto.ActionsCorrection,
                MatriculeApprobateur = dto.MatriculeApprobateur
            };
        }
    }
}
