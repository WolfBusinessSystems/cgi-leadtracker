using FluentValidation;

namespace CGI.LeadTracker.API.Application.Commands;

public class ReceiveLeadCommandValidator : AbstractValidator<ReceiveLeadCommand>
{
    public ReceiveLeadCommandValidator()
    {
        RuleFor(c => c.RdStationId)
            .NotEmpty().WithMessage("O ID do RD Station é obrigatório.")
            .MaximumLength(100).WithMessage("O ID do RD Station não pode ultrapassar 100 caracteres.");

        RuleFor(c => c.IdentifierType)
            .IsInEnum().WithMessage("Tipo de identificador inválido. Use 1 (Gclid) ou 2 (Fbclid).");

        RuleFor(c => c.IdentifierValue)
            .NotEmpty().WithMessage("O valor do identificador é obrigatório.")
            .MaximumLength(500).WithMessage("O identificador não pode ultrapassar 500 caracteres.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(200).WithMessage("O nome não pode ultrapassar 200 caracteres.");

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(200).WithMessage("O e-mail não pode ultrapassar 200 caracteres.");

        RuleFor(c => c.Phone)
            .MaximumLength(30).WithMessage("O telefone não pode ultrapassar 30 caracteres.")
            .When(c => c.Phone is not null);

        RuleFor(c => c.Cpf)
            .MaximumLength(20).WithMessage("O CPF não pode ultrapassar 20 caracteres.")
            .When(c => c.Cpf is not null);
    }
}
