using DotNetEnv;

namespace Cashflow.Infrastructure.Configuration;

/// <summary>
/// Utilitário para carregar variáveis de ambiente do arquivo .env
/// </summary>
public static class EnvironmentLoader
{
    private static bool _loaded;

    /// <summary>
    /// Carrega as variáveis de ambiente do arquivo .env
    /// </summary>
    /// <param name="envFilePath">Caminho para o arquivo .env (opcional)</param>
    public static void Load(string? envFilePath = null)
    {
        if (_loaded) return;

        try
        {
            if (!string.IsNullOrEmpty(envFilePath) && File.Exists(envFilePath))
            {
                Env.Load(envFilePath);
            }
            else
            {
                // Tenta carregar .env do diretório atual ou parent
                var possiblePaths = new[]
                {
                    ".env",
                    "../.env",
                    "../../.env",
                    "../../../.env"
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        Env.Load(path);
                        break;
                    }
                }
            }

            // Carrega as configurações das variáveis de ambiente
            InfrastructureSettings.LoadFromEnvironment();
            
            _loaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Aviso: Não foi possível carregar o arquivo .env: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtém uma variável de ambiente ou retorna o valor padrão
    /// </summary>
    public static string GetEnv(string key, string defaultValue = "")
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    /// <summary>
    /// Obtém uma variável de ambiente como int ou retorna o valor padrão
    /// </summary>
    public static int GetEnvInt(string key, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Obtém uma variável de ambiente como bool ou retorna o valor padrão
    /// </summary>
    public static bool GetEnvBool(string key, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
}


