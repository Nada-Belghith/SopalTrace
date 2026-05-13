using System.ComponentModel.DataAnnotations.Schema;

namespace SopalTrace.Domain.Entities;

public partial class PlanAssSection
{
    [NotMapped]
    public string? RegleEchantillonnageLibelle { get; set; }
}
