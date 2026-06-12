using System.Text.Json.Serialization;

namespace CGI.LeadTracker.API.Application.Adapters.RdStation;

// Modelos que espelham o JSON (snake_case) da API do RD Station CRM v1.0
// Ajuste os nomes dos campos conforme a documentação real da sua conta

public record RdStationDealsResponse(
    [property: JsonPropertyName("deals")]      IReadOnlyList<RdStationDealItem> Deals,
    [property: JsonPropertyName("pagination")] RdStationPagination? Pagination);

public record RdStationPagination(
    [property: JsonPropertyName("total")]     int Total,
    [property: JsonPropertyName("next_page")] int? NextPage);

public record RdStationDealItem(
    [property: JsonPropertyName("id")]         string Id,
    [property: JsonPropertyName("name")]       string Name,
    [property: JsonPropertyName("amount")]     decimal? Amount,
    [property: JsonPropertyName("stage")]      RdStationStage? Stage,
    [property: JsonPropertyName("contacts")]   IReadOnlyList<RdStationContact>? Contacts,
    [property: JsonPropertyName("updated_at")] DateTime UpdatedAt);

public record RdStationStage(
    [property: JsonPropertyName("id")]   string Id,
    [property: JsonPropertyName("name")] string Name);

public record RdStationContact(
    [property: JsonPropertyName("name")]          string Name,
    [property: JsonPropertyName("emails")]        IReadOnlyList<RdStationEmail>? Emails,
    [property: JsonPropertyName("phones")]        IReadOnlyList<RdStationPhone>? Phones,
    [property: JsonPropertyName("custom_fields")] IReadOnlyList<RdStationCustomField>? CustomFields);

public record RdStationEmail([property: JsonPropertyName("email")] string Email);
public record RdStationPhone([property: JsonPropertyName("phone")] string Phone);

public record RdStationCustomField(
    [property: JsonPropertyName("custom_field")] RdStationCustomFieldDef? CustomField,
    [property: JsonPropertyName("value")]        string? Value);

public record RdStationCustomFieldDef(
    [property: JsonPropertyName("label")] string Label);
