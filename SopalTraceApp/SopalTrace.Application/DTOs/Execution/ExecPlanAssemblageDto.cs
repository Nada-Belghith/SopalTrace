using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SopalTrace.Application.DTOs.Execution
{
    /// <summary>
    /// Plan d'assemblage d'un poste vu par l'équipe active : seules les sections de lot s'y saisissent
    /// (LOT_POSTE : première pièce à chaque équipe, dernière pièce en fin de poste ; LOT_OF : première pièce
    /// pour la première équipe de l'OF, dernière pièce à la clôture de l'OF). Les contrôles de production
    /// (ECHANTILLONNAGE_2859) passent par les occurrences.
    /// </summary>
    public class ExecPlanAssemblageDto
    {
        public Guid ExecControleOfId { get; set; }
        public string? NumeroOf { get; set; }
        public string? CodeArticle { get; set; }
        public string? DesignationArticle { get; set; }
        public string PosteCode { get; set; } = string.Empty;
        public string? Statut { get; set; }
        public DateTime DateDebut { get; set; }
        public string? LibellePlan { get; set; }

        /// <summary>La session active est la première de l'OF : le contrôle de début d'OF lui revient.</summary>
        public bool PremiereEquipe { get; set; }

        /// <summary>FIN_POSTE ou FIN_OF lorsque l'équipe a demandé la fin ; null en production.</summary>
        public string? DemandeCloture { get; set; }

        public List<ExecPlanAssemblageSectionDto> Sections { get; set; } = new();

        /// <summary>Sections contrôlées quand l'opérateur le décide : réglage (REGLAGE, REGLAGE_PROD) et toutes les pièces (TOUS_PIECES).</summary>
        public List<SectionALaDemandeDto> SectionsALaDemande { get; set; } = new();
    }

    public class SectionALaDemandeDto
    {
        public Guid Id { get; set; }
        public string? Libelle { get; set; }
        public string TypeSection { get; set; } = string.Empty;
        public int Ordre { get; set; }
        /// <summary>Valeur à envoyer comme moment de saisie : REGLAGE ou 100PCT.</summary>
        public string Contexte { get; set; } = string.Empty;
        public List<ExecPlanAssemblageLignePlanDto> LignesPlan { get; set; } = new();
        /// <summary>Dernier contrôle complet de l'équipe active (non requis : l'opérateur choisit le moment).</summary>
        public ControleLotDto DernierControle { get; set; } = new();
        public int NombreControlesEquipe { get; set; }
    }

    public class ExecPlanAssemblageSectionDto
    {
        public Guid Id { get; set; }
        public string? Libelle { get; set; }
        /// <summary>LOT_POSTE ou LOT_OF.</summary>
        public string TypeSection { get; set; } = string.Empty;
        public int Ordre { get; set; }

        /// <summary>Lignes du plan d'assemblage (caractéristiques à contrôler).</summary>
        public List<ExecPlanAssemblageLignePlanDto> LignesPlan { get; set; } = new();

        /// <summary>Contrôle de la première pièce (début de poste / début d'OF).</summary>
        public ControleLotDto PremierePiece { get; set; } = new();

        /// <summary>Contrôle de la dernière pièce (fin de poste / fin d'OF).</summary>
        public ControleLotDto DernierePiece { get; set; } = new();
    }

    /// <summary>État d'un contrôle de lot : attendu maintenant, fait (toutes les lignes saisies) et son résultat.</summary>
    public class ControleLotDto
    {
        public bool Requis { get; set; }
        public bool Fait { get; set; }
        /// <summary>"C", "NC" ou null si non fait.</summary>
        public string? Resultat { get; set; }
        public DateTime? DateSaisie { get; set; }
        public string? Equipe { get; set; }
        /// <summary>Dernière réponse de chaque ligne déjà saisie (même si le contrôle n'est pas complet).</summary>
        public List<ReponseLigneLotDto> Lignes { get; set; } = new();
    }

    public class ReponseLigneLotDto
    {
        public Guid LigneId { get; set; }
        /// <summary>"C" ou "NC".</summary>
        public string Resultat { get; set; } = string.Empty;
        public string? Nc { get; set; }
        public string? ActionCorrective { get; set; }
    }

    public class ExecPlanAssemblageLignePlanDto
    {
        public Guid Id { get; set; }
        public int Numero { get; set; }
        public string? Caracteristique { get; set; }
        public string? LimiteSpecTexte { get; set; }
        public string? TypeControle { get; set; }
        public string? MoyenControle { get; set; }
        public string? Instrument { get; set; }
        public string? Observations { get; set; }
        public string? ImageBase64 { get; set; }
        public Dictionary<string, string> ExtraColonnes { get; set; } = new();
    }

    /// <summary>Saisie du contrôle d'une section : toutes ses caractéristiques, une fois chacune.</summary>
    public class SaveResultatAssRequest
    {
        [Required, StringLength(30)]
        public string PosteCode { get; set; } = null!;

        public Guid SectionId { get; set; }

        /// <summary>PREMIERE_PIECE ou DERNIERE_PIECE (sections de lot), REGLAGE ou 100PCT (contrôles à la demande).</summary>
        [Required, RegularExpression("^(PREMIERE_PIECE|DERNIERE_PIECE|REGLAGE|100PCT)$", ErrorMessage = "Le moment du contrôle doit valoir PREMIERE_PIECE, DERNIERE_PIECE, REGLAGE ou 100PCT.")]
        public string Moment { get; set; } = null!;

        [Required, MinLength(1, ErrorMessage = "Au moins une ligne de contrôle est requise."), MaxLength(500)]
        public List<SaveResultatAssLigneDto> LignesControle { get; set; } = new();
    }

    public class SaveResultatAssLigneDto
    {
        public Guid LigneId { get; set; }

        [Required, RegularExpression("^(C|NC)$", ErrorMessage = "Le résultat d'une ligne doit valoir C ou NC.")]
        public string Resultat { get; set; } = null!;

        [StringLength(500)]
        public string? Nc { get; set; }

        [StringLength(500)]
        public string? ActionCorrective { get; set; }
    }
}
