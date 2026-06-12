using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.API.Application.Commands;
using CGI.LeadTracker.API.Application.Queries;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CGI.LeadTracker.API.Controllers;

[Authorize]
[ApiController]
[Route("api/leads")]
public class LeadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeadsController(IMediator mediator) => _mediator = mediator;

    /// <summary>POST /api/leads — recebe/cria um novo lead</summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveLead([FromBody] ReceiveLeadCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Errors.Select(e => e.Message));
    }

    /// <summary>GET /api/leads/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLeadByIdQuery(id), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Errors.Select(e => e.Message));
    }

    /// <summary>GET /api/leads/by-identifier?identifierValue=xxx&identifierType=1</summary>
    [HttpGet("by-identifier")]
    public async Task<IActionResult> GetByIdentifier(
        [FromQuery] string identifierValue,
        [FromQuery] IdentifierType identifierType,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLeadByIdentifierQuery(identifierValue, identifierType), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Errors.Select(e => e.Message));
    }

    /// <summary>PATCH /api/leads/{id}/stage — avança a etapa do funil</summary>
    [HttpPatch("{id:guid}/stage")]
    public async Task<IActionResult> AdvanceStage(Guid id, [FromBody] AdvanceStageRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AdvanceLeadStageCommand(id, request.NewStage), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Errors.Select(e => e.Message));
    }

    /// <summary>PATCH /api/leads/{id}/contract-value — atualiza valor do contrato</summary>
    [HttpPatch("{id:guid}/contract-value")]
    public async Task<IActionResult> UpdateContractValue(Guid id, [FromBody] UpdateContractValueRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateContractValueCommand(id, request.ContractValue), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Errors.Select(e => e.Message));
    }

    /// <summary>POST /api/leads/sync — força sincronização com RD Station (somente Admin)</summary>
    [Authorize(Policy = "Admin")]
    [HttpPost("sync")]
    public async Task<IActionResult> ForceSync(CancellationToken ct)
    {
        var result = await _mediator.Send(new ForceSyncCommand(), ct);
        return result.IsSuccess
            ? Ok(new { message = "Sincronização concluída." })
            : BadRequest(result.Errors.Select(e => e.Message));
    }
}

/// <summary>Body do PATCH /stage</summary>
public record AdvanceStageRequest(FunnelStage NewStage);

/// <summary>Body do PATCH /contract-value</summary>
public record UpdateContractValueRequest(decimal ContractValue);
