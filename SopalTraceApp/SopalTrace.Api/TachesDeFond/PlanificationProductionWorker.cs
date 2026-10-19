using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SopalTrace.Application.Interfaces.Execution;

namespace SopalTrace.Api.TachesDeFond;

/// <summary>
/// Chaque minute, planifie les contrôles de production des OF d'assemblage en cours (ouverture / fermeture des
/// périodes, contrôles dus, retards, escalade) : un contrôle apparaît à son heure sur toutes les tablettes,
/// même si personne n'a l'écran des contrôles ouvert.
/// </summary>
public class PlanificationProductionWorker : BackgroundService
{
    private static readonly TimeSpan Periode = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PlanificationProductionWorker> _logger;

    public PlanificationProductionWorker(IServiceScopeFactory scopes, ILogger<PlanificationProductionWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var minuteur = new PeriodicTimer(Periode);
        do
        {
            await PlanifierAsync(stoppingToken);
        }
        while (await minuteur.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PlanifierAsync(CancellationToken stoppingToken)
    {
        try
        {
            List<Guid> ofs;
            // Portées libérées en asynchrone : certains services (UnitOfWork) ne sont libérables qu'ainsi.
            await using (var scope = _scopes.CreateAsyncScope())
                ofs = await scope.ServiceProvider.GetRequiredService<IPlanificationProductionService>().GetOfsAPlanifierAsync();

            foreach (var execControleOfId in ofs)
            {
                if (stoppingToken.IsCancellationRequested) return;
                // Un contexte par OF : l'échec d'un OF n'empêche pas les autres d'être planifiés.
                await using var scope = _scopes.CreateAsyncScope();
                try
                {
                    await scope.ServiceProvider.GetRequiredService<IPlanificationProductionService>().SynchroniserAsync(execControleOfId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Planification des contrôles de production impossible pour l'OF {ExecControleOfId}", execControleOfId);
                }
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Planification des contrôles de production interrompue");
        }
    }
}
