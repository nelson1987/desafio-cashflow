using Cashflow.Application;
using Cashflow.Infrastructure;
using Cashflow.WebApi.Extensions;
using Cashflow.WebApi.Middlewares;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// Observabilidade (Serilog + OpenTelemetry)
// ========================================
builder.Host.ConfigureSerilog(builder.Configuration);

// ========================================
// Configuração de Serviços
// ========================================

// Camadas da aplicação
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// OpenTelemetry (Jaeger + Prometheus)
builder.Services.AddObservability(builder.Configuration);

// Health checks
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Cashflow API",
        Version = "v1",
        Description = "API para controle de fluxo de caixa - Lançamentos e Consolidado Diário",
        Contact = new()
        {
            Name = "Cashflow Team"
        }
    });
});

// Compressão de resposta
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Problem Details para erros padronizados
builder.Services.AddProblemDetails();

var app = builder.Build();

// ========================================
// Aplicar Migrations (se configurado)
// ========================================
var databaseSettings = builder.Configuration.GetSection("Database");
if (databaseSettings.GetValue<bool>("ApplyMigrationsOnStartup"))
{
    await app.Services.ApplyMigrationsAsync();
}

// ========================================
// Configuração do Pipeline HTTP
// ========================================

// Middleware de tratamento de exceções
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger (sempre habilitado para facilitar testes)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cashflow API v1");
    options.RoutePrefix = string.Empty; // Swagger na raiz
});

// Compressão
app.UseResponseCompression();

// CORS
app.UseCors();

// Mapeia todos os endpoints automaticamente
app.MapEndpoints();

// Endpoint de métricas para Prometheus
app.MapObservabilityEndpoints();

// Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

// ========================================
// Inicialização
// ========================================

app.Run();

// Necessário para testes de integração
public partial class Program;