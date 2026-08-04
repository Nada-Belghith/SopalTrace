using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

public partial class OccurrenceService
{
    public async Task<bool> RepondreOccurrenceAsync(Guid occurrenceId, RepondreOccurrenceRequest request)
    {
        var idsToProcess = request.AssociatedIds?.Any() == true
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
            occurrence.EstRepondu = !(occurrence.TrancheHoraire.StartsWith("REGLAGE") && request.Resultat != "C");
            occurrence.HeureReponse = DateTime.Now;

                if (!trancheUpdated)
                {
                    await MettreAJourRemarquesTrancheAsync(occurrence, request);
                    trancheUpdated = true;
                }

                string contexte = await _occurrenceRepository.GetContexteOccurrenceAsync(occurrence.SectionId);
                if (occurrence.TrancheHoraire.StartsWith("REGLAGE")) contexte = "REGLAGE";

                await EnregistrerReponsesLignesAsync(occurrence, ofContext, request, contexte);
        }

        await _occurrenceRepository.SaveChangesAsync();

        if (ofContext != null && trancheHoraire != null)
        {
            await MettreAJourResultatTrancheAsync(ofContext.Id, trancheHoraire);
        }

        if (ofContext?.EstEnReglage == true)
        {
            await TransitionnerReglageVersProductionAsync(ofContext);
        }

        if (ofContext != null)
        {
            await NettoyerTranchesReponduesAsync(ofContext.Id);
        }

