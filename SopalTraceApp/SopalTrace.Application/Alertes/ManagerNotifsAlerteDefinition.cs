using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.Interfaces;

namespace SopalTrace.Application.Alertes;

public class ManagerNotifsAlerteDefinition : IAlerteDefinition<NotifsManagerContexte>
{
    private readonly IErpService _erpService;

    public ManagerNotifsAlerteDefinition(IErpService erpService)
    {
        _erpService = erpService;
    }

    public string TypeAlerte => "NOTIFS_10_RETARD";

    // 24 heures de délai anti-spam
    public TimeSpan FenetreAntiSpam => TimeSpan.FromHours(24);

    public string ExtraireCleEntite(NotifsManagerContexte contexte) => contexte.ExecControleOfId.ToString();

    public async Task<List<string>> ResoudreDestinatairesAsync(NotifsManagerContexte contexte)
    {
        return await _erpService.GetEmailsByRoleAsync("MANAGER_PROD");
    }

    public string ConstruireSujet(NotifsManagerContexte contexte)
    {
        return $"[CRITIQUE] 10 Notifications ignorées pour l'OF {contexte.NumeroOf} - Opération {contexte.OperationCode}";
    }

    public string ConstruireCorps(NotifsManagerContexte contexte)
    {
        var h = contexte.DureeDepuisPremierRetard.Hours;
        var m = contexte.DureeDepuisPremierRetard.Minutes;
        var dureeStr = h > 0 ? $"{h} heures et {m} minutes" : (m > 0 ? $"{m} minutes" : "moins d'une minute");

        return $@"
        <h2>Alerte de Contrôle Critique</h2>
        <p>Bonjour,</p>
        <p>Nous vous informons d'une situation critique sur l'Ordre de Fabrication <b>{contexte.NumeroOf}</b> pour l'article <b>{contexte.ArticleCode}</b> à l'opération <b>{contexte.OperationCode}</b>.</p>
        <p>Actuellement, <b>10</b> occurrences de contrôle sont sans réponse. Le premier manquement date de <b>{dureeStr}</b>.</p>
        <p>Cette situation nécessite une attention managériale immédiate pour s'assurer de la qualité de la production en cours.</p>
        <br/>
        <p><i>Ceci est un message automatique, merci de ne pas y répondre.</i></p>";
    }
}
