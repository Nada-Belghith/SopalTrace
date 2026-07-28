using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services
{
    public class ExecPlanAssemblageService : IExecPlanAssemblageService
    {
        private readonly IExecPlanAssemblageRepository _repository;
        private readonly IOperateurService _operateurService;

        public ExecPlanAssemblageService(IExecPlanAssemblageRepository repository, IOperateurService operateurService)
        {
            _repository = repository;
            _operateurService = operateurService;
        }

        public async Task<ExecPlanAssemblageDto> GetOrInitExecutionAsync(Guid execControleOfId, string? posteCode)
        {
            var execOf = await _repository.GetExecOfAsync(execControleOfId);
            if (execOf == null)
            {
                throw new Exception("Exécution OF introuvable.");
            }

            var codeArticle = execOf.NumeroOfNavigation?.CodeArticle ?? "";
            var (planAss, docRes) = await _repository.GetDocumentsAssemblageEtResultatAsync(codeArticle);

            var statut = await _repository.GetStatutPlanAssAsync(execControleOfId);
            if (statut != null && statut.DocId.HasValue)
            {
                var existingPlan = await _repository.GetDocumentByIdAsync(statut.DocId.Value);
                if (existingPlan != null) planAss = existingPlan;
            }

            var statutsAll = await _repository.GetStatutsAssemblageAsync(execControleOfId);
            var statutResExisting = statutsAll.FirstOrDefault(s => s.TypeDocument == "RESULTAT_CF" || s.TypeDocument == "RCCF" || s.TypeDocument == "CTRL_POSTE");
            if (statutResExisting != null && statutResExisting.DocId.HasValue)
            {
                var existingRes = await _repository.GetDocumentByIdAsync(statutResExisting.DocId.Value);
                if (existingRes != null) docRes = existingRes;
            }

            var plan = planAss ?? docRes;
            if (plan == null)
            {
                throw new Exception($"Aucun document (Plan d'assemblage ou Résultat en cours) trouvé pour l'article {codeArticle}.");
            }

            var ech = await _repository.GetExecEchantillonnageAsync(execControleOfId);
            int effectifParHeure = ech?.EffectifParPosteAb ?? 4;
            if (effectifParHeure <= 0) effectifParHeure = 4;

            var tranches = await _repository.GetTranchesAsync(execControleOfId);
            if (statut == null)
            {
                statut = new ExecControleDocumentStatut
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfId = execControleOfId,
                    TypeDocument = "PLAN_ASS",
                    EstTermine = false,
                    DateExecution = DateTime.Now,
                    PosteCode = null,
                    DocId = plan.Id
                };
                _repository.AddStatut(statut);
                await _repository.SaveChangesAsync();
            }
            else if (statut.DocId == null)
            {
                statut.DocId = plan.Id;
                await _repository.SaveChangesAsync();
            }

            if (docRes != null)
            {
                var statutRes = statutResExisting ?? await _repository.GetStatutResultatCfAsync(execControleOfId, docRes.Id);
                if (statutRes == null)
                {
                    statutRes = new ExecControleDocumentStatut
                    {
                        Id = Guid.NewGuid(),
                        ExecControleOfId = execControleOfId,
                        TypeDocument = !string.IsNullOrEmpty(docRes.TypeDocumentCode) ? docRes.TypeDocumentCode : "RESULTAT_CF",
                        EstTermine = false,
                        DateExecution = DateTime.Now,
                        PosteCode = null,
                        DocId = docRes.Id
                    };
                    _repository.AddStatut(statutRes);
                    await _repository.SaveChangesAsync();
                }
                else if (statutRes.DocId == null)
                {
                    statutRes.DocId = docRes.Id;
                    await _repository.SaveChangesAsync();
                }
            }

            // Supprimer les anciennes tranches ECH|... et LOT|... non répondues (migration vers OccurrenceService)
            var staticTranchesAll = tranches.Where(t => t.TrancheHoraire != null && (t.TrancheHoraire.StartsWith("ECH|") || (t.TrancheHoraire.StartsWith("LOT|") && string.IsNullOrEmpty(t.ResultatFinal)))).ToList();
            if (staticTranchesAll.Count > 0)
            {
                _repository.RemoveTranches(staticTranchesAll);
                await _repository.SaveChangesAsync();
                tranches = await _repository.GetTranchesAsync(execControleOfId);
            }

            // Supprimer les anciennes tranches REG (les réponses lignes vont uniquement dans Exec_ControleLigne_Reponse)
            var regTranches = tranches.Where(t => t.TrancheHoraire == "REG").ToList();
            if (regTranches.Count > 0)
            {
                _repository.RemoveTranches(regTranches);
                await _repository.SaveChangesAsync();
                tranches = await _repository.GetTranchesAsync(execControleOfId);
            }

            // Auto-configuration initiale si aucune tranche n'existe (REGLAGE et LOT_POSTE)
            if (tranches.Count == 0)
            {
                await ConfigurerHorairesAsync(execControleOfId, new ConfigHeuresPosteRequest
                {
                    Postes = new List<PosteHoraireDto>
                    {
                        new PosteHoraireDto { PosteCode = !string.IsNullOrEmpty(posteCode) ? posteCode : "A", HeureDebut = "06:00", HeureFin = "14:00" }
                    }
                });
                tranches = await _repository.GetTranchesAsync(execControleOfId);
            }

            var dto = new ExecPlanAssemblageDto
            {
                ExecControleOfId = execControleOfId,
                NumeroOf = execOf.NumeroOf,
                CodeArticle = codeArticle,
                DesignationArticle = execOf.NumeroOfNavigation?.CodeArticleNavigation?.Designation ?? "",
                Atelier = "ASSEMBLAGE",
                Statut = execOf.Statut,
                DateDebut = execOf.DateDebut,
                EffectifEchantillonParHeure = effectifParHeure
            };

            var cfgTranches = tranches.Where(t => t.TrancheHoraire == "CFG_POSTE").ToList();
            foreach (var cfg in cfgTranches)
            {
                if (!string.IsNullOrEmpty(cfg.Remarques) && cfg.Remarques.StartsWith("CFG|"))
                {
                    var parts = cfg.Remarques.Split('|');
                    if (parts.Length >= 4)
                    {
                        dto.PostesConfigures.Add(new PosteHoraireDto
                        {
                            PosteCode = parts[1],
                            HeureDebut = parts[2],
                            HeureFin = parts[3]
                        });
                    }
                }
            }

            var sectionsMap = new Dictionary<string, ExecPlanAssemblageSectionDto>();

            ExecPlanAssemblageLignePlanDto MapLigne(DocumentLigne ligne)
            {
                string carTxt = ligne.LibelleAffiche ?? ligne.Caracteristique?.Libelle ?? "";
                string typeCtrlTxt = ligne.TypeControle?.Libelle ?? "";
                string moyenCtrlTxt = !string.IsNullOrEmpty(ligne.MoyenTexteLibre) ? ligne.MoyenTexteLibre : (ligne.MoyenControle?.Libelle ?? "");

                return new ExecPlanAssemblageLignePlanDto
                {
                    Id = ligne.Id,
                    Numero = ligne.OrdreAffiche,
                    Caracteristique = carTxt,
                    LimiteSpecTexte = ligne.LimiteSpecTexte,
                    TypeControle = typeCtrlTxt,
                    MoyenControle = moyenCtrlTxt,
                    Instrument = ligne.InstrumentCode,
                    Observations = ligne.Observations,
                    ImageBase64 = ligne.ImageBase64
                };
            }

            string GetSectionMapKey(DocumentSection sec)
            {
                string type = DetecterTypeSection(sec);
                if (type == "REGLAGE" || type == "REGLAGE_PROD")
                {
                    return "REGLAGE";
                }
                string lib = (sec.LibelleSection ?? "").Trim();
                int idx = lib.IndexOf("(");
                if (idx > 0)
                {
                    lib = lib.Substring(0, idx).Trim();
                }
                return type + "|" + lib.ToUpper();
            }

            if (planAss != null && planAss.DocumentSections != null)
            {
                foreach (var sec in planAss.DocumentSections.OrderBy(s => s.OrdreAffiche))
                {
                    string key = GetSectionMapKey(sec);
                    var secDto = new ExecPlanAssemblageSectionDto
                    {
                        Id = sec.Id,
                        Libelle = sec.LibelleSection,
                        Ordre = sec.OrdreAffiche,
                        TypeSection = DetecterTypeSection(sec)
                    };
                    foreach (var ligne in sec.DocumentLignes.OrderBy(l => l.OrdreAffiche))
                    {
                        secDto.LignesPlan.Add(MapLigne(ligne));
                    }
                    if (!sectionsMap.ContainsKey(key))
                        sectionsMap.Add(key, secDto);
                }
            }

            if (docRes != null && docRes != planAss && docRes.DocumentSections != null)
            {
                foreach (var sec in docRes.DocumentSections.OrderBy(s => s.OrdreAffiche))
                {
                    string key = GetSectionMapKey(sec);
                    if (sectionsMap.TryGetValue(key, out var existingDto))
                    {
                        foreach (var ligne in sec.DocumentLignes.OrderBy(l => l.OrdreAffiche))
                        {
                            existingDto.LignesResultat.Add(MapLigne(ligne));
                        }
                    }
                    else
                    {
                        var secDto = new ExecPlanAssemblageSectionDto
                        {
                            Id = sec.Id,
                            Libelle = sec.LibelleSection,
                            Ordre = sec.OrdreAffiche,
                            TypeSection = DetecterTypeSection(sec)
                        };
                        foreach (var ligne in sec.DocumentLignes.OrderBy(l => l.OrdreAffiche))
                        {
                            secDto.LignesResultat.Add(MapLigne(ligne));
                        }
                        sectionsMap.Add(key, secDto);
                    }
                }
            }

            var reponsesLignes = await _repository.GetLigneReponsesAsync(execControleOfId);

            // Le réglage est considéré terminé si toutes les lignes des sections REGLAGE ont une réponse dans Exec_ControleLigne_Reponse
            var sections = sectionsMap.Values.OrderBy(s => s.Ordre).ToList();
            var reglageSection = sections.FirstOrDefault(s => s.TypeSection == "REGLAGE" || s.TypeSection == "REGLAGE_PROD");
            bool isReglageTermine = reglageSection == null ||
                (reglageSection.LignesPlan.Count == 0) ||
                reglageSection.LignesPlan.All(l => reponsesLignes.Any(r => r.DocumentLigneId == l.Id && r.Contexte == "REGLAGE"));

            foreach (var secDto in sections)
            {

                // Lignes Réglage : on lit uniquement depuis Exec_ControleLigne_Reponse — pas de tranche REG
                if (secDto.TypeSection == "REGLAGE" || secDto.TypeSection == "REGLAGE_PROD")
                {
                    foreach (var ligne in secDto.LignesPlan)
                    {
                        var rep = reponsesLignes.FirstOrDefault(r => r.DocumentLigneId == ligne.Id && r.Contexte == "REGLAGE");
                        secDto.Resultats.Add(new ExecPlanAssemblageRowDto
                        {
                            Id = Guid.Empty,   // Pas de tranche — on utilise Guid.Empty pour indiquer "réglage direct"
                            LigneId = ligne.Id,
                            Frequence = !string.IsNullOrEmpty(ligne.Caracteristique) ? ligne.Caracteristique : "Caractéristique au réglage",
                            HeurePrevue = execOf.DateDebut,
                            StatutNotif = rep != null ? "FAIT" : "A_FAIRE",
                            Resultat = rep != null ? (rep.ResultatType == "NON_CONFORME" ? "NC" : "C") : null,
                            NonConformite = rep?.DetailsNc,
                            ActionCorrective = rep?.ActionsCorrection,
                            Approbation = rep?.OperateurId.ToString(),
                            Remarques = null
                        });
                    }
                }

                if (secDto.TypeSection == "ECHANTILLONNAGE" || secDto.TypeSection == "REGLAGE_PROD")
                {
                    // Les occurrences ECHANTILLONNAGE sont affìhées par AlerteControle.vue (Exec_Prelevement_Intermediaire)
                    // On ne charge plus les tranches ECH statiques ici.
                    // Le Resultats reste vide pour cette section — AlerteControle.vue gère son propre polling.
                }
                else if (secDto.TypeSection != "REGLAGE" && secDto.TypeSection != "REGLAGE_PROD")
                {
                    var secLigneIds = secDto.LignesPlan.Select(l => l.Id.ToString()).ToList();
                    var lotTranches = tranches.Where(t => t.TrancheHoraire != null && t.TrancheHoraire.StartsWith("LOT|") && (t.Remarques == null || secLigneIds.Count == 0 || secLigneIds.Any(id => t.Remarques.Contains($"|{id}|")))).OrderBy(t => t.HeureDebut).ToList();
                    foreach (var tr in lotTranches)
                    {
                        string label = tr.Remarques != null && tr.Remarques.StartsWith("LOT|") ? tr.Remarques.Split('|')[1] : (tr.TrancheHoraire?.Substring(4) ?? "Heure");
                        Guid lId = Guid.Empty;
                        if (tr.Remarques != null)
                        {
                            var parts = tr.Remarques.Split('|');
                            if (parts.Length >= 4 && Guid.TryParse(parts[2], out Guid parsedLId))
                            {
                                lId = parsedLId;
                                label = $"{parts[1]} — {parts[3]}";
                            }
                            else if (parts.Length >= 2)
                            {
                                label = parts[1];
                            }
                        }
                        var rep = reponsesLignes.FirstOrDefault(r => r.DocumentLigneId == lId);
                        secDto.Resultats.Add(new ExecPlanAssemblageRowDto
                        {
                            Id = tr.Id,
                            LigneId = lId,
                            Frequence = label,
                            HeurePrevue = tr.HeureDebut,
                            StatutNotif = GetStatutNotif(tr, execOf.Statut, isReglageTermine),
                            Resultat = rep != null ? (rep.ResultatType == "NON_CONFORME" ? "NC" : "C") : tr.ResultatFinal,
                            NonConformite = rep?.DetailsNc,
                            ActionCorrective = rep?.ActionsCorrection,
                            Approbation = tr.MatriculeApprobateur,
                            Remarques = tr.Remarques
                        });
                    }
                }

                dto.Sections.Add(secDto);
            }

            return dto;
        }

        public async Task<object> VerifierDocumentsAsync(Guid execControleOfId, string? posteCode)
        {
            var execOf = await _repository.GetExecOfAsync(execControleOfId);
            if (execOf == null) throw new Exception("Exécution OF introuvable.");

            var codeArticle = execOf.NumeroOfNavigation?.CodeArticle ?? "";
            var (planAss, docResultat) = await _repository.GetDocumentsAssemblageEtResultatAsync(codeArticle);

            var statutPlan = await _repository.GetStatutPlanAssAsync(execControleOfId);
            if (statutPlan != null && statutPlan.DocId.HasValue)
            {
                var existingPlan = await _repository.GetDocumentByIdAsync(statutPlan.DocId.Value);
                if (existingPlan != null) planAss = existingPlan;
            }

            var statutsAll = await _repository.GetStatutsAssemblageAsync(execControleOfId);
            var statutResExisting = statutsAll.FirstOrDefault(s => s.TypeDocument == "RESULTAT_CF" || s.TypeDocument == "RCCF" || s.TypeDocument == "CTRL_POSTE");
            if (statutResExisting != null && statutResExisting.DocId.HasValue)
            {
                var existingRes = await _repository.GetDocumentByIdAsync(statutResExisting.DocId.Value);
                if (existingRes != null) docResultat = existingRes;
            }

            var pf = execOf.NumeroOfNavigation?.CodeArticleNavigation;
            bool isSoupape = pf != null && (!string.IsNullOrEmpty(pf.Designation) && pf.Designation.IndexOf("soupape", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            !string.IsNullOrEmpty(pf.Designation2) && pf.Designation2.IndexOf("soupape", StringComparison.OrdinalIgnoreCase) >= 0) ||
                             !string.IsNullOrEmpty(codeArticle) && (codeArticle.IndexOf("PAS", StringComparison.OrdinalIgnoreCase) >= 0 || codeArticle.IndexOf("soupape", StringComparison.OrdinalIgnoreCase) >= 0);

            bool valide = (planAss != null && docResultat != null);
            string messageErreur = "";
            string docManquant = "";

            if (planAss == null && docResultat == null)
            {
                messageErreur = $"Le Document Plan d'assemblage ET le Document de résultat en cours d'assemblage sont introuvables ou inactifs pour l'article {codeArticle}.";
                docManquant = $"Plan d'assemblage ET Document de résultat en cours d'assemblage (pour l'article {codeArticle})";
            }
            else if (planAss == null)
            {
                messageErreur = $"Le Document Plan d'assemblage (instructions) est introuvable ou inactif pour l'article {codeArticle} (le Document de résultat en cours d'assemblage est quant à lui disponible).";
                docManquant = $"Plan d'assemblage (instructions techniques pour {codeArticle})";
            }
            else if (docResultat == null)
            {
                string soupapeTxt = isSoupape ? " (pour article avec soupape)" : "";
                messageErreur = $"Le Document de résultat en cours d'assemblage{soupapeTxt} est introuvable ou inactif pour l'article {codeArticle} (le Plan d'assemblage est quant à lui disponible).";
                docManquant = $"Document de résultat en cours d'assemblage / Contrôle CF{soupapeTxt} pour l'article {codeArticle}";
            }

            return new
            {
                valide = valide,
                planAssExiste = planAss != null,
                planAssNom = planAss?.Nom ?? planAss?.Designation ?? planAss?.Formulaire?.Designation ?? "Aucun",
                resultatCfExiste = docResultat != null,
                resultatCfNom = docResultat?.Nom ?? docResultat?.Designation ?? docResultat?.Formulaire?.Designation ?? "Aucun",
                isSoupape = isSoupape,
                codeArticle = codeArticle,
                messageErreur = messageErreur,
                docManquantDescription = docManquant
            };
        }

        public async Task<ExecPlanAssemblageDto> ConfigurerHorairesAsync(Guid execControleOfId, ConfigHeuresPosteRequest request)
        {
            var execOf = await _repository.GetExecOfAsync(execControleOfId);
            if (execOf == null) throw new Exception("Exécution introuvable.");

            var codeArticle = execOf.NumeroOfNavigation?.CodeArticle ?? "";
            var statut = await _repository.GetStatutPlanAssAsync(execControleOfId);
            DocumentEntete? plan = null;
            if (statut != null && statut.DocId.HasValue)
            {
                plan = await _repository.GetDocumentByIdAsync(statut.DocId.Value);
            }
            if (plan == null)
            {
                plan = await _repository.GetPlanAssemblageActifAsync(codeArticle);
            }
            if (plan == null) throw new Exception("Aucun plan d'assemblage trouvé.");

            var tranches = await _repository.GetTranchesAsync(execControleOfId);
            
            var tranchesToRemove = tranches.Where(t => string.IsNullOrEmpty(t.ResultatFinal)).ToList();
            if (tranchesToRemove.Count > 0)
            {
                _repository.RemoveTranches(tranchesToRemove);
            }

            var oldCfg = tranches.Where(t => t.TrancheHoraire == "CFG_POSTE").ToList();
            if (oldCfg.Count > 0)
            {
                _repository.RemoveTranches(oldCfg);
            }

            var now = DateTime.Now;

            var ech = await _repository.GetExecEchantillonnageAsync(execControleOfId);
            int effectifParHeure = ech?.EffectifParPosteAb ?? 4;
            if (effectifParHeure <= 0) effectifParHeure = 4;

            foreach (var poste in request.Postes)
            {
                DateTime startDt = ParseHoraire(now, poste.HeureDebut, 8, 0);
                DateTime endDt = ParseHoraire(now, poste.HeureFin, 14, 0);
                if (endDt <= startDt) endDt = startDt.AddHours(6);

                _repository.AddTranche(new ExecControleTranche
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfid = execControleOfId,
                    TrancheHoraire = "CFG_POSTE",
                    HeureDebut = startDt,
                    HeureFin = endDt,
                    Remarques = $"CFG|{poste.PosteCode}|{poste.HeureDebut}|{poste.HeureFin}"
                });

                foreach (var sec in plan.DocumentSections)
                {
                    string typeSec = DetecterTypeSection(sec);
                    if (typeSec == "REGLAGE")
                    {
                        // Les réponses pour les lignes de réglage sont stockées uniquement dans Exec_ControleLigne_Reponse.
                        // Aucune tranche REG n'est créée dans Exec_ControleTranche.
                    }
                    else if (typeSec == "ECHANTILLONNAGE")
                    {
                        // Les occurrences ECHANTILLONNAGE sont générées dynamiquement par OccurrenceService
                        // dans Exec_Prelevement_Intermediaire. On ne crée plus de tranches ECH statiques.
                        // (Le composant AlerteControle.vue se charge de les afficher en temps réel)
                    }
                    else
                    {
                        DateTime curr = startDt;
                        while (curr < endDt)
                        {
                            DateTime next = curr.AddHours(1);
                            if (next > endDt) next = endDt;
                            string label = !string.IsNullOrEmpty(poste.PosteCode) && poste.PosteCode.Length <= 8 
                                ? $"Poste {poste.PosteCode} ({curr:HH:mm} - {next:HH:mm})" 
                                : $"{curr:HH:mm} - {next:HH:mm}";
                            
                            foreach (var ligne in sec.DocumentLignes)
                            {
                                string carTxt = ligne.LibelleAffiche ?? ligne.Caracteristique?.Libelle ?? "Caractéristique";
                                _repository.AddTranche(new ExecControleTranche
                                {
                                    Id = Guid.NewGuid(),
                                    ExecControleOfid = execControleOfId,
                                    TrancheHoraire = $"LOT|{curr:HH:mm}-{next:HH:mm}",
                                    HeureDebut = curr,
                                    HeureFin = next,
                                    Remarques = $"LOT|{label}|{ligne.Id}|{carTxt}"
                                });
                            }
                            curr = next;
                        }
                    }
                }
            }

            await _repository.SaveChangesAsync();
            return await GetOrInitExecutionAsync(execControleOfId, request.Postes.FirstOrDefault()?.PosteCode);
        }

        public async Task<bool> SaveResultatAsync(Guid execControleOfId, SaveResultatAssRequest request)
        {
            // Cas : réponse pour des lignes de réglage uniquement (TrancheId == Guid.Empty)
            // On ne touche pas à Exec_ControleTranche — on enregistre directement dans Exec_ControleLigne_Reponse
            bool isReglageOnly = request.TrancheId == Guid.Empty;

            if (!isReglageOnly)
            {
                // Cas normal : réponse liée à une tranche (LOT, CFG_POSTE, etc.)
                var tr = await _repository.GetTrancheByIdAsync(request.TrancheId);
                if (tr == null || tr.ExecControleOfid != execControleOfId)
                {
                    return false;
                }

                tr.ResultatFinal = request.Resultat;
                tr.DetailsNc = null;
                tr.ActionsCorrection = null;
                tr.MatriculeApprobateur = request.Approbation;
                if (!string.IsNullOrEmpty(request.Remarques))
                {
                    tr.Remarques = request.Remarques;
                }
            }

            if (request.LignesControle != null && request.LignesControle.Any())
            {
                var operateurId = await _repository.GetUtilisateurIdByMatriculeAsync(request.Approbation)
                                 ?? await _repository.GetDefaultUtilisateurIdAsync();

                if (operateurId != Guid.Empty)
                {
                    // Pour les réglages, toujours contexte REGLAGE.
                    // Pour les tranches LOT/autre, détecter selon TrancheHoraire.
                    string contexte = "REGLAGE";
                    if (!isReglageOnly)
                    {
                        var trForCtx = await _repository.GetTrancheByIdAsync(request.TrancheId);
                        if (trForCtx?.TrancheHoraire != null && trForCtx.TrancheHoraire.StartsWith("LOT"))
                            contexte = "LOT";
                        else if (trForCtx?.TrancheHoraire != null && trForCtx.TrancheHoraire != "REG")
                            contexte = "100PCT";
                    }

                    var reponses = new List<ExecControleLigneReponse>();
                    foreach (var l in request.LignesControle)
                    {
                        // Si une réponse existe déjà pour cette ligne (réglage), on la met à jour plutôt que d'en créer une nouvelle
                        if (isReglageOnly && l.LigneId != Guid.Empty)
                        {
                            var existing = await _repository.GetLigneReponseByLigneIdAsync(execControleOfId, l.LigneId, "REGLAGE");
                            if (existing != null)
                            {
                                existing.ResultatType = (l.Resultat == "NC" || l.Resultat == "NON_CONFORME") ? "NON_CONFORME" : "CONFORME";
                                existing.DetailsNc = l.Nc;
                                existing.ActionsCorrection = l.ActionCorrective;
                                existing.OperateurId = operateurId;
                                existing.DateSaisie = DateTime.Now;
                                continue;
                            }
                        }

                        reponses.Add(new ExecControleLigneReponse
                        {
                            Id = Guid.NewGuid(),
                            ExecControleOfid = execControleOfId,
                            DocumentLigneId = l.LigneId != Guid.Empty ? l.LigneId : null,
                            Contexte = contexte,
                            NumeroReglage = contexte == "REGLAGE" ? "REG" : null,
                            ResultatType = (l.Resultat == "NC" || l.Resultat == "NON_CONFORME") ? "NON_CONFORME" : "CONFORME",
                            DetailsNc = l.Nc,
                            ActionsCorrection = l.ActionCorrective,
                            OperateurId = operateurId,
                            DateSaisie = DateTime.Now
                        });
                    }

                    if (reponses.Any())
                        _repository.AddLigneReponses(reponses);
                }
            }

            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CloturerExecutionAsync(Guid execControleOfId)
        {
            var statuts = await _repository.GetStatutsAssemblageAsync(execControleOfId);
            foreach (var s in statuts)
            {
                s.EstTermine = true;
                s.DateTermine = DateTime.Now;
            }

            await _repository.SaveChangesAsync();

            // Clôturer également l'OF et ignorer toutes les occurrences restantes avec le commentaire "Clôturé"
            await _operateurService.CloturerOfAsync(execControleOfId);

            return true;
        }

        private string DetecterTypeSection(DocumentSection sec)
        {
            string tsCode = (sec.TypeSection?.Code ?? "").ToUpper();
            if (tsCode == "REGLAGE") return "REGLAGE";
            if (tsCode == "REGLAGE_PROD") return "REGLAGE_PROD";
            if (tsCode == "EN_COURS" || tsCode == "ECHANT_NQA" || tsCode == "OF") return "ECHANTILLONNAGE";

            string lib = (sec.LibelleSection ?? "").ToLower();
            if (lib.Contains("réglage") && !lib.Contains("produc")) return "REGLAGE";
            
            return "ECHANTILLONNAGE";
        }

        private string GetStatutNotif(ExecControleTranche tranche, string? statutOf = null, bool isReglageTermine = true)
        {
            if (!string.IsNullOrEmpty(tranche.ResultatFinal))
            {
                return "FAIT";
            }

            if (statutOf == "EN_PAUSE")
            {
                return "EN_PAUSE";
            }

            if (statutOf == "REGLAGE")
            {
                return "EN_REGLAGE";
            }

            var now = DateTime.Now;
            if (now > tranche.HeureFin.AddMinutes(15))
            {
                return "EN_RETARD";
            }
            else if (now >= tranche.HeureDebut.AddMinutes(-15) && now <= tranche.HeureFin.AddMinutes(15))
            {
                return "A_FAIRE";
            }
            else
            {
                return "A_VENIR";
            }
        }

        private DateTime ParseHoraire(DateTime refDate, string horaire, int defHour, int defMin)
        {
            if (string.IsNullOrEmpty(horaire)) return new DateTime(refDate.Year, refDate.Month, refDate.Day, defHour, defMin, 0);
            var parts = horaire.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
            {
                return new DateTime(refDate.Year, refDate.Month, refDate.Day, h, m, 0);
            }
            return new DateTime(refDate.Year, refDate.Month, refDate.Day, defHour, defMin, 0);
        }
    }
}
