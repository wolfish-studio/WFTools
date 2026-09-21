using Microsoft.Extensions.Configuration;
using System.Text;
using Wolfish.ChatAgent;
using Wolfish.Shared;

namespace Wolfish.Maia.Commands
{
    /// <summary>
    /// Comando "ask" — burst (rajada): envia uma pergunta para um ou mais agentes de IA.
    /// Uso: ask <agentName|all> <pergunta...>
    /// </summary>
    public class AskCommand : ICliCommand
    {
        public string Name => "ask";

        public async Task ExecuteAsync(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Uso: ask <agentName|all> <pergunta...>");
                return;
            }

            var agentName = args[1];
            var allAgents = new List<CloudAgent>();

            if (string.IsNullOrWhiteSpace(agentName))
            {
                Console.WriteLine("Please provide a valid agent name.");
                return;
            }

            if (string.Equals(agentName, "all", StringComparison.OrdinalIgnoreCase))
            {
                allAgents = GetAllAgents() ?? new List<CloudAgent>();
            }
            else
            {
                var busca = GetAllAgents() ?? new List<CloudAgent>();
                var agentFound = SearchAgentByName(agentName, busca);
                if (agentFound != null)
                {
                    allAgents.Add(agentFound);
                }
                else
                {
                    Console.WriteLine($"Agent '{agentName}' not found.");
                    return;
                }
            }

            if (allAgents == null || allAgents.Count == 0)
            {
                Console.WriteLine("No agents available.");
                return;
            }

            foreach (var agent in allAgents)
            {
                Console.Write($"Asking {agent.Name}... ");
                var provider = ConfigProvider(agent!.ProviderName);
                if (provider == null)
                {
                    Console.Write($"Provider '{agent.ProviderName}' não encontrado em appsettings.json.");
                    return;
                }

                var allArguments = new StringBuilder();
                for (var i = 2; i < args.Length; i++) allArguments.Append(" " + args[i]);

                var agentHistory = new AgentHistory($"history-{agent.Name}.json");

                if (agent.History == "self")
                {
                    agentHistory.Load();
                }
                else if (agent.History == "global")
                {
                    AgentHistory.LoadGlobalHistories(".", "history-*.json");
                }

                agentHistory.AddSystem(agent.SystemMessage!);
                agentHistory.AddUser(allArguments.ToString());

                var cloudAgent = new OpenAiAgent(agent.Model, provider.Endpoint!, provider.ApiKey!, agentHistory);
                var outputFile = Path.Combine(Directory.GetCurrentDirectory(), $"ask-{agent.Name}-{DateTime.Now:yyyyMMdd-HHmmss}.md");
                var responseBuilder = new StringBuilder();

                if (agent.History == "self" || agent.History == "none")
                {
                    agentHistory.Save();
                }

                try
                {
                    var teste = cloudAgent.SendMessageStreamingAndGetModelAsync();
                    string? usedModel = null;

                    await foreach (var chunk in teste)
                    {
                        responseBuilder.Append(chunk.Text);
                        if (usedModel == null && !string.IsNullOrEmpty(chunk.ModelId))
                        {
                            usedModel = chunk.ModelId;
                        }
                    }

                    if (usedModel != null)
                    {
                        Console.Write($"[{usedModel}] ");
                        responseBuilder.AppendLine();
                        responseBuilder.AppendLine($"\n> **Modelo utilizado:** `{usedModel}`");
                    }

                    await File.WriteAllTextAsync(outputFile, responseBuilder.ToString(), Encoding.UTF8);
                    Console.Write($"Answer written to {outputFile}\n");
                    return;
                }
                catch (Exception ex)
                {
                    Console.Write($"Error: {ex.Message}");
                }
            }
        }

        private static CloudAgent? SearchAgentByName(string agentName, List<CloudAgent>? allAgents)
        {
            var selectedAgent = allAgents?.FirstOrDefault(c => string.Equals(c.Name, agentName, StringComparison.OrdinalIgnoreCase));
            if (selectedAgent is null) return null;
            return selectedAgent;
        }

        private static List<CloudAgent>? GetAllAgents()
        {
            var baseDirectory = AppContext.BaseDirectory;
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"{baseDirectory}cloudagents.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();
            var cloudAgent = new CloudAgent();
            config.GetSection("CloudAgents").Bind(cloudAgent);
            var allAgents = config.GetSection("CloudAgents").Get<List<CloudAgent>>();

            return allAgents;
        }

        private static LlmProvider? ConfigProvider(string providerName)
        {
            var baseDirectory = AppContext.BaseDirectory;
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"{baseDirectory}appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();
            var providers = new LlmProvider();
            config.GetSection("LLMProviders").Bind(providers);
            var allProviders = config.GetSection("LLMProviders").Get<List<LlmProvider>>();

            var selectedProvider = allProviders?.FirstOrDefault(c => string.Equals(c.Name, providerName, StringComparison.OrdinalIgnoreCase));
            if (selectedProvider is null) return null;
            return selectedProvider;
        }
    }
}
