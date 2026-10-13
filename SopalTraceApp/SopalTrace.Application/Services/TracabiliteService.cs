using SopalTrace.Domain.Constants;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Tracabilite;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services
{
    /// <summary>
    /// Registre de traçabilité de l'OF : une ligne par changement de lots (corps, volant, lots des composants
    /// scannés par le magasinier), avec l'équipe, le poste et l'opérateur. Seule l'équipe en production saisit ;
    /// chaque équipe confirme ses lots avant sa fin de poste (le registre reste ouvert jusqu'à la clôture de
    /// l'OF). Une ligne erronée est annulée avec son motif, jamais supprimée.
    /// </summary>
    public class TracabiliteService : ITracabiliteService
    {
        private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> Verrous = new();
        private readonly ITracabiliteRepository _tracabiliteRepository;

        public TracabiliteService(ITracabiliteRepository tracabiliteRepository)
        {
            _tracabiliteRepository = tracabiliteRepository;
        }

        public async Task<RegistreTracabiliteDto> GetRegistreTracabiliteAsync(Guid execControleOfId)
        {
            var execOf = await ExecOfAsync(execControleOfId);
            var historique = await _tracabiliteRepository.GetHistoriqueAsync(execControleOfId);
            var session = await _tracabiliteRepository.GetSessionEnCoursAsync(execControleOfId);
            var confirmations = await _tracabiliteRepository.GetConfirmationsAsync(execControleOfId);
            var motif = MotifLectureSeule(execOf, session);

            return new RegistreTracabiliteDto
            {
                EstTermine = execOf.Statut == StatutsExecution.Cloture,
                ComposantsDisponibles = (await LotsDisponiblesAsync(execOf.NumeroOf)).Values.ToList(),
                LignesHistorique = historique.Select(VersDto).ToList(),
                EquipeSaisie = session?.Equipe,
                PeutSaisir = motif == null,
                MotifLectureSeule = motif,
                EquipeAConfirme = session != null && confirmations.Any(c => c.SessionId == session.Id),
                Confirmations = confirmations.Select(c => new ConfirmationTracabiliteDto
                {
                    Equipe = c.Equipe, PosteCode = c.PosteCode, Date = c.DateTermine, MatriculeOperateur = c.MatriculeOperateur
                }).ToList()
            };
        }

        /// <summary>
        /// Nouvelle ligne de l'équipe en production : corps et volant obligatoires et reconnus, un lot scanné pour
        /// chaque composant préparé par le magasinier. La version augmente quand un lot change.
        /// </summary>
        public async Task<RegistreTracabiliteRowDto> AddLigneTracabiliteAsync(AddRegistreTracabiliteDto dto, string matriculeOperateur)
        {
            return await SousVerrouAsync(dto.ExecControleOfId, async () =>
            {
                var execOf = await ExecOfAsync(dto.ExecControleOfId);
                var session = await SessionSaisieAsync(execOf);
                VerifierPoste(execOf, dto.PosteCode);

                var corps = dto.Corps?.Trim();
                var volant = dto.Volant?.Trim();
                if (string.IsNullOrEmpty(corps) || string.IsNullOrEmpty(volant))
                    throw new RegleMetierException("Le lot du corps et le lot du volant sont obligatoires.");
                var article = execOf.NumeroOfNavigation.CodeArticle;
                if (!await _tracabiliteRepository.IsLotValideAsync(corps, NaturesArticle.Corps, article))
                    throw new RegleMetierException($"Le lot de corps « {corps} » n'est pas reconnu pour l'article {article}.");
                if (!await _tracabiliteRepository.IsLotValideAsync(volant, NaturesArticle.Volant, article))
                    throw new RegleMetierException($"Le lot de volant « {volant} » n'est pas reconnu pour l'article {article}.");

                var composants = ComposantsValides(dto.Composants, await LotsDisponiblesAsync(execOf.NumeroOf));

                var historique = await _tracabiliteRepository.GetHistoriqueAsync(execOf.Id);
                var derniere = historique.Where(l => l.CancelledAt == null).MaxBy(l => l.DateHeure);
                var version = derniere == null ? 1 : derniere.Version + (MemesLots(derniere, corps, volant, composants) ? 0 : 1);

                var ligne = new ExecRegistreTracabilite
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfId = execOf.Id,
                    DateHeure = DateTime.Now,
                    Version = version,
                    Corps = corps,
                    Volant = volant,
                    SessionId = session.Id,
                    Equipe = session.Equipe,
                    PosteCode = dto.PosteCode,
                    MatriculeOperateur = matriculeOperateur,
                    ExecRegistreTracabiliteComposants = composants.Select(c => new ExecRegistreTracabiliteComposant
                    {
                        Id = Guid.NewGuid(),
                        CodeArticle = c.CodeArticle,
                        DesignationComposant = c.Designation,
                        LotSelectionne = c.Lot
                    }).ToList()
                };
                _tracabiliteRepository.AddLigne(ligne);
                await _tracabiliteRepository.SaveChangesAsync();
                return VersDto(ligne);
            });
        }

        /// <summary>
        /// L'équipe en production confirme ses lots : sans ligne de sa part, la dernière ligne valide est reprise
        /// à son nom (mêmes lots). Sa tâche « registre de traçabilité » est faite ; le registre reste ouvert.
        /// </summary>
        public async Task ConfirmerEquipeAsync(Guid execControleOfId, string posteCode, string matriculeOperateur)
        {
            await SousVerrouAsync(execControleOfId, async () =>
            {
                var execOf = await ExecOfAsync(execControleOfId);
                var session = await SessionSaisieAsync(execOf);
                VerifierPoste(execOf, posteCode);
                if ((await _tracabiliteRepository.GetConfirmationsAsync(execOf.Id)).Any(c => c.SessionId == session.Id)) return true;

                var actives = (await _tracabiliteRepository.GetHistoriqueAsync(execOf.Id)).Where(l => l.CancelledAt == null).ToList();
                var derniere = actives.MaxBy(l => l.DateHeure)
                    ?? throw new RegleMetierException("Ajoutez d'abord la ligne des lots utilisés (corps, volant, composants).");
                var maintenant = DateTime.Now;
                if (!actives.Any(l => l.SessionId == session.Id))
                {
                    _tracabiliteRepository.AddLigne(new ExecRegistreTracabilite
                    {
                        Id = Guid.NewGuid(),
                        ExecControleOfId = execOf.Id,
                        DateHeure = maintenant,
                        Version = derniere.Version,
                        Corps = derniere.Corps,
                        Volant = derniere.Volant,
                        SessionId = session.Id,
                        Equipe = session.Equipe,
                        PosteCode = posteCode,
                        MatriculeOperateur = matriculeOperateur,
                        ExecRegistreTracabiliteComposants = derniere.ExecRegistreTracabiliteComposants.Select(c => new ExecRegistreTracabiliteComposant
                        {
                            Id = Guid.NewGuid(), CodeArticle = c.CodeArticle, DesignationComposant = c.DesignationComposant, LotSelectionne = c.LotSelectionne
                        }).ToList()
                    });
                }

                _tracabiliteRepository.AddStatut(new ExecControleDocumentStatut
                {
                    Id = Guid.NewGuid(),
                    ExecControleOfId = execOf.Id,
                    PosteCode = posteCode,
                    TypeDocument = TypesDocumentExecution.RegistreTracabilite,
                    SessionId = session.Id,
                    Equipe = session.Equipe,
                    EstTermine = true,
                    DateExecution = maintenant,
                    DateTermine = maintenant,
                    MatriculeOperateur = matriculeOperateur
                });
                await _tracabiliteRepository.SaveChangesAsync();
                return true;
            });
        }

        public async Task AnnulerLigneAsync(Guid ligneId, string raison, string matriculeOperateur)
        {
            var ligne = await _tracabiliteRepository.GetLigneAsync(ligneId)
                ?? throw new RessourceIntrouvableException("Ligne du registre introuvable.");
            await SousVerrouAsync(ligne.ExecControleOfId, async () =>
            {
                var execOf = await ExecOfAsync(ligne.ExecControleOfId);
                await SessionSaisieAsync(execOf);
                if (ligne.CancelledAt != null) throw new RegleMetierException("Cette ligne est déjà annulée.");
                if (string.IsNullOrWhiteSpace(raison) || raison.Trim().Length < 3)
                    throw new RegleMetierException("Le motif de l'annulation est obligatoire.");

                ligne.CancelledAt = DateTime.Now;
                ligne.CancelledBy = matriculeOperateur;
                ligne.CancellationReason = raison.Trim();
                await _tracabiliteRepository.SaveChangesAsync();
                return true;
            });
        }

        public async Task<bool> ValidateLotAsync(Guid execControleOfId, string typeArticle, string numeroLot)
        {
            if (string.IsNullOrWhiteSpace(numeroLot)) return false;
            var execOf = await ExecOfAsync(execControleOfId);
            return await _tracabiliteRepository.IsLotValideAsync(numeroLot.Trim(), typeArticle, execOf.NumeroOfNavigation.CodeArticle);
        }

        public async Task<List<string>> SearchLotsAsync(Guid execControleOfId, string typeArticle, string query)
        {
            var execOf = await ExecOfAsync(execControleOfId);
            return await _tracabiliteRepository.SearchLotsAsync(typeArticle, execOf.NumeroOfNavigation.CodeArticle, query);
        }

        // ── Règles ──────────────────────────────────────────────

        private sealed record ComposantSaisi(string CodeArticle, string Designation, string Lot);

        private async Task<ExecControleOf> ExecOfAsync(Guid execControleOfId) =>
            await _tracabiliteRepository.GetExecOfAsync(execControleOfId)
            ?? throw new RessourceIntrouvableException("Exécution OF introuvable.");

        private static string? MotifLectureSeule(ExecControleOf execOf, ExecControleOfSession? session) =>
            execOf.Statut == StatutsExecution.Cloture ? "OF clôturé : le registre est en lecture seule."
            : session == null ? "Aucune équipe ne produit sur cet OF : reprenez la production pour saisir."
            : null;

        private async Task<ExecControleOfSession> SessionSaisieAsync(ExecControleOf execOf)
        {
            var session = await _tracabiliteRepository.GetSessionEnCoursAsync(execOf.Id);
            var motif = MotifLectureSeule(execOf, session);
            if (motif != null) throw new EtatExecutionInvalideException(motif);
            return session!;
        }

        private static void VerifierPoste(ExecControleOf execOf, string posteCode)
        {
            if (!execOf.ExecControleOfPostes.Any(p => p.PosteCode == posteCode))
                throw new RegleMetierException($"Le poste {posteCode} n'est pas affecté à cet OF.");
        }

        /// <summary>Lots scannés par le magasinier pour l'OF, par article de composant.</summary>
        private async Task<Dictionary<string, ComposantDisponibleDto>> LotsDisponiblesAsync(string numeroOf)
        {
            var lots = new Dictionary<string, ComposantDisponibleDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var lot in (await _tracabiliteRepository.GetPreparationsMagasinAsync(numeroOf)).SelectMany(p => p.MagPreparationOfLots))
            {
                if (!lots.TryGetValue(lot.CodeArticle, out var composant))
                    lots[lot.CodeArticle] = composant = new ComposantDisponibleDto
                    {
                        CodeArticle = lot.CodeArticle,
                        DesignationComposant = lot.CodeArticleNavigation?.Designation ?? lot.CodeArticle
                    };
                if (!string.IsNullOrEmpty(lot.NumeroLotScanne) && !composant.LotsDisponibles.Contains(lot.NumeroLotScanne))
                    composant.LotsDisponibles.Add(lot.NumeroLotScanne);
            }
            return lots;
        }

        /// <summary>Un lot scanné pour chaque composant préparé, repéré par son code article ; rien d'autre.</summary>
        private static List<ComposantSaisi> ComposantsValides(List<ComposantSelectionneDto> saisis, Dictionary<string, ComposantDisponibleDto> disponibles)
        {
            var parArticle = new Dictionary<string, ComposantSelectionneDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in saisis)
            {
                if (string.IsNullOrWhiteSpace(c.CodeArticle))
                    throw new RegleMetierException($"Composant « {c.DesignationComposant} » sans code article.");
                if (!disponibles.ContainsKey(c.CodeArticle))
                    throw new RegleMetierException($"Le composant {c.CodeArticle} n'a pas été préparé par le magasin pour cet OF.");
                if (!parArticle.TryAdd(c.CodeArticle, c))
                    throw new RegleMetierException($"Le composant {c.CodeArticle} est saisi deux fois.");
            }

            return disponibles.Values.Where(d => d.LotsDisponibles.Count > 0).Select(d =>
            {
                var lot = parArticle.GetValueOrDefault(d.CodeArticle)?.LotSelectionne?.Trim();
                if (string.IsNullOrEmpty(lot))
                    throw new RegleMetierException($"Choisissez le lot utilisé pour le composant {d.DesignationComposant}.");
                if (!d.LotsDisponibles.Contains(lot))
                    throw new RegleMetierException($"Le lot « {lot} » n'a pas été scanné par le magasin pour le composant {d.DesignationComposant}.");
                return new ComposantSaisi(d.CodeArticle, d.DesignationComposant, lot);
            }).ToList();
        }

        private static bool MemesLots(ExecRegistreTracabilite ligne, string corps, string volant, List<ComposantSaisi> composants)
        {
            if (ligne.Corps != corps || ligne.Volant != volant) return false;
            var precedents = ligne.ExecRegistreTracabiliteComposants
                .Where(c => c.CodeArticle != null)
                .GroupBy(c => c.CodeArticle!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().LotSelectionne, StringComparer.OrdinalIgnoreCase);
            return precedents.Count == composants.Count &&
                   composants.All(c => precedents.TryGetValue(c.CodeArticle, out var lot) && lot == c.Lot);
        }

        private static RegistreTracabiliteRowDto VersDto(ExecRegistreTracabilite l) => new()
        {
            Id = l.Id,
            DateHeure = l.DateHeure,
            Version = l.Version,
            Corps = l.Corps,
            Volant = l.Volant,
            Equipe = l.Equipe,
            PosteCode = l.PosteCode,
            MatriculeOperateur = l.MatriculeOperateur,
            Annulee = l.CancelledAt != null,
            AnnuleeLe = l.CancelledAt,
            AnnuleePar = l.CancelledBy,
            MotifAnnulation = l.CancellationReason,
            Composants = l.ExecRegistreTracabiliteComposants.Select(c => new ComposantSelectionneDto
            {
                CodeArticle = c.CodeArticle,
                DesignationComposant = c.DesignationComposant,
                LotSelectionne = c.LotSelectionne
            }).ToList()
        };

        private static async Task<T> SousVerrouAsync<T>(Guid execControleOfId, Func<Task<T>> action)
        {
            var verrou = Verrous.GetOrAdd(execControleOfId, _ => new SemaphoreSlim(1, 1));
            await verrou.WaitAsync();
            try { return await action(); }
            finally { verrou.Release(); }
        }
    }
}
