using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SopalTrace.Application.Tests.Services
{
    /// <summary>
    /// Le fournisseur EF en mémoire ne génère pas les colonnes ROWVERSION (SQL Server le fait) :
    /// cet intercepteur leur attribue une nouvelle valeur à chaque enregistrement.
    /// </summary>
    internal sealed class RowVersionEnMemoire : SaveChangesInterceptor
    {
        public static DbContextOptionsBuilder<TContext> Activer<TContext>(DbContextOptionsBuilder<TContext> options) where TContext : DbContext
            => options.AddInterceptors(new RowVersionEnMemoire());

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Attribuer(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Attribuer(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void Attribuer(DbContext? context)
        {
            if (context == null) return;
            foreach (var entree in context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
            {
                var rowVersion = entree.Properties.FirstOrDefault(p => p.Metadata.IsConcurrencyToken && p.Metadata.ClrType == typeof(byte[]));
                if (rowVersion != null) rowVersion.CurrentValue = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
