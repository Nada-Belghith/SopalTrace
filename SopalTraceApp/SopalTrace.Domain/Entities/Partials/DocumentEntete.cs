using SopalTrace.Domain.Constants;

namespace SopalTrace.Domain.Entities;

// Complément du code généré (Database First) : ce fichier n'est pas écrasé par le scaffold.
public partial class DocumentEntete
{
    /// <summary>
    /// Portée GENERIQUE par défaut, comme le DEFAULT SQL : un document est générique
    /// tant qu'il n'est pas rattaché à un code article (plans de fabrication).
    /// </summary>
    public DocumentEntete()
    {
        Portee = PorteesDocument.Generique;
    }
}
