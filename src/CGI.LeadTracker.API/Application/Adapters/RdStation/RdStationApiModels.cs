namespace CGI.LeadTracker.API.Application.Adapters.RdStation;

// Modelos que espelham o JSON da API do RD Station CRM v1.0
// Ajuste os nomes dos campos conforme a documentação real da sua conta

public record RdStationTokenResponse(
    string AccessToken,
    int ExpiresIn);

public record RdStationDealsResponse(
    IReadOnlyList<RdStationDealItem> Deals,
    RdStationPagination? Pagination);

public record RdStationPagination(int Total, int? NextPage);

public record RdStationDealItem(
    string Id,
    string Name,
    decimal? Amount,
    RdStationStage? Stage,
    IReadOnlyList<RdStationContact>? Contacts,
    DateTime UpdatedAt);

public record RdStationStage(string Id, string Name);

public record RdStationContact(
    string Name,
    IReadOnlyList<RdStationEmail>? Emails,
    IReadOnlyList<RdStationPhone>? Phones,
    IReadOnlyList<RdStationCustomField>? CustomFields);

public record RdStationEmail(string Email);
public record RdStationPhone(string Phone);

public record RdStationCustomField(
    RdStationCustomFieldDef? CustomField,
    string? Value);

public record RdStationCustomFieldDef(string Label);
