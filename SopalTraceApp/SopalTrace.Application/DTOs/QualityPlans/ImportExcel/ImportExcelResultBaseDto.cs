using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.ImportExcel;

public abstract class ImportExcelResultBaseDto
{
    public List<string> Erreurs { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int LignesLues { get; set; } = 0;
    public int LignesIgnorees { get; set; } = 0;
    public bool Succes { get; set; } = true;
}
