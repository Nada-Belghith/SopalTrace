using ClosedXML.Excel;
using SopalTrace.Application.Interfaces;
using System.Collections.Generic;

namespace SopalTrace.Infrastructure.Services.ExcelImport.Strategies;

public class MasFusionStrategy : IFusionStrategy
{
    public string MachineCode => "MAS";

    public List<LigneLogique> Regrouper(List<IXLRow> lignesPhysiques, int startRow, int endRow, int colRefIndex)
    {
        var result = new List<LigneLogique>();
        
        // MAS: 1 ligne physique = 1 ligne logique
        for (int i = startRow; i <= endRow; i++)
        {
            if (i >= lignesPhysiques.Count) break;
            var row = lignesPhysiques[i];
            
            result.Add(new LigneLogique { Principale = row });
        }

        return result;
    }
}
