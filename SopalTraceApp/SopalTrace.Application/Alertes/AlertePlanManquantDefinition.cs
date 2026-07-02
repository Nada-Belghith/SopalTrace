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
        return await _unitOfWork.UserRepository.GetEmailsByRoleAsync("RESPONSABLE_DI");
    }

    public string ConstruireSujet(PlanManquantContexte contexte)
    {
        return $"[Alerte] Plan manquant pour l'opération {contexte.OperationCode}, le poste/machine {contexte.PosteCode} et l'article {contexte.ArticleCode}";
    }

    public string ConstruireCorps(PlanManquantContexte contexte)
    {
        return $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #d9534f;'>Alerte : Plan manquant signalé</h2>
    <p>L'opérateur <b>{contexte.NomOperateur}</b> a signalé un plan manquant lors de sa saisie.</p>
    <ul>
        <li><b>Opération :</b> {contexte.OperationCode}</li>
        <li><b>Poste / Machine :</b> {contexte.PosteCode}</li>
        <li><b>Article :</b> {contexte.ArticleCode}</li>
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
        return $"{contexte.OperationCode}_{contexte.PosteCode}_{contexte.ArticleCode}";
    }
}
