using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

public partial class OccurrenceService
{
    public async Task GenererOccurrencesInitialesAsync(Guid execControleOfId)
    {
        var semaphore = _locks.GetOrAdd(execControleOfId, _ => new System.Threading.SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
            if (of == null || of.Statut == "EN_PAUSE") return;

            if (of.TypeOf == "FAB")
                await GenererOccurrencesFabAsync(of);
            else if (of.TypeOf == "ASS")
                await GenererOccurrencesAssAsync(of, execControleOfId);

            await RecalculerNumerosOccurrencesAsync(of);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task AjusterDateDebutApresReglageAsync(Guid execControleOfId)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        if (of == null) return;

        int maxNumero = 0;
        double? interval = null;

        if (of.TypeOf == "FAB" && of.PlanSourceId.HasValue)
        {
            var sectionsActives = await _occurrenceRepository.GetSectionsActivesAsync(of.PlanSourceId.Value);
            var prodSection = sectionsActives.FirstOrDefault(s => EstSectionProductionTemporelle(s));
            if (prodSection != null)
            {
                interval = CalculerIntervalleMinutes(prodSection.Periodicite, prodSection.RegleEchantillonnage);
                if (interval.HasValue)
                {
                    var existantes = of.ExecPrelevementIntermediaires.Where(x => x.SectionId == prodSection.Id && x.NumeroOccurrence > 0).ToList();
                    maxNumero = existantes.Any() ? existantes.Max(x => x.NumeroOccurrence) : 0;
                }
            }
        }
        else if (of.TypeOf == "ASS")
        {
            var assSections = await _occurrenceRepository.GetDocumentSectionsEchantillonnageAsync(of.Id);
            var prodSection = assSections.FirstOrDefault();
            if (prodSection != null)
            {
                interval = CalculerIntervalleMinutes(prodSection.Periodicite);
                if (interval.HasValue)
                {
                    var existantes = of.ExecPrelevementIntermediaires.Where(x => x.SectionId == prodSection.Id && x.NumeroOccurrence > 0).ToList();
                    maxNumero = existantes.Any() ? existantes.Max(x => x.NumeroOccurrence) : 0;
                }
            }
        }

        if (interval.HasValue)
        {
            var virtualDateDebut = DateTime.Now.AddMinutes(-(maxNumero * interval.Value));
            of.TempsPauseTotalMinutes = (virtualDateDebut - of.DateDebut).TotalMinutes;
        }
        else
        {
            of.TempsPauseTotalMinutes = (DateTime.Now - of.DateDebut).TotalMinutes;
        }

        await _occurrenceRepository.SaveChangesAsync();
    }

    public async Task GenererOccurrencesReglageCoursAsync(Guid execControleOfId)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        if (of == null) return;

        string trancheReglage = $"REGLAGE_COURS_{DateTime.Now:HH_mm}";
        var newIntermediaires = new List<ExecPrelevementIntermediaire>();

        if (of.PlanSourceId.HasValue)
        {
            var sectionsActives = await _occurrenceRepository.GetSectionsActivesAsync(of.PlanSourceId.Value);
            newIntermediaires.AddRange(sectionsActives
                .Where(s => s.TypeSection?.Code is "REGLAGE" or "REGLAGE_PROD")
                .Select(s => CreerIntermediaire(of.Id, s.Id, trancheReglage, 0, DateTime.Now)));
        }

        if (of.TypeOf == "ASS")
        {
            var assSections = await _occurrenceRepository.GetDocumentSectionsActivesAsync(execControleOfId);
            newIntermediaires.AddRange(assSections
                .Where(s => s.TypeSection?.Code is "REGLAGE" or "REGLAGE_PROD")
                .Select(s => CreerIntermediaire(of.Id, s.Id, trancheReglage, 0, DateTime.Now)));
        }

        if (newIntermediaires.Any())
        {
            _occurrenceRepository.AddIntermediaires(newIntermediaires);
            await _occurrenceRepository.SaveChangesAsync();
        }
    }

    public async Task NettoyerOccurrencesReglageAsync(Guid execControleOfId)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        if (of == null) return;

        var reglageOccurrences = of.ExecPrelevementIntermediaires
            .Where(o => o.TrancheHoraire.StartsWith("REGLAGE")).ToList();

