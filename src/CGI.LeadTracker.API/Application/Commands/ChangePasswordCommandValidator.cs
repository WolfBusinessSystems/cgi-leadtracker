using FluentValidation;

namespace CGI.LeadTracker.API.Application.Commands;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.CurrentPassword)
            .NotEmpty().WithMessage("A senha atual é obrigatória.");

        RuleFor(c => c.NewPassword)
            .NotEmpty().WithMessage("A nova senha é obrigatória.")
            .MinimumLength(8).WithMessage("A nova senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(100).WithMessage("A nova senha não pode ultrapassar 100 caracteres.")
            .NotEqual(c => c.CurrentPassword).WithMessage("A nova senha deve ser diferente da atual.");
    }
}
