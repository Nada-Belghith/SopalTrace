namespace SopalTrace.Domain.Exceptions;

/// <summary>
/// Exception levée lorsqu'un plan maître actif existe déjà
/// </summary>
public class PlanMaitreAlreadyExistsException : DomainException
{
    public string OperationCode { get; }
    public string TypeRobinetCode { get; }

    public PlanMaitreAlreadyExistsException(string operationCode, string typeRobinetCode)
        : base($"Un Plan Maître ACTIF existe déjà pour l'opération '{operationCode}' et le type de robinet '{typeRobinetCode}'.")
    {
        OperationCode = operationCode;
        TypeRobinetCode = typeRobinetCode;
    }
}

/// <summary>
/// Exception levée lorsqu'un plan d'exception actif existe déjà
/// </summary>
public class PlanExceptionAlreadyExistsException : DomainException
{
    public string OperationCode { get; }
    public string TypeRobinetCode { get; }
    public string CodeArticle { get; }

    public PlanExceptionAlreadyExistsException(string operationCode, string typeRobinetCode, string codeArticle)
        : base($"Un Plan d'exception ACTIF existe déjà pour l'article '{codeArticle}' (Opération: '{operationCode}', Type: '{typeRobinetCode}').")
    {
        OperationCode = operationCode;
        TypeRobinetCode = typeRobinetCode;
        CodeArticle = codeArticle;
    }
}

/// <summary>
/// Exception levée lorsqu'un article SAGE n'existe pas dans l'ERP
/// </summary>
public class ArticleNotFoundInErpException : DomainException
{
    public string CodeArticle { get; }

    public ArticleNotFoundInErpException(string codeArticle)
        : base($"L'article SAGE '{codeArticle}' n'existe pas dans l'ERP.")
    {
        CodeArticle = codeArticle;
    }
}

/// <summary>
/// Exception levée lorsqu'un plan n'existe pas
/// </summary>
public class PlanNotFoundException : DomainException
{
    public Guid PlanId { get; }

    public PlanNotFoundException(Guid planId)
        : base($"Le plan avec l'ID '{planId}' n'existe pas.")
    {
        PlanId = planId;
    }
}

/// <summary>
/// Exception levée lorsqu'un code article est manquant ou invalide
/// </summary>
public class MissingArticleCodeException : DomainException
{
    public MissingArticleCodeException()
        : base("Le code article SAGE est obligatoire pour un plan d'exception.")
    {
    }
}