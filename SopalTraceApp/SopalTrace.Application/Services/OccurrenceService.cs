using System;
using System.Collections.Concurrent;
using System.Threading;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;

namespace SopalTrace.Application.Services;

public partial class OccurrenceService : IOccurrenceService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
    private readonly IOccurrenceRepository _occurrenceRepository;
    private readonly SopalTrace.Application.Alertes.AlerteService<SopalTrace.Application.Alertes.NotifsSuperviseurContexte> _alerteSuperviseurService;
    private readonly SopalTrace.Application.Alertes.AlerteService<SopalTrace.Application.Alertes.NotifsManagerContexte> _alerteManagerService;
    private readonly IUnitOfWork _unitOfWork;

    public const double SimulationSpeedFactor = 1.0;

    public OccurrenceService(
        IOccurrenceRepository occurrenceRepository,
        SopalTrace.Application.Alertes.AlerteService<SopalTrace.Application.Alertes.NotifsSuperviseurContexte> alerteSuperviseurService,
        SopalTrace.Application.Alertes.AlerteService<SopalTrace.Application.Alertes.NotifsManagerContexte> alerteManagerService,
        IUnitOfWork unitOfWork)
    {
        _occurrenceRepository = occurrenceRepository;
        _alerteSuperviseurService = alerteSuperviseurService;
        _alerteManagerService = alerteManagerService;
        _unitOfWork = unitOfWork;
    }
}
