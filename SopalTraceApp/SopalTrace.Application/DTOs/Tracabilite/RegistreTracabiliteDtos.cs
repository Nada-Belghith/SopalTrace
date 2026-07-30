using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.Tracabilite
{
    public class RegistreTracabiliteDto
    {
        public bool EstTermine { get; set; }
        public List<RegistreTracabiliteRowDto> LignesHistorique { get; set; } = new();
        public List<ComposantDisponibleDto> ComposantsDisponibles { get; set; } = new();
    }

    public class RegistreTracabiliteRowDto
    {
        public Guid Id { get; set; }
        public DateTime DateHeure { get; set; }
        public int Version { get; set; }
        public string? Corps { get; set; }
        public string? Volant { get; set; }
        public List<ComposantSelectionneDto> Composants { get; set; } = new();
    }

    public class ComposantSelectionneDto
    {
        public string DesignationComposant { get; set; } = null!;
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
        public string? Corps { get; set; }
        public string? Volant { get; set; }
        public List<ComposantSelectionneDto> Composants { get; set; } = new();
    }
}
