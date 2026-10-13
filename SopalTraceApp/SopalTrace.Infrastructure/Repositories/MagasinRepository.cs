using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories;

public class MagasinRepository : IMagasinRepository
{
    private readonly SopalTraceDbContext _context;

    public MagasinRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<MfgheadOrdreFabrication?> GetOfAvecNomenclatureAsync(string numeroOf) =>
        await _context.MfgheadOrdreFabrications
            .AsNoTracking()
            .AsSplitQuery()
            .Include(o => o.CodeArticleNavigation).ThenInclude(a => a.NatureArticleCodeNavigation)
            .Include(o => o.MfgmatBesoinOfs).ThenInclude(b => b.CodeArticleNavigation).ThenInclude(a => a.NatureArticleCodeNavigation)
            .FirstOrDefaultAsync(o => o.NumeroOf == numeroOf);

    public async Task<MagPreparationOf?> GetPreparationActiveAsync(string numeroOf) =>
        await _context.MagPreparationOfs
            .Include(p => p.MagPreparationOfLots)
            .FirstOrDefaultAsync(p => p.NumeroOf == numeroOf && p.Statut != StatutsPreparationMagasin.Annule);

    public async Task<MagPreparationOf?> GetPreparationAsync(Guid id) =>
        await _context.MagPreparationOfs
            .Include(p => p.MagPreparationOfLots)
            .FirstOrDefaultAsync(p => p.Id == id);

    public void AjouterPreparation(MagPreparationOf preparation) => _context.MagPreparationOfs.Add(preparation);
    public void AjouterLot(MagPreparationOfLot lot) => _context.MagPreparationOfLots.Add(lot);
    public void SupprimerLot(MagPreparationOfLot lot) => _context.MagPreparationOfLots.Remove(lot);

    public async Task<Sdelivery?> GetBlAsync(string numeroBl) =>
        await _context.Sdeliveries.AsNoTracking().FirstOrDefaultAsync(b => b.NumeroBl == numeroBl);

    public async Task<MagExpeditionBl?> GetExpeditionDuBlAsync(string numeroBl) =>
        await RequeteExpedition().FirstOrDefaultAsync(e => e.NumeroBl == numeroBl);

    public async Task<MagExpeditionBl?> GetExpeditionAsync(Guid id) =>
        await RequeteExpedition().FirstOrDefaultAsync(e => e.Id == id);

    private IQueryable<MagExpeditionBl> RequeteExpedition() =>
        _context.MagExpeditionBls
            .Include(e => e.MagExpeditionBlScanOfs)
                .ThenInclude(s => s.NumeroOfscanneNavigation)
                    .ThenInclude(o => o.CodeArticleNavigation);

    public void AjouterExpedition(MagExpeditionBl expedition) => _context.MagExpeditionBls.Add(expedition);
    public void AjouterOfExpedie(MagExpeditionBlScanOf scan) => _context.MagExpeditionBlScanOfs.Add(scan);
    public void SupprimerOfExpedie(MagExpeditionBlScanOf scan) => _context.MagExpeditionBlScanOfs.Remove(scan);

    public async Task<bool> ProductionTermineeAsync(string numeroOf) =>
        await _context.MfgheadOrdreFabrications
            .Where(o => o.NumeroOf == numeroOf)
            .AnyAsync(o =>
                o.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations.Any() &&
                o.CodeArticleNavigation.NatureArticleCodeNavigation.NatureArticleOperations.All(op =>
                    o.ExecControleOfs.Any(e => e.OperationCode == op.OperationCode && e.Statut == StatutsExecution.Cloture) &&
                    !o.ExecControleOfs.Any(e => e.OperationCode == op.OperationCode && e.Statut != StatutsExecution.Cloture)));

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
