using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

public record ReceiveLeadCommand(
    string RdStationId,
    IdentifierType IdentifierType,
    string IdentifierValue,
    string Name,
    string Email,
    string? Phone,
    string? Cpf) : IRequest<Result<LeadDto>>;
