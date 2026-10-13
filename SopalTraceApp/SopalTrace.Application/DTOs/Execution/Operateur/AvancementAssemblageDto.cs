using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SopalTrace.Application.DTOs.Execution.Operateur;

/// <summary>
/// Avancement d'une session d'assemblage pour un poste, calculé par le serveur : toutes les tablettes
/// affichent la même étape, et la clôture n'est acceptée que si les tâches de fin sont faites.
/// </summary>
public class AvancementAssemblageDto
{
    public Guid ExecControleOfId { get; set; }
    public string NumeroOf { get; set; } = string.Empty;
    public string? CodeArticle { get; set; }
    public string? DesignationArticle { get; set; }
    public string? FamilleProduitFiniCode { get; set; }
    public string? TypeRobinetCode { get; set; }
    public string StatutOf { get; set; } = string.Empty;

    /// <summary>Valeur de PhasesAssemblage.</summary>
    public string Phase { get; set; } = string.Empty;
    public string? Equipe { get; set; }
    /// <summary>L'équipe a démarré ce poste.</summary>
    public bool PosteDemarre { get; set; }
    /// <summary>Premier démarrage du poste sur l'OF : contrôle « début d'OF » attendu.</summary>
    public bool PremierDemarragePoste { get; set; }
    /// <summary>FIN_POSTE (fin de ce poste demandée) ou FIN_OF (clôture de l'OF demandée par l'équipe).</summary>
    public string? DemandeCloture { get; set; }
    /// <summary>Équipe qui tient ce poste (null s'il est libre).</summary>
    public string? EquipeTitulaire { get; set; }
    public DateTime? EnPauseDepuis { get; set; }
    public bool PauseDejeunerPrise { get; set; }

    /// <summary>Réglage en cours sur le poste : contrôles de production suspendus jusqu'à un réglage conforme.</summary>
    public bool ReglageEnCours { get; set; }



    public string PosteCode { get; set; } = string.Empty;

    /// <summary>Les quatre étapes du parcours (échantillonnage, démarrage, production, fin) et leur état.</summary>
    public List<EtapeAssemblageDto> Etapes { get; set; } = new();

    /// <summary>Tâches de l'étape courante pour le poste demandé.</summary>
    public List<TacheAssemblageDto> Taches { get; set; } = new();

    /// <summary>Contrôles que l'opérateur déclenche quand il le juge utile (réglage, toutes les pièces), selon le plan du poste.</summary>
    public List<TacheAssemblageDto> ControlesALaDemande { get; set; } = new();

    /// <summary>Tâches bloquantes restantes de l'étape courante, poste par poste.</summary>
    public List<PosteAvancementDto> Postes { get; set; } = new();

    /// <summary>En fin de poste : tâches de fin du poste faites ; en fin d'OF : celles de tous les postes.</summary>
    public bool PeutTerminer { get; set; }
    /// <summary>Ce qui manque pour terminer (« PAS71 : Vérification machine fin de poste »).</summary>
    public List<string> Manquants { get; set; } = new();
}

public class EtapeAssemblageDto
{
    /// <summary>ECHANTILLONNAGE, DEMARRAGE, PRODUCTION ou FIN.</summary>
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    /// <summary>FAIT, EN_COURS ou A_VENIR.</summary>
    public string Etat { get; set; } = string.Empty;
}

public class TacheAssemblageDto
{
    /// <summary>Valeur de TachesAssemblage (ECHANTILLONNAGE, VERIF_MACHINE, PLAN_PREMIERE_PIECE, REGLAGE…).</summary>
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    /// <summary>Valeur de EtatsTache.</summary>
    public string Etat { get; set; } = string.Empty;
    public string? Detail { get; set; }
    /// <summary>Une tâche bloquante empêche de passer à l'étape suivante (ou de terminer).</summary>
    public bool Bloquante { get; set; }
    /// <summary>Périodicité à ouvrir pour la vérification machine (DEMARRAGE, APRES_PAUSE, FIN_POSTE).</summary>
    public string? Periodicite { get; set; }
    /// <summary>Production : nombre d'occurrences échues non répondues.</summary>
    public int? Compteur { get; set; }
}

public class PosteAvancementDto
{
    /// <summary>L'équipe a démarré ce poste.</summary>
    public bool Demarre { get; set; }
    public string PosteCode { get; set; } = string.Empty;
    /// <summary>Étape du poste pour l'équipe (valeur de PhasesAssemblage).</summary>
    public string Phase { get; set; } = string.Empty;
    /// <summary>Équipe qui tient le poste (null s'il est libre).</summary>
    public string? EquipeTitulaire { get; set; }
    public int TachesRestantes { get; set; }
}

public class DemandeClotureRequest
{
    /// <summary>FIN_POSTE ou FIN_OF.</summary>
    [Required, RegularExpression("^(FIN_POSTE|FIN_OF)$", ErrorMessage = "La demande de fin doit valoir FIN_POSTE ou FIN_OF.")]
    public string Mode { get; set; } = null!;

    /// <summary>Poste dont la fin est demandée (FIN_POSTE) ou depuis lequel la clôture est demandée (FIN_OF).</summary>
    [Required, StringLength(30), RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "Code de poste invalide.")]
    public string PosteCode { get; set; } = null!;

    /// <summary>Équipe de la tablette (par défaut celle qui tient le poste).</summary>
    [StringLength(10)]
    public string? Equipe { get; set; }
}
