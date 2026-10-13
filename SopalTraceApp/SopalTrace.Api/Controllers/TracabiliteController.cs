using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SopalTrace.Api.Extensions;
using SopalTrace.Application.DTOs.Tracabilite;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Api.Controllers
{
    [Authorize(Roles = RolesApp.Admin + "," + RolesApp.Operateur)]
    [Route("api/Operateur/tracabilite")]
    [ApiController]
    public class TracabiliteController : ControllerBase
    {
        private const string TypeLot = "^(" + NaturesArticle.Corps + "|" + NaturesArticle.Volant + ")$";

        private readonly ITracabiliteService _tracabiliteService;

        public TracabiliteController(ITracabiliteService tracabiliteService)
        {
            _tracabiliteService = tracabiliteService;
        }

        [HttpGet("{execControleOfId:guid}")]
        public async Task<IActionResult> GetRegistre(Guid execControleOfId,
            [FromQuery, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string? posteCode = null)
        {
            var result = await _tracabiliteService.GetRegistreTracabiliteAsync(execControleOfId, posteCode);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("ligne")]
        public async Task<IActionResult> AddLigne([FromBody] AddRegistreTracabiliteDto dto)
        {
            if (dto.ExecControleOfId == Guid.Empty)
                return BadRequest(new { success = false, message = "Données invalides." });

            var result = await _tracabiliteService.AddLigneTracabiliteAsync(dto, User.GetMatricule());
            return Ok(new { success = true, data = result });
        }

        /// <summary>Lot de corps / volant reconnu pour l'article de l'OF.</summary>
        [HttpGet("{execControleOfId:guid}/validate-lot")]
        public async Task<IActionResult> ValidateLot(
            Guid execControleOfId,
            [FromQuery, Required, RegularExpression(TypeLot)] string type,
            [FromQuery, Required, StringLength(100)] string lot)
        {
            var isValid = await _tracabiliteService.ValidateLotAsync(execControleOfId, type, lot);
            return Ok(new { success = true, isValid });
        }

        [HttpGet("{execControleOfId:guid}/search-lots")]
        public async Task<IActionResult> SearchLots(
            Guid execControleOfId,
            [FromQuery, Required, RegularExpression(TypeLot)] string type,
            [FromQuery, StringLength(100)] string? query = null)
        {
            var lots = await _tracabiliteService.SearchLotsAsync(execControleOfId, type, query ?? "");
            return Ok(new { success = true, data = lots });
        }

        /// <summary>L'équipe en production confirme ses lots (obligatoire avant sa fin de poste).</summary>
        [HttpPost("{execControleOfId:guid}/confirmer")]
        public async Task<IActionResult> ConfirmerEquipe(
            Guid execControleOfId,
            [FromQuery, Required, RegularExpression(FormatsEntree.Code, ErrorMessage = FormatsEntree.MessageCodeInvalide)] string posteCode)
        {
            await _tracabiliteService.ConfirmerEquipeAsync(execControleOfId, posteCode, User.GetMatricule());
            return Ok(new { success = true });
        }

        /// <summary>Annule une ligne erronée : elle reste dans l'historique avec son motif.</summary>
        [HttpPost("ligne/{ligneId:guid}/annuler")]
        public async Task<IActionResult> AnnulerLigne(Guid ligneId, [FromBody] AnnulerLigneTracabiliteDto dto)
        {
            await _tracabiliteService.AnnulerLigneAsync(ligneId, dto.Raison, User.GetMatricule());
            return Ok(new { success = true });
        }
    }
}
