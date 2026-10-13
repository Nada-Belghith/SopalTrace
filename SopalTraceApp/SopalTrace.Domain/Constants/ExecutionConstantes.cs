namespace SopalTrace.Domain.Constants;

/// <summary>Statuts d'une exécution (table Exec_ControleOF, contrainte CHECK).</summary>
public static class StatutsExecution
{
    public const string EnCours = "EN_COURS";
    public const string EnPause = "EN_PAUSE";
    public const string Reglage = "REGLAGE";
    public const string Cloture = "CLOTURE";
}

/// <summary>Statuts d'une session d'équipe (table Exec_ControleOf_Session, contrainte CHECK).</summary>
public static class StatutsSession
{
    public const string EnCours = "EN_COURS";
    public const string EnPause = "EN_PAUSE";
    public const string Termine = "TERMINE";
}

/// <summary>
/// Statuts de préparation magasin d'un OF (table Mag_PreparationOF). Terminée = composants servis :
/// l'OF devient disponible pour la production. Le statut de production se lit dans Exec_ControleOF.
/// </summary>
public static class StatutsPreparationMagasin
{
    public const string EnCours = "EN_COURS";
    public const string Termine = "TERMINE";
    public const string Annule = "ANNULE";
}

/// <summary>Statuts d'une expédition (table Mag_ExpeditionBL).</summary>
public static class StatutsExpeditionMagasin
{
    public const string EnCours = "EN_COURS";
    public const string Termine = "TERMINE";
}

/// <summary>Type d'OF exécuté (table Exec_ControleOF, contrainte CHECK).</summary>
public static class TypesOf
{
    public const string Fabrication = "FAB";
    public const string Assemblage = "ASS";
}

/// <summary>Codes d'opération utilisés par l'exécution.</summary>
public static class CodesOperation
{
    public const string Assemblage = "ASS";
    /// <summary>Usinage : les OF d'usinage de corps et de volants donnent les lots du registre de traçabilité.</summary>
    public const string Usinage = "USINAG";
}

/// <summary>Valeurs de Exec_ControleDocumentStatut.TypeDocument (table ExecutionDocumentType).</summary>
public static class TypesDocumentExecution
{
    public const string VerifMachine = "VERIF_MACHINE";
    public const string ClotureVerifMachinePoste = "CLOTURE_VM_POSTE";
    public const string Echantillonnage = "ECHANTILLONNAGE";
    public const string PlanAssemblage = "PLAN_ASS";
    public const string ResultatControlePoste = "RESULTAT_CONTROLE_POSTE";
    public const string ClotureResultatControlePoste = "CLOTURE_RC_POSTE";
    public const string RegistreTracabilite = "REGISTRE_TRACA";
    /// <summary>Résultat contrôle en cours de fabrication (ENCF), document RESULTAT_CF exécuté pendant l'OF.</summary>
    public const string ResultatControleCf = "RESULTAT_CONTROLE_CF";
}

/// <summary>Types de valeur d'une colonne de formulaire (table ColumnValueType).</summary>
public static class TypesValeurColonne
{
    public const string Texte   = "TEXT";
    public const string Nombre  = "NUMBER";
    public const string Date    = "DATE";
    public const string Booleen = "BOOLEAN";
    public const string Liste   = "LIST";

    /// <summary>
    /// Ramène un type reçu à son code (table ColumnValueType). Accepte les anciens libellés français
    /// envoyés par le front (« Texte », « Nombre », « Liste »…) ; vide = texte ; inconnu = conservé tel quel
    /// (la clé étrangère le refusera).
    /// </summary>
    public static string Normaliser(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return Texte;
        return type.Trim().ToUpperInvariant() switch
        {
            "TEXT" or "TEXTE" => Texte,
            "NUMBER" or "NOMBRE" => Nombre,
            "DATE" => Date,
            "BOOLEAN" or "BOOLEEN" or "BOOLÉEN" or "OUI/NON" => Booleen,
            "LIST" or "LISTE" => Liste,
            var autre => autre
        };
    }
}

/// <summary>Moments de contrôle (démarrage, après pause, fin de poste).</summary>
public static class PeriodicitesControle
{
    public const string Demarrage = "DEMARRAGE";
    public const string ApresPause = "APRES_PAUSE";
    public const string FinPoste = "FIN_POSTE";

    /// <summary>Normalise la valeur reçue du client ; toute valeur inconnue ou vide vaut DEMARRAGE.</summary>
    public static string Normaliser(string? valeur) => valeur?.Trim().ToUpperInvariant() switch
    {
        ApresPause => ApresPause,
        FinPoste => FinPoste,
        _ => Demarrage
    };
}

/// <summary>Types de pause déclarables par l'opérateur.</summary>
/// <summary>Fin demandée par l'équipe d'assemblage (Exec_ControleOf_Session.ClosingRequest).</summary>
public static class DemandesCloture
{
    public const string FinPoste = "FIN_POSTE";
    public const string FinOf = "FIN_OF";

    public static readonly IReadOnlySet<string> Autorisees = new HashSet<string> { FinPoste, FinOf };
}

/// <summary>Étape courante d'une session d'assemblage, calculée par le serveur.</summary>
public static class PhasesAssemblage
{
    public const string Echantillonnage = "ECHANTILLONNAGE";
    public const string Demarrage = "DEMARRAGE";
    public const string Production = "PRODUCTION";
    public const string Pause = "PAUSE";
    /// <summary>Retour de pause déjeuner : vérification machine « après pause » avant la production.</summary>
    public const string Reprise = "REPRISE";
    public const string FinPoste = "FIN_POSTE";
    public const string FinOf = "FIN_OF";
    public const string Cloture = "CLOTURE";
    /// <summary>Aucune équipe active : l'OF attend qu'une équipe le rejoigne.</summary>
    public const string SansEquipe = "SANS_EQUIPE";
}

