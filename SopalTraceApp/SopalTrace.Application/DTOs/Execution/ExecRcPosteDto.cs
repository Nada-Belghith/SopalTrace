using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.Execution
{
    public class ExecRcPosteDto
    {
        public Guid ExecControleDocumentStatutId { get; set; }
        public Guid ExecControleOfId { get; set; }
        public string? PosteCode { get; set; }
        public string? Equipe { get; set; }
        
        // Plan data for display
        public string? PlanNom { get; set; }
        public string? PlanRemarques { get; set; }
        public string? PlanLegendeMoyens { get; set; }
        public string? ConfigurationColonnesJson { get; set; }

        public List<ExecRcPosteLigneDto> Lignes { get; set; } = new List<ExecRcPosteLigneDto>();
        public List<ExecRcPosteReponseDto> Reponses { get; set; } = new List<ExecRcPosteReponseDto>();
        public ExecRcPosteBilanDto Bilan { get; set; }
        
        public List<ExecRcPosteSessionInfoDto> AvailableSessions { get; set; } = new List<ExecRcPosteSessionInfoDto>();
    }

    public class ExecRcPosteSessionInfoDto
    {
        public Guid StatutId { get; set; }
        public DateTime? DateExecution { get; set; }
        public string? Equipe { get; set; }
        public bool EstTermine { get; set; }
    }

    public class ExecRcPosteLigneDto
    {
        public Guid DocLigneId { get; set; }
        public string? MachineCodeCtrlPoste { get; set; }
        public Guid? RisqueDefautId { get; set; }
        public string? LibelleAffiche { get; set; }
        public int OrdreAffiche { get; set; }
        
        public List<ExecRcPosteHeureDto> Heures { get; set; } = new List<ExecRcPosteHeureDto>();
    }

    public class ExecRcPosteHeureDto
    {
        public Guid? Id { get; set; }
        public Guid DocLigneId { get; set; }
        public string TrancheHoraire { get; set; }
        public double NbNcParHeure { get; set; }
        public string? MatriculeOp { get; set; }
    }

    public class ExecRcPosteReponseDto
    {
        public Guid? Id { get; set; }
        public string TrancheHoraire { get; set; }
        public double TotalNcHeure { get; set; }
        public double TotalRealiseHeure { get; set; }
    }

    public class ExecRcPosteBilanDto
    {
        public Guid? Id { get; set; }
        public double TotalDefauts { get; set; }
        public double TotalPiecesTestees { get; set; }
        public double TauxNc { get; set; }
        public double NbPiecesRebutees { get; set; }
        public double NbPieceConforme { get; set; }
    }
}
