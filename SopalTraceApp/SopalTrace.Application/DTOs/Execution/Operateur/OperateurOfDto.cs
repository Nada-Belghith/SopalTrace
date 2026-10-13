namespace SopalTrace.Application.DTOs.Execution.Operateur;
using System;
using System.Collections.Generic;

public class OperateurOfDto
{
    public string NumeroOf { get; set; } = null!;
    public string StatutOf { get; set; } = null!;
    public string CodeArticle { get; set; } = null!;
    public string DesignationArticle { get; set; } = null!;
    public decimal QuantitePrevue { get; set; }
    public decimal QuantiteLancee { get; set; }
    public DateTime? DateDebut { get; set; }
    
    public List<OperateurOperationDto> GammeOperatoire { get; set; } = new();
}

public class OperateurOperationDto
{
    public string OperationCode { get; set; } = null!;
    public string Libelle { get; set; } = null!;
    public string MachinePrevueCode { get; set; } = null!;
    /// <summary>L'opération s'exécute sur une machine (à choisir au démarrage).</summary>
    public bool AvecMachine { get; set; }
    /// <summary>Longueur et diamètre nominaux à saisir au démarrage.</summary>
    public bool AvecCotesTronconnage { get; set; }

    public Guid? ActiveExecControleOfId { get; set; }
    public string? ActiveExecStatut { get; set; }
    public string? ActiveMachineCode { get; set; }
    public bool? EstEnReglage { get; set; }
    public bool? A_Des_Controles_Reglage { get; set; }
    
    public string? LegendeMoyens { get; set; }
    public string? Remarques { get; set; }
}
