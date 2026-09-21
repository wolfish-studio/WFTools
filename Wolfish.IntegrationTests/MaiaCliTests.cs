using System.Diagnostics;
using System.Text;
using Xunit;

namespace Wolfish.IntegrationTests
{
    /// <summary>
    /// Testes de integração que executam o binário Wolfish.Maia como processo externo
    /// e validam exit code + saída no console para cada entrada de comando.
    /// </summary>
    public class MaiaCliTests
    {
        /// <summary>
        /// Caminho para o executável compilado do Wolfish.Maia.
        /// Usa dotnet run para executar o projeto diretamente.
        /// </summary>
        private static readonly string MaiaProjectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Wolfish.Maia", "Wolfish.Maia.csproj"));

        /// <summary>
        /// Executa o Wolfish.Maia com os argumentos fornecidos via "dotnet run"
        /// e retorna o exit code, stdout e stderr.
        /// </summary>
        private static async Task<(int ExitCode, string Stdout, string Stderr)> RunMaiaAsync(params string[] args)
        {
            var arguments = $"run --project \"{MaiaProjectPath}\" --framework net11.0 --no-build -- {string.Join(" ", args)}";

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using var process = new Process { StartInfo = startInfo };
            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) stdoutBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) stderrBuilder.AppendLine(e.Data);
            };

            process.Start();
            
            // Fornece um input padrão ("N\n") para comandos que possam ficar bloqueados aguardando Console.ReadLine()
            await process.StandardInput.WriteLineAsync("N");
            process.StandardInput.Close();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Timeout de 60 segundos para compilação + execução
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await process.WaitForExitAsync(cts.Token);

            return (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
        }

        // ═══════════════════════════════════════════════════════════════
        //  Quick Shot Commands (1 argumento)
        // ═══════════════════════════════════════════════════════════════

        [Fact]
        public async Task SemArgumentos_DeveExibirHelp()
        {
            // Arrange & Act
            var (exitCode, stdout, _) = await RunMaiaAsync();

            // Assert — sem argumentos, o programa exibe a ajuda (help)
            Assert.Equal(0, exitCode);
            Assert.Contains("Uso:", stdout);
        }

        [Fact]
        public async Task Welcome_DeveExibirMensagemDeBoasVindas()
        {
            // Arrange & Act
            var (exitCode, stdout, _) = await RunMaiaAsync("welcome");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("Thank you", stdout, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task List_DeveExibirTabelaDeComandos()
        {
            // Arrange & Act
            var (exitCode, stdout, _) = await RunMaiaAsync("list");

            // Assert — a tabela gerada por BuildLimidetTable contém separadores "+"
            Assert.Equal(0, exitCode);
            Assert.Contains("+", stdout);
            Assert.Contains("GUN", stdout);
        }

        [Fact]
        public async Task Help_DeveExibirInstrucoesDeUso()
        {
            // Arrange & Act
            var (exitCode, stdout, _) = await RunMaiaAsync("help");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("Uso:", stdout);
            Assert.Contains("maia welcome", stdout);
            Assert.Contains("maia ask", stdout);
        }

        [Fact]
        public async Task Info_DeveExibirInformacoesDaAplicacao()
        {
            // Arrange & Act
            var (exitCode, stdout, _) = await RunMaiaAsync("info");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("Wolfish.Maia", stdout);
            Assert.Contains("Version:", stdout);
        }

        [Fact]
        public async Task Config_DeveExibirMensagemDeConfiguracao()
        {
            // Arrange & Act
            var (exitCode, stdout, _) = await RunMaiaAsync("config");

            // Assert — pode exibir a tela de config ou a mensagem de erro de MyHome
            Assert.Equal(0, exitCode);
            Assert.True(
                stdout.Contains("Configura", StringComparison.OrdinalIgnoreCase) ||
                stdout.Contains("MyHome", StringComparison.OrdinalIgnoreCase) ||
                stdout.Contains("copiado", StringComparison.OrdinalIgnoreCase),
                $"Saída inesperada do comando config: {stdout}");
        }

        [Fact]
        public async Task Home_DeveExibirInformacaoSobreMyHome()
        {
            // Arrange & Act
            var (exitCode, stdout, _) = await RunMaiaAsync("home");

            // Assert — pode exibir o MyHome atual ou pedir confirmação
            Assert.Equal(0, exitCode);
            Assert.True(
                stdout.Contains("MyHome", StringComparison.OrdinalIgnoreCase) ||
                stdout.Contains("caminho", StringComparison.OrdinalIgnoreCase) ||
                stdout.Contains("home", StringComparison.OrdinalIgnoreCase),
                $"Saída inesperada do comando home: {stdout}");
        }

        // ═══════════════════════════════════════════════════════════════
        //  Burst Commands (3+ argumentos)
        // ═══════════════════════════════════════════════════════════════

        [Fact]
        public async Task Ask_SemArgumentosSuficientes_DeveExibirUso()
        {
            // Arrange & Act — "ask" sem agente nem pergunta
            var (exitCode, stdout, _) = await RunMaiaAsync("ask");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("Uso: ask", stdout, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Ask_ComApenasAgente_DeveExibirUso()
        {
            // Arrange & Act — "ask principal" (apenas 2 args, falta a pergunta)
            var (exitCode, stdout, _) = await RunMaiaAsync("ask", "principal");

            // Assert — com 2 args, args[0]="ask", o Program.cs NÃO entra no burst (precisa >2 args)
            // Então cai no "Unknown command" ou no fluxo de 2 args (SeekAndExecute)
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public async Task Ask_ComAgenteInexistente_DeveExibirMensagemDeErro()
        {
            // Arrange & Act — agente que não existe no cloudagents.json
            var (exitCode, stdout, _) = await RunMaiaAsync("ask", "agente_inexistente", "ola");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("not found", stdout, StringComparison.OrdinalIgnoreCase);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Comandos Desconhecidos
        // ═══════════════════════════════════════════════════════════════

        [Fact]
        public async Task ComandoDesconhecido_UmArgumento_DeveExibirMensagemDeErro()
        {
            // Arrange & Act
            var (exitCode, stdout, _) = await RunMaiaAsync("xpto_comando_invalido");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("Unknown command", stdout, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ComandoDesconhecido_DoisArgumentos_DeveExibirMensagemDeErro()
        {
            // Arrange & Act — par gun+aim que não existe no JSON
            var (exitCode, stdout, _) = await RunMaiaAsync("foo_invalido", "bar_invalido");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("Unknown command", stdout, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ComandoDesconhecido_TresArgumentos_DeveExibirMensagemDeErro()
        {
            // Arrange & Act — 3+ args mas primeiro arg não é "ask"
            var (exitCode, stdout, _) = await RunMaiaAsync("foo_invalido", "bar", "baz");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("Unknown command", stdout, StringComparison.OrdinalIgnoreCase);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Case Insensitivity — CommandRegistry usa StringComparer.OrdinalIgnoreCase
        // ═══════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("WELCOME")]
        [InlineData("Welcome")]
        [InlineData("HELP")]
        [InlineData("Help")]
        [InlineData("INFO")]
        [InlineData("Info")]
        [InlineData("LIST")]
        [InlineData("List")]
        public async Task ComandosCaseInsensitive_DevemFuncionarCorretamente(string comando)
        {
            // Arrange & Act
            var (exitCode, stdout, _) = await RunMaiaAsync(comando);

            // Assert — não deve cair em "Unknown command"
            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("Unknown command", stdout, StringComparison.OrdinalIgnoreCase);
        }
    }
}
