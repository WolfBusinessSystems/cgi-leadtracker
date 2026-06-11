using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

public record ForceSyncCommand : IRequest<Result>;
