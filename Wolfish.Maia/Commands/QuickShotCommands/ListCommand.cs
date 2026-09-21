using Wolfish.Commands;

namespace Wolfish.Maia.Commands
{
    public class ListCommand : ICliCommand
    {
        public string Name => "list";

        public Task ExecuteAsync(string[] args)
        {
            var baseDirectory = AppContext.BaseDirectory;
            var terminalCommand = new WolfishCommand($"{baseDirectory}TerminalCommands.json");
            var commandList = terminalCommand.LoadFromJson();
            var commandTable = terminalCommand.BuildLimidetTable(commandList);
            Console.WriteLine(commandTable);
            return Task.CompletedTask;
        }
    }
}
