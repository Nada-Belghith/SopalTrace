using System.Collections.Generic;
using System.Linq;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Helpers;

/// <summary>
/// Moments de contrôle d'un plan de vérification machine, lus dans la donnée PeriodiciteMachine.ControlMoment
/// (DEMARRAGE, APRES_PAUSE, FIN_POSTE) de ses périodicités : le libellé n'est jamais interprété.
/// </summary>
public static class PeriodiciteMachineHelper
{
    public static bool ADemarrage(IEnumerable<string> moments) => moments.Contains(PeriodicitesControle.Demarrage);

    public static bool AApresPause(IEnumerable<string> moments) => moments.Contains(PeriodicitesControle.ApresPause);

    public static bool AFinPoste(IEnumerable<string> moments) => moments.Contains(PeriodicitesControle.FinPoste);

    /// <summary>La périodicité (son moment de contrôle) correspond-elle au moment demandé ?</summary>
    public static bool Correspond(string? momentPeriodicite, string moment) => momentPeriodicite == moment;

    public static MachineDto VersDto(Machine machine, IReadOnlyDictionary<string, List<string>> momentsParMachine)
    {
        var moments = momentsParMachine.TryGetValue(machine.CodeMachine, out var liste) ? liste : new List<string>();
        return new MachineDto
        {
            CodeMachine = machine.CodeMachine,
            Libelle = machine.Libelle,
            HasDemarragePlan = ADemarrage(moments),
            HasApresPausePlan = AApresPause(moments),
            HasFinPostePlan = AFinPoste(moments)
        };
    }
}
