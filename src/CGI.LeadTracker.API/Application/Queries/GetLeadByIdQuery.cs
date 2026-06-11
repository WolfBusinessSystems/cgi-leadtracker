using CGI.LeadTracker.API.Application.Adapters;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Queries;

public record GetLeadByIdQuery(Guid LeadId) : IRequest<Result<LeadDto>>;
