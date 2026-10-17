using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Services;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Domain.Constants;
using static SopalTrace.Application.Rapports.Execution.FormatsRapport;

namespace SopalTrace.Application.Rapports.Execution;

/// <summary>
/// Fiche d'échantillonnage d'un poste de l'OF (FE0591) : plan ISO 2859-1 appliqué (niveau, type de plan, mode de
/// contrôle, NQA, cochés « X ») et effectifs calculés à l'ouverture du poste. Valeurs lues par le même service que
/// l'écran de l'opérateur.
/// </summary>
public sealed class DefinitionRapportEchantillonnage : IDefinitionRapportExecution
{
    private readonly IExecEchantillonnageService _echantillonnage;
    private readonly IDonneesExecutionRepository _donnees;

    public DefinitionRapportEchantillonnage(IExecEchantillonnageService echantillonnage, IDonneesExecutionRepository donnees)
    {
        _echantillonnage = echantillonnage;
        _donnees = donnees;
    }

    public string TypeExecution => TypesDocumentExecution.Echantillonnage;

    public IReadOnlyList<string> ChampsObligatoires { get; } = new[] { "LettreCode", "EffectifEchantillon" };

    public IReadOnlyList<ChampRapport> Champs { get; } = new List<ChampRapport>
    {
        new("CodeArticle", "Article de l'OF", "25B2A01-1-1"),
        new("Designation", "Désignation de l'article", "Robinet gaz avec soupape"),
        new("NumeroOf", "Numéro de l'OF", "OF2607ASS02"),
        new("Atelier", "Atelier", "ASS"),
        new("Poste", "Poste de la fiche", "PAS71"),
        new("DateFabrication", "Date de début de l'OF", "14/10/2026"),
        new("DateEchantillonnage", "Date de la fiche", "14/10/2026"),
        new("CodeMachine", "Machine", "BEE46"),
        new("Instruments", "Codes des instruments de mesure", "PC-01, CAL-02"),
        new("TailleLot", "Effectif du lot (quantité de l'OF)", "5000"),
        new("NiveauI", "« X » si niveau de contrôle I", "X"),
        new("NiveauII", "« X » si niveau de contrôle II", ""),
        new("NiveauIII", "« X » si niveau de contrôle III", ""),
        new("NiveauControle", "Niveau de contrôle (code)", "II"),
        new("PlanSimple", "« X » si plan d'échantillonnage simple", "X"),
        new("PlanDouble", "« X » si plan d'échantillonnage double", ""),
        new("ModeReduit", "« X » si contrôle réduit", ""),
        new("ModeNormal", "« X » si contrôle normal", "X"),
        new("ModeRenforce", "« X » si contrôle renforcé", ""),
        new("Nqa", "NQA", "0,65"),
        new("LettreCode", "Lettre code", "L"),
        new("EffectifEchantillon", "Effectif de l'échantillon global (A)", "200"),
        new("NombrePostes", "Nombre de postes prévus (B)", "2"),
        new("EffectifParPoste", "Effectif de l'échantillon par poste (A/B)", "100"),
        new("CritereAcceptation", "Critère d'acceptation", "0"),
        new("CritereRejet", "Critère de rejet", "1"),
        new("Equipe", "Équipe qui a rempli la fiche", "E1"),
        new("Operateur", "Nom de l'opérateur qui a rempli la fiche", "Ali Ben Salah"),
        new("Matricule", "Matricule de l'opérateur", "55555"),
        new("CodeFormulaire", "Code du formulaire du plan d'échantillonnage", "FE0591"),
        new("VersionFormulaire", "Version du formulaire", "0"),
    };

    public string? TypeDocumentApplique => TypesDocument.PlanEchantillonnage;

    public Task<Guid?> FormulaireFicheViergeAsync(Guid documentEnteteId) => _donnees.GetFormulaireDocumentAsync(documentEnteteId);

    public async Task<DonneesExecution?> ChargerViergeAsync(Guid documentEnteteId, FormulaireDuDocument? formulaire)
    {
        var statut = await _donnees.GetStatutViergeAsync(documentEnteteId, TypeExecution, formulaire);
        var parametres = statut == null ? null : await _donnees.GetParametresEchantillonnageAsync(documentEnteteId);
        if (statut == null || parametres == null) return null;
        return new DonneesExecution(ConstruireVierge(parametres, statut), TexteFichier.Nettoyer($"FicheVierge_Echantillonnage_{statut.NomDocument}"));
    }

