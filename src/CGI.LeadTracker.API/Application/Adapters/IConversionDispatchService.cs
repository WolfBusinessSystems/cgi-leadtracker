using CGI.LeadTracker.Domain.AggregatesModel.Lead;

namespace CGI.LeadTracker.API.Application.Adapters;

public interface IConversionDispatchService
{
    /// <summary>
    /// Envia o evento de conversão da etapa para a plataforma de origem do lead
    /// (Meta ou Google), respeitando a deduplicação do domínio.
    /// </summary>
    Task DispatchAsync(Lead lead, FunnelStage stage, CancellationToken cancellationToken = default);
}
