using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CGI.LeadTracker.API.Application.Adapters.RdStation;

// DelegatingHandler que obtém/renova automaticamente o Bearer token do RD Station CRM.
// Fluxo: usa o refresh_token (configurado em RdStation:RefreshToken) para obter um
// novo access_token. O RD Station rotaciona o refresh_token a cada uso — o novo valor
// é mantido em memória durante a vida da aplicação. Em produção, persista o novo
// refresh_token no Key Vault / App Configuration para sobreviver a restarts.
public class RdStationTokenHandler : DelegatingHandler
{
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RdStationTokenHandler> _logger;

    private const string AccessTokenCacheKey  = "rdstation_access_token";
    private const string RefreshTokenCacheKey = "rdstation_refresh_token";

    // O RD Station rotaciona o refresh_token a cada uso — renovações concorrentes
    // invalidariam o token uma da outra, então a renovação é serializada.
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public RdStationTokenHandler(
        IHttpClientFactory factory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<RdStationTokenHandler> logger)
    {
        _factory       = factory;
        _configuration = configuration;
        _cache         = cache;
        _logger        = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(AccessTokenCacheKey, out string? cached) && cached is not null)
            return cached;

        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Outra chamada pode ter renovado enquanto aguardava o lock
            if (_cache.TryGetValue(AccessTokenCacheKey, out cached) && cached is not null)
                return cached;

            return await RefreshAccessTokenAsync(cancellationToken);
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private async Task<string> RefreshAccessTokenAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Renovando access token do RD Station via refresh_token.");

        // Usa o refresh_token em memória (já rotacionado) ou o do config (inicial/restart)
        var refreshToken = _cache.TryGetValue(RefreshTokenCacheKey, out string? rt) && rt is not null
            ? rt
            : _configuration["RdStation:RefreshToken"]!;

        var client = _factory.CreateClient();
        var body = new
        {
            client_id     = _configuration["RdStation:ClientId"],
            client_secret = _configuration["RdStation:ClientSecret"],
            refresh_token = refreshToken
        };

        var response = await client.PostAsJsonAsync(
            $"{_configuration["RdStation:BaseUrl"]}/auth/token",
            body,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Falha ao renovar token RD Station ({Status}): {Error}", response.StatusCode, error);
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(
            cancellationToken: cancellationToken);

        // Armazena access_token com margem de 5 min antes do vencimento
        var expiry = TimeSpan.FromSeconds(Math.Max(60, result!.ExpiresIn - 300));
        _cache.Set(AccessTokenCacheKey, result.AccessToken, expiry);

        // RD Station rotaciona o refresh_token — armazena o novo em memória
        if (!string.IsNullOrEmpty(result.RefreshToken))
        {
            _cache.Set(RefreshTokenCacheKey, result.RefreshToken, TimeSpan.FromDays(30));
            _logger.LogInformation(
                "Refresh token RD Station rotacionado. Atualize RdStation:RefreshToken no ambiente se reiniciar a API.");
        }

        return result.AccessToken;
    }

    private record TokenResponse(
        [property: JsonPropertyName("access_token")]  string  AccessToken,
        [property: JsonPropertyName("expires_in")]    int     ExpiresIn,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken);
}
