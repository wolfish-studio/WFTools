using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Wolfish.Shared;

namespace Wolfish.Maia.Commands
{
    /// <summary>
    /// Comando "provider" — gerencia LLM providers (adicionar, editar, listar).
    /// Uso: maia provider [list|add|edit]
    /// </summary>
    public class ProviderCommand : ICliCommand
    {
        public string Name => "provider";

        public async Task ExecuteAsync(string[] args)
        {
            var baseDirectory = AppContext.BaseDirectory;
            var configPath = Path.Combine(baseDirectory, "appsettings.json");

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"❌ Arquivo de configuração não encontrado: {configPath}");
                return;
            }

            // Se não tiver subcomando, mostra ajuda
            if (args.Length < 2)
            {
                ShowHelp();
                return;
            }

            var subCommand = args[1].ToLowerInvariant();

            switch (subCommand)
            {
                case "list":
                    await ListProviders(configPath);
                    break;
                case "add":
                    await AddProvider(configPath);
                    break;
                case "edit":
                    await EditProvider(configPath);
                    break;
                default:
                    Console.WriteLine($"❌ Subcomando desconhecido: {subCommand}");
                    ShowHelp();
                    break;
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("📋 Gerenciamento de LLM Providers");
            Console.WriteLine();
            Console.WriteLine("Uso:");
            Console.WriteLine("  maia provider list      Lista todos os providers configurados");
            Console.WriteLine("  maia provider add       Adiciona um novo provider");
            Console.WriteLine("  maia provider edit      Edita um provider existente");
            Console.WriteLine();
        }

        private async Task ListProviders(string configPath)
        {
            var providers = LoadProviders(configPath);
            
            if (providers == null || providers.Count == 0)
            {
                Console.WriteLine("📭 Nenhum provider configurado.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("📋 Providers configurados:");
            Console.WriteLine();

            for (int i = 0; i < providers.Count; i++)
            {
                var p = providers[i];
                var hasKey = !string.IsNullOrWhiteSpace(p.ApiKey) && 
                             !p.ApiKey.StartsWith("API_KEY_", StringComparison.OrdinalIgnoreCase) &&
                             !p.ApiKey.StartsWith("SUA_CHAVE_", StringComparison.OrdinalIgnoreCase) &&
                             p.ApiKey != "no_api_key_required";

                Console.WriteLine($"  [{i + 1}] {p.Name}");
                Console.WriteLine($"      Endpoint: {p.Endpoint}");
                Console.WriteLine($"      API Key:  {(hasKey ? "✓ configurada" : "⚠ placeholder/ausente")}");
                Console.WriteLine();
            }

            await Task.CompletedTask;
        }

        private async Task AddProvider(string configPath)
        {
            Console.WriteLine();
            Console.WriteLine("➕ Adicionar novo Provider");
            Console.WriteLine();

            // Solicitar dados
            Console.Write("Nome do provider: ");
            var name = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("❌ Nome não pode ser vazio.");
                return;
            }

            Console.Write("Endpoint (URL completa da API): ");
            var endpoint = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                Console.WriteLine("❌ Endpoint não pode ser vazio.");
                return;
            }

            Console.Write("API Key (deixe vazio se não precisar): ");
            var apiKey = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = "no_api_key_required";
            }

            // Carregar configuração existente
            var providers = LoadProviders(configPath);
            if (providers == null)
            {
                providers = new List<LlmProvider>();
            }

            // Verificar se já existe
            if (providers.Any(p => p.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) == true))
            {
                Console.WriteLine($"❌ Já existe um provider com o nome '{name}'. Use 'maia provider edit' para alterá-lo.");
                return;
            }

            // Adicionar novo
            providers.Add(new LlmProvider
            {
                Name = name,
                Endpoint = endpoint,
                ApiKey = apiKey
            });

            // Salvar
            await SaveProviders(configPath, providers);
            
            Console.WriteLine();
            Console.WriteLine($"✅ Provider '{name}' adicionado com sucesso!");
            Console.WriteLine();
        }

        private async Task EditProvider(string configPath)
        {
            var providers = LoadProviders(configPath);
            
            if (providers == null || providers.Count == 0)
            {
                Console.WriteLine("📭 Nenhum provider configurado para editar.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("✏️  Editar Provider");
            Console.WriteLine();

            // Listar providers
            for (int i = 0; i < providers.Count; i++)
            {
                Console.WriteLine($"  [{i + 1}] {providers[i].Name}");
            }

            Console.WriteLine();
            Console.Write("Escolha o número do provider (ou nome): ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("❌ Entrada inválida.");
                return;
            }

            LlmProvider? selectedProvider = null;

            // Tentar como número
            if (int.TryParse(input, out var index) && index >= 1 && index <= providers.Count)
            {
                selectedProvider = providers[index - 1];
            }
            else
            {
                // Tentar como nome
                selectedProvider = providers.FirstOrDefault(p => 
                    p.Name?.Equals(input, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (selectedProvider == null)
            {
                Console.WriteLine($"❌ Provider '{input}' não encontrado.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Editando: {selectedProvider.Name}");
            Console.WriteLine();

            // Editar endpoint
            Console.Write($"Endpoint [{selectedProvider.Endpoint}]: ");
            var newEndpoint = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(newEndpoint))
            {
                selectedProvider.Endpoint = newEndpoint;
            }

            // Editar API key
            Console.Write($"API Key [pressione Enter para manter a atual]: ");
            var newApiKey = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(newApiKey))
            {
                selectedProvider.ApiKey = newApiKey;
            }

            // Salvar
            await SaveProviders(configPath, providers);
            
            Console.WriteLine();
            Console.WriteLine($"✅ Provider '{selectedProvider.Name}' atualizado com sucesso!");
            Console.WriteLine();
        }

        private List<LlmProvider>? LoadProviders(string configPath)
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(configPath)!)
                    .AddJsonFile(Path.GetFileName(configPath), optional: false, reloadOnChange: false);

                IConfiguration config = builder.Build();
                return config.GetSection("LLMProviders").Get<List<LlmProvider>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao carregar providers: {ex.Message}");
                return null;
            }
        }

        private async Task SaveProviders(string configPath, List<LlmProvider> providers)
        {
            try
            {
                // Carregar o JSON completo
                var jsonText = await File.ReadAllTextAsync(configPath);
                var doc = JsonDocument.Parse(jsonText);
                
                // Criar novo documento preservando outras seções
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();

                    // Copiar todas as propriedades exceto LLMProviders
                    foreach (var property in doc.RootElement.EnumerateObject())
                    {
                        if (property.Name != "LLMProviders")
                        {
                            property.WriteTo(writer);
                        }
                    }

                    // Escrever LLMProviders atualizado
                    writer.WritePropertyName("LLMProviders");
                    JsonSerializer.Serialize(writer, providers, new JsonSerializerOptions { WriteIndented = true });

                    writer.WriteEndObject();
                }

                // Escrever de volta ao arquivo
                var updatedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                await File.WriteAllTextAsync(configPath, updatedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao salvar providers: {ex.Message}");
                throw;
            }
        }
    }
}
