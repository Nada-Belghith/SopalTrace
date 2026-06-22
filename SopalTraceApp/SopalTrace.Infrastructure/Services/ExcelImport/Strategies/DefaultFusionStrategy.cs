using ClosedXML.Excel;
using SopalTrace.Application.Interfaces;
using System.Collections.Generic;

namespace SopalTrace.Infrastructure.Services.ExcelImport.Strategies;

public class DefaultFusionStrategy : IFusionStrategy
{
    public string MachineCode => "DEFAULT";

    public List<LigneLogique> Regrouper(List<IXLRow> lignesPhysiques, int startRow, int endRow, int colRefIndex)
    {
        var result = new List<LigneLogique>();
        LigneLogique currentLigne = null;

        for (int i = startRow; i <= endRow; i++)
        {
            if (i >= lignesPhysiques.Count) break;
            var row = lignesPhysiques[i];

            var cellRef = row.Cell(colRefIndex);
            bool isNewGroup = !string.IsNullOrWhiteSpace(cellRef.GetString());

            if (isNewGroup || currentLigne == null)
            {
                currentLigne = new LigneLogique { Principale = row };
                result.Add(currentLigne);
            }
            else
            {
                var dict = new Dictionary<int, string>();
                int lastCol = lignesPhysiques.Max(r => r.LastCellUsed()?.Address.ColumnNumber ?? 1);
                
                for (int colIdx = 1; colIdx <= lastCol; colIdx++)
                {
                    var cell = row.Cell(colIdx);
                    string valeur = string.Empty;

                    if (cell.IsMerged())
                        valeur = cell.MergedRange().FirstCell().GetString().Trim();
                    else
                        valeur = cell.GetString().Trim();

                    dict[colIdx] = valeur;
                }
                currentLigne.SousLignes.Add(dict);
            }
        }

        return result;
    }
}