        return true;
    }

    private async Task NettoyerTranchesReponduesAsync(Guid execControleOfId)
    {
        var all = await _occurrenceRepository.GetIntermediairesParOfAsync(execControleOfId);
        bool cleanedAny = false;

        foreach (var grp in all.Where(o => o.EstRepondu).GroupBy(o => o.TrancheHoraire.Split('|')[0]))
        {
            var trEx = await _occurrenceRepository.GetTrancheExistanteAsync(execControleOfId, grp.Key);
            var allForTranche = all.Where(o => o.TrancheHoraire == grp.Key || o.TrancheHoraire.StartsWith(grp.Key)).ToList();

            if (trEx != null && !string.IsNullOrEmpty(trEx.ResultatFinal) && allForTranche.All(o => o.EstRepondu))
            {
                _occurrenceRepository.RemoveIntermediaires(allForTranche);
                cleanedAny = true;
            }
        }

        if (cleanedAny) await _occurrenceRepository.SaveChangesAsync();
    }

    public async Task<bool> IgnorerOccurrencesAsync(List<Guid> occurrenceIds, string matriculeOperateur, string? raison = null)
    {
        foreach (var id in occurrenceIds)
        {
            await RepondreOccurrenceAsync(id, new RepondreOccurrenceRequest
            {
                Resultat = "IGNORE",
                MatriculeOperateur = matriculeOperateur,
                Raison = raison
            });
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

        var firstOcc = occurrences.First();
        var tranche = await _occurrenceRepository.GetTrancheExistanteAsync(firstOcc.ExecControleOfid, firstOcc.TrancheHoraire);
        string remarqueHeure = $"Réglage à {firstOcc.HeureNotifPrevue:HH:mm}";

        if (tranche == null)
        {
            _occurrenceRepository.AddTranche(new ExecControleTranche
            {
                Id = Guid.NewGuid(),
                ExecControleOfid = firstOcc.ExecControleOfid,
                TrancheHoraire = firstOcc.TrancheHoraire,
                HeureDebut = occurrences.Min(o => o.HeureNotifPrevue),
                HeureFin = occurrences.Max(o => o.HeureNotifPrevue).AddHours(1),
                ResultatFinal = "REGLAGE",
                MatriculeApprobateur = matriculeOperateur,
                Remarques = remarqueHeure
            });
        }
        else
        {
            if (tranche.ResultatFinal != "NC") tranche.ResultatFinal = "REGLAGE";
            tranche.MatriculeApprobateur = matriculeOperateur;
            if (string.IsNullOrWhiteSpace(tranche.Remarques))
                tranche.Remarques = remarqueHeure;
            else if (!tranche.Remarques.Contains("Réglage"))
                tranche.Remarques += " | " + remarqueHeure;
        }

        await _occurrenceRepository.SaveChangesAsync();

        await NettoyerTranchesReponduesAsync(firstOcc.ExecControleOfid);

        return true;
    }

    public async Task AjouterRemarqueTrancheEnCoursAsync(Guid execControleOfId, string remarque)
    {
        var tranche = await _occurrenceRepository.GetDerniereTrancheAsync(execControleOfId);
        if (tranche == null) return;

        tranche.Remarques = string.IsNullOrWhiteSpace(tranche.Remarques)
            ? remarque
            : tranche.Remarques + " | " + remarque;

        await _occurrenceRepository.SaveChangesAsync();
    }

    private async Task EnregistrerReponsesLignesAsync(
        ExecPrelevementIntermediaire occurrence, ExecControleOf? ofContext,
        RepondreOccurrenceRequest request, string contexte)
    {
        // Ne pas enregistrer le détail des lignes pour les contrôles en fréquence (échantillonnage/lot)
        // Les résultats globaux sont déjà enregistrés dans Exec_Prelevement_Intermediaire et Exec_ControleTranche
        if (contexte == "LOT")
        {
            return;
        }

        if (ofContext?.TypeOf == "FAB" && contexte == "REGLAGE")
        {
            string resultat = request.Lignes.Any(l => l.Resultat == "NC") ? "NC" : "C";
            string? remarques = string.Join(" | ", request.Lignes
                .Where(l => !string.IsNullOrWhiteSpace(l.Remarque)).Select(l => l.Remarque));

            _occurrenceRepository.AddPieceType(new ExecPieceType
            {
                Id = Guid.NewGuid(),
                ExecControleOfid = occurrence.ExecControleOfid,
                HeureValidation = DateTime.Now,
                Resultat = resultat,
                Remarque = string.IsNullOrWhiteSpace(remarques) ? null : remarques,
                MatriculeOperateur = request.MatriculeOperateur ?? ""
            });
        }
        else
        {
            var operateurId = await _occurrenceRepository.GetUtilisateurIdByMatriculeAsync(request.MatriculeOperateur ?? "");
            if (operateurId == null)
                throw new Exception($"Opérateur avec le matricule '{request.MatriculeOperateur}' introuvable. Veuillez vous reconnecter.");

            _occurrenceRepository.AddLigneReponses(request.Lignes.Select(l => new ExecControleLigneReponse
            {
                Id = Guid.NewGuid(),
                ExecControleOfid = occurrence.ExecControleOfid,
                DocumentLigneId = ofContext?.TypeOf == "ASS" ? l.LignePlanId : null,
                Contexte = contexte,
                NumeroReglage = contexte == "REGLAGE" ? "REG" + occurrence.NumeroOccurrence : null,
                ResultatType = l.Resultat == "C" ? "CONFORME" : l.Resultat == "NC" ? "NON_CONFORME" : "MESURE",
                ValeurMesuree = (decimal?)l.ValeurMesuree,
                DetailsNc = l.Resultat == "NC" ? l.Remarque : null,
                ActionsCorrection = l.ActionCorrective,
                OperateurId = operateurId.Value,
                DateSaisie = DateTime.Now
            }).ToList());
        }
    }

    private async Task MettreAJourRemarquesTrancheAsync(
        ExecPrelevementIntermediaire occurrence, RepondreOccurrenceRequest request)
    {
        var tranche = await _occurrenceRepository.GetTrancheExistanteAsync(occurrence.ExecControleOfid, occurrence.TrancheHoraire);

        if (tranche == null)
        {
            var (debut, fin) = ParseHeureTranche(occurrence.TrancheHoraire, occurrence.HeureNotifPrevue);
            _occurrenceRepository.AddTranche(tranche = new ExecControleTranche
            {
                Id = Guid.NewGuid(),
                ExecControleOfid = occurrence.ExecControleOfid,
                TrancheHoraire = occurrence.TrancheHoraire,
                HeureDebut = debut, HeureFin = fin,
                ResultatFinal = null,
                MatriculeApprobateur = request.MatriculeOperateur
            });
        }
        else if (string.IsNullOrEmpty(tranche.MatriculeApprobateur) && !string.IsNullOrEmpty(request.MatriculeOperateur))
        {
            tranche.MatriculeApprobateur = request.MatriculeOperateur;
        }

        var remarquesC = request.Lignes.Where(l => l.Resultat == "C" && !string.IsNullOrWhiteSpace(l.Remarque)).Select(l => l.Remarque!).ToList();
        var detailsNc = request.Lignes.Where(l => l.Resultat == "NC" && !string.IsNullOrWhiteSpace(l.Remarque)).Select(l => l.Remarque!).ToList();
        var actions = request.Lignes.Where(l => l.Resultat == "NC" && !string.IsNullOrWhiteSpace(l.ActionCorrective)).Select(l => l.ActionCorrective!).ToList();

        if (!string.IsNullOrWhiteSpace(request.Raison))
            remarquesC.Add(request.Resultat == "IGNORE" ? $"Ignoré : {request.Raison}" : request.Raison);

        string joinedRemarques = string.Join(" | ", remarquesC);
        string joinedDetailsNc = string.Join(" | ", detailsNc);
        string joinedActions = string.Join(" | ", actions);

        if (!string.IsNullOrWhiteSpace(joinedRemarques))
            tranche.Remarques = string.IsNullOrWhiteSpace(tranche.Remarques) ? joinedRemarques
                : tranche.Remarques.Contains(joinedRemarques) ? tranche.Remarques
                : tranche.Remarques + " | " + joinedRemarques;

        if (!string.IsNullOrWhiteSpace(joinedDetailsNc))
            tranche.DetailsNc = string.IsNullOrWhiteSpace(tranche.DetailsNc) ? joinedDetailsNc
                : tranche.DetailsNc.Contains(joinedDetailsNc) ? tranche.DetailsNc
                : tranche.DetailsNc + " | " + joinedDetailsNc;

        if (!string.IsNullOrWhiteSpace(joinedActions))
            tranche.ActionsCorrection = string.IsNullOrWhiteSpace(tranche.ActionsCorrection)
                ? joinedActions
                : tranche.ActionsCorrection + " | " + joinedActions;
    }

    private async Task MettreAJourResultatTrancheAsync(Guid ofId, string trancheHoraire)
    {
        var allOccurrences = await _occurrenceRepository.GetIntermediairesParTrancheAsync(ofId, trancheHoraire);
        if (!allOccurrences.Any()) return;

        var tranche = await _occurrenceRepository.GetTrancheExistanteAsync(ofId, trancheHoraire);
        if (tranche == null) return;

        bool hasNC = allOccurrences.Any(o => o.Resultat == "NC");
        bool hasC = allOccurrences.Any(o => o.Resultat == "C");
        bool allReglage = allOccurrences.All(o => o.Resultat == "REGLAGE");
        bool allRepondu = allOccurrences.All(o => o.EstRepondu);

        tranche.ResultatFinal = hasNC ? "NC"
            : allRepondu ? (hasC ? "C" : allReglage ? "REGLAGE" : "IGNORE")
            : null;



        await _occurrenceRepository.SaveChangesAsync();
    }

    private async Task TransitionnerReglageVersProductionAsync(ExecControleOf ofContext)
    {
        var reglageOccurrences = ofContext.ExecPrelevementIntermediaires
            .Where(o => o.TrancheHoraire.StartsWith("REGLAGE")).ToList();

        if (reglageOccurrences.Any(o => !o.EstRepondu)) return;
        if (!reglageOccurrences.Any()) return;

        var latestPerSection = reglageOccurrences
            .GroupBy(o => o.SectionId)
            .Select(g => g.OrderByDescending(o => o.HeureNotifPrevue).First())
            .ToList();

        if (!latestPerSection.All(o => o.Resultat == "C")) return;

        ofContext.EstEnReglage = false;
        ofContext.Statut = "EN_COURS";
        ofContext.DateFin = null;
        await AjusterDateDebutApresReglageAsync(ofContext.Id);

        await _occurrenceRepository.SaveChangesAsync();
        await GenererOccurrencesInitialesAsync(ofContext.Id);
    }
}
