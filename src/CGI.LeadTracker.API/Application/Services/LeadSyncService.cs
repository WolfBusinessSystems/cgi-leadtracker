using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using CGI.LeadTracker.Domain.SeedWork;
using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CGI.LeadTracker.API.Application.Services;

public class LeadSyncService : ILeadSyncService
{
    private readonly IRdStationService _rdStation;
    private readonly ILeadRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LeadSyncService> _logger;

    public LeadSyncService(
        IRdStationService rdStation,
        ILeadRepository repository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<LeadSyncService> logger)
    {
        _rdStation     = rdStation;
        _repository    = repository;
        _unitOfWork    = unitOfWork;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task<Result> SyncAsync(CancellationToken cancellationToken = default)
    {
        var lookbackHours = _configuration.GetValue("RdStation:SyncLookbackHours", 24);
        var since         = DateTime.UtcNow.AddHours(-lookbackHours);

        _logger.LogInformation("Iniciando sync com RD Station desde {Since}.", since);

        IReadOnlyList<RdStationLeadData> rdLeads;
        try
        {
            rdLeads = await _rdStation.GetLeadsUpdatedSinceAsync(since, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao buscar leads do RD Station.");
            return Result.Fail("Falha ao buscar leads do RD Station: " + ex.Message);
        }

        var errors = new List<string>();

        foreach (var rdLead in rdLeads)
        {
            try
            {
                await ProcessLeadAsync(rdLead, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar lead RdStationId={Id}.", rdLead.RdStationId);
                errors.Add($"Lead {rdLead.RdStationId}: {ex.Message}");
            }
        }

        _logger.LogInformation(
            "Sync concluído. Processados: {Total}. Erros: {Errors}.",
            rdLeads.Count, errors.Count);

        return errors.Count == 0
            ? Result.Ok()
            : Result.Fail(string.Join(" | ", errors));
    }

    private async Task ProcessLeadAsync(RdStationLeadData rdLead, CancellationToken ct)
    {
        var personalData = new PersonalData(
            rdLead.Name,
            rdLead.Email,
            rdLead.Phone,
            rdLead.Cpf);

        var existing = await _repository.GetByRdStationIdAsync(rdLead.RdStationId, ct);

        if (existing is null)
        {
            await CreateLeadAsync(rdLead, personalData, ct);
        }
        else
        {
            await UpdateLeadAsync(existing, rdLead, personalData, ct);
        }
    }

    private async Task CreateLeadAsync(
        RdStationLeadData rdLead,
        PersonalData personalData,
        CancellationToken ct)
    {
        // Determina o identificador de rastreamento do anúncio
        var (identifierType, identifierValue) = rdLead.Gclid is not null
            ? (IdentifierType.Gclid,  rdLead.Gclid)
            : (IdentifierType.Fbclid, rdLead.Fbclid!);

        // Verifica se já existe pelo identificador (duplicata via outro canal)
        var byIdentifier = await _repository.GetByIdentifierAsync(identifierValue, identifierType, ct);
        if (byIdentifier is not null)
        {
            _logger.LogWarning(
                "Lead com identificador '{Value}' já existe (RdStation: {Id}). Ignorando.",
                identifierValue, rdLead.RdStationId);
            return;
        }

        var lead = Lead.Create(
            rdLead.RdStationId,
            new LeadIdentifier(identifierType, identifierValue),
            personalData);

        // Avança para o estágio atual do RD Station (se não for LeadReceived)
        if (rdLead.Stage != FunnelStage.LeadReceived)
            lead.AdvanceStage(rdLead.Stage);

        if (rdLead.Stage == FunnelStage.ContractClosed && rdLead.DealValue > 0)
            lead.UpdateContractValue(rdLead.DealValue!.Value);

        await _repository.AddAsync(lead, ct);
        await _unitOfWork.SaveEntitiesAsync(ct);

        _logger.LogInformation("Lead criado: RdStationId={Id}, Etapa={Stage}.", rdLead.RdStationId, rdLead.Stage);
    }

    private async Task UpdateLeadAsync(
        Lead existing,
        RdStationLeadData rdLead,
        PersonalData personalData,
        CancellationToken ct)
    {
        var changed = false;

        existing.UpdatePersonalData(personalData);

        if (rdLead.Stage != existing.CurrentStage)
        {
            var result = existing.AdvanceStage(rdLead.Stage);
            if (result.IsFailed)
            {
                _logger.LogWarning(
                    "Não foi possível avançar lead {Id} para {Stage}: {Error}",
                    rdLead.RdStationId, rdLead.Stage, result.Errors.First().Message);
            }
            else
            {
                changed = true;
            }
        }

        if (rdLead.Stage == FunnelStage.ContractClosed && rdLead.DealValue > 0)
        {
            var result = existing.UpdateContractValue(rdLead.DealValue!.Value);
            if (result.IsSuccess) changed = true;
        }

        await _unitOfWork.SaveEntitiesAsync(ct);

        if (changed)
            _logger.LogInformation("Lead atualizado: RdStationId={Id}, Etapa={Stage}.", rdLead.RdStationId, rdLead.Stage);
    }
}
