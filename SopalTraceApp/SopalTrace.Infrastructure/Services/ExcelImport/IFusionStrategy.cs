using ClosedXML.Excel;
using System.Collections.Generic;

namespace SopalTrace.Infrastructure.Services.ExcelImport;

public class LigneLogique
{
    public IXLRow Principale { get; set; } = null!;
    public List<Dictionary<int, string>> SousLignes { get; set; } = new();
}

public interface IFusionStrategy
{
    string MachineCode { get; }
    List<LigneLogique> Regrouper(List<IXLRow> lignesPhysiques, int startRow, int endRow, int colRefIndex);
}
