using System.ComponentModel.DataAnnotations.Schema;

namespace SopalTrace.Domain.Entities;

public partial class PlanFabSection
{
    [NotMapped]
    public string? RegleEchantillonnageLibelle { get; set; }
}
