using System;

#nullable enable

namespace SopalTrace.Application.DTOs.QualityPlans.Referentiels;

public class RefFormulaireDto
{
    public Guid Id { get; set; }
    public string CodeReference { get; set; } = null!;
    public string Designation { get; set; } = null!;
    public int Version { get; set; }
    public string Statut { get; set; } = null!;
    public DateTime CreeLe { get; set; }
    public string? Role { get; set; }
    public string? ConfigurationStructureJson { get; set; }
}

public class UpdateRefFormulaireDto
{
    public string? ConfigurationStructureJson { get; set; }
    public string ModifiePar { get; set; } = "ADMIN";
}

public class NouvelleVersionRefFormulaireDto
{
    public Guid AncienId { get; set; }
    public string? ConfigurationStructureJson { get; set; }
    public string ModifiePar { get; set; } = "ADMIN";
    public string MotifModification { get; set; } = string.Empty;
}
