using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.ImportExcel;

public class ImportVerifMachineExcelResultDto : ImportExcelResultBaseDto
{
    public string MachineCode { get; set; } = string.Empty;
    public string NomPlan { get; set; } = string.Empty;
    public List<ImportVerifMachineLigneDto> LignesConformite { get; set; } = new();
    public List<ImportVerifMachineLigneDto> LignesRisques { get; set; } = new();
    public List<string> Familles { get; set; } = new(); // NOUVEAU : Liste des codes/libellés de colonnes (familles)
    public string Remarques { get; set; } = string.Empty;
    public string LegendeMoyens { get; set; } = string.Empty;
}

public class ImportVerifMachineLigneDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LibelleRisque { get; set; } = string.Empty;
    public string LibelleMethode { get; set; } = string.Empty;
    public Dictionary<string, string> ColonnesSupplementaires { get; set; } = new();
    public List<ImportVerifMachineEcheanceDto> Echeances { get; set; } = new();
}

public class ImportVerifMachineEcheanceDto
{
    public string PeriodiciteLibelle { get; set; } = string.Empty;
    public Guid? PeriodiciteId { get; set; }
    // 🟢 NOUVEAU : Liste des Rows (Moyens) sous cette périodicité
    public List<ImportVerifMachineRowDto> Rows { get; set; } = new();
}

public class ImportVerifMachineRowDto
{
    public string MoyenDetectionLibelle { get; set; } = string.Empty;
    public Guid? MoyenDetectionId { get; set; }
    public List<ImportVerifMachineMatriceDto> MatricePieces { get; set; } = new();
}

public class ImportVerifMachineMatriceDto
{
    public string PieceRefCode { get; set; } = string.Empty;
    public Guid? PieceRefId { get; set; }
    public string RoleVerif { get; set; } = string.Empty; // PRC, PRNC, FEC, FENC
    public string FamilleCode { get; set; } = string.Empty; // NOUVEAU : Lie la pièce à une colonne famille
}
