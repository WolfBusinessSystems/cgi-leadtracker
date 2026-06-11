using CGI.LeadTracker.API.Application.Adapters;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

public record LoginCommand(string Email, string Password) : IRequest<Result<TokenDto>>;
