using FluentValidation;

namespace CGI.LeadTracker.API.Application.Commands;

public class UpdateContractValueCommandValidator : AbstractValidator<UpdateContractValueCommand>
{
    public UpdateContractValueCommandValidator()
    {
        RuleFor(c => c.LeadId)
            .NotEmpty().WithMessage("O ID do lead é obrigatório.");

        RuleFor(c => c.ContractValue)
            .GreaterThan(0).WithMessage("O valor do contrato deve ser maior que zero.");
    }
}
