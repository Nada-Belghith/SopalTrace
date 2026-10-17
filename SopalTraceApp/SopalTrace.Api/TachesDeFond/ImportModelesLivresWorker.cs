using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Services.Rapports;
using SopalTrace.Application.Settings;

namespace SopalTrace.Api.TachesDeFond;

/// <summary>
/// Si Rapports:ImporterModelesLivres est activé (tests), importe le fichier du dossier Rapports:DossierModelesLivres de
/// chaque modèle de rapport qui n'a encore aucune version : au
/// démarrage puis toutes les quelques minutes (modèle ajouté par une migration pendant que l'API tourne). Aucun document
/// n'est généré ici : un PDF est produit quand quelqu'un le consulte ou l'imprime.
/// </summary>
public class ImportModelesLivresWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly RapportsSettings _parametres;
    private readonly ILogger<ImportModelesLivresWorker> _logger;

    public ImportModelesLivresWorker(IServiceScopeFactory scopes, RapportsSettings parametres, ILogger<ImportModelesLivresWorker> logger)
    {
        _scopes = scopes;
        _parametres = parametres;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_parametres.ImporterModelesLivres || string.IsNullOrWhiteSpace(_parametres.DossierModelesLivres))
        {
            _logger.LogInformation("Aucun import automatique de modèles Excel : l'administrateur envoie lui-même les modèles.");
            return;
        }
        var intervalle = TimeSpan.FromMinutes(Math.Max(1, _parametres.IntervalleVerificationMinutes));
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IModeleRapportService>().ImporterModelesLivresAsync();
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // Base pas encore migrée, par exemple : nouvel essai au passage suivant.
                    _logger.LogError(ex, "Import des modèles de rapport livrés impossible : nouvel essai au prochain passage.");
                }
                await Task.Delay(intervalle, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Arrêt de l'API.
        }
    }
}
