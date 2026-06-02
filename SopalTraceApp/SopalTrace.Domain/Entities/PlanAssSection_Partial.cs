using System.ComponentModel.DataAnnotations.Schema;

namespace SopalTrace.Domain.Entities;

public partial class PlanAssemblageSection
{
    [NotMapped]
    public string? RegleEchantillonnageLibelle { get; set; }
}
