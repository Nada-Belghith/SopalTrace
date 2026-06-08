using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.PlanRCCF
{
    public class PlanRccfDto
    {
        public Guid Id { get; set; }
        public string PosteCode { get; set; } = null!;
        public string Nom { get; set; } = null!;
        public int Version { get; set; }
        public string Statut { get; set; } = null!;
        public Guid? FormulaireId { get; set; }
        public string? ConfigurationJson { get; set; }
        public string? Remarques { get; set; }
        public string CreePar { get; set; } = null!;
        public DateTime CreeLe { get; set; }
        public string? ModifiePar { get; set; }
        public DateTime? ModifieLe { get; set; }
        public List<PlanRccfSectionDto> Sections { get; set; } = new List<PlanRccfSectionDto>();
    }

    public class PlanRccfSectionDto
    {
        public Guid Id { get; set; }
        public Guid PlanRCCFEnteteId { get; set; }
        public string SectionType { get; set; } = null!;
        public string? LibelleAffiche { get; set; }
        public int OrdreAffiche { get; set; }
        public List<PlanRccfLigneDto> Lignes { get; set; } = new List<PlanRccfLigneDto>();
    }

    public class PlanRccfLigneDto
    {
        public Guid Id { get; set; }
        public Guid SectionId { get; set; }
        public string Caracteristique { get; set; } = null!;
        public int OrdreAffiche { get; set; }
        
        public Guid? TypeControleId { get; set; }
        public string? InstrumentCode { get; set; }
        public string? LimiteSpecTexte { get; set; }
        public string? Observations { get; set; }
        public Guid? MoyenControleId { get; set; }
    }

    public class CreatePlanRccfRequest
    {
        public string PosteCode { get; set; } = null!;
        public Guid? FormulaireId { get; set; }
        public string Nom { get; set; } = null!;
        public string? Remarques { get; set; }
        public string? ConfigurationJson { get; set; }
        public List<CreatePlanRccfSectionRequest> Sections { get; set; } = new List<CreatePlanRccfSectionRequest>();
    }

    public class CreatePlanRccfSectionRequest
    {
        public string SectionType { get; set; } = null!;
        public string? LibelleAffiche { get; set; }
        public int OrdreAffiche { get; set; }
        public List<CreatePlanRccfLigneRequest> Lignes { get; set; } = new List<CreatePlanRccfLigneRequest>();
    }

    public class CreatePlanRccfLigneRequest
    {
        public string Caracteristique { get; set; } = null!;
        public int OrdreAffiche { get; set; }
        
        public Guid? TypeControleId { get; set; }
        public string? InstrumentCode { get; set; }
        public string? LimiteSpecTexte { get; set; }
        public string? Observations { get; set; }
        public Guid? MoyenControleId { get; set; }
    }

    public class UpdatePlanRccfRequest
    {
        public string Nom { get; set; } = null!;
        public Guid? FormulaireId { get; set; }
        public string? Remarques { get; set; }
        public string? ConfigurationJson { get; set; }
        public List<CreatePlanRccfSectionRequest> Sections { get; set; } = new List<CreatePlanRccfSectionRequest>();
    }
}
