using System.ComponentModel.DataAnnotations.Schema;

namespace SopalTrace.Domain.Entities;

public partial class PlanPfSection
{
    [NotMapped]
    public string? RegleEchantillonnageLibelle { get; set; }
}
