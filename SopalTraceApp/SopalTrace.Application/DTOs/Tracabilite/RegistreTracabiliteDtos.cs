using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SopalTrace.Application.DTOs.Tracabilite
{
    public class RegistreTracabiliteDto
    {
        /// <summary>OF clôturé : registre en lecture seule.</summary>
        public bool EstTermine { get; set; }
        public List<RegistreTracabiliteRowDto> LignesHistorique { get; set; } = new();
        public List<ComposantDisponibleDto> ComposantsDisponibles { get; set; } = new();

        /// <summary>Équipe en production : la seule qui ajoute des lignes et confirme ses lots.</summary>
        public string? EquipeSaisie { get; set; }
        public bool PeutSaisir { get; set; }
        public string? MotifLectureSeule { get; set; }

        /// <summary>L'équipe en production a confirmé ses lots (sa tâche « registre de traçabilité » est faite).</summary>
        public bool EquipeAConfirme { get; set; }
        public List<ConfirmationTracabiliteDto> Confirmations { get; set; } = new();
    }

    public class ConfirmationTracabiliteDto
    {
        public string? Equipe { get; set; }
        public string? PosteCode { get; set; }
        public DateTime? Date { get; set; }
        public string? MatriculeOperateur { get; set; }
    }

    public class RegistreTracabiliteRowDto
    {
        public Guid Id { get; set; }
        public DateTime DateHeure { get; set; }
        public int Version { get; set; }
        public string? Corps { get; set; }
        public string? Volant { get; set; }
        public string? Equipe { get; set; }
        public string? PosteCode { get; set; }
        public string? MatriculeOperateur { get; set; }
        /// <summary>Ligne annulée (erreur de saisie) : gardée dans l'historique avec son motif.</summary>
        public bool Annulee { get; set; }
        public DateTime? AnnuleeLe { get; set; }
        public string? AnnuleePar { get; set; }
        public string? MotifAnnulation { get; set; }
        public List<ComposantSelectionneDto> Composants { get; set; } = new();
    }

    public class ComposantSelectionneDto
    {
        /// <summary>Article du composant (obligatoire en saisie) ; la désignation n'est qu'un libellé.</summary>
        [StringLength(30)]
        public string? CodeArticle { get; set; }
        public string DesignationComposant { get; set; } = string.Empty;
        [StringLength(100)]
        public string? LotSelectionne { get; set; }
    }

    public class ComposantDisponibleDto
    {
        public string DesignationComposant { get; set; } = null!;
        public string CodeArticle { get; set; } = null!;
        public List<string> LotsDisponibles { get; set; } = new();
    }

    public class AddRegistreTracabiliteDto
    {
        public Guid ExecControleOfId { get; set; }
        /// <summary>Poste de la tablette (doit être affecté à l'OF).</summary>
        [Required, StringLength(30)]
        public string PosteCode { get; set; } = string.Empty;
        [StringLength(100)]
        public string? Corps { get; set; }
        [StringLength(100)]
        public string? Volant { get; set; }
        public List<ComposantSelectionneDto> Composants { get; set; } = new();
    }

    public class AnnulerLigneTracabiliteDto
    {
        [Required, StringLength(250, MinimumLength = 3)]
        public string Raison { get; set; } = string.Empty;
    }
}
