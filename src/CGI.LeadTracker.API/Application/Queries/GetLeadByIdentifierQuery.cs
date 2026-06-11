using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Queries;

public record GetLeadByIdentifierQuery(
    string IdentifierValue,
    IdentifierType IdentifierType) : IRequest<Result<LeadDto>>;
