using System.Net.Http.Json;
using CGI.LeadTracker.Domain.AggregatesModel.Lead;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CGI.LeadTracker.API.Application.Adapters.RdStation;

public class RdStationService : IRdStationService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RdStationService> _logger;

    // Label dos campos customizados no RD Station CRM — configure conforme sua conta
    private const string GclidFieldLabel  = "GCLID";
    private const string FbclidFieldLabel = "FBCLID";
    private const string CpfFieldLabel    = "CPF";

    public RdStationService(
        HttpClient http,
        IConfiguration configuration,
        ILogger<RdStationService> logger)
    {
        _http          = http;
        _configuration = configuration;
        _logger        = logger;
    }

    public async Task<IReadOnlyList<RdStationLeadData>> GetLeadsUpdatedSinceAsync(
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var sinceStr = since.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var leads    = new List<RdStationLeadData>();
        int page     = 1;

        while (true)
        {
            var url      = $"/v1.0/deals?page[size]=100&page[number]={page}&order[field]=updated_at&updated_at[from]={sinceStr}";
            var response = await _http.GetFromJsonAsync<RdStationDealsResponse>(url, cancellationToken);

            if (response?.Deals is null || response.Deals.Count == 0) break;

            foreach (var deal in response.Deals)
            {
                var lead = MapDeal(deal);
                if (lead is not null) leads.Add(lead);
            }

            if (response.Pagination?.NextPage is null) break;
            page++;
        }

        _logger.LogInformation("RD Station: {Count} leads buscados desde {Since}.", leads.Count, since);
        return leads;
    }

    public async Task<RdStationLeadData?> GetLeadByIdAsync(
        string rdStationId,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<RdStationDealItem>(
            $"/v1.0/deals/{rdStationId}",
            cancellationToken);

        return response is null ? null : MapDeal(response);
    }

    // ── Mapeamento ──────────────────────────────────────────────────────

    private RdStationLeadData? MapDeal(RdStationDealItem deal)
    {
        var contact = deal.Contacts?.FirstOrDefault();
        if (contact is null) return null;

        var stage = MapStage(deal.Stage?.Name);
        if (stage is null) return null;

        var customFields = contact.CustomFields ?? [];
        var gclid  = GetCustomField(customFields, GclidFieldLabel);
        var fbclid = GetCustomField(customFields, FbclidFieldLabel);
        var cpf    = GetCustomField(customFields, CpfFieldLabel);

        // Só processa leads que vieram de um clique rastreável
        if (gclid is null && fbclid is null) return null;

        return new RdStationLeadData(
            RdStationId: deal.Id,
            Gclid:       gclid,
            Fbclid:      fbclid,
            Name:        contact.Name,
            Email:       contact.Emails?.FirstOrDefault()?.Email ?? string.Empty,
            Phone:       contact.Phones?.FirstOrDefault()?.Phone,
            Cpf:         cpf,
            Stage:       stage.Value,
            DealValue:   deal.Amount,
            UpdatedAt:   deal.UpdatedAt);
    }

    private FunnelStage? MapStage(string? stageName)
    {
        if (stageName is null) return null;

        var mappings = _configuration.GetSection("RdStation:StageMapping")
            .GetChildren()
            .ToDictionary(x => x.Key, x => x.Value ?? string.Empty);

        if (mappings.TryGetValue(stageName, out var mapped) &&
            Enum.TryParse<FunnelStage>(mapped, out var stage))
            return stage;

        _logger.LogWarning("RD Station: etapa '{Stage}' não mapeada — ignorada.", stageName);
        return null;
    }

    private static string? GetCustomField(
        IReadOnlyList<RdStationCustomField> fields,
        string label) =>
        fields.FirstOrDefault(f => string.Equals(
            f.CustomField?.Label, label, StringComparison.OrdinalIgnoreCase))?.Value;
}
