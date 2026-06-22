using SopalTrace.Domain.Constants;
using SopalTrace.Application.DTOs.QualityPlans.Referentiels;
using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using SopalTrace.Application.Helpers;

namespace SopalTrace.Application.Services.QualityPlans.Referentiels;

public class RefFormulaireService : IRefFormulaireService
{
    private readonly IUnitOfWork _unitOfWork;

    public RefFormulaireService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RefFormulaireDto> GetByIdAsync(Guid id)
    {
        var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(id);
        if (form == null) throw new InvalidOperationException("Formulaire introuvable.");

        var cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByCodeReferenceAsync(form.CodeReference);

        return new RefFormulaireDto
        {
            Id = form.Id,
            CodeReference = form.CodeReference,
            Designation = form.Designation,
            Version = form.Version,
            Statut = form.Statut,
            CreeLe = form.CreeLe,
            Role = form.Role,
            ConfigurationStructureJson = ColonneJsonMapper.Serialize(cols)
        };
    }

    public async Task<bool> UpdateConfigurationAsync(Guid id, UpdateRefFormulaireDto dto)
    {
        var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(id);
        if (form == null) return false;

        var parsedCols = ColonneJsonMapper.Deserialize(dto.ConfigurationStructureJson);
        await _unitOfWork.RefFormulaireRepository.SyncColonnesAsync(form.CodeReference, parsedCols);

        await _unitOfWork.RefFormulaireRepository.UpdateAsync(form);
        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<Guid> NouvelleVersionAsync(NouvelleVersionRefFormulaireDto dto)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var oldForm = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(dto.AncienId);
            if (oldForm == null) throw new InvalidOperationException("Ancien formulaire introuvable.");

            var oldCols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByCodeReferenceAsync(oldForm.CodeReference);

            // Archiver l'ancien
            await _unitOfWork.RefFormulaireRepository.UpdateStatutAsync(oldForm.Id, StatutsPlan.Archive);

            // Créer le nouveau
            var newForm = new RefFormulaire
            {
                Id = Guid.NewGuid(),
                CodeReference = oldForm.CodeReference,
                Designation = oldForm.Designation,
                Version = oldForm.Version + 1,
                Statut = StatutsPlan.Actif,
                CreeLe = DateTime.UtcNow,
                Role = oldForm.Role,
            };

            var parsedCols = ColonneJsonMapper.Deserialize(dto.ConfigurationStructureJson ?? ColonneJsonMapper.Serialize(oldCols));
            await _unitOfWork.RefFormulaireRepository.SyncColonnesAsync(newForm.CodeReference, parsedCols);

            await _unitOfWork.RefFormulaireRepository.AddAsync(newForm);
            
            return newForm.Id;
        });
    }
}
