using FluentResults;
using Google.Ads.GoogleAds.Config;
using Google.Ads.GoogleAds.Lib;
using Google.Ads.GoogleAds.V21.Errors;
using Google.Ads.GoogleAds.V21.Services;
using Google.Ads.Gax.Config;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GadsServices = Google.Ads.GoogleAds.Services;

namespace CGI.LeadTracker.API.Application.Adapters.Google;

public class GoogleAdsService : IGoogleAdsService
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GoogleAdsService> _logger;

    public GoogleAdsService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<GoogleAdsService> logger)
    {
        _configuration = configuration;
        _cache         = cache;
        _logger        = logger;
    }

    public async Task<Result<string>> UploadClickConversionAsync(
        GoogleConversionData data,
        CancellationToken cancellationToken = default)
    {
        if (!bool.TryParse(_configuration["GoogleAds:Enabled"], out var enabled) || !enabled)
        {
            _logger.LogWarning("Google Ads desabilitado. Conversão ignorada: {Action}", data.ConversionActionName);
            return Result.Fail<string>("Google Ads não habilitado.");
        }

        try
        {
            var client     = BuildClient();
            var customerId = _configuration["GoogleAds:CustomerId"]!;

            var actionResource = await GetConversionActionResourceAsync(client, customerId, data.ConversionActionName);
            if (actionResource is null)
            {
                _logger.LogWarning(
                    "Conversion action '{Action}' não encontrada no Google Ads (cliente {CustomerId}).",
                    data.ConversionActionName, customerId);
                return Result.Fail<string>($"Conversion action '{data.ConversionActionName}' não encontrada.");
            }

            ConversionUploadServiceClient conversionUpload =
                client.GetService(GadsServices.V21.ConversionUploadService);

            var clickConversion = new ClickConversion
            {
                Gclid              = data.Gclid,
                ConversionAction   = actionResource,
                ConversionDateTime = data.ConversionDateTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:sszzz"),
            };

            if (data.ConversionValue.HasValue)
            {
                clickConversion.ConversionValue = (double)data.ConversionValue.Value;
                clickConversion.CurrencyCode    = data.CurrencyCode;
            }

            var request = new UploadClickConversionsRequest
            {
                CustomerId     = customerId,
                PartialFailure = true,
            };
            request.Conversions.Add(clickConversion);

            var response = await conversionUpload.UploadClickConversionsAsync(request);

            if (response.PartialFailureError is not null)
            {
                var errorMsg = response.PartialFailureError.Message;
                _logger.LogError("Google Ads partial failure Gclid={Gclid}: {Error}", data.Gclid, errorMsg);
                return Result.Fail<string>(errorMsg);
            }

            var jobId = response.JobId.ToString();
            _logger.LogInformation(
                "Conversão Google Ads enviada: Gclid={Gclid}, Action={Action}, Job={Job}",
                data.Gclid, data.ConversionActionName, jobId);

            return Result.Ok(jobId);
        }
        catch (GoogleAdsException ex)
        {
            var message = ex.Failure?.Errors?.FirstOrDefault()?.Message ?? ex.Message;
            _logger.LogError(ex, "Erro Google Ads API: {Error}", message);
            return Result.Fail<string>(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao enviar conversão Google Ads.");
            return Result.Fail<string>(ex.Message);
        }
    }

    private GoogleAdsClient BuildClient()
    {
        var config = new GoogleAdsConfig
        {
            DeveloperToken     = _configuration["GoogleAds:DeveloperToken"]!,
            OAuth2ClientId     = _configuration["GoogleAds:OAuthClientId"]!,
            OAuth2ClientSecret = _configuration["GoogleAds:OAuthClientSecret"]!,
            OAuth2RefreshToken = _configuration["GoogleAds:RefreshToken"]!,
            OAuth2Mode         = OAuth2Flow.APPLICATION,
        };

        if (_configuration["GoogleAds:LoginCustomerId"] is { Length: > 0 } loginId)
            config.LoginCustomerId = loginId;

        return new GoogleAdsClient(config);
    }

    private async Task<string?> GetConversionActionResourceAsync(
        GoogleAdsClient client,
        string customerId,
        string actionName)
    {
        var cacheKey = $"gads_action_{customerId}_{actionName}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        GoogleAdsServiceClient googleAdsService =
            client.GetService(GadsServices.V21.GoogleAdsService);

        var query = $"SELECT conversion_action.resource_name " +
                    $"FROM conversion_action " +
                    $"WHERE conversion_action.name = '{actionName}' " +
                    $"AND conversion_action.status = 'ENABLED'";

        string? resourceName = null;

        await googleAdsService.SearchStreamAsync(
            customerId,
            query,
            (SearchGoogleAdsStreamResponse response) =>
            {
                if (resourceName is null && response.Results.Count > 0)
                    resourceName = response.Results[0].ConversionAction.ResourceName;
            });

        if (resourceName is not null)
            _cache.Set(cacheKey, resourceName, TimeSpan.FromHours(24));

        return resourceName;
    }
}
