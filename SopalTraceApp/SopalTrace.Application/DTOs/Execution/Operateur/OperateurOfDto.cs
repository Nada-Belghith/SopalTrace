namespace SopalTrace.Application.DTOs.Execution.Operateur;
using System;
using System.Collections.Generic;

public class OperateurOfDto
{
    public string NumeroOf { get; set; } = null!;
    public string StatutOf { get; set; } = null!;
    public string CodeArticle { get; set; } = null!;
    public string DesignationArticle { get; set; } = null!;
    public double QuantitePrevue { get; set; }
    public double QuantiteLancee { get; set; }
    public DateTime? DateDebut { get; set; }
    
    public Guid? ActiveExecControleOfId { get; set; }
    public string? ActiveExecStatut { get; set; }
    public string? ActiveOperationCode { get; set; }
    public string? ActiveMachineCode { get; set; }

    public List<OperateurOperationDto> GammeOperatoire { get; set; } = new();
}

public class OperateurOperationDto
{
    public string OperationCode { get; set; } = null!;
    public string Libelle { get; set; } = null!;
    public string MachinePrevueCode { get; set; } = null!;
}