/// <summary>État de la vérification d'une machine du poste pour un moment donné.</summary>
public static class EtatsVerifMachine
{
    public const string AFaire = "A_FAIRE";
    public const string EnCours = "EN_COURS";
    public const string Validee = "VALIDEE";
    /// <summary>Le plan de la machine n'a pas de point pour ce moment.</summary>
    public const string NonPrevue = "NON_PREVUE";
    /// <summary>Aucun plan de vérification actif pour la machine : à signaler.</summary>
    public const string PlanManquant = "PLAN_MANQUANT";
}

/// <summary>Tâches de l'assistant d'assemblage.</summary>
public static class TachesAssemblage
{
    public const string Echantillonnage = "ECHANTILLONNAGE";
    public const string VerifMachine = "VERIF_MACHINE";
    /// <summary>Contrôle de la première pièce (sections LOT_POSTE, et LOT_OF pour la première équipe).</summary>
    public const string PlanPremierePiece = "PLAN_PREMIERE_PIECE";
    /// <summary>Contrôles de production (sections ECHANTILLONNAGE_2859, selon leur périodicité).</summary>
    public const string ControlesProduction = "CONTROLES_PRODUCTION";
    public const string ControlePoste = "CONTROLE_POSTE";
    /// <summary>Contrôle de la dernière pièce (LOT_POSTE en fin de poste, LOT_OF en plus à la fin de l'OF).</summary>
    public const string PlanDernierePiece = "PLAN_DERNIERE_PIECE";
    public const string Tracabilite = "TRACABILITE";
    /// <summary>Contrôle aux réglages (sections REGLAGE / REGLAGE_PROD), à la demande de l'opérateur.</summary>
    public const string Reglage = "REGLAGE";
    /// <summary>Contrôle de toutes les pièces (sections TOUS_PIECES), à la demande de l'opérateur.</summary>
    public const string ToutesPieces = "TOUTES_PIECES";
}

/// <summary>État d'une tâche de l'assistant d'assemblage.</summary>
public static class EtatsTache
{
    public const string AFaire = "A_FAIRE";
    public const string Fait = "FAIT";
    /// <summary>Document de référence absent : la tâche ne peut pas être faite, elle se signale.</summary>
    public const string Manquant = "MANQUANT";
    /// <summary>Rien à faire à ce moment (ex. plan sans section LOT_POSTE / LOT_OF) : tâche masquée, jamais bloquante.</summary>
    public const string NonRequis = "NON_REQUIS";
}

public static class TypesPause
{
    public const string Dejeuner = "DEJEUNER";
    public const string Autre = "AUTRE";

    public static readonly IReadOnlySet<string> Autorises = new HashSet<string> { Dejeuner, Autre };
}

/// <summary>Natures d'article (NatureArticle.Code) suivies dans le registre de traçabilité.</summary>
public static class NaturesArticle
{
    public const string Corps = "CORPS";
    public const string Volant = "VOLANT";
}

/// <summary>Motif de fin d'une période de production (miroir du CHECK de Exec_ProductionPeriod.EndReason).</summary>
public static class MotifsFinPeriode
{
    public const string Pause = "PAUSE";
    /// <summary>Réglage commencé : les contrôles reprennent après un réglage conforme sur tous les critères.</summary>
    public const string Reglage = "SETTING";
    /// <summary>Réglage annulé avant toute saisie : la production reprend.</summary>
    public const string ReglageAnnule = "SETTING_CANCELLED";
    public const string FinPoste = "END_OF_SHIFT";
    /// <summary>Le poste a quitté la production (fin demandée, vérification de reprise à faire…).</summary>
    public const string Arret = "STOPPED";
}

/// <summary>Unité d'une périodicité de contrôle (Periodicite.Unite).</summary>
public static class UnitesPeriodicite
{
    public const string Heure = "HEURE";
    /// <summary>Contrôle d'une série : jamais planifié dans le temps (aucune occurrence).</summary>
    public const string Serie = "SERIE";
}

/// <summary>Contextes d'une réponse ligne (Exec_ControleLigne_Reponse.Contexte).</summary>
public static class ContextesReponse
{
    public const string Reglage = "REGLAGE";
    /// <summary>Contrôle de la première pièce d'une section de lot (début de poste / début d'OF).</summary>
    public const string PremierePiece = "PREMIERE_PIECE";
    /// <summary>Contrôle de la dernière pièce d'une section de lot (fin de poste / fin d'OF).</summary>
    public const string DernierePiece = "DERNIERE_PIECE";
    public const string CentPourCent = "100PCT";
    /// <summary>Comptage des non-conformités par ligne et par tranche horaire (contrôle au poste).</summary>
    public const string ControlePoste = "CONTROLE_POSTE";
}

/// <summary>Type de réponse sur une ligne (miroir du CHECK de Exec_ControleLigne_Reponse.ResultatType).</summary>
public static class ResultatsLigne
{
    public const string Conforme    = "CONFORME";
    public const string NonConforme = "NON_CONFORME";
    public const string Mesure      = "MESURE";
    public const string Comptage    = "COMPTAGE";
}

/// <summary>Résultats de contrôle saisis par l'opérateur.</summary>
public static class ResultatsControle
{
    public const string Conforme = "C";
    public const string NonConforme = "NC";
}
