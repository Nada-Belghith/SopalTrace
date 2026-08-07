using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.Execution
{
    public class ExecPlanAssemblageDto
    {
        public Guid ExecControleOfId { get; set; }
        public string? NumeroOf { get; set; }
        public string? CodeArticle { get; set; }
        public string? DesignationArticle { get; set; }
        public string? Atelier { get; set; }
        public string? Statut { get; set; }
        public DateTime DateDebut { get; set; }
        public int EffectifEchantillonParHeure { get; set; } = 4;
        
        public bool IsFirstExecutionOf { get; set; } = false;
        public bool IsFirstExecutionEquipe { get; set; } = false;

        /// <summary>
        /// Vrai si l'un des documents dédiés au poste (Plan ASS ou Résultat CF) est introuvable.
        /// Dans ce cas, le contrôle ne peut pas démarrer.
        /// </summary>
        public bool HasDocumentsManquants { get; set; } = false;

        /// <summary>
        /// Liste des documents manquants pour ce poste, pour affichage dans l'alerte.
        /// Ex: ["Plan d'Assemblage (PLAN_ASS)", "Résultat Contrôle CF (RESULTAT_CF)"]
        /// </summary>
        public List<string> DocumentsManquants { get; set; } = new List<string>();
        
        public List<PosteHoraireDto> PostesConfigures { get; set; } = new List<PosteHoraireDto>();
        public List<ExecPlanAssemblageSectionDto> Sections { get; set; } = new List<ExecPlanAssemblageSectionDto>();
    }

    public class PosteHoraireDto
    {
        public string PosteCode { get; set; } = string.Empty;
        public string HeureDebut { get; set; } = "08:00";
        public string HeureFin { get; set; } = "14:00";
    }

    public class ExecPlanAssemblageSectionDto
    {
        public Guid Id { get; set; }
        public string? Libelle { get; set; }
        public string? TypeSection { get; set; }
        public int Ordre { get; set; }
        
        // Image 2 : Les lignes du doc d'assemblage (plan de contrôle)
        public List<ExecPlanAssemblageLignePlanDto> LignesPlan { get; set; } = new List<ExecPlanAssemblageLignePlanDto>();
        
        // Lignes provenant du document "Résultat de contrôle" (s'il y en a)
        public List<ExecPlanAssemblageLignePlanDto> LignesResultat { get; set; } = new List<ExecPlanAssemblageLignePlanDto>();

        // Image 3 : Le résultat de contrôle en cours (Tranches)
        public List<ExecPlanAssemblageRowDto> Resultats { get; set; } = new List<ExecPlanAssemblageRowDto>();
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
    }

    public class ExecPlanAssemblageRowDto
    {
        public Guid Id { get; set; } // Tranche Id
        public Guid? LigneId { get; set; }
        public string Frequence { get; set; } = string.Empty; // e.g. "08:00 - 09:00", "Poste 1 - Première pièce (08:00)", "Etanchéité pneumatique"
        public DateTime HeurePrevue { get; set; }
        public string StatutNotif { get; set; } = "A_FAIRE"; // "A_FAIRE", "EN_RETARD", "FAIT", "A_VENIR"
        public string? Resultat { get; set; } // "C", "NC" or null
        public string? NonConformite { get; set; }
        public string? ActionCorrective { get; set; }
        public string? Approbation { get; set; }
        public string? Remarques { get; set; }
    }

    public class ConfigHeuresPosteRequest
    {
        public List<PosteHoraireDto> Postes { get; set; } = new List<PosteHoraireDto>();
    }

    public class SaveResultatAssRequest
    {
        public Guid TrancheId { get; set; }
        public string? Contexte { get; set; }
        public string? Periodicite { get; set; } // demarrage, FIN_POSTE
        public string? Resultat { get; set; }
        public string? NonConformite { get; set; }
        public string? ActionCorrective { get; set; }
        public string? Approbation { get; set; }
        public string? Remarques { get; set; }
        public List<SaveResultatAssLigneDto> LignesControle { get; set; } = new List<SaveResultatAssLigneDto>();
    }

    public class SaveResultatAssLigneDto
    {
        public Guid LigneId { get; set; }
        public int Numero { get; set; }
        public string? Caracteristique { get; set; }
        public string? Resultat { get; set; }
        public string? Nc { get; set; }
        public string? ActionCorrective { get; set; }
    }
}
