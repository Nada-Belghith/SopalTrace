using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

public class OccurrenceService : IOccurrenceService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
    private readonly IOccurrenceRepository _occurrenceRepository;
    private readonly SopalTrace.Application.Alertes.AlerteService<SopalTrace.Application.Alertes.NotifsSuperviseurContexte> _alerteSuperviseurService;
    private readonly SopalTrace.Application.Alertes.AlerteService<SopalTrace.Application.Alertes.NotifsManagerContexte> _alerteManagerService;
    private readonly IUnitOfWork _unitOfWork;

    // --- MODE SIMULATION ---
    // 1 minute réelle écoulée = 15 minutes simulées (accélère le temps pour les tests)
    public const double SimulationSpeedFactor = 1.0; // 15.0;

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

    public async Task GenererOccurrencesInitialesAsync(Guid execControleOfId)
    {
        var semaphore = _locks.GetOrAdd(execControleOfId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);

        if (of == null) return;
        
        // Ne pas générer d'occurrences si l'OF est en pause
        if (of.Statut == "EN_PAUSE") return;


        DateTime now = DateTime.Now;
        
        // --- MODE SIMULATION ---
        // 1 minute réelle écoulée = 15 minutes simulées (accélère le temps pour les tests)
        double simulationSpeedFactor = 1.0; // 15.0; 
        double simulatedIntervalMinutesTotal = (now - of.DateDebut).TotalMinutes * simulationSpeedFactor;

        if (of.TypeOf == "FAB")
        {
            var sectionsActives = of.PlanSourceId.HasValue ? await _occurrenceRepository.GetSectionsActivesAsync(of.PlanSourceId.Value) : new List<PlanFabricationSection>();
            var newIntermediaires = new List<ExecPrelevementIntermediaire>();

            foreach (var s in sectionsActives)
            {
                var typeCode = s.TypeSection?.Code;
                bool isReglage = typeCode == "REGLAGE" || typeCode == "REGLAGE_PROD";
                bool isProd = typeCode == "EN_COURS" || typeCode == "ECHANT_NQA" || typeCode == "REGLAGE_PROD" || string.IsNullOrEmpty(typeCode);

                // 1. Génération des occurrences de Réglage de démarrage
                bool hasValidStartupReglage = of.ExecPieceTypes != null && of.ExecPieceTypes.Any(pt => pt.Resultat == "C");
                
                // Si on a déjà un réglage valide (C), cela veut dire qu'on est en milieu de production (ex: suite à un MettreEnReglage)
                // Donc on ne doit plus afficher le réglage de démarrage initial.
                if (isReglage && of.EstEnReglage && !hasValidStartupReglage)
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
                if (isProd && s.Periodicite != null && !of.EstEnReglage)
                {
                    int freqNum = s.Periodicite.FrequenceNum ?? 1;
                    int freqHeures = 1;
                    if (!string.IsNullOrEmpty(s.Periodicite.FrequenceUnite))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(s.Periodicite.FrequenceUnite, @"\d+");
                        if (match.Success) freqHeures = int.Parse(match.Value);
                    }
                    
                    double simulatedIntervalMinutes = (freqHeures * 60.0) / freqNum;
                
                    DateTime baseline = of.DateDebut;

                    double simulatedIntervalSinceBaseline = (now - baseline).TotalMinutes * simulationSpeedFactor;
                    // On ajoute +2 : 
                    // +1 pour l'occurrence actuelle (ex: 0 intervalle passé -> occurrence #1)
                    // +1 supplémentaire pour générer la PROCHAINE occurrence (À VENIR)
                    int expectedOccurrencesSinceBaseline = (int)Math.Floor(simulatedIntervalSinceBaseline / simulatedIntervalMinutes) + 2;

                    int expectedTotalOccurrences = expectedOccurrencesSinceBaseline;

                    var sectionOccurrences = of.ExecPrelevementIntermediaires.Where(x => x.SectionId == s.Id && x.NumeroOccurrence > 0).ToList();
                    int maxNumeroOccurrence = sectionOccurrences.Any() ? sectionOccurrences.Max(x => x.NumeroOccurrence) : 0;

                    // SÉCURITÉ : Ne générer qu'un maximum de 500 nouvelles occurrences d'un coup (batch)
                    // pour éviter les boucles trop longues, mais SANS bloquer la génération globale !
                    if (expectedTotalOccurrences > maxNumeroOccurrence + 500)
                    {
                        expectedTotalOccurrences = maxNumeroOccurrence + 500;
                    }

                    for (int i = maxNumeroOccurrence + 1; i <= expectedTotalOccurrences; i++)
                    {
                        int occurrenceIndexSinceBaseline = i;

                        
                        // Le temps RÉEL où l'alerte aurait dû apparaître
                        DateTime intendedRealTime = baseline.AddMinutes((simulatedIntervalMinutes * occurrenceIndexSinceBaseline) / simulationSpeedFactor);
                        
                        // Le temps LOGIQUE (affichage pour l'utilisateur, ex: 14:30)
                        DateTime logicalTime = of.DateDebut.AddMinutes((intendedRealTime - of.DateDebut).TotalMinutes * SimulationSpeedFactor);
                        string tranche = $"H_{logicalTime.Hour:00}_{logicalTime.Hour + 1:00}";

                        // SÉCURITÉ CONTRAINTE UNIQUE: On utilise un numéro négatif temporaire pour l'insertion
                        // Il sera recalculé globalement et mis à jour juste après.
                        if (!of.ExecPrelevementIntermediaires.Any(x => x.SectionId == s.Id && Math.Abs((x.HeureNotifPrevue - intendedRealTime).TotalMinutes) < 1))
                        {
                            newIntermediaires.Add(new ExecPrelevementIntermediaire
                            {
                                ExecControleOfid = of.Id,
                                SectionId = s.Id,
                                TrancheHoraire = tranche,
                                NumeroOccurrence = -(10000 + i + s.Id.GetHashCode() % 1000), 
                                HeureNotifPrevue = intendedRealTime, 
                                EstRepondu = false,
                                EstEnRetard = false
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
        
        // --- NOUVELLE LOGIQUE : Recalculer les NumeroOccurrence pour qu'ils soient globaux à l'OF ---
        // Le client veut que les occurrences de différentes sections ayant la même heure aient le MÊME NumeroOccurrence
        var allOccurrences = of.ExecPrelevementIntermediaires.ToList();
        var prodOccurrences = allOccurrences
            .Where(o => !o.TrancheHoraire.StartsWith("REGLAGE"))
            .OrderBy(o => o.HeureNotifPrevue)
            .ToList();

        var groupedByTime = prodOccurrences
            .GroupBy(o => o.HeureNotifPrevue.ToString("yyyyMMddHHmmss"))
            .OrderBy(g => g.First().HeureNotifPrevue)
            .ToList();

        int globalNumero = 1;
        bool changedNumbers = false;
        
        // Etape 1: Assigner un ID négatif temporaire pour éviter la contrainte UNIQUE
        int tempNeg = -20000;
        foreach (var group in groupedByTime)
        {
            foreach (var occ in group)
            {
                if (occ.NumeroOccurrence != globalNumero)
                {
                    occ.NumeroOccurrence = tempNeg--;
                    changedNumbers = true;
                }
            }
            globalNumero++;
        }

        if (changedNumbers)
        {
            await _occurrenceRepository.SaveChangesAsync(); // Sauvegarder les temporaires

            // Etape 2: Assigner les vrais ID
            globalNumero = 1;
            foreach (var group in groupedByTime)
            {
                foreach (var occ in group)
                {
                    if (occ.NumeroOccurrence < 0)
                    {
                        occ.NumeroOccurrence = globalNumero;
                    }
                }
                globalNumero++;
            }
            await _occurrenceRepository.SaveChangesAsync(); // Sauvegarder les définitifs
        }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task GenererOccurrencesReglageCoursAsync(Guid execControleOfId)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        if (of == null) return;

        var sectionsActives = of.PlanSourceId.HasValue ? await _occurrenceRepository.GetSectionsActivesAsync(of.PlanSourceId.Value) : new List<PlanFabricationSection>();
        var newIntermediaires = new List<ExecPrelevementIntermediaire>();
        
        string trancheReglage = $"REGLAGE_COURS_{DateTime.Now:HH_mm}";

        foreach (var s in sectionsActives)
        {
            var typeCode = s.TypeSection?.Code;
            bool isReglage = typeCode == "REGLAGE" || typeCode == "REGLAGE_PROD";

            if (isReglage)
            {
                newIntermediaires.Add(new ExecPrelevementIntermediaire
                {
                    ExecControleOfid = of.Id,
                    SectionId = s.Id,
                    TrancheHoraire = trancheReglage,
                    NumeroOccurrence = 0,
                    HeureNotifPrevue = DateTime.Now,
                    EstRepondu = false,
                    EstEnRetard = false
                });
            }
        }

        if (newIntermediaires.Any())
        {
            _occurrenceRepository.AddIntermediaires(newIntermediaires);
            await _occurrenceRepository.SaveChangesAsync();
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
        if (of == null) return new List<TrancheAlertesDto>();

        bool isPaused = of.Statut == "EN_PAUSE";
        DateTime pauseStart = of.DateFin ?? now;
        
        var sectionsActives = of.PlanSourceId.HasValue ? await _occurrenceRepository.GetSectionsActivesAsync(of.PlanSourceId.Value) : new List<PlanFabricationSection>();
        
        var sectionIntervals = new Dictionary<Guid, double>();
        var sectionSimulatedIntervals = new Dictionary<Guid, double>();
        foreach (var s in sectionsActives)
        {
            if (s.Periodicite != null)
            {
                int freqHeures = 1;
                int freqNum = s.Periodicite.FrequenceNum ?? 1;
                if (freqNum <= 0) freqNum = 1;
                if (!string.IsNullOrEmpty(s.Periodicite.FrequenceUnite))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(s.Periodicite.FrequenceUnite, @"\d+");
                    if (match.Success) freqHeures = int.Parse(match.Value);
                }
                double simulatedIntervalMinutes = (freqHeures * 60.0) / freqNum;
                double realIntervalMinutes = simulatedIntervalMinutes / SimulationSpeedFactor;
                sectionIntervals[s.Id] = realIntervalMinutes;
                sectionSimulatedIntervals[s.Id] = simulatedIntervalMinutes;
            }
        }

        // Mise à jour du retard
        foreach (var occ in occurrences)
        {
            if (!occ.EstEnRetard)
            {
                double gracePeriod = sectionIntervals.ContainsKey(occ.SectionId) ? sectionIntervals[occ.SectionId] : 1.0;
                
                DateTime timeToCheckAgainst = isPaused ? pauseStart : now;

                var nextOcc = occurrences.FirstOrDefault(o => o.SectionId == occ.SectionId && o.NumeroOccurrence == occ.NumeroOccurrence + 1);

                if (nextOcc != null)
                {
                    if (nextOcc.HeureNotifPrevue <= timeToCheckAgainst)
                    {
                        occ.EstEnRetard = true;
                        hasChanges = true;
                    }
                }
                else
                {
                    if (occ.HeureNotifPrevue.AddMinutes(gracePeriod) <= timeToCheckAgainst)
                    {
                        occ.EstEnRetard = true;
                        hasChanges = true;
                    }
                }
            }
        }

        if (hasChanges)
            await _occurrenceRepository.SaveChangesAsync();

        // Workflow d'escalade des alertes (Notifications sans réponse)
        if (of.Statut == "EN_COURS")
        {
            var derniereReponse = occurrences
                .Where(o => o.EstRepondu)
                .OrderByDescending(o => o.HeureNotifPrevue)
                .FirstOrDefault();

            var retards = occurrences
                .Where(o => o.EstEnRetard && !o.EstRepondu)
                .Where(o => derniereReponse == null || o.HeureNotifPrevue > derniereReponse.HeureNotifPrevue)
                .OrderBy(o => o.HeureNotifPrevue)
                .ToList();
            // L'opérateur voit les occurrences groupées par heure/minute/seconde
            int nbRetardsLogiques = retards.GroupBy(o => o.HeureNotifPrevue.ToString("yyyyMMddHHmmss")).Count();

            if (nbRetardsLogiques >= 3)
            {
                var premierRetard = retards.First();
                var dureeRetard = now - premierRetard.HeureNotifPrevue;

                if (nbRetardsLogiques >= 10)
                {
                    await _alerteManagerService.DeclencherAsync(new SopalTrace.Application.Alertes.NotifsManagerContexte
                    {
                        ExecControleOfId = of.Id,
                        NumeroOf = of.NumeroOf,
                        ArticleCode = of.NumeroOfNavigation?.CodeArticle ?? "Inconnu",
                        OperationCode = of.OperationCode ?? "Inconnue",
                        NbNotificationsManquees = nbRetardsLogiques,
                        DureeDepuisPremierRetard = dureeRetard
                    });
                }
                else
                {
                    await _alerteSuperviseurService.DeclencherAsync(new SopalTrace.Application.Alertes.NotifsSuperviseurContexte
                    {
                        ExecControleOfId = of.Id,
                        NumeroOf = of.NumeroOf,
                        ArticleCode = of.NumeroOfNavigation?.CodeArticle ?? "Inconnu",
                        OperationCode = of.OperationCode ?? "Inconnue",
                        NbNotificationsManquees = nbRetardsLogiques,
                        DureeDepuisPremierRetard = dureeRetard
                    });
                }
            }
            else
            {
                // Moins de 3 retards = la série a été cassée (ou jamais atteinte).
                // On s'assure de clore toute ancienne alerte restée ouverte.
                await _unitOfWork.AlerteRepository.ResoudreAlertesPourEntiteAsync(of.Id.ToString(), "Système Automatique");
                await _unitOfWork.CommitAsync();
            }
        }

        var visibleOccurrences = new List<ExecPrelevementIntermediaire>();
        DateTime timeToCheckForVisibility = isPaused ? pauseStart : now;
        bool futureGroupAdded = false;

        var groupedForVisibility = occurrences
            .GroupBy(o => o.HeureNotifPrevue.ToString("yyyyMMddHHmmss"))
            .OrderBy(g => g.First().HeureNotifPrevue)
            .ToList();

        foreach (var g in groupedForVisibility)
        {
            var firstTime = g.First().HeureNotifPrevue;
            if (firstTime <= timeToCheckForVisibility)
            {
                visibleOccurrences.AddRange(g);
            }
            else
            {
                if (!futureGroupAdded)
                {
                    visibleOccurrences.AddRange(g);
                    futureGroupAdded = true;
                }
            }
        }

        // Limiter à 50 alertes max
        var alertesVisibles = visibleOccurrences.Take(50).ToList();

        var sectionIds = alertesVisibles.Select(o => o.SectionId).Distinct().ToList();
        var lignesPerSection = new Dictionary<Guid, List<PlanFabricationLigne>>();
        foreach (var sid in sectionIds)
        {
            lignesPerSection[sid] = await _occurrenceRepository.GetLignesForSectionAsync(sid);
        }

        var tranchesList = alertesVisibles.Select(o => o.TrancheHoraire.Split('|')[0]).Distinct().ToList();
        var execTranches = new Dictionary<string, string?>();
        foreach (var t in tranchesList)
        {
            var tr = await _occurrenceRepository.GetTrancheExistanteAsync(execControleOfId, t);
            // Si on ne le trouve pas par H_12_13, on cherche par StartsWith si nécessaire, 
            // mais l'idéal est de vérifier si n'importe quelle sous-tranche a un résultat.
            if (tr == null)
            {
                var allTr = await _occurrenceRepository.GetIntermediairesActifsAsync(execControleOfId);
                // (Just fallback logic if needed, but usually we just want to know if the global tranche has a result)
            }
            if (tr != null) execTranches[t] = tr.ResultatFinal;
        }

        var grouped = alertesVisibles.GroupBy(o => o.TrancheHoraire.Split('|')[0]).Select(g => new TrancheAlertesDto
        {
            TrancheHoraire = g.Key,
            ResultatFinal = execTranches.ContainsKey(g.Key) ? execTranches[g.Key] : null,
            Occurrences = g.GroupBy(o => o.HeureNotifPrevue.ToString("yyyyMMddHHmmss")).Select(timeGroup => 
            {
                var firstOcc = timeGroup.First();
                var numeros = timeGroup.Where(x => x.NumeroOccurrence > 0).Select(x => x.NumeroOccurrence).Distinct().ToList();
                string titreCombine = numeros.Count > 0 
                    ? "Occurrence #" + numeros.Max()
                    : "Occurrence #" + firstOcc.NumeroOccurrence;

                DateTime computedHeureLogique = of != null ? of.DateDebut.AddMinutes((firstOcc.HeureNotifPrevue - of.DateDebut).TotalMinutes * SimulationSpeedFactor) : firstOcc.HeureNotifPrevue;

                return new OccurrenceDto
                {
                    Id = firstOcc.Id,
                    AssociatedIds = timeGroup.Select(x => x.Id).ToList(),
                    SectionId = firstOcc.SectionId,
                    TrancheHoraire = firstOcc.TrancheHoraire,
                    NumeroOccurrence = firstOcc.NumeroOccurrence,
                    TitreCombine = titreCombine,
                    HeureNotifPrevue = firstOcc.HeureNotifPrevue,
                    HeureSimulee = computedHeureLogique, // Utilisé pour l'affichage dans Vue
                    EstEnRetard = timeGroup.Any(x => x.EstEnRetard),
                    Resultat = firstOcc.Resultat,
                    Caracteristiques = timeGroup.SelectMany(x => 
                    {
                        var lignes = lignesPerSection.ContainsKey(x.SectionId) ? lignesPerSection[x.SectionId] : new List<PlanFabricationLigne>();
                        var section = sectionsActives.FirstOrDefault(s => s.Id == x.SectionId);
                        string sectionLibelle = section?.LibelleSection ?? "Caractéristiques";
                        
                        return lignes.Select(l => new CaracteristiqueARepondreDto
                        {
                            SectionId = x.SectionId,
                            SectionLibelle = sectionLibelle,
                            LignePlanId = l.Id,
                            Libelle = l.LibelleAffiche ?? "",
                            LimiteSpecTexte = l.LimiteSpecTexte,
                            Observations = l.Observations,
                            TypeControle = l.TypeControle?.Code ?? "N/A",
                            MoyenControle = l.MoyenControle?.Code,
                            Instrument = l.InstrumentCode,
                            ImageBase64 = l.ImageBase64
                        }).ToList();
                    }).ToList()
                };
            }).ToList()
        }).ToList();

        return grouped;
    }

    public async Task<bool> RepondreOccurrenceAsync(Guid occurrenceId, RepondreOccurrenceRequest request)
    {
        var idsToProcess = request.AssociatedIds != null && request.AssociatedIds.Any() 
            ? request.AssociatedIds 
            : new List<Guid> { occurrenceId };

        bool trancheUpdated = false;
        ExecControleOf? ofContext = null;
        string? trancheHoraire = null;
        
        foreach (var id in idsToProcess)
        {
            var occurrence = await _occurrenceRepository.GetIntermediaireAsync(id);
            if (occurrence == null || occurrence.EstRepondu) continue;

            ofContext ??= await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(occurrence.ExecControleOfid);
            trancheHoraire ??= occurrence.TrancheHoraire;

            occurrence.Resultat = request.Resultat;
            
            if (occurrence.TrancheHoraire.StartsWith("REGLAGE") && request.Resultat != "C")
            {
                occurrence.EstRepondu = false;
            }
            else
            {
                occurrence.EstRepondu = true;
            }
            
            occurrence.HeureReponse = DateTime.Now;

            // Logique de création de la tranche (Exécutée une seule fois par groupe)
            if (!trancheUpdated)
            {
                var remarquesC = request.Lignes.Where(l => l.Resultat == "C" && !string.IsNullOrWhiteSpace(l.Remarque)).Select(l => l.Remarque).ToList();
                var detailsNc = request.Lignes.Where(l => l.Resultat == "NC" && !string.IsNullOrWhiteSpace(l.Remarque)).Select(l => l.Remarque).ToList();
                
                if (!string.IsNullOrWhiteSpace(request.Raison))
                {
                    remarquesC.Add(request.Resultat == "IGNORE" ? $"Ignoré : {request.Raison}" : request.Raison);
                }

                var nouvellesRemarques = string.Join(" | ", remarquesC);
                var nouveauxDetailsNc = string.Join(" | ", detailsNc);
                var nouvellesActions = string.Join(" | ", request.Lignes.Where(l => l.Resultat == "NC" && !string.IsNullOrWhiteSpace(l.ActionCorrective)).Select(l => l.ActionCorrective));

                if (occurrence.TrancheHoraire.StartsWith("REGLAGE"))
                {
                    var finalRemarque = string.IsNullOrWhiteSpace(nouvellesActions) ? nouvellesRemarques : $"{nouvellesRemarques} (Actions: {nouvellesActions})";
                    
                    var pieceType = new ExecPieceType
                    {
                        Id = Guid.NewGuid(),
                        ExecControleOfid = occurrence.ExecControleOfid,
                        HeureValidation = DateTime.Now,
                        Resultat = request.Resultat,
                        Remarque = finalRemarque,
                        MatriculeOperateur = request.MatriculeOperateur ?? "SYSTEM"
                    };
                    _occurrenceRepository.AddPieceType(pieceType);
                }
                else
                {
                    var trancheExistante = await _occurrenceRepository.GetTrancheExistanteAsync(occurrence.ExecControleOfid, occurrence.TrancheHoraire);
                    
                    if (trancheExistante == null)
                    {
                        DateTime debut = occurrence.HeureNotifPrevue;
                        DateTime fin = debut.AddHours(1);

                        var match = System.Text.RegularExpressions.Regex.Match(occurrence.TrancheHoraire, @"H_(\d+)_(\d+)");
                        if (match.Success)
                        {
                            int heureDebut = int.Parse(match.Groups[1].Value);
                            int heureFin = int.Parse(match.Groups[2].Value);
                            debut = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, heureDebut, 0, 0);
                            if (heureFin == 0 || heureFin == 24 || heureFin < heureDebut)
                            {
                                fin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0).AddDays(1);
                            }
                            else
                            {
                                fin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, heureFin, 0, 0);
                            }
                        }

                        trancheExistante = new ExecControleTranche
                        {
                            Id = Guid.NewGuid(),
                            ExecControleOfid = occurrence.ExecControleOfid,
                            TrancheHoraire = occurrence.TrancheHoraire,
                            HeureDebut = debut,
                            HeureFin = fin,
                            ResultatFinal = null,
                            MatriculeApprobateur = request.MatriculeOperateur
                        };
                        _occurrenceRepository.AddTranche(trancheExistante);
                    }
                    else if (string.IsNullOrEmpty(trancheExistante.MatriculeApprobateur) && !string.IsNullOrEmpty(request.MatriculeOperateur))
                    {
                        trancheExistante.MatriculeApprobateur = request.MatriculeOperateur;
                    }

                    if (!string.IsNullOrWhiteSpace(nouvellesRemarques))
                    {
                        if (string.IsNullOrWhiteSpace(trancheExistante.Remarques))
                        {
                            trancheExistante.Remarques = nouvellesRemarques;
                        }
                        else if (!trancheExistante.Remarques.Contains(nouvellesRemarques))
                        {
                            trancheExistante.Remarques += " | " + nouvellesRemarques;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(nouveauxDetailsNc))
                    {
                        if (string.IsNullOrWhiteSpace(trancheExistante.DetailsNc))
                        {
                            trancheExistante.DetailsNc = nouveauxDetailsNc;
                        }
                        else if (!trancheExistante.DetailsNc.Contains(nouveauxDetailsNc))
                        {
                            trancheExistante.DetailsNc += " | " + nouveauxDetailsNc;
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(nouvellesActions))
                    {
                        trancheExistante.ActionsCorrection = string.IsNullOrWhiteSpace(trancheExistante.ActionsCorrection) ? nouvellesActions : trancheExistante.ActionsCorrection + " | " + nouvellesActions;
                    }
                }
                
                trancheUpdated = true;
            }
        }

        await _occurrenceRepository.SaveChangesAsync();

        if (ofContext != null && trancheHoraire != null)
        {
            var allOccurrencesTranche = await _occurrenceRepository.GetIntermediairesParTrancheAsync(ofContext.Id, trancheHoraire);
            if (allOccurrencesTranche.Any())
            {
                var trancheExistante = await _occurrenceRepository.GetTrancheExistanteAsync(ofContext.Id, trancheHoraire);
                if (trancheExistante != null)
                {
                    bool hasNC = allOccurrencesTranche.Any(o => o.Resultat == "NC");
                    bool hasC = allOccurrencesTranche.Any(o => o.Resultat == "C");
                    bool allReglage = allOccurrencesTranche.All(o => o.Resultat == "REGLAGE");
                    bool allRepondu = allOccurrencesTranche.All(o => o.EstRepondu);
                    
                    if (hasNC)
                        trancheExistante.ResultatFinal = "NC";
                    else if (allRepondu)
                    {
                        if (hasC)
                            trancheExistante.ResultatFinal = "C";
                        else if (allReglage)
                            trancheExistante.ResultatFinal = "REGLAGE";
                        else
                            trancheExistante.ResultatFinal = "IGNORE";
                    }
                    else
                    {
                        trancheExistante.ResultatFinal = null;
                    }
                        
                    await _occurrenceRepository.SaveChangesAsync();
                }
            }
        }

        if (ofContext != null && ofContext.EstEnReglage)
        {
            var reglageOccurrences = ofContext.ExecPrelevementIntermediaires.Where(o => o.TrancheHoraire.StartsWith("REGLAGE")).ToList();
            bool hasUnanswered = reglageOccurrences.Any(o => !o.EstRepondu);

            if (!hasUnanswered && reglageOccurrences.Any())
            {
                var latestReglagePerSection = reglageOccurrences
                    .GroupBy(o => o.SectionId)
                    .Select(g => g.OrderByDescending(o => o.HeureNotifPrevue).First())
                    .ToList();

                if (latestReglagePerSection.All(o => o.Resultat == "C"))
                {
                    ofContext.EstEnReglage = false;
                    ofContext.Statut = "EN_COURS";

                    var reglageStart = ofContext.DateFin ?? DateTime.Now;
                    var reglageDuration = DateTime.Now - reglageStart;
                    ofContext.DateDebut = ofContext.DateDebut.Add(reglageDuration);
                    ofContext.DateFin = null; // Clear DateFin since we are EN_COURS

                    _occurrenceRepository.RemoveIntermediaires(reglageOccurrences);
                    await _occurrenceRepository.SaveChangesAsync();

                    // Générer automatiquement les occurrences de production
                    await GenererOccurrencesInitialesAsync(ofContext.Id);
                }
            }
        }

        return true;
    }

    public async Task NettoyerOccurrencesReglageAsync(Guid execControleOfId)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        if (of != null)
        {
            var reglageOccurrences = of.ExecPrelevementIntermediaires.Where(o => o.TrancheHoraire.StartsWith("REGLAGE")).ToList();
            if (reglageOccurrences.Any())
            {
                _occurrenceRepository.RemoveIntermediaires(reglageOccurrences);
                await _occurrenceRepository.SaveChangesAsync();
            }
        }
    }

    public async Task<bool> IgnorerOccurrencesAsync(List<Guid> occurrenceIds, string matriculeOperateur, string? raison = null)
    {
        foreach (var id in occurrenceIds)
        {
            var request = new RepondreOccurrenceRequest 
            { 
                Resultat = "IGNORE", 
                MatriculeOperateur = matriculeOperateur,
                Raison = raison
            };
            await RepondreOccurrenceAsync(id, request);
        }
        return true;
    }

    public async Task<bool> DeclarerOccurrencesReglageAsync(List<Guid> occurrenceIds, string matriculeOperateur)
    {
        if (occurrenceIds == null || !occurrenceIds.Any()) return false;

        var occurrences = new List<ExecPrelevementIntermediaire>();
        foreach (var id in occurrenceIds)
        {
            var occ = await _occurrenceRepository.GetIntermediaireAsync(id);
            if (occ != null && !occ.EstRepondu)
            {
                occ.Resultat = "REGLAGE";
                occ.EstRepondu = true;
                occ.HeureReponse = DateTime.Now;
                occurrences.Add(occ);
            }
        }

        if (!occurrences.Any()) return false;

        // Toutes les occurrences sont censées appartenir à la même tranche et au même OF
        var firstOcc = occurrences.First();
        var trancheHoraire = firstOcc.TrancheHoraire;
        var ofId = firstOcc.ExecControleOfid;

        var trancheExistante = await _occurrenceRepository.GetTrancheExistanteAsync(ofId, trancheHoraire);
        string remarqueHeure = $"Réglage à {firstOcc.HeureNotifPrevue:HH:mm}";

        if (trancheExistante == null)
        {
            trancheExistante = new ExecControleTranche
            {
                Id = Guid.NewGuid(),
                ExecControleOfid = ofId,
                TrancheHoraire = trancheHoraire,
                HeureDebut = occurrences.Min(o => o.HeureNotifPrevue),
                HeureFin = occurrences.Max(o => o.HeureNotifPrevue).AddHours(1),
                ResultatFinal = "REGLAGE",
                MatriculeApprobateur = matriculeOperateur,
                Remarques = remarqueHeure
            };
            _occurrenceRepository.AddTranche(trancheExistante);
        }
        else
        {
            if (trancheExistante.ResultatFinal != "NC")
            {
                trancheExistante.ResultatFinal = "REGLAGE";
            }
            trancheExistante.MatriculeApprobateur = matriculeOperateur;
            if (string.IsNullOrWhiteSpace(trancheExistante.Remarques))
                trancheExistante.Remarques = remarqueHeure;
            else if (!trancheExistante.Remarques.Contains("Réglage"))
                trancheExistante.Remarques += " | " + remarqueHeure;
        }

        await _occurrenceRepository.SaveChangesAsync();

        // Suppression des contrôles intermédiaires de réglage pour nettoyer la base
        _occurrenceRepository.RemoveIntermediaires(occurrences);
        await _occurrenceRepository.SaveChangesAsync();

        return true;
    }

    public async Task AjouterRemarqueTrancheEnCoursAsync(Guid execControleOfId, string remarque)
    {
        var tranche = await _occurrenceRepository.GetDerniereTrancheAsync(execControleOfId);
        if (tranche != null)
        {
            tranche.Remarques = string.IsNullOrWhiteSpace(tranche.Remarques) ? remarque : tranche.Remarques + " | " + remarque;
            await _occurrenceRepository.SaveChangesAsync();
        }
    }
}