    /// <summary>
    /// Fiche vierge : paramètres du plan (niveau, plan, sévérité, NQA, Ac, Re) ; taille du lot, lettre code et effectifs
    /// vides (ils dépendent de la quantité de chaque OF).
    /// </summary>
    public static DonneesRapport ConstruireVierge(ParametresEchantillonnageRapport parametres, StatutExecutionRapport statut)
    {
        var donnees = Construire(new ExecEchantillonnageDto
        {
            NiveauControle = parametres.NiveauControle,
            TypePlan = parametres.TypePlan,
            ModeControle = parametres.ModeControle,
            NqaValeur = parametres.Nqa,
            CritereAcceptationAc = parametres.CritereAcceptation,
            CritereRejetRe = parametres.CritereRejet,
            LettreCode = ""
        }, statut, null);
        foreach (var champ in new[] { "TailleLot", "EffectifEchantillon", "NombrePostes", "EffectifParPoste", "LettreCode" })
            donnees.Champs[champ] = "";
        return donnees;
    }

    public async Task<DonneesExecution?> ChargerAsync(Guid execControleDocumentStatutId)
    {
        var statut = await _donnees.GetStatutAsync(execControleDocumentStatutId);
        if (statut == null || statut.TypeDocument != TypeExecution) return null;
        var fiche = await _echantillonnage.GetPlanPourOfAsync(statut.ExecControleOfId, statut.PosteCode);
        if (fiche == null || fiche.ExecControleDocumentStatutId != statut.Id) return null;

        var noms = statut.MatriculeOperateur == null ? new Dictionary<string, string>() : await _donnees.GetNomsAsync(new[] { statut.MatriculeOperateur });
        var donnees = Construire(fiche, statut, statut.MatriculeOperateur == null ? null : noms.GetValueOrDefault(statut.MatriculeOperateur));
        return new DonneesExecution(donnees, TexteFichier.Nettoyer($"Echantillonnage_{statut.NumeroOf}_{statut.PosteCode}"));
    }

    /// <summary>Valeurs de la fiche (sans accès aux données).</summary>
    public static DonneesRapport Construire(ExecEchantillonnageDto fiche, StatutExecutionRapport statut, string? nomOperateur)
    {
        var donnees = new DonneesRapport();
        var c = donnees.Champs;
        var niveau = Texte(fiche.NiveauControle).ToUpperInvariant();
        var plan = Texte(fiche.TypePlan).ToUpperInvariant();
        var mode = Texte(fiche.ModeControle).ToUpperInvariant();

        c["CodeArticle"] = Texte(fiche.CodeArticle ?? statut.CodeArticle);
        c["Designation"] = Texte(fiche.Designation ?? statut.DesignationArticle);
        c["NumeroOf"] = Texte(fiche.NumeroOf ?? statut.NumeroOf);
        c["Atelier"] = Texte(fiche.Atelier);
        c["Poste"] = Texte(fiche.PosteCode ?? statut.PosteCode);
        c["DateFabrication"] = Date(fiche.DateFabrication);
        c["DateEchantillonnage"] = Date(fiche.DateEchantillonnage);
        c["CodeMachine"] = Texte(fiche.CodeMachine);
        c["Instruments"] = string.Join(", ", fiche.InstrumentCodes.Where(i => !string.IsNullOrWhiteSpace(i)));
        c["TailleLot"] = Nombre(fiche.TailleLot);
        c["NiveauI"] = Croix(niveau == "I");
        c["NiveauII"] = Croix(niveau == "II");
        c["NiveauIII"] = Croix(niveau == "III");
        c["NiveauControle"] = niveau;
        c["PlanSimple"] = Croix(plan == "SIMPLE");
        c["PlanDouble"] = Croix(plan == "DOUBLE");
        c["ModeReduit"] = Croix(mode == "REDUIT");
        c["ModeNormal"] = Croix(mode == "NORMAL");
        c["ModeRenforce"] = Croix(mode == "RENFORCE");
        c["Nqa"] = fiche.NqaValeur?.ToString("0.###", Francais) ?? "";
        c["LettreCode"] = Texte(fiche.LettreCode);
        c["EffectifEchantillon"] = Nombre(fiche.EffectifEchantillonA);
        c["NombrePostes"] = Nombre(fiche.NbPostesB);
        c["EffectifParPoste"] = fiche.EffectifParPosteAb.HasValue ? Nombre(fiche.EffectifParPosteAb.Value) : "";
        c["CritereAcceptation"] = Nombre(fiche.CritereAcceptationAc);
        c["CritereRejet"] = Nombre(fiche.CritereRejetRe);
        c["Equipe"] = Texte(statut.Equipe);
        c["Operateur"] = Texte(nomOperateur);
        c["Matricule"] = Texte(statut.MatriculeOperateur);
        c["CodeFormulaire"] = Texte(statut.CodeFormulaire);
        c["VersionFormulaire"] = statut.VersionFormulaire?.ToString(Francais) ?? "";
        return donnees;
    }
}
