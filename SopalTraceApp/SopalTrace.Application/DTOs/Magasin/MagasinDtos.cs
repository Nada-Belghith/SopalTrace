namespace SopalTrace.Application.DTOs.Magasin;

/// <summary>Contenu brut lu par la douchette (ou la caméra) ; le serveur le nettoie et le lit.</summary>
public record ScanRequest(string Contenu);

public record OuvrirPreparationRequest(string NumeroOf);

/// <summary>Motif obligatoire si des composants de la nomenclature n'ont pas été servis.</summary>
public record TerminerPreparationRequest(string? Motif);

public record OuvrirExpeditionRequest(string NumeroBl);

public record LotScanneDto(Guid Id, string NumeroLot, decimal? Quantite, string? RapportControle, DateTime? DateScan);

/// <summary>QuantiteServie : somme des quantités portées par les étiquettes (null si aucune n'en indique).</summary>
public record ComposantPreparationDto(
    string CodeArticle,
    string Designation,
    string? Nature,
    decimal QuantiteRequise,
    decimal? QuantiteServie,
    List<LotScanneDto> Lots);

/// <summary>OF à préparer, avec sa nomenclature et, si elle existe, sa préparation (Id null sinon).</summary>
public record PreparationDto(
    Guid? Id,
    string NumeroOf,
    string CodeArticle,
    string DesignationArticle,
    string? NatureArticle,
    decimal QuantitePrevue,
    string? Statut,
    string? Magasinier,
    DateTime? DateDebut,
    DateTime? DateFin,
    string? MotifIncomplet,
    int ComposantsServis,
    List<ComposantPreparationDto> Composants);

/// <summary>Résultat d'un scan : la préparation à jour et la ligne ajoutée (pour la mettre en évidence).</summary>
public record ScanPreparationDto(PreparationDto Preparation, string CodeArticle, LotScanneDto Lot);

public record OfExpedieDto(Guid Id, string NumeroOf, string CodeArticle, string Designation, decimal Quantite, DateTime? DateScan);

/// <summary>BL à expédier et, si elle existe, son expédition (Id null sinon).</summary>
public record ExpeditionDto(
    Guid? Id,
    string NumeroBl,
    string CodeClient,
    DateOnly DateExpedition,
    string StatutBl,
    string? Statut,
    string? Magasinier,
    DateTime? DateDebut,
    DateTime? DateFin,
    List<OfExpedieDto> Ofs);
