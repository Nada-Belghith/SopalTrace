using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class OfAssemblageStatutDto
{
    public string NumeroOf { get; set; } = null!;
    public string? DesignationArticle { get; set; }
    public string? CodeArticle { get; set; }
    public double? QuantiteLancee { get; set; }
    public DateTime? DateDebut { get; set; }
    
    /// <summary>
    /// null = NON_COMMENCE, "EN_COURS" = déjà démarré
    /// </summary>
    public string? Statut { get; set; }
    
    /// <summary>
    /// Id de l'ExecControleOf si déjà démarré, sinon null
    /// </summary>
    public Guid? ExecControleOfId { get; set; }
    
    /// <summary>
    /// Postes déjà enregistrés dans Exec_ControleOf_Poste (vide si non commencé)
    /// </summary>
    public List<string> PostesExistants { get; set; } = new();
}
