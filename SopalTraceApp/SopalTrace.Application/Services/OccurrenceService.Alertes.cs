using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Domain.Constants;
using SopalTrace.Application.Helpers;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

public partial class OccurrenceService
{
    public async Task<List<TrancheAlertesDto>> GetAlertesActivesAsync(Guid execControleOfId, string? posteCode = null, string? equipe = null)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        if (of == null) return new List<TrancheAlertesDto>();

        var occurrences = (await _occurrenceRepository.GetIntermediairesActifsAsync(execControleOfId))
            .OrderBy(o => o.HeureNotifPrevue).ToList();

        bool isAssOf = of.TypeOf == TypesOf.Assemblage;

        var docSectionsForLabels = isAssOf
            ? (!string.IsNullOrEmpty(posteCode)
                ? await _occurrenceRepository.GetDocumentSectionsActivesParPosteAsync(execControleOfId, posteCode)
                : await _occurrenceRepository.GetDocumentSectionsActivesAsync(execControleOfId))
            : new List<DocumentSection>();

        // Assemblage : contrôles de l'équipe (par défaut celle qui tient le poste) sur le poste, planifiés par le serveur.
        if (isAssOf)
        {
            var sessionId = string.IsNullOrWhiteSpace(equipe)
                ? string.IsNullOrEmpty(posteCode) ? null : await _occurrenceRepository.GetSessionIdDuPosteAsync(execControleOfId, posteCode)
                : of.ExecControleOfSessions?
                    .Where(s => s.Statut != StatutsSession.Termine && string.Equals(s.Equipe, equipe, StringComparison.OrdinalIgnoreCase))
                    .Select(s => (Guid?)s.Id)
                    .FirstOrDefault();
            occurrences = occurrences
                .Where(o => o.PosteCode != null && o.SessionId == sessionId &&
                            (string.IsNullOrEmpty(posteCode) || o.PosteCode == posteCode))
                .ToList();
        }

        if (of.Statut == StatutsExecution.Reglage)
        {
            occurrences = occurrences.Where(o => o.TrancheHoraire.StartsWith("REGLAGE") || o.EstRepondu).ToList();
        }

        bool isPaused = of.Statut == StatutsExecution.EnPause;
        DateTime pauseStart = of.SuspendedAt ?? DateTime.Now;

        var sectionsActives = of.PlanSourceId.HasValue
            ? await _occurrenceRepository.GetSectionsActivesAsync(of.PlanSourceId.Value)
            : new List<DocumentSection>();

        // Retards et escalade : tenus par la planification (tâche de fond et synchronisation avant lecture).

        var alertesVisibles = FiltrerOccurrencesVisibles(occurrences, isPaused, pauseStart);

        var sectionIds = alertesVisibles.Select(o => o.SectionId).Distinct().ToList();
        var lignesPerSection = await ChargerLignesParSectionAsync(sectionIds, isAssOf);

        var colDefsMap = await ResolveColDefsAsync(of, isAssOf);

        var tranches = new List<TrancheAlertesDto>();
        var trancheNoms = alertesVisibles.Select(o => o.TrancheHoraire.Split('|')[0]).Distinct().ToList();

        foreach (var trancheHoraire in trancheNoms)
        {
            var trancheResult = await _occurrenceRepository.GetTrancheExistanteAsync(execControleOfId, trancheHoraire);
            
            tranches.Add(new TrancheAlertesDto
            {
                TrancheHoraire = trancheHoraire,
                ResultatFinal = trancheResult?.ResultatFinal,
                Remarques = trancheResult?.Remarques,
                DetailsNc = trancheResult?.DetailsNc,
                Occurrences = alertesVisibles
                    .Where(o => o.TrancheHoraire.StartsWith(trancheHoraire))
                    .GroupBy(o => o.HeureNotifPrevue.ToString("yyyyMMddHHmmss"))
                    .Select(timeGroup => MapperOccurrenceDto(timeGroup, of, isAssOf, sectionsActives, lignesPerSection, docSectionsForLabels, colDefsMap))
                    .ToList()
            });
        }

