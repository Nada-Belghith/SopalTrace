using System.ComponentModel.DataAnnotations.Schema;

namespace SopalTrace.Domain.Entities;

public partial class PlanProduitFiniSection
{
    [NotMapped]
    public string? RegleEchantillonnageLibelle { get; set; }
}
