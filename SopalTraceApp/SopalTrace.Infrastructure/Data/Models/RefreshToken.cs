using System;
using System.Collections.Generic;

namespace SopalTrace.Infrastructure.Data.Models;

public partial class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UtilisateurId { get; set; }

    public string Token { get; set; } = null!;

    public string JwtId { get; set; } = null!;

    public DateTime? DateCreation { get; set; }

    public DateTime DateExpiration { get; set; }

    public bool? EstRevoque { get; set; }

    public virtual UtilisateursApp Utilisateur { get; set; } = null!;
}
