using SopalTrace.Application.DTOs.Magasin;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services;

/// <summary>
/// Magasin : préparation des composants d'un OF et expédition des OF sur un BL, à la douchette.
/// Chaque scan est enregistré immédiatement (une coupure ou un rafraîchissement ne perd rien) et validé ici :
/// article de la nomenclature, lot non déjà scanné, OF terminé en production avant expédition.
/// Le magasinier est toujours celui du jeton.
/// </summary>
public class MagasinService : IMagasinService
{
    private readonly IMagasinRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public MagasinService(IMagasinRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    // ───────────────────────────── Préparation des OF ─────────────────────────────

    public async Task<PreparationDto> GetOfAsync(string scanOuNumero)
    {
        var of = await OfAsync(EtiquetteMagasin.LireNumeroOf(scanOuNumero));
        return VersDto(of, await _repository.GetPreparationActiveAsync(of.NumeroOf));
    }

    /// <summary>Reprend la préparation de l'OF si elle existe (en cours ou terminée), sinon en ouvre une.</summary>
    public async Task<PreparationDto> OuvrirPreparationAsync(string scanOuNumero)
    {
        var of = await OfAsync(EtiquetteMagasin.LireNumeroOf(scanOuNumero));
        if (!BesoinsMagasin(of).Any())
            throw new RegleMetierException($"L'OF {of.NumeroOf} n'a aucun composant à servir (nomenclature vide dans l'ERP).");

        var preparation = await _repository.GetPreparationActiveAsync(of.NumeroOf);
        if (preparation == null)
        {
            preparation = new MagPreparationOf
            {
                NumeroOf = of.NumeroOf,
                MatriculeMagasinier = _currentUser.MatriculeRequis,
                Statut = StatutsPreparationMagasin.EnCours,
                DateDebut = DateTime.Now
            };
            _repository.AjouterPreparation(preparation);
            await _repository.SaveChangesAsync();
        }
        return VersDto(of, preparation);
    }

    public async Task<ScanPreparationDto> ScannerComposantAsync(Guid preparationId, string contenu)
    {
        var preparation = await PreparationEnCoursAsync(preparationId);
        var of = await OfAsync(preparation.NumeroOf);
        var etiquette = EtiquetteMagasin.LireComposant(contenu);

        var besoin = BesoinsMagasin(of).FirstOrDefault(b => string.Equals(b.CodeArticle, etiquette.CodeArticle, StringComparison.OrdinalIgnoreCase))
            ?? throw new RegleMetierException($"L'article {etiquette.CodeArticle} ne fait pas partie de la nomenclature de l'OF {of.NumeroOf}.");

        if (preparation.MagPreparationOfLots.Any(l => l.CodeArticle == besoin.CodeArticle
                                                     && string.Equals(l.NumeroLotScanne, etiquette.NumeroLot, StringComparison.OrdinalIgnoreCase)))
            throw new ScanEnDoubleException($"Le lot {etiquette.NumeroLot} de l'article {besoin.CodeArticle} est déjà scanné pour cet OF.");

        var lot = new MagPreparationOfLot
        {
            PreparationOfid = preparation.Id,
            CodeArticle = besoin.CodeArticle,
            NumeroLotScanne = etiquette.NumeroLot,
            Quantite = etiquette.Quantite,
            QualityControlReport = etiquette.RapportControle,
            ScannedContent = EtiquetteMagasin.Normaliser(contenu),
            DateScan = DateTime.Now
        };
        _repository.AjouterLot(lot);
        // EF rattache déjà le lot à la préparation suivie (même clé) : l'ajouter encore le compterait deux fois.
        if (!preparation.MagPreparationOfLots.Contains(lot)) preparation.MagPreparationOfLots.Add(lot);
        await _repository.SaveChangesAsync();

        return new ScanPreparationDto(VersDto(of, preparation), besoin.CodeArticle, VersDto(lot));
    }

    public async Task<PreparationDto> RetirerLotAsync(Guid preparationId, Guid lotId)
    {
        var preparation = await PreparationEnCoursAsync(preparationId);
        var lot = preparation.MagPreparationOfLots.FirstOrDefault(l => l.Id == lotId)
            ?? throw new RessourceIntrouvableException("Ce scan n'existe pas dans la préparation.");

        _repository.SupprimerLot(lot);
        preparation.MagPreparationOfLots.Remove(lot);
        await _repository.SaveChangesAsync();
        return VersDto(await OfAsync(preparation.NumeroOf), preparation);
    }

    /// <summary>Composants servis : l'OF devient disponible pour la production. Motif obligatoire s'il manque des composants.</summary>
    public async Task<PreparationDto> TerminerPreparationAsync(Guid preparationId, string? motif)
    {
        var preparation = await PreparationEnCoursAsync(preparationId);
        var of = await OfAsync(preparation.NumeroOf);

        // Composant servi : quantité scannée au moins égale à la quantité requise.
        var manquants = BesoinsMagasin(of)
            .Select(b => (b.CodeArticle, b.QuantiteRequise, Servie: QuantiteServie(preparation.MagPreparationOfLots, b.CodeArticle)))
            .Where(c => (c.Servie ?? 0) < c.QuantiteRequise)
            .Select(c => $"{c.CodeArticle} ({c.Servie?.ToString("0.###") ?? "—"} / {c.QuantiteRequise:0.###})")
            .ToList();
        motif = string.IsNullOrWhiteSpace(motif) ? null : motif.Trim();

        if (manquants.Count > 0 && motif == null)
            throw new RegleMetierException($"Quantité requise non servie : {string.Join(", ", manquants)}. Scannez les lots manquants ou indiquez le motif de la préparation incomplète.");
        if (motif?.Length > 300)
            throw new RegleMetierException("Le motif ne doit pas dépasser 300 caractères.");

        preparation.Statut = StatutsPreparationMagasin.Termine;
        preparation.DateFin = DateTime.Now;
        preparation.IncompleteReason = manquants.Count > 0 ? motif : null;
        await _repository.SaveChangesAsync();
        return VersDto(of, preparation);
    }

    /// <summary>Abandon d'une préparation en cours : une nouvelle pourra être ouverte pour l'OF.</summary>
    public async Task<PreparationDto> AnnulerPreparationAsync(Guid preparationId)
    {
        var preparation = await PreparationEnCoursAsync(preparationId);
        preparation.Statut = StatutsPreparationMagasin.Annule;
        preparation.DateFin = DateTime.Now;
        await _repository.SaveChangesAsync();
        return VersDto(await OfAsync(preparation.NumeroOf), null);
    }

    // ───────────────────────────── Expédition des BL ─────────────────────────────

    public async Task<ExpeditionDto> GetBlAsync(string scanOuNumero)
    {
        var bl = await BlAsync(EtiquetteMagasin.LireNumeroBl(scanOuNumero));
        return VersDto(bl, await _repository.GetExpeditionDuBlAsync(bl.NumeroBl));
    }

    public async Task<ExpeditionDto> OuvrirExpeditionAsync(string scanOuNumero)
    {
        var bl = await BlAsync(EtiquetteMagasin.LireNumeroBl(scanOuNumero));
        var expedition = await _repository.GetExpeditionDuBlAsync(bl.NumeroBl);
        if (expedition == null)
        {
            expedition = new MagExpeditionBl
            {
                NumeroBl = bl.NumeroBl,
                MatriculeMagasinier = _currentUser.MatriculeRequis,
                Statut = StatutsExpeditionMagasin.EnCours,
                DateDebut = DateTime.Now
            };
            _repository.AjouterExpedition(expedition);
            await _repository.SaveChangesAsync();
        }
        return VersDto(bl, expedition);
    }

    public async Task<ExpeditionDto> ScannerOfAsync(Guid expeditionId, string contenu)
    {
        var expedition = await ExpeditionEnCoursAsync(expeditionId);
        var numeroOf = EtiquetteMagasin.LireNumeroOf(contenu);
        var of = await OfAsync(numeroOf);

        if (expedition.MagExpeditionBlScanOfs.Any(s => string.Equals(s.NumeroOfscanne, of.NumeroOf, StringComparison.OrdinalIgnoreCase)))
            throw new ScanEnDoubleException($"L'OF {of.NumeroOf} est déjà scanné pour ce BL.");
        if (!await _repository.ProductionTermineeAsync(of.NumeroOf))
            throw new RegleMetierException($"L'OF {of.NumeroOf} n'est pas terminé en production : il ne peut pas être expédié.");

        var scan = new MagExpeditionBlScanOf { ExpeditionBlid = expedition.Id, NumeroOfscanne = of.NumeroOf, DateScan = DateTime.Now };
        _repository.AjouterOfExpedie(scan);
        await _repository.SaveChangesAsync();
        return await ExpeditionDtoAsync(expedition.Id);
    }

    public async Task<ExpeditionDto> RetirerOfAsync(Guid expeditionId, Guid scanId)
    {
        var expedition = await ExpeditionEnCoursAsync(expeditionId);
        var scan = expedition.MagExpeditionBlScanOfs.FirstOrDefault(s => s.Id == scanId)
            ?? throw new RessourceIntrouvableException("Cet OF n'est pas associé au BL.");
        _repository.SupprimerOfExpedie(scan);
        await _repository.SaveChangesAsync();
        return await ExpeditionDtoAsync(expedition.Id);
    }

    public async Task<ExpeditionDto> TerminerExpeditionAsync(Guid expeditionId)
    {
        var expedition = await ExpeditionEnCoursAsync(expeditionId);
        if (expedition.MagExpeditionBlScanOfs.Count == 0)
            throw new RegleMetierException("Scannez au moins un OF avant de valider l'expédition.");

        expedition.Statut = StatutsExpeditionMagasin.Termine;
        expedition.DateFin = DateTime.Now;
        await _repository.SaveChangesAsync();
        return await ExpeditionDtoAsync(expedition.Id);
    }

    // ───────────────────────────── Lectures et contrôles ─────────────────────────────

    private async Task<MfgheadOrdreFabrication> OfAsync(string numeroOf) =>
        await _repository.GetOfAvecNomenclatureAsync(numeroOf)
        ?? throw new RessourceIntrouvableException($"L'OF {numeroOf} est introuvable dans l'ERP.");

    private async Task<Sdelivery> BlAsync(string numeroBl) =>
        await _repository.GetBlAsync(numeroBl)
        ?? throw new RessourceIntrouvableException($"Le BL {numeroBl} est introuvable dans l'ERP.");

    private async Task<MagPreparationOf> PreparationEnCoursAsync(Guid id)
    {
        var preparation = await _repository.GetPreparationAsync(id)
            ?? throw new RessourceIntrouvableException("Préparation introuvable.");
        if (preparation.Statut != StatutsPreparationMagasin.EnCours)
            throw new EtatExecutionInvalideException(preparation.Statut == StatutsPreparationMagasin.Termine
                ? "Cette préparation est terminée : elle ne peut plus être modifiée."
                : "Cette préparation est annulée.");
        return preparation;
    }

    private async Task<MagExpeditionBl> ExpeditionEnCoursAsync(Guid id)
    {
        var expedition = await _repository.GetExpeditionAsync(id)
            ?? throw new RessourceIntrouvableException("Expédition introuvable.");
        if (expedition.Statut != StatutsExpeditionMagasin.EnCours)
            throw new EtatExecutionInvalideException("Cette expédition est validée : elle ne peut plus être modifiée.");
        return expedition;
    }

    private async Task<ExpeditionDto> ExpeditionDtoAsync(Guid id)
    {
        var expedition = (await _repository.GetExpeditionAsync(id))!;
        return VersDto((await _repository.GetBlAsync(expedition.NumeroBl))!, expedition);
    }

    /// <summary>
    /// Composants servis par le magasin : les semi-finis (sauf le piston) n'y passent pas, ils arrivent de l'atelier qui
    /// les fabrique ; l'opérateur choisit leur OF dans le registre de traçabilité.
    /// </summary>
    private static IEnumerable<MfgmatBesoinOf> BesoinsMagasin(MfgheadOrdreFabrication of) =>
        of.MfgmatBesoinOfs.Where(b => b.CodeArticleNavigation is not { TypeArticle: "SF" } a || a.NatureArticleCode == "PISTON");

    private static PreparationDto VersDto(MfgheadOrdreFabrication of, MagPreparationOf? preparation)
    {
        var lots = preparation?.MagPreparationOfLots.ToList() ?? new List<MagPreparationOfLot>();
        var composants = BesoinsMagasin(of)
            .OrderBy(b => b.CodeArticle)
            .Select(b =>
            {
                var lotsArticle = lots.Where(l => l.CodeArticle == b.CodeArticle).OrderBy(l => l.DateScan).ToList();
                var servie = QuantiteServie(lotsArticle, b.CodeArticle);
                return new ComposantPreparationDto(
                    b.CodeArticle,
                    b.CodeArticleNavigation?.Designation ?? b.CodeArticle,
                    b.CodeArticleNavigation?.NatureArticleCodeNavigation?.Libelle,
                    b.QuantiteRequise,
                    servie,
                    (servie ?? 0) >= b.QuantiteRequise,
                    lotsArticle.Select(VersDto).ToList());
            })
            .ToList();

        return new PreparationDto(
            preparation?.Id, of.NumeroOf, of.CodeArticle,
            of.CodeArticleNavigation?.Designation ?? of.CodeArticle,
            of.CodeArticleNavigation?.NatureArticleCodeNavigation?.Libelle,
            of.QuantitePrevue,
            preparation?.Statut, preparation?.MatriculeMagasinier, preparation?.DateDebut, preparation?.DateFin, preparation?.IncompleteReason,
            composants.Count(c => c.Servi), composants);
    }

    /// <summary>Somme des quantités des étiquettes scannées pour l'article (null si aucune n'en indique).</summary>
    private static decimal? QuantiteServie(IEnumerable<MagPreparationOfLot> lots, string codeArticle)
    {
        var quantites = lots.Where(l => l.CodeArticle == codeArticle && l.Quantite.HasValue).Select(l => l.Quantite!.Value).ToList();
        return quantites.Count > 0 ? quantites.Sum() : null;
    }

    private static LotScanneDto VersDto(MagPreparationOfLot l) =>
        new(l.Id, l.NumeroLotScanne, l.Quantite, l.QualityControlReport, l.DateScan);

    private static ExpeditionDto VersDto(Sdelivery bl, MagExpeditionBl? expedition) => new(
        expedition?.Id, bl.NumeroBl, bl.CodeClient, bl.DateExpedition, bl.StatutBl,
        expedition?.Statut, expedition?.MatriculeMagasinier, expedition?.DateDebut, expedition?.DateFin,
        expedition?.MagExpeditionBlScanOfs
            .OrderBy(s => s.DateScan)
            .Select(s => new OfExpedieDto(s.Id, s.NumeroOfscanne,
                s.NumeroOfscanneNavigation?.CodeArticle ?? "",
                s.NumeroOfscanneNavigation?.CodeArticleNavigation?.Designation ?? "",
                s.NumeroOfscanneNavigation?.QuantitePrevue ?? 0, s.DateScan))
            .ToList() ?? new List<OfExpedieDto>());
}
