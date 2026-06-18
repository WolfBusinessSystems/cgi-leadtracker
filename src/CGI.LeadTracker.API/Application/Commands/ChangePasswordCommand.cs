using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

// O usuário é identificado pelo JWT (claim sub), não pelo corpo da requisição.
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;
