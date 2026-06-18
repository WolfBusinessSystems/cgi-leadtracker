using System.Text;
using System.Text.Json.Serialization;
using CGI.LeadTracker.API.Application.Adapters;
using CGI.LeadTracker.API.Application.Adapters.Google;
using CGI.LeadTracker.API.Application.Adapters.Meta;
using CGI.LeadTracker.API.Application.Adapters.RdStation;
using CGI.LeadTracker.API.Application.Behaviors;
using CGI.LeadTracker.API.Application.Services;
using CGI.LeadTracker.API.Extensions;
using CGI.LeadTracker.API.Middleware;
using CGI.LeadTracker.API.Services;
using CGI.LeadTracker.Infrastructure;
using CGI.LeadTracker.Infrastructure.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        // Aceita/serializa enums como string ("Gclid", "ContractClosed") no JSON
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// ── MediatR ──────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// ── Infraestrutura ────────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Autenticação JWT ──────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Auth:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32 || jwtSecret.StartsWith('['))
    throw new InvalidOperationException(
        "Auth:Secret ausente ou inseguro. Defina um segredo de no mínimo 32 caracteres via variável de " +
        "ambiente Auth__Secret ou appsettings.Production.json antes de iniciar a aplicação.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Mantém os claims originais do JWT (sub, email, name) sem remapeamento legado
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer          = true,
            ValidIssuer             = builder.Configuration["Auth:Issuer"],
            ValidateAudience        = true,
            ValidAudience           = builder.Configuration["Auth:Audience"],
            ValidateLifetime        = true,
            ClockSkew               = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// ── Integrações externas ──────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddTransient<RdStationTokenHandler>();

// BaseAddress precisa terminar em '/' para preservar o segmento de versão da URL
builder.Services.AddHttpClient<IRdStationService, RdStationService>(client =>
    client.BaseAddress = new Uri(builder.Configuration["RdStation:BaseUrl"]!.TrimEnd('/') + "/"))
    .AddHttpMessageHandler<RdStationTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient<IMetaConversionsService, MetaConversionsService>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Meta:BaseUrl"]!.TrimEnd('/') + "/"))
    .AddStandardResilienceHandler();

builder.Services.AddScoped<IGoogleAdsService, GoogleAdsService>();
builder.Services.AddScoped<IConversionDispatchService, ConversionDispatchService>();
builder.Services.AddScoped<ILeadSyncService, LeadSyncService>();
builder.Services.AddHostedService<LeadSyncBackgroundService>();

// ── Serviços de identidade ────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIdentityService, IdentityService>();

// ── Tratamento de erros e health ──────────────────────────────────────
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<LeadTrackerContext>();

// ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "CGI LeadTracker API";
        options.Theme = ScalarTheme.DeepSpace;
    });
}

// Aplica migrations pendentes e semeia o admin (idempotente, todos os ambientes)
await app.InitializeDatabaseAsync();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Iniciando CGI LeadTracker API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API encerrada inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
