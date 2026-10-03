namespace Wolfish.Maia.Commands
{
    /// <summary>
    /// Registro central de comandos CLI.
    /// Para adicionar um novo comando:
    ///   1. Crie uma classe que implemente ICliCommand
    ///   2. Adicione-a ao método RegisterDefaults() abaixo
    /// </summary>
    public class CommandRegistry
    {
        private readonly Dictionary<string, ICliCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registra um comando no registry.
        /// </summary>
        public CommandRegistry Register(ICliCommand command)
        {
            _commands[command.Name] = command;
            return this;
        }

        /// <summary>
        /// Tenta encontrar e executar o comando pelo nome.
        /// Retorna true se o comando foi encontrado e executado.
        /// </summary>
        public async Task<bool> TryExecuteAsync(string commandName, string[] args)
        {
            if (_commands.TryGetValue(commandName, out var command))
            {
                await command.ExecuteAsync(args);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cria um registry com todos os comandos padrão registrados.
        /// </summary>
        public static CommandRegistry CreateDefault()
        {
            var registry = new CommandRegistry();

            // Registre novos comandos aqui:
            registry.Register(new WelcomeCommand())
                    .Register(new ListCommand())
                    //.Register(new PlatformCommand())
                    .Register(new ConfigCommand())
                    .Register(new HomeCommand())
                    .Register(new HelpCommand())
                    .Register(new InfoCommand())
                    .Register(new ProviderCommand())
                    .Register(new AskCommand());

            return registry;
        }
    }
}
