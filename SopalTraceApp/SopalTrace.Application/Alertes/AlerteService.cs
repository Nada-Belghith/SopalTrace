using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Alertes;

public class AlerteService<TContexte>
{
    private readonly IAlerteDefinition<TContexte> _definition;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public AlerteService(
        IAlerteDefinition<TContexte> definition,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _definition = definition;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task DeclencherAsync(TContexte contexte)
    {
        var cle = _definition.ExtraireCleEntite(contexte);
        var seuil = DateTime.Now - _definition.FenetreAntiSpam;

        var dejaAlerte = await _unitOfWork.AlerteRepository
            .ExisteNonResolueDepuisAsync(_definition.TypeAlerte, cle, seuil);
            
        if (dejaAlerte) return;

        var destinataires = await _definition.ResoudreDestinatairesAsync(contexte);
        var sujet = _definition.ConstruireSujet(contexte);
        var corps = _definition.ConstruireCorps(contexte);

        if (destinataires.Any()) 
        {
            foreach (var email in destinataires)
            {
                try { await _emailService.EnvoyerAsync(email, sujet, corps, isHtml: true); }
                catch { /* log, ne pas bloquer */ }
            }
        }

        await _unitOfWork.AlerteRepository.AddAsync(new Alerte
        {
            TypeAlerte = _definition.TypeAlerte,
            CleEntite = cle,
            DonneesContexte = JsonSerializer.Serialize(contexte),
            Destinataires = string.Join(",", destinataires)
        });
        await _unitOfWork.CommitAsync();
    }

    public async Task ResoudreAsync(Guid alerteId, string nomUtilisateurResoluteur)
    {
        var alerte = await _unitOfWork.AlerteRepository.GetByIdAsync(alerteId);
        if (alerte == null || alerte.EstResolu) return;

        alerte.EstResolu = true;
        alerte.DateResolution = DateTime.UtcNow;
        
        await _unitOfWork.AlerteRepository.UpdateAsync(alerte);
        await _unitOfWork.CommitAsync();

        // Notification globale aux autres destinataires
        var destinataires = alerte.Destinataires.Split(',').Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
        if (destinataires.Any())
        {
            var sujet = $"[Résolu] Alerte traitée ({alerte.TypeAlerte})";
            var corps = $"<p>L'alerte de type <b>{alerte.TypeAlerte}</b> concernant <b>{alerte.CleEntite}</b> a été prise en charge et résolue par <b>{nomUtilisateurResoluteur}</b>.</p><p>Merci.</p>";

            foreach (var email in destinataires)
            {
                try { await _emailService.EnvoyerAsync(email, sujet, corps, isHtml: true); }
                catch { /* log */ }
            }
        }
    }
}
