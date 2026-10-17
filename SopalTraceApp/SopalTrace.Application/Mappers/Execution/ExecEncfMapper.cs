using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Domain.Entities;
using System.Linq;

namespace SopalTrace.Application.Mappers.Execution
{
    public static class ExecEncfMapper
    {
        /// <param name="entity">Exécution de l'OF (pièces types et résultats par tranche).</param>
        /// <param name="statut">Statut ENCF du poste sur cette exécution : fournit le poste.</param>
        /// <param name="planId">Plan dont la fiche affiche les lignes (plan suivi par l'OF, plan d'assemblage du poste).</param>
        public static ExecEncfDto ToDto(ExecControleOf entity, ExecControleDocumentStatut? statut, Guid? planId)
        {
            return new ExecEncfDto
            {
                Id = entity.Id,
                NumeroOf = entity.NumeroOf,
                OperationCode = entity.OperationCode,
                PosteCode = statut?.PosteCode ?? entity.ExecControleOfPostes?.FirstOrDefault()?.PosteCode,
                MachineCode = entity.MachineCode,
                // Plan dont les lignes sont affichées par le formulaire ENCF
                PlanSourceId = planId,
                TypeOf = entity.TypeOf,
                Statut = entity.Statut,
                DateDebut = entity.DateDebut,
                DateFin = entity.DateFin,
                
                // Info OF if available
                CodeArticle = entity.NumeroOfNavigation?.CodeArticle,
                DesignationArticle = entity.NumeroOfNavigation?.CodeArticleNavigation?.Designation,
                Atelier = entity.ExecControleOfPostes?.FirstOrDefault()?.PosteCodeNavigation?.Libelle,

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
                }).OrderBy(t => t.HeureDebut).ToList() ?? new System.Collections.Generic.List<ExecControleTrancheDto>(),
                
                PiecesTypes = entity.ExecPieceTypes?.Select(p => new ExecPieceTypeDto
                {
                    Id = p.Id,
                    ExecControleOFId = p.ExecControleOfid,
                    HeureValidation = p.HeureValidation,
                    Resultat = p.Resultat,
                    Remarque = p.Remarque,
                    MatriculeOperateur = p.MatriculeOperateur
                }).OrderBy(p => p.HeureValidation).ToList() ?? new System.Collections.Generic.List<ExecPieceTypeDto>()
            };
        }
    }
}
