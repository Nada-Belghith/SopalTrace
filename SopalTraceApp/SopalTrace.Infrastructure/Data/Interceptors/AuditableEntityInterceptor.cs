using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SopalTrace.Application.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Data.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditableEntityInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var userInfo = _currentUserService.UserInfo;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                var creeParProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreePar");
                if (creeParProperty != null)
                {
                    creeParProperty.CurrentValue = userInfo;
                }

                var creeLeProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreeLe");
                if (creeLeProperty != null)
                {
                    creeLeProperty.CurrentValue = DateTime.Now;
                }
                
                var modifieParProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "ModifiePar");
                if (modifieParProperty != null)
                {
                    modifieParProperty.CurrentValue = userInfo;
                }

                var modifieLeProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "ModifieLe");
                if (modifieLeProperty != null)
                {
                    modifieLeProperty.CurrentValue = DateTime.Now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                var modifieParProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "ModifiePar");
                if (modifieParProperty != null)
                {
                    modifieParProperty.CurrentValue = userInfo;
                }

                var modifieLeProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "ModifieLe");
                if (modifieLeProperty != null)
                {
                    modifieLeProperty.CurrentValue = DateTime.Now;
                }
            }
        }
    }
}
