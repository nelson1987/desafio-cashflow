namespace Cashflow.ConsolidationWorker.Services;

/// <summary>
/// Serviço que mantém um arquivo de health check para o container Docker.
/// O arquivo /tmp/healthy é criado e atualizado periodicamente para indicar
/// que o worker está saudável.
/// </summary>
public class HealthCheckFileService : BackgroundService
{
    private const string HealthFilePath = "/tmp/healthy";
    private readonly ILogger<HealthCheckFileService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(10);

    public HealthCheckFileService(ILogger<HealthCheckFileService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check file service iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Cria ou atualiza o arquivo de health check
                await File.WriteAllTextAsync(
                    HealthFilePath,
                    DateTime.UtcNow.ToString("O"),
                    stoppingToken);
            }
            catch (Exception ex)
            {
                // Em ambientes não-Docker (Windows), o path /tmp não existe
                // Isso é esperado e não deve interromper o worker
                _logger.LogDebug(ex, "Não foi possível atualizar arquivo de health check");
            }

            await Task.Delay(_updateInterval, stoppingToken);
        }

        // Remove o arquivo ao parar
        try
        {
            if (File.Exists(HealthFilePath))
            {
                File.Delete(HealthFilePath);
            }
        }
        catch
        {
            // Ignora erros ao remover
        }
    }
}

