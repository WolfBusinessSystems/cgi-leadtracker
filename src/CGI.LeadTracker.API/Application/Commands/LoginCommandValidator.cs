using FluentValidation;

namespace CGI.LeadTracker.API.Application.Commands;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.");

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.");
    }
}
