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
    
    /// <summary>Équipes ouvertes sur l'OF (plusieurs à la fois, une par poste), avec les postes qu'elles tiennent.</summary>
    public List<EquipeOfAssemblageDto> Equipes { get; set; } = new();

    /// <summary>Assemblage clôturé (OF en consultation seule, visible quelques jours au tableau de bord).</summary>
    public DateTime? DateCloture { get; set; }

    /// <summary>
    /// Postes déjà enregistrés dans Exec_ControleOf_Poste (vide si non commencé)
    /// </summary>
    public List<string> PostesExistants { get; set; } = new();
}

/// <summary>Équipe ouverte sur un OF d'assemblage.</summary>
public class EquipeOfAssemblageDto
{
    public string Equipe { get; set; } = null!;
    public bool EnPause { get; set; }
    /// <summary>L'équipe clôture l'OF : aucune autre équipe ne peut commencer.</summary>
    public bool ClotureDemandee { get; set; }
    /// <summary>Postes que l'équipe tient (non terminés).</summary>
    public List<string> Postes { get; set; } = new();
}
