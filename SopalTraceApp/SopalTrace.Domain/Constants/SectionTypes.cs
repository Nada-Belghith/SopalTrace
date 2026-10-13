namespace SopalTrace.Domain.Constants
{
    /// <summary>
    /// Types de section (TypeSection.TypeNormalise). Le type porte seul la règle de contrôle ;
    /// la périodicité, facultative, se choisit sur la section.
    /// </summary>
    public static class SectionTypes
    {
        public const string Reglage = "REGLAGE";
        public const string ReglageProd = "REGLAGE_PROD";
        public const string Echantillonnage3789 = "ECHANTILLONNAGE_3789";
        /// <summary>Échantillonnage par attributs selon ISO 2859-1 (dont la fiche interne FE0591).</summary>
        public const string Echantillonnage2859 = "ECHANTILLONNAGE_2859";
        /// <summary>Échantillonnage par mesures selon ISO 3951-1.</summary>
        public const string Echantillonnage3951 = "ECHANTILLONNAGE_3951";
        public const string LotPoste = "LOT_POSTE";
        public const string LotOf = "LOT_OF";
        public const string TousPieces = "TOUS_PIECES";

        /// <summary>NQA appliqué aux échantillonnages selon norme (valeur Sopal, fixe pour le moment).</summary>
        public const decimal NqaParDefaut = 0.65m;

        /// <summary>Contrôles en cours de production par échantillonnage : relevés répartis dans le temps.</summary>
        public static bool EstEchantillonnage(string? typeNormalise) =>
            typeNormalise is Echantillonnage3789 or Echantillonnage2859 or Echantillonnage3951;
    }
}
