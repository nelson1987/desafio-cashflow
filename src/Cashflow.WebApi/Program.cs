using Cashflow.Application;
using Cashflow.Infrastructure;
using Cashflow.WebApi.Extensions;
using Cashflow.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// Configuração de Serviços
// ========================================

// Camadas da aplicação
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

// ========================================
// Inicialização
// ========================================

app.Run();

// Necessário para testes de integração
public partial class Program;
