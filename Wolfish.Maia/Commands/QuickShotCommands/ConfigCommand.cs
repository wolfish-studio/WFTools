using Microsoft.Extensions.Configuration;

namespace Wolfish.Maia.Commands
{
    /// <summary>
    /// Comando "config" — copia appsettings.json, cloudagents.json e TerminalCommands.json
    /// para a pasta configurada em appsettings:MyHome.
    /// </summary>
    public class ConfigCommand : ICliCommand
    {
        public string Name => "config";

        // Arquivos relativos ao BaseDirectory para config
        private static readonly string[] FilesToConfig =
        [
            "appsettings.json",
            "cloudagents.json",
            "TerminalCommands.json"
        ];

        public Task ExecuteAsync(string[] args)
        {
            var baseDirectory = AppContext.BaseDirectory;

            // Lê o MyHome do appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(baseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var myHome = config["MyHome"];

            if (string.IsNullOrWhiteSpace(myHome))
            {
                Console.WriteLine("\n  'MyHome' não está definido no appsettings.json.");
                Console.WriteLine("  Adicione a chave \"MyHome\": \"/seu/caminho\" no appsettings.json.\n");
                return Task.CompletedTask;
            }

            Console.WriteLine();
            Console.WriteLine("  ╔══════════════════════════════════════════╗");
            Console.WriteLine("  ║      Busca as Configurações da Maia      ║");
            Console.WriteLine("  ╚══════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"  Origem:  {baseDirectory}");
            Console.WriteLine($"  Destino: {myHome}");
            Console.WriteLine($"  Data:    {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            Console.WriteLine();

            // Cria a pasta de destino se não existir
            if (!Directory.Exists(myHome))
            {
                try
                {
                    Directory.CreateDirectory(myHome);
                    Console.WriteLine($" Pasta criada: {myHome}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Erro ao criar pasta: {ex.Message}\n");
                    return Task.CompletedTask;
                }
            }

            var copied = 0;
            var errors = 0;

            foreach (var relativeFile in FilesToConfig)
            {
                var sourcePath = Path.Combine(baseDirectory, relativeFile);
                var fileName = Path.GetFileName(relativeFile);
                var destPath = Path.Combine(myHome, fileName);

                if (!File.Exists(sourcePath))
                {
                    Console.WriteLine($"  Não encontrado: {relativeFile}");
                    errors++;
                    continue;
                }

                try
                {
                    File.Copy(sourcePath, destPath, overwrite: true);
                    var size = new FileInfo(destPath).Length;
                    Console.WriteLine($" {fileName} ({size} bytes)");
                    copied++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" {fileName}: {ex.Message}");
                    errors++;
                }
            }

            Console.WriteLine();
            Console.WriteLine("  ────────────────────────────────────────");
            Console.WriteLine($"  Resumo: {copied} copiado(s), {errors} erro(s)");
            Console.WriteLine("  ────────────────────────────────────────");

            if (errors == 0)
                Console.WriteLine("\n  Config iniciada com sucesso!\n");
            else
                Console.WriteLine($"\n  Config iniciada com {errors} erro(s).\n");

            return Task.CompletedTask;
        }
    }
}
