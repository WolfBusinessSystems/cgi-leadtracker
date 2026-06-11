using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

public record AdvanceLeadStageCommand(Guid LeadId, FunnelStage NewStage) : IRequest<Result<LeadDto>>;
