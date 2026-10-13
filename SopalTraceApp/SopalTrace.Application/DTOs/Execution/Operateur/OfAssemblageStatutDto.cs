using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

public class OfAssemblageStatutDto
{
    public string NumeroOf { get; set; } = null!;
    public string? DesignationArticle { get; set; }
    public string? CodeArticle { get; set; }
    public decimal? QuantiteLancee { get; set; }
    public DateTime? DateDebut { get; set; }
    
    /// <summary>
    /// null = non commencé, sinon statut de l'exécution (EN_COURS, EN_PAUSE, REGLAGE)
    /// </summary>
    public string? Statut { get; set; }
    
    /// <summary>
    /// Id de l'ExecControleOf si déjà démarré, sinon null
    /// </summary>
    public Guid? ExecControleOfId { get; set; }
    
    /// <summary>Équipe qui a la main sur l'OF (en production, sinon la dernière mise en pause).</summary>
    public string? EquipeActuelle { get; set; }

    /// <summary>Équipes en pause sur l'OF : elles reprendront leur étape à leur retour.</summary>
    public List<string> EquipesEnPause { get; set; } = new();

    /// <summary>
    /// Postes déjà enregistrés dans Exec_ControleOf_Poste (vide si non commencé)
    /// </summary>
    public List<string> PostesExistants { get; set; } = new();
}
