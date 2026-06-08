using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace SopalTrace.Infrastructure.Repositories;

public class RefFormulaireRepository : IRefFormulaireRepository
{
    private readonly SopalTraceDbContext _context;

    public RefFormulaireRepository(SopalTraceDbContext context)
    {
        _context = context;
    }

    public async Task<RefFormulaire?> GetByIdAsync(Guid id)
    {
        return await _context.RefFormulaires.FirstOrDefaultAsync(r => r.Id == id);
    }

    public Task UpdateAsync(RefFormulaire entity)
    {
        _context.RefFormulaires.Update(entity);
        return Task.CompletedTask;
    }

    public async Task AddAsync(RefFormulaire entity)
    {
        await _context.RefFormulaires.AddAsync(entity);
    }
}
