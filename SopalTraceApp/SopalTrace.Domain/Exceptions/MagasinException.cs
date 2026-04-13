using System;

namespace SopalTrace.Domain.Exceptions;

// =================================================================
// EXCEPTIONS SPÉCIFIQUES AU MODULE "MAGASIN / ERP" (Sprint 2)
// =================================================================

public class OfIntrouvableException : DomainException
{
    public OfIntrouvableException(string numeroOf)
        : base($"L'Ordre de Fabrication '{numeroOf}' n'existe pas ou est introuvable dans l'ERP.") { }
}

public class BlIntrouvableException : DomainException
{
    public BlIntrouvableException(string numeroBl)
        : base($"Le Bon de Livraison '{numeroBl}' est introuvable dans l'ERP.") { }
}

public class NomenclatureIntrouvableException : DomainException
{
    public NomenclatureIntrouvableException(string numeroOf)
        : base($"Aucune nomenclature (composants) n'a été trouvée pour l'OF '{numeroOf}'.") { }
}