using FluentResults;

namespace CGI.LeadTracker.API.Application.Adapters;

public record GoogleConversionData(
    string Gclid,
    string ConversionActionName,
    DateTime ConversionDateTime,
    decimal? ConversionValue,
    string CurrencyCode = "BRL");

public interface IGoogleAdsService
{
    Task<Result<string>> UploadClickConversionAsync(
        GoogleConversionData conversionData,
        CancellationToken cancellationToken = default);
}
