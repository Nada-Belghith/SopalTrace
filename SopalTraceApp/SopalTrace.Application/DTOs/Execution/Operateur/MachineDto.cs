namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class MachineDto
{
    public string CodeMachine { get; set; } = null!;
    public string Libelle { get; set; } = null!;
    public bool HasDemarragePlan { get; set; }
    public bool HasApresPausePlan { get; set; }
    public bool HasFinPostePlan { get; set; }
}
