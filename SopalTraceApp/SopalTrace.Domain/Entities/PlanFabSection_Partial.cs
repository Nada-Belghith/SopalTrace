using System.ComponentModel.DataAnnotations.Schema;

namespace SopalTrace.Domain.Entities;

public partial class PlanFabricationSection
{
    [NotMapped]
    public string? RegleEchantillonnageLibelle { get; set; }
}
