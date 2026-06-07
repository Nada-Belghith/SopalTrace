using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.Execution
{
    public class ExecEncfDto
    {
        public Guid? Id { get; set; }
        public string? NumeroOf { get; set; }
        public string? OperationCode { get; set; }
        public string? PosteCode { get; set; }
        public string? MachineCode { get; set; }
        public int NumEquipe { get; set; }
        public Guid PlanSourceId { get; set; }
        public string? TypePlan { get; set; }
        public string? Statut { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }

        // Additional information from OF / Article for UI
        public string? CodeArticle { get; set; }
        public string? DesignationArticle { get; set; }
        public string? Atelier { get; set; }

        public List<ExecPieceTypeDto> PiecesTypes { get; set; } = new List<ExecPieceTypeDto>();
        public List<ExecControleTrancheDto> Tranches { get; set; } = new List<ExecControleTrancheDto>();
    }

    public class ExecPieceTypeDto
    {
        public Guid? Id { get; set; }
        public Guid ExecControleOFId { get; set; }
        public DateTime HeureValidation { get; set; }
        public string? Resultat { get; set; } // 'C' or 'NC'
        public string? Remarque { get; set; }
        public string? MatriculeOperateur { get; set; }
    }

    public class ExecControleTrancheDto
    {
        public Guid? Id { get; set; }
        public Guid ExecControleOFId { get; set; }
        public string? TrancheHoraire { get; set; }
        public DateTime HeureDebut { get; set; }
        public DateTime HeureFin { get; set; }
        
        // This maps to the Resultat C/NC in the grid
        public string? ResultatFinal { get; set; } // 'C' or 'NC'
        
        public string? DetailsNC { get; set; }
        public string? ActionsCorrection { get; set; }
        public string? MatriculeApprobateur { get; set; }
    }
}
