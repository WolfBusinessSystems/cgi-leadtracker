using CGI.LeadTracker.Domain.Events;
using CGI.LeadTracker.Domain.SeedWork;
using FluentResults;

namespace CGI.LeadTracker.Domain.AggregatesModel.Lead;

public class Lead : Entity, IAggregateRoot
{
    public string RdStationId { get; private set; } = default!;
    public LeadIdentifier Identifier { get; private set; } = default!;
    public PersonalData PersonalData { get; private set; } = default!;
    public decimal? ContractValue { get; private set; }
    public FunnelStage CurrentStage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<ConversionEvent> _conversionEvents = [];
    public IReadOnlyCollection<ConversionEvent> ConversionEvents => _conversionEvents.AsReadOnly();

    private Lead() { }

    public static Lead Create(string rdStationId, LeadIdentifier identifier, PersonalData personalData)
    {
        var lead = new Lead
        {
            RdStationId   = rdStationId,
            Identifier    = identifier,
            PersonalData  = personalData,
            CurrentStage  = FunnelStage.LeadReceived,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow
        };

        lead.AddDomainEvent(new LeadStageChangedDomainEvent(lead, null, FunnelStage.LeadReceived));
        return lead;
    }

    // ── Regras de negócio ────────────────────────────────────────────

    public Result AdvanceStage(FunnelStage newStage)
    {
        if (newStage == CurrentStage)
            return Result.Fail($"Lead já está na etapa '{CurrentStage}'.");

        if (CurrentStage == FunnelStage.Disqualified)
            return Result.Fail("Lead desqualificado não pode avançar no funil.");

        if (newStage != FunnelStage.Disqualified && (int)newStage < (int)CurrentStage)
            return Result.Fail($"Não é permitido retroceder o funil de '{CurrentStage}' para '{newStage}'.");

        var oldStage = CurrentStage;
        CurrentStage = newStage;
        UpdatedAt    = DateTime.UtcNow;

        AddDomainEvent(new LeadStageChangedDomainEvent(this, oldStage, newStage));
        return Result.Ok();
    }

    public Result UpdatePersonalData(PersonalData personalData)
    {
        if (PersonalData == personalData)
            return Result.Ok();

        PersonalData = personalData;
        UpdatedAt    = DateTime.UtcNow;
        return Result.Ok();
    }

    public Result UpdateContractValue(decimal value)
    {
        if (CurrentStage != FunnelStage.ContractClosed)
            return Result.Fail("Valor do contrato só pode ser atualizado na etapa 'Contrato Fechado'.");

        if (value <= 0)
            return Result.Fail("O valor do contrato deve ser maior que zero.");

        if (ContractValue == value)
            return Result.Ok();

        var oldValue  = ContractValue;
        ContractValue = value;
        UpdatedAt     = DateTime.UtcNow;

        AddDomainEvent(new ContractValueUpdatedDomainEvent(this, oldValue, value));
        return Result.Ok();
    }

    // ── Deduplicação ─────────────────────────────────────────────────

    public bool NeedsConversionEvent(FunnelStage stage, AdPlatform platform)
    {
        var sent = _conversionEvents
            .Where(e => e.Stage == stage && e.Platform == platform && e.Status == ConversionEventStatus.Sent)
            .ToList();

        if (sent.Count == 0) return true;

        // Reenviar para Contrato Fechado somente se o valor mudou
        if (stage == FunnelStage.ContractClosed)
            return sent.All(e => e.ContractValue != ContractValue);

        return false;
    }

    public void RegisterConversionEvent(ConversionEvent conversionEvent) =>
        _conversionEvents.Add(conversionEvent);
}
