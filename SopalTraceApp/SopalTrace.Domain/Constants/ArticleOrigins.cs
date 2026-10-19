namespace SopalTrace.Domain.Constants
{
    /// <summary>
    /// Origine d'un article (Article.Origin, colonne calculée depuis TypeArticle) :
    /// PF et SF sont fabriqués, COMPOSANT est acheté.
    /// </summary>
    public static class ArticleOrigins
    {
        public const string Manufactured = "MANUFACTURED";
        public const string Purchased = "PURCHASED";
    }
}
