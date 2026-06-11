using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.ImportExcel;

public class ImportExcelResultDto
{
    public List<ImportExcelSectionDto> Sections { get; set; } = new();
    public string Remarques { get; set; } = string.Empty;
}

public class ImportExcelSectionDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nom { get; set; } = string.Empty;
    public string LibelleSection { get; set; } = string.Empty;
    public string FrequenceLibelle { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid? TypeSectionId { get; set; }
    public Guid? PeriodiciteId { get; set; }
    public Guid? RegleEchantillonnageId { get; set; }
    public string RegleEchantillonnageLibelle { get; set; } = string.Empty;
    public string ModeFreq { get; set; } = "SANS";
    public int FreqNum { get; set; } = 1;
    public string TypeVariable { get; set; } = "HEURE";
    public int FreqHours { get; set; } = 1;

    public List<ImportExcelLigneDto> Lignes { get; set; } = new();
}

public class ImportExcelLigneDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LibelleAffiche { get; set; } = string.Empty;
    public Guid? TypeCaracteristiqueId { get; set; }
    public Guid? TypeControleId { get; set; }
    public Guid? MoyenControleId { get; set; }
    public string InstrumentCode { get; set; } = string.Empty;
    public string LimiteSpecTexte { get; set; } = string.Empty;
    public string Observations { get; set; } = string.Empty;
    public Dictionary<string, string> ColonnesSupplementaires { get; set; } = new();
    public string? ImageBase64 { get; set; } = null;
}