        return tranches;
    }



    public async Task VerifierEscaladeAsync(Guid execControleOfId)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        if (of == null) return;
        var occurrences = await _occurrenceRepository.GetIntermediairesParOfAsync(execControleOfId);
        await DeclencherAlerteEscaladeAsync(of, occurrences);
    }

    private async Task DeclencherAlerteEscaladeAsync(ExecControleOf of, List<ExecPrelevementIntermediaire> occurrences)
    {
        var derniereReponse = occurrences.Where(o => o.EstRepondu)
            .OrderByDescending(o => o.HeureNotifPrevue).FirstOrDefault();

        var retards = occurrences
            .Where(o => o.EstEnRetard && !o.EstRepondu)
            .Where(o => derniereReponse == null || o.HeureNotifPrevue > derniereReponse.HeureNotifPrevue)
            .OrderBy(o => o.HeureNotifPrevue).ToList();

        int nbRetards = retards.GroupBy(o => o.HeureNotifPrevue.ToString("yyyyMMddHHmmss")).Count();

        if (nbRetards >= 3)
        {
            var duree = DateTime.Now - retards.First().HeureNotifPrevue;
            if (nbRetards >= 10)
                await _alerteManagerService.DeclencherAsync(new SopalTrace.Application.Alertes.NotifsManagerContexte
                {
                    ExecControleOfId = of.Id, NumeroOf = of.NumeroOf,
                    ArticleCode = of.NumeroOfNavigation?.CodeArticle ?? "Inconnu",
                    OperationCode = of.OperationCode ?? "Inconnue",
                    NbNotificationsManquees = nbRetards, DureeDepuisPremierRetard = duree
                });
            else
                await _alerteSuperviseurService.DeclencherAsync(new SopalTrace.Application.Alertes.NotifsSuperviseurContexte
                {
                    ExecControleOfId = of.Id, NumeroOf = of.NumeroOf,
                    ArticleCode = of.NumeroOfNavigation?.CodeArticle ?? "Inconnu",
                    OperationCode = of.OperationCode ?? "Inconnue",
                    NbNotificationsManquees = nbRetards, DureeDepuisPremierRetard = duree
                });
        }
        else
        {
            await _unitOfWork.AlerteRepository.ResoudreAlertesPourEntiteAsync(of.Id.ToString(), "Système Automatique");
            await _unitOfWork.CommitAsync();
        }
    }

    private List<ExecPrelevementIntermediaire> FiltrerOccurrencesVisibles(
        List<ExecPrelevementIntermediaire> occurrences, bool isPaused, DateTime pauseStart)
    {
        DateTime refTime = isPaused ? pauseStart : DateTime.Now;
        bool futureAdded = false;

        var visible = new List<ExecPrelevementIntermediaire>();
        var unanswered = occurrences.Where(o => !o.EstRepondu).ToList();
        
        foreach (var g in unanswered.GroupBy(o => o.HeureNotifPrevue.ToString("yyyyMMddHHmmss"))
                                      .OrderBy(g => g.First().HeureNotifPrevue))
        {
            if (g.First().HeureNotifPrevue <= refTime)
                visible.AddRange(g);
            else if (!futureAdded)
            {
                visible.AddRange(g);
                futureAdded = true;
            }
        }

        return visible.Take(50).ToList();
    }

    private async Task<(Dictionary<Guid, List<DocumentLigne>> fab, Dictionary<Guid, List<DocumentLigne>> ass)>
        ChargerLignesParSectionAsync(List<Guid> sectionIds, bool isAssOf)
    {
        var fab = new Dictionary<Guid, List<DocumentLigne>>();
        var ass = new Dictionary<Guid, List<DocumentLigne>>();

        foreach (var sid in sectionIds)
        {
            if (isAssOf) ass[sid] = await _occurrenceRepository.GetDocumentLignesForSectionAsync(sid);
            else fab[sid] = await _occurrenceRepository.GetLignesForSectionAsync(sid);
        }

        return (fab, ass);
    }

    private async Task<Dictionary<string, string?>> ChargerResultatsTranchesAsync(
        Guid execControleOfId, List<ExecPrelevementIntermediaire> occurrences)
    {
        var result = new Dictionary<string, string?>();
        foreach (var t in occurrences.Select(o => o.TrancheHoraire.Split('|')[0]).Distinct())
        {
            var tr = await _occurrenceRepository.GetTrancheExistanteAsync(execControleOfId, t);
            if (tr != null) result[t] = tr.ResultatFinal;
        }
        return result;
    }

    private async Task<Dictionary<string, string>> ResolveColDefsAsync(ExecControleOf of, bool isAssOf)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Guid? formulaireId = null;

        if (of.PlanSourceId.HasValue)
        {
            var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(of.PlanSourceId.Value);
            formulaireId = plan?.FormulaireId;
        }
        else if (isAssOf)
        {
            var stat = of.ExecControleDocumentStatuts?.FirstOrDefault(s => s.TypeDocument == TypesDocumentExecution.PlanAssemblage);
            if (stat?.DocId.HasValue == true)
            {
                var doc = await _unitOfWork.DocumentEnteteRepository.GetByIdAsync(stat.DocId.Value);
                formulaireId = doc?.FormulaireId;
            }
        }

        if (formulaireId.HasValue)
        {
            var cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(formulaireId.Value);
            foreach (var c in cols.Where(c => !string.IsNullOrEmpty(c.CleColonne)))
                map[c.CleColonne] = c.LabelAffiche;
        }

        return map;
    }

    private OccurrenceDto MapperOccurrenceDto(
        IGrouping<string, ExecPrelevementIntermediaire> timeGroup,
        ExecControleOf of,
        bool isAssOf,
        List<DocumentSection> sectionsActives,
        (Dictionary<Guid, List<DocumentLigne>> fab, Dictionary<Guid, List<DocumentLigne>> ass) lignesPerSection,
        List<DocumentSection> docSectionsForLabels,
        Dictionary<string, string> colDefsMap)
    {
        var firstOcc = timeGroup.First();
        var numeros = timeGroup.Where(x => x.NumeroOccurrence > 0).Select(x => x.NumeroOccurrence).Distinct().ToList();
        string titreCombine = numeros.Count > 0 ? "Occurrence #" + numeros.Max() : "Occurrence #" + firstOcc.NumeroOccurrence;

        return new OccurrenceDto
        {
            Id = firstOcc.Id,
            AssociatedIds = timeGroup.Select(x => x.Id).ToList(),
            SectionId = firstOcc.SectionId,
            TrancheHoraire = firstOcc.TrancheHoraire,
            NumeroOccurrence = firstOcc.NumeroOccurrence,
            TitreCombine = titreCombine,
            HeureNotifPrevue = firstOcc.HeureNotifPrevue,
            HeureSimulee = firstOcc.HeureNotifPrevue,
            EstEnRetard = timeGroup.Any(x => x.EstEnRetard),
            Resultat = firstOcc.Resultat,
            Caracteristiques = isAssOf
                ? MapperCaracteristiquesAss(timeGroup, lignesPerSection.ass, docSectionsForLabels, colDefsMap)
                : MapperCaracteristiquesFab(timeGroup, lignesPerSection.fab, sectionsActives, colDefsMap, of)
        };
    }

    private List<CaracteristiqueARepondreDto> MapperCaracteristiquesFab(
        IEnumerable<ExecPrelevementIntermediaire> timeGroup,
        Dictionary<Guid, List<DocumentLigne>> lignesPerSection,
        List<DocumentSection> sectionsActives,
        Dictionary<string, string> colDefsMap,
        ExecControleOf of)
    {
        return timeGroup.SelectMany(x =>
        {
            var lignes = lignesPerSection.GetValueOrDefault(x.SectionId, new());
            var section = sectionsActives.FirstOrDefault(s => s.Id == x.SectionId);
            string libelle = NormaliserLibelle(section?.LibelleSection ?? "Caractéristiques", section);

            return lignes.Select(l => new CaracteristiqueARepondreDto
            {
                SectionId = x.SectionId, SectionLibelle = libelle, LignePlanId = l.Id,
                // Cotes de tronçonnage saisies au démarrage de l'OF (le plan garde sa propre valeur).
                Libelle = CotesTronconnage.Appliquer(l.LibelleAffiche ?? "", of.LongueurNominale, of.DiametreNominal), LimiteSpecTexte = l.LimiteSpecTexte,
                Observations = l.Observations, TypeControle = l.TypeControle?.Code ?? "N/A",
                MoyenControle = l.MoyenControle?.Libelle, Instrument = l.InstrumentCode,
                ImageBase64 = l.EnDataUrl(),
                ExtraColonnes = l.DocumentLigneExtraColonnes?.Select(ec => new CaracteristiqueExtraColonneDto
                {
                    CleColonne = ec.CleColonne,
                    LabelAffiche = colDefsMap.TryGetValue(ec.CleColonne, out var lbl) ? lbl : ec.CleColonne,
                    ValeurColonne = ec.ValeurColonne
                }).ToList() ?? new()
            });
        }).ToList();
    }

    private List<CaracteristiqueARepondreDto> MapperCaracteristiquesAss(
        IEnumerable<ExecPrelevementIntermediaire> timeGroup,
        Dictionary<Guid, List<DocumentLigne>> docLignesPerSection,
        List<DocumentSection> docSectionsForLabels,
        Dictionary<string, string> colDefsMap)
    {
        return timeGroup.SelectMany(x =>
        {
            var lignes = docLignesPerSection.GetValueOrDefault(x.SectionId, new());
            var sec = docSectionsForLabels.FirstOrDefault(s => s.Id == x.SectionId);
            string baseLibelle = NormaliserLibelle(sec?.LibelleSection ?? "Caractéristiques", sec);
            string docPrefix = "";
            if (sec?.Entete != null)
            {
                string typeCode = (sec.Entete.TypeDocumentCode ?? "").ToUpper();
                if (typeCode is TypesDocument.ResultatCF or TypesDocument.ControlePoste)
                    docPrefix = "[Contrôle CF] ";
                else if (typeCode is TypesDocument.PlanAssemblage)
                    docPrefix = "[Plan d'Assemblage] ";
            }
            string libelle = docPrefix + baseLibelle;

            return lignes.Select(l => new CaracteristiqueARepondreDto
            {
                SectionId = x.SectionId, SectionLibelle = libelle, LignePlanId = l.Id,
                Libelle = l.LibelleAffiche ?? l.Caracteristique?.Libelle ?? "", LimiteSpecTexte = l.LimiteSpecTexte,
                Observations = l.Observations, TypeControle = l.TypeControle?.Code ?? "N/A",
                MoyenControle = l.MoyenControle?.Libelle, Instrument = l.InstrumentCode,
                ImageBase64 = l.EnDataUrl(),
                ExtraColonnes = l.DocumentLigneExtraColonnes?.Select(ec => new CaracteristiqueExtraColonneDto
                {
                    CleColonne = ec.CleColonne,
                    LabelAffiche = colDefsMap.TryGetValue(ec.CleColonne, out var lbl) ? lbl : ec.CleColonne,
                    ValeurColonne = ec.ValeurColonne
                }).ToList() ?? new()
            });
        }).ToList();
    }
}
