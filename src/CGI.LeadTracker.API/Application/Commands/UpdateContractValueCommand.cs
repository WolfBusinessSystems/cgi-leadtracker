using CGI.LeadTracker.API.Application.Adapters;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

public record UpdateContractValueCommand(Guid LeadId, decimal ContractValue) : IRequest<Result<LeadDto>>;
