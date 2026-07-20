using System;

namespace SopalTrace.Application.DTOs.Execution
{
    public class ExecEchantillonnageDto
    {
        public Guid Id { get; set; }
        public Guid ExecControleDocumentStatutId { get; set; }
        public Guid ExecControleOfId { get; set; }
        public int TailleLot { get; set; }
        public int NbPostesB { get; set; }
        public string LettreCode { get; set; } = null!;
        public int EffectifEchantillonA { get; set; }
        public int? EffectifParPosteAb { get; set; }
        public int CritereAcceptationAc { get; set; }
        public int CritereRejetRe { get; set; }
        
        // Paramètres du plan
        public string? NiveauControle { get; set; }
        public string? PosteCode { get; set; }
        public string? TypePlan { get; set; }
        public string? ModeControle { get; set; }
        public double? NqaValeur { get; set; }

        // En-tête du document
        public string? CodeArticle { get; set; }
        public string? Designation { get; set; }
        public string? NumeroOf { get; set; }
        public string? Atelier { get; set; }
        public DateTime? DateFabrication { get; set; }
        public DateTime? DateEchantillonnage { get; set; }
        public string? CodeMachine { get; set; }
        public bool EstTermine { get; set; }

        public System.Collections.Generic.List<string> InstrumentCodes { get; set; } = new System.Collections.Generic.List<string>();
    }
}
