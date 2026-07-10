using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Application.Alertes;

public class SuperviseurNotifsAlerteDefinition : IAlerteDefinition<NotifsSuperviseurContexte>
{
    private readonly IErpService _erpService;

    public SuperviseurNotifsAlerteDefinition(IErpService erpService)
    {
        _erpService = erpService;
    }

    public string TypeAlerte => "NOTIFS_3_RETARD";

    // 24 heures de délai anti-spam pour ne pas bombarder le superviseur
    public TimeSpan FenetreAntiSpam => TimeSpan.FromHours(24);

    public string ExtraireCleEntite(NotifsSuperviseurContexte contexte) => contexte.ExecControleOfId.ToString();

    public async Task<List<string>> ResoudreDestinatairesAsync(NotifsSuperviseurContexte contexte)
    {
        return await _erpService.GetEmailsByRoleAsync("SUPERVISEUR_PROD");
    }

    public string ConstruireSujet(NotifsSuperviseurContexte contexte)
    {
        return $"[URGENT] 3 Notifications sans réponse pour l'OF {contexte.NumeroOf} - Opération {contexte.OperationCode}";
    }

    public string ConstruireCorps(NotifsSuperviseurContexte contexte)
    {
        var h = contexte.DureeDepuisPremierRetard.Hours;
        var m = contexte.DureeDepuisPremierRetard.Minutes;
        var dureeStr = h > 0 ? $"{h} heures et {m} minutes" : (m > 0 ? $"{m} minutes" : "moins d'une minute");

        return $@"
        <h2>Alerte de Contrôle en Retard</h2>
        <p>Bonjour,</p>
        <p>L'Ordre de Fabrication <b>{contexte.NumeroOf}</b> pour l'article <b>{contexte.ArticleCode}</b> à l'opération <b>{contexte.OperationCode}</b> présente actuellement <b>3</b> occurrences de contrôle sans aucune réponse de l'opérateur.</p>
        <p>Le premier contrôle manqué est en retard depuis : <b>{dureeStr}</b>.</p>
        <p>Merci d'intervenir auprès du poste de travail pour vérifier la situation.</p>
        <br/>
        <p><i>Ceci est un message automatique, merci de ne pas y répondre.</i></p>";
    }
}