        if (reglageOccurrences.Any())
        {
            _occurrenceRepository.RemoveIntermediaires(reglageOccurrences);
            await _occurrenceRepository.SaveChangesAsync();
        }
    }

    private async Task GenererOccurrencesFabAsync(ExecControleOf of)
    {
        if (!of.PlanSourceId.HasValue) return;

        var sectionsActives = await _occurrenceRepository.GetSectionsActivesAsync(of.PlanSourceId.Value);
        var newIntermediaires = new List<ExecPrelevementIntermediaire>();
        DateTime now = DateTime.Now;

        foreach (var s in sectionsActives)
        {
            if (!EstSectionProductionTemporelle(s) || of.EstEnReglage || of.Statut == "EN_PAUSE") continue;

            double? intervalMinutes = CalculerIntervalleMinutes(s.Periodicite, s.RegleEchantillonnage);
            if (intervalMinutes.HasValue)
            {
                var occurrences = GenererOccurrencesSectionProd(of, s.Id, intervalMinutes.Value, now);
                newIntermediaires.AddRange(occurrences);
            }
        }

        if (newIntermediaires.Any())
        {
            _occurrenceRepository.AddIntermediaires(newIntermediaires);
            await _occurrenceRepository.SaveChangesAsync();
        }
    }

    private async Task GenererOccurrencesAssAsync(ExecControleOf of, Guid execControleOfId)
    {
        var sectionsEch = await _occurrenceRepository.GetDocumentSectionsEchantillonnageAsync(execControleOfId);
        var newIntermediaires = new List<ExecPrelevementIntermediaire>();
        DateTime now = DateTime.Now;

        // Lire l'effectif p/h depuis la fiche d'échantillonnage (15 p/h => intervalle 60/15 = 4 min)
        int? effectifParHeure = await _occurrenceRepository.GetEffectifEchantillonnageAsync(execControleOfId);
        double intervalMinutesParDefaut = effectifParHeure.HasValue && effectifParHeure.Value > 0
            ? 60.0 / effectifParHeure.Value
            : 60.0; // fallback 1 occ/heure si pas de fiche

        foreach (var sec in sectionsEch)
        {
            if (of.Statut == "EN_PAUSE") continue;

            // Essayer la périodicité de la section, sinon utiliser l'effectif de la fiche
            double? intervalMinutes = null;
            if (sec.Periodicite != null)
                intervalMinutes = CalculerIntervalleMinutes(sec.Periodicite);
            if (!intervalMinutes.HasValue || intervalMinutes.Value <= 0)
                intervalMinutes = intervalMinutesParDefaut;

            var occurrences = GenererOccurrencesSectionProd(of, sec.Id, intervalMinutes.Value, now);
            newIntermediaires.AddRange(occurrences);
        }

        if (newIntermediaires.Any())
        {
            _occurrenceRepository.AddIntermediaires(newIntermediaires);
            await _occurrenceRepository.SaveChangesAsync();
        }
    }

    private List<ExecPrelevementIntermediaire> GenererOccurrencesSectionProd(
        ExecControleOf of, Guid sectionId, double intervalMinutes, DateTime now)
    {
        DateTime virtualDateDebut = of.DateDebut.AddMinutes(of.TempsPauseTotalMinutes);
        double simulatedElapsed = (now - virtualDateDebut).TotalMinutes;
        int expectedTotal = Math.Min((int)Math.Floor(simulatedElapsed / intervalMinutes) + 1, 500);

        var existantes = of.ExecPrelevementIntermediaires
            .Where(x => x.SectionId == sectionId && x.NumeroOccurrence > 0).ToList();
        int maxNumero = existantes.Any() ? existantes.Max(x => x.NumeroOccurrence) : 0;

        if (expectedTotal > maxNumero + 500) expectedTotal = maxNumero + 500;

        var result = new List<ExecPrelevementIntermediaire>();
        for (int i = maxNumero + 1; i <= expectedTotal; i++)
        {
            DateTime intendedTime = virtualDateDebut.AddMinutes(intervalMinutes * i);
            string tranche = $"H_{intendedTime.Hour:00}_{intendedTime.Hour + 1:00}";
            
            bool trancheIsFinalized = of.ExecControleTranches?.Any(t => t.TrancheHoraire == tranche && !string.IsNullOrEmpty(t.ResultatFinal)) == true;
            if (trancheIsFinalized) continue;

            bool exists = of.ExecPrelevementIntermediaires.Any(x =>
                x.SectionId == sectionId && Math.Abs((x.HeureNotifPrevue - intendedTime).TotalMinutes) < 1);
            if (!exists)
                result.Add(CreerIntermediaire(of.Id, sectionId, tranche, -(10000 + i + sectionId.GetHashCode() % 1000), intendedTime));
        }
        return result;
    }

    private async Task RecalculerNumerosOccurrencesAsync(ExecControleOf of)
    {
        var prodOccurrences = of.ExecPrelevementIntermediaires
            .Where(o => !o.TrancheHoraire.StartsWith("REGLAGE"))
            .OrderBy(o => o.HeureNotifPrevue).ToList();

        var groupedByTime = prodOccurrences
            .GroupBy(o => o.HeureNotifPrevue.ToString("yyyyMMddHHmmss"))
            .OrderBy(g => g.First().HeureNotifPrevue).ToList();

        bool changed = false;
        int tempNeg = -20000, globalNumero = 1;

        // Passe 1 : temporaires négatifs pour éviter la contrainte UNIQUE
        foreach (var group in groupedByTime)
        {
            foreach (var occ in group)
                if (occ.NumeroOccurrence != globalNumero) { occ.NumeroOccurrence = tempNeg--; changed = true; }
            globalNumero++;
        }

        if (!changed) return;

        await _occurrenceRepository.SaveChangesAsync();

        // Passe 2 : valeurs définitives
        globalNumero = 1;
        foreach (var group in groupedByTime)
        {
            foreach (var occ in group)
                if (occ.NumeroOccurrence < 0) occ.NumeroOccurrence = globalNumero;
            globalNumero++;
        }

        await _occurrenceRepository.SaveChangesAsync();
    }
}
