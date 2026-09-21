using Wolfish.Commands;
using Wolfish.Maia.Commands;

namespace Wolfish.Maia
{
    public class Program
    {        
        private static async Task Main(string[] args)
        {
            var found = false;
            var baseDirectory = AppContext.BaseDirectory;
            var terminalCommand = new WolfishCommand($"{baseDirectory}TerminalCommands.json");
            var commandRegistry = CommandRegistry.CreateDefault();

            //args = ["welcome"];
            //args = ["info"];
            //args = ["home"];
            //args = ["list"];

            //args = ["install","github"];
            //args = ["merge","developer","master"];
            //args = ["download","chrome"];
            //args = ["update","system"];

            //args = ["install", "sdk10"];

            //args = ["apt", "search", "octopi"];
            //args = ["uninstall", "dotnet8"];
            //args = ["ask", "especial", "para", "me", "dar", "dicas", "de", "comandos", "shell", "windows", "e", "linux", "mais", "utilizados", "em", "desenvolvimento", "de", "software", "em", "no", "máximo", "200", "palavras", "e", "em", "portugues"];

            if (args.Length == 0) { await commandRegistry.TryExecuteAsync("help", args); return; }

            //quick shots tiro rapido
            if (args.Length == 1) { found = await commandRegistry.TryExecuteAsync(args[0], args); }

            //clean shots tiro certeiro
            if (!found && args.Length == 2) { found = await terminalCommand.SeekAndExecute(args[0], args[1]); }

            //burst rajada            
            if (!found && args.Length > 2 && args[0] == "ask") { found = await commandRegistry.TryExecuteAsync("ask", args); }

            if (!found) { Console.WriteLine($"Unknown command! Try Again!"); }

        }
                
    }

}

