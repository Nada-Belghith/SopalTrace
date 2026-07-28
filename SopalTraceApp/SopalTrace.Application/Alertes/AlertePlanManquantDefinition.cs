using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Application.Alertes;

public class AlertePlanManquantDefinition : IAlerteDefinition<PlanManquantContexte>
{
    private readonly IUnitOfWork _unitOfWork;

    public AlertePlanManquantDefinition(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public string TypeAlerte => "PLAN_MANQUANT";

    // Un opérateur ne peut déclencher qu'une seule alerte pour la même opération/poste/article par minute (modifié pour tests)
    public TimeSpan FenetreAntiSpam => TimeSpan.FromMinutes(1);

    public async Task<List<string>> ResoudreDestinatairesAsync(PlanManquantContexte contexte)
    {
        if (string.IsNullOrWhiteSpace(contexte.FamilleArticle))
        {
            contexte.FamilleArticle = await _unitOfWork.DictionnaireQualiteRepository.GetFamilleArticleLibelleAsync(contexte.ArticleCode, contexte.NumeroOf);
        }

        if (contexte.OperationCode == "ASS")
        {
            return await _unitOfWork.UserRepository.GetEmailsByRoleAsync("SUPERVISEUR_QUALITE");
        }
        return await _unitOfWork.UserRepository.GetEmailsByRoleAsync("RESPONSABLE_DI");
    }

    public string ConstruireSujet(PlanManquantContexte contexte)
    {
        var refCible = contexte.OperationCode == "ASS" ? "" : $" et l'article {contexte.ArticleCode}";
        var machineText = string.IsNullOrEmpty(contexte.MachineCode) ? "" : $" / {contexte.MachineCode}";
        return $"[Alerte] Plan manquant pour l'opération {contexte.OperationCode}, le poste/machine {contexte.PosteCode}{machineText}{refCible}";
    }

    public string ConstruireCorps(PlanManquantContexte contexte)
    {
        var articleLigne = contexte.OperationCode == "ASS" 
            ? $"<li><b>OF / Produit :</b> {contexte.NumeroOf ?? contexte.ArticleCode} — {contexte.DesignationArticle ?? string.Empty}</li>" 
            : $"<li><b>Article :</b> {contexte.ArticleCode}</li>";
            
        var familleLigne = !string.IsNullOrWhiteSpace(contexte.FamilleArticle)
            ? $"<li><b>Famille de l'article :</b> {contexte.FamilleArticle}</li>"
            : string.Empty;

        var machineText = string.IsNullOrEmpty(contexte.MachineCode) ? contexte.PosteCode : $"{contexte.PosteCode} / {contexte.MachineCode}";

        return $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #d9534f;'>Alerte : Document manquant signalé</h2>
    <p>L'opérateur <b>{contexte.NomOperateur}</b> a signalé un document manquant lors de sa saisie.</p>
    <ul>
        <li><b>Opération :</b> {contexte.OperationCode}</li>
        <li><b>Poste / Machine :</b> {machineText}</li>
        {articleLigne}
        {familleLigne}
    </ul>
    <p><b>Description du problème / Note de l'opérateur :</b><br/>
    <i>{contexte.DescriptionProbleme}</i></p>
    <br/>
    <p>Merci de vérifier sur la plateforme et de résoudre l'alerte une fois le plan mis à disposition.</p>
</body>
</html>";
    }

    public string ExtraireCleEntite(PlanManquantContexte contexte)
    {
        return $"{contexte.OperationCode}_{contexte.PosteCode}_{contexte.MachineCode}_{contexte.ArticleCode}";
    }
}
