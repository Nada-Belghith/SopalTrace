using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

public class OccurrenceService : IOccurrenceService
{
    private readonly IOccurrenceRepository _occurrenceRepository;

    public OccurrenceService(IOccurrenceRepository occurrenceRepository)
    {
        _occurrenceRepository = occurrenceRepository;
    }

    public async Task GenererOccurrencesInitialesAsync(Guid execControleOfId)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);

        if (of == null) return;

        DateTime now = DateTime.Now;
        
        // --- MODE SIMULATION POUR DEMONSTRATION ---
        // 1 minute réelle écoulée = 15 minutes simulées
        double simulationSpeedFactor = 15.0; 
        double simulatedIntervalMinutesTotal = (now - of.DateDebut).TotalMinutes * simulationSpeedFactor;

        if (of.TypePlan == "FAB")
        {
            var sectionsActives = await _occurrenceRepository.GetSectionsActivesAsync(of.PlanSourceId);
            var newIntermediaires = new List<ExecPrelevementIntermediaire>();

            foreach (var s in sectionsActives)
            {
                var typeCode = s.TypeSection?.Code;
                bool isReglage = typeCode == "REGLAGE" || typeCode == "REGLAGE_PROD";
                bool isProd = typeCode == "EN_COURS" || typeCode == "ECHANT_NQA" || typeCode == "REGLAGE_PROD" || string.IsNullOrEmpty(typeCode);

                // 1. Génération des occurrences de Réglage (Une seule fois au début)
                if (isReglage)
                {
                    string trancheReglage = "REGLAGE_DEBUT";
                    
                    if (!of.ExecPrelevementIntermediaires.Any(x => x.SectionId == s.Id && x.TrancheHoraire == trancheReglage))
                    {
                        newIntermediaires.Add(new ExecPrelevementIntermediaire
                        {
                            ExecControleOfid = of.Id,
                            SectionId = s.Id,
                            TrancheHoraire = trancheReglage,
                            NumeroOccurrence = 0, // 0 pour signifier que ce n'est pas une prod périodique
                            HeureNotifPrevue = of.DateDebut, // Le contrôle de réglage doit être fait dès le début
                            EstRepondu = false,
                            EstEnRetard = false
                        });
                    }
                }

                // 2. Génération des occurrences Périodiques (Prod)
                if (isProd && s.Periodicite != null)
                {
                    int freqNum = s.Periodicite.FrequenceNum ?? 1;
                    int freqHeures = 1;
                    if (!string.IsNullOrEmpty(s.Periodicite.FrequenceUnite))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(s.Periodicite.FrequenceUnite, @"\d+");
                        if (match.Success) freqHeures = int.Parse(match.Value);
                    }
                    
                    double simulatedIntervalMinutes = (freqHeures * 60.0) / freqNum;
                
                    int expectedOccurrences = (int)Math.Floor(simulatedIntervalMinutesTotal / simulatedIntervalMinutes);

                    var existingCount = of.ExecPrelevementIntermediaires.Count(x => x.SectionId == s.Id && x.NumeroOccurrence > 0);

                    for (int i = existingCount + 1; i <= expectedOccurrences; i++)
                    {
                        // Le temps RÉEL où l'alerte aurait dû apparaître
                        DateTime intendedRealTime = of.DateDebut.AddMinutes((simulatedIntervalMinutes * i) / simulationSpeedFactor);
                        
                        // Le temps SIMULÉ (virtuel) pour calculer la tranche horaire
                        DateTime simulatedTime = of.DateDebut.AddMinutes(simulatedIntervalMinutes * i);
                        string tranche = $"H_{simulatedTime.Hour}_{simulatedTime.Hour + 1}";

                        if (!of.ExecPrelevementIntermediaires.Any(x => x.SectionId == s.Id && x.NumeroOccurrence == i))
                        {
                            newIntermediaires.Add(new ExecPrelevementIntermediaire
                            {
                                ExecControleOfid = of.Id,
                                SectionId = s.Id,
                                TrancheHoraire = tranche,
                                NumeroOccurrence = i,
                                HeureNotifPrevue = intendedRealTime, 
                                EstRepondu = false,
                                EstEnRetard = false // Sera mis à jour par GetAlertesActivesAsync
                            });
                        }
                    }
                }
            }

            if (newIntermediaires.Any())
            {
                _occurrenceRepository.AddIntermediaires(newIntermediaires);
                await _occurrenceRepository.SaveChangesAsync();
            }
        }

    }

    public async Task ShiftOccurrencesApresPauseAsync(Guid execControleOfId)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        if (of == null) return;

        var unanswered = of.ExecPrelevementIntermediaires
            .Where(o => !o.EstRepondu && o.TrancheHoraire != null && !o.TrancheHoraire.StartsWith("REGLAGE"))
            .OrderBy(o => o.HeureNotifPrevue)
            .ToList();

        if (unanswered.Any())
        {
            var earliest = unanswered.First();
            var now = DateTime.Now;
            if (earliest.HeureNotifPrevue < now)
            {
                var delta = now - earliest.HeureNotifPrevue;
                foreach (var occ in unanswered)
                {
                    occ.HeureNotifPrevue = occ.HeureNotifPrevue.Add(delta);
                    occ.EstEnRetard = false;
                }
                await _occurrenceRepository.SaveChangesAsync();
            }
        }
    }

    public async Task<List<TrancheAlertesDto>> GetAlertesActivesAsync(Guid execControleOfId)
    {
        // Dynamically generate any missing occurrences first
        await GenererOccurrencesInitialesAsync(execControleOfId);

        var occurrences = await _occurrenceRepository.GetIntermediairesActifsAsync(execControleOfId);
        occurrences = occurrences.OrderBy(o => o.HeureNotifPrevue).ToList();

        DateTime now = DateTime.Now;
        bool hasChanges = false;
        
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        var sectionsActives = of != null ? await _occurrenceRepository.GetSectionsActivesAsync(of.PlanSourceId) : new List<PlanFabricationSection>();
        
        var sectionIntervals = new Dictionary<Guid, double>();
        foreach (var s in sectionsActives)
        {
            if (s.Periodicite != null)
            {
                int freqHeures = 1;
                int freqNum = s.Periodicite.FrequenceNum.GetValueOrDefault(0) > 0 ? s.Periodicite.FrequenceNum.Value : 1;
                if (!string.IsNullOrEmpty(s.Periodicite.FrequenceUnite))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(s.Periodicite.FrequenceUnite, @"\d+");
                    if (match.Success) freqHeures = int.Parse(match.Value);
                }
                double simulatedIntervalMinutes = (freqHeures * 60.0) / freqNum;
                double realIntervalMinutes = simulatedIntervalMinutes / 15.0; // 15.0 = simulationSpeedFactor
                sectionIntervals[s.Id] = realIntervalMinutes;
            }
        }

        // Mise à jour du retard
        foreach (var occ in occurrences)
        {
            if (!occ.EstEnRetard)
            {
                double gracePeriod = sectionIntervals.ContainsKey(occ.SectionId) ? sectionIntervals[occ.SectionId] : 1.0;
                if (occ.HeureNotifPrevue.AddMinutes(gracePeriod) < now)
                {
                    occ.EstEnRetard = true;
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
            await _occurrenceRepository.SaveChangesAsync();

        var alertesVisibles = occurrences.Where(o => o.EstEnRetard || o.HeureNotifPrevue <= now).ToList();

        var sectionIds = alertesVisibles.Select(o => o.SectionId).Distinct().ToList();
        var lignesPerSection = new Dictionary<Guid, List<PlanFabricationLigne>>();
        foreach (var sid in sectionIds)
        {
            lignesPerSection[sid] = await _occurrenceRepository.GetLignesForSectionAsync(sid);
        }

        var grouped = alertesVisibles.GroupBy(o => o.TrancheHoraire).Select(g => new TrancheAlertesDto
        {
            TrancheHoraire = g.Key,
            Occurrences = g.Select(o => new OccurrenceDto
            {
                Id = o.Id,
                SectionId = o.SectionId,
                TrancheHoraire = o.TrancheHoraire,
                NumeroOccurrence = o.NumeroOccurrence,
                HeureNotifPrevue = o.HeureNotifPrevue,
                EstEnRetard = o.EstEnRetard,
                Resultat = o.Resultat,
                Caracteristiques = lignesPerSection.ContainsKey(o.SectionId) 
                    ? lignesPerSection[o.SectionId].Select(l => new CaracteristiqueARepondreDto
                    {
                        LignePlanId = l.Id,
                        Libelle = l.LibelleAffiche ?? "Caractéristique sans nom",
                        LimiteSpecTexte = l.LimiteSpecTexte,
                        TypeControle = l.TypeControle?.Code ?? "N/A",
                        MoyenControle = l.MoyenControle?.Code,
                        Instrument = l.InstrumentCode
                    }).ToList()
                    : new List<CaracteristiqueARepondreDto>()
            }).ToList()
        }).ToList();

        return grouped;
    }

    public async Task<bool> RepondreOccurrenceAsync(Guid occurrenceId, RepondreOccurrenceRequest request)
    {
        var occurrence = await _occurrenceRepository.GetIntermediaireAsync(occurrenceId);
        if (occurrence == null || occurrence.EstRepondu) return false;

        occurrence.Resultat = request.Resultat;
        occurrence.EstRepondu = true;
        occurrence.HeureReponse = DateTime.Now;

        // Si l'alerte n'est pas ignorée, on doit créer un prélèvement avec ses lignes
        if (request.Resultat != "REGLAGE")
        {
            var trancheExistante = await _occurrenceRepository.GetTrancheExistanteAsync(occurrence.ExecControleOfid, occurrence.TrancheHoraire);
            if (trancheExistante == null)
            {
                var parts = occurrence.TrancheHoraire.Split('_');
                int heureDebut = 0;
                int heureFin = 1;
                if (parts.Length == 3 && int.TryParse(parts[1], out int hd) && int.TryParse(parts[2], out int hf))
                {
                    heureDebut = hd;
                    heureFin = hf;
                }

                DateTime debut = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, heureDebut, 0, 0);
                DateTime fin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, heureFin, 0, 0);
                if (heureFin == 0 || heureFin == 24) fin = debut.Date.AddDays(1);

                trancheExistante = new ExecControleTranche
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfid = occurrence.ExecControleOfid,
                    TrancheHoraire = occurrence.TrancheHoraire,
                    HeureDebut = debut,
                    HeureFin = fin,
                    ResultatFinal = null
                };
                _occurrenceRepository.AddTranche(trancheExistante);
            }



            // Concaténer les remarques et actions correctives dans la tranche
            var nouvellesRemarques = string.Join(" | ", request.Lignes.Where(l => !string.IsNullOrWhiteSpace(l.Remarque)).Select(l => l.Remarque));
            var nouvellesActions = string.Join(" | ", request.Lignes.Where(l => !string.IsNullOrWhiteSpace(l.ActionCorrective)).Select(l => l.ActionCorrective));

            if (!string.IsNullOrWhiteSpace(nouvellesRemarques))
            {
                trancheExistante.DetailsNc = string.IsNullOrWhiteSpace(trancheExistante.DetailsNc) ? nouvellesRemarques : trancheExistante.DetailsNc + " | " + nouvellesRemarques;
            }
            if (!string.IsNullOrWhiteSpace(nouvellesActions))
            {
                trancheExistante.ActionsCorrection = string.IsNullOrWhiteSpace(trancheExistante.ActionsCorrection) ? nouvellesActions : trancheExistante.ActionsCorrection + " | " + nouvellesActions;
            }
        }

        await _occurrenceRepository.SaveChangesAsync();

        var allOccurrencesTranche = await _occurrenceRepository.GetIntermediairesParTrancheAsync(occurrence.ExecControleOfid, occurrence.TrancheHoraire);

        if (allOccurrencesTranche.All(o => o.EstRepondu))
        {
            var trancheExistante = await _occurrenceRepository.GetTrancheExistanteAsync(occurrence.ExecControleOfid, occurrence.TrancheHoraire);
            
            if (trancheExistante != null)
            {
                var resultats = allOccurrencesTranche.Select(o => o.Resultat).ToList();
                if (resultats.Contains("NC")) trancheExistante.ResultatFinal = "NC";
                else trancheExistante.ResultatFinal = "C"; // C si toutes = C ou REGLAGE
            }

            _occurrenceRepository.RemoveIntermediaires(allOccurrencesTranche);
            await _occurrenceRepository.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> IgnorerOccurrencesAsync(List<Guid> occurrenceIds, string matriculeOperateur)
    {
        foreach (var id in occurrenceIds)
        {
            var request = new RepondreOccurrenceRequest 
            { 
                Resultat = "REGLAGE", 
                MatriculeOperateur = matriculeOperateur 
            };
            await RepondreOccurrenceAsync(id, request);
        }
        return true;
    }
}
