using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.ImportExcel;

public class ImportExcelResultDto
{
    public List<ImportExcelSectionDto> Sections { get; set; } = new();
}

public class ImportExcelSectionDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nom { get; set; } = string.Empty;
    public string FrequenceLibelle { get; set; } = string.Empty;
    public Guid? TypeSectionId { get; set; }
    public Guid? PeriodiciteId { get; set; }
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
    public decimal? ValeurNominale { get; set; }
    public decimal? ToleranceSuperieure { get; set; }
    public decimal? ToleranceInferieure { get; set; }
    public string Observations { get; set; } = string.Empty;
    public bool EstCritique { get; set; } = false;
    public string Unite { get; set; } = string.Empty;
}
