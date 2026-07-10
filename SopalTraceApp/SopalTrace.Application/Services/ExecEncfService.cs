using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Application.Mappers.Execution;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SopalTrace.Application.Services
{
    public class ExecEncfService : IExecEncfService
    {
        private readonly IExecEncfRepository _repository;
        private readonly IOccurrenceService _occurrenceService;

        public ExecEncfService(IExecEncfRepository repository, IOccurrenceService occurrenceService)
        {
            _repository = repository;
            _occurrenceService = occurrenceService;
        }

        public async Task<ExecEncfDto?> GetExecEncfAsync(Guid id)
        {
            var entity = await _repository.GetExecEncfAsync(id);
            return ExecEncfMapper.ToDto(entity);
        }

        public async Task<ExecEncfDto?> GetOrCreateExecEncfByOfAsync(string numeroOf, string posteCode)
        {
            var existingExec = await _repository.GetEnCoursExecEncfByOfAsync(numeroOf, posteCode);

            if (existingExec != null)
            {
                return ExecEncfMapper.ToDto(existingExec);
            }

            // Create new
            var ofDetails = await _repository.GetOfDetailsAsync(numeroOf);

            if (ofDetails == null)
            {
                throw new Exception($"OF {numeroOf} introuvable.");
            }

            var newExec = new ExecControleOf
            {
                Id = Guid.NewGuid(),
                NumeroOf = numeroOf,
                OperationCode = "OP_ENCF", // Arbitrary default or fetched from somewhere
                PosteCode = posteCode,
                TypePlan = "ENCF",
                Statut = "EN_COURS",
                DateDebut = DateTime.Now,
                PlanSourceId = Guid.Empty // Default
            };

            await _repository.AddExecEncfAsync(newExec);
            await _repository.SaveChangesAsync();

            // Refresh to get nav props
            return await GetExecEncfAsync(newExec.Id);
        }

        public async Task<ExecEncfDto?> SaveExecEncfAsync(ExecEncfDto dto)
        {
            if (!dto.Id.HasValue) throw new Exception("ID manquant.");
            var existing = await _repository.GetExecEncfAsync(dto.Id.Value);

            if (existing == null)
            {
                throw new Exception("Exécution introuvable.");
            }

            // Update main fields
            existing.Statut = dto.Statut ?? existing.Statut;
            if (dto.Statut == "CLOTURE" && existing.DateFin == null)
            {
                existing.DateFin = DateTime.Now;
            }

            // Update PieceTypes
            await _repository.RemovePieceTypes(existing.ExecPieceTypes);
            bool hasConformePieceType = false;
            foreach (var ptDto in dto.PiecesTypes)
            {
                ptDto.ExecControleOFId = existing.Id;
                var pt = ExecEncfMapper.ToEntity(ptDto);
                if (pt != null)
                {
                    await _repository.AddPieceType(pt);
                    if (pt.Resultat == "C")
                    {
                        hasConformePieceType = true;
                    }
                }
            }

            if (hasConformePieceType && existing.EstEnReglage)
            {
                existing.EstEnReglage = false;
            }

            // Update Tranches
            await _repository.RemoveTranches(existing.ExecControleTranches);
            foreach (var trDto in dto.Tranches)
            {
                trDto.ExecControleOFId = existing.Id;
                var tr = ExecEncfMapper.ToEntity(trDto);
                if (tr != null)
                {
                    await _repository.AddTranche(tr);
                }
            }

            await _repository.SaveChangesAsync();

            return await GetExecEncfAsync(existing.Id);
        }
    }
}
