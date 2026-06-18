using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.User;
using CGI.LeadTracker.Domain.SeedWork;
using CGI.LeadTracker.Infrastructure.Security;
using FluentResults;
using MediatR;

namespace CGI.LeadTracker.API.Application.Commands;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IIdentityService identityService)
    {
        _userRepository  = userRepository;
        _unitOfWork      = unitOfWork;
        _identityService = identityService;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _identityService.GetUserId();
        if (userId == Guid.Empty)
            return Result.Fail("Usuário não autenticado.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result.Fail("Usuário não encontrado.");

        if (!PasswordHelper.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Fail("Senha atual incorreta.");

        user.ChangePassword(PasswordHelper.Hash(request.NewPassword));
        await _unitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result.Ok();
    }
}
