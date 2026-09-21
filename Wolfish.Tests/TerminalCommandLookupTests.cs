using Wolfish.Commands;
using Xunit;

namespace Wolfish.Tests
{
    /// <summary>
    /// Testes que validam o lookup de comandos "clean shot" (2 argumentos)
    /// a partir do arquivo TerminalCommands.json.
    /// Não executam os processos reais — apenas validam que os pares gun+aim
    /// são localizáveis no JSON.
    /// </summary>
    public class TerminalCommandLookupTests
    {
        /// <summary>
        /// Caminho para o TerminalCommands.json copiado para o output.
        /// </summary>
        private static readonly string JsonPath = Path.Combine(
            AppContext.BaseDirectory, "TerminalCommands.json");

        // ═══════════════════════════════════════════════════════════════
        //  Validação de carregamento do JSON
        // ═══════════════════════════════════════════════════════════════

        [Fact]
        public void TerminalCommandsJson_DeveExistirNoOutput()
        {
            // Assert
            Assert.True(File.Exists(JsonPath),
                $"Arquivo TerminalCommands.json não encontrado em: {JsonPath}");
        }

        [Fact]
        public void LoadFromJson_DeveRetornarListaNaoVazia()
        {
            // Arrange
            var wolfishCommand = new WolfishCommand(JsonPath);

            // Act
            var commands = wolfishCommand.LoadFromJson();

            // Assert
            Assert.NotNull(commands);
            Assert.NotEmpty(commands);
        }

        [Fact]
        public void LoadFromJson_TodosOsComandosDevemTerGunAimEStp()
        {
            // Arrange
            var wolfishCommand = new WolfishCommand(JsonPath);

            // Act
            var commands = wolfishCommand.LoadFromJson();

            // Assert — cada comando deve ter gun, aim e pelo menos 1 step
            foreach (var cmd in commands)
            {
                Assert.False(string.IsNullOrWhiteSpace(cmd.Gun),
                    "Comando com 'gun' vazio encontrado");
                Assert.False(string.IsNullOrWhiteSpace(cmd.Aim),
                    $"Comando '{cmd.Gun}' com 'aim' vazio");
                Assert.NotNull(cmd.Stp);
                Assert.NotEmpty(cmd.Stp);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Validação de cada par gun+aim existente no JSON
        // ═══════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("download", "chrome")]
        [InlineData("build", "app-win")]
        [InlineData("update", "linux")]
        [InlineData("install", "node")]
        [InlineData("build", "app-tux")]
        [InlineData("update", "system")]
        [InlineData("upgrade", "system")]
        [InlineData("example", "command")]
        [InlineData("my", "system")]
        [InlineData("download", "qwen")]
        [InlineData("download", "llama")]
        public void SeekAndExecute_ParExistente_DeveEncontrarComandoNoJson(string gun, string aim)
        {
            // Arrange
            var wolfishCommand = new WolfishCommand(JsonPath);
            var commands = wolfishCommand.LoadFromJson();

            // Act — busca o par gun+aim no JSON (mesma lógica do SeekAndExecute)
            var found = commands.Any(c => c.Gun == gun && c.Aim == aim);

            // Assert
            Assert.True(found,
                $"Par '{gun} {aim}' não encontrado no TerminalCommands.json");
        }

        [Theory]
        [InlineData("install", "inexistente")]
        [InlineData("comando_fake", "alvo_fake")]
        [InlineData("", "")]
        [InlineData("download", "firefox")]
        public void SeekAndExecute_ParInexistente_NaoDeveEncontrarComandoNoJson(string gun, string aim)
        {
            // Arrange
            var wolfishCommand = new WolfishCommand(JsonPath);
            var commands = wolfishCommand.LoadFromJson();

            // Act
            var found = commands.Any(c => c.Gun == gun && c.Aim == aim);

            // Assert
            Assert.False(found,
                $"Par '{gun} {aim}' foi encontrado no JSON, mas não deveria existir");
        }

        // ═══════════════════════════════════════════════════════════════
        //  Validação de integridade dos steps de cada comando
        // ═══════════════════════════════════════════════════════════════

        [Fact]
        public void TodosOsSteps_DevemTerCmdPreenchido()
        {
            // Arrange
            var wolfishCommand = new WolfishCommand(JsonPath);
            var commands = wolfishCommand.LoadFromJson();

            // Assert — cada step deve ter "cmd" preenchido
            foreach (var cmd in commands)
            {
                foreach (var step in cmd.Stp!)
                {
                    Assert.False(string.IsNullOrWhiteSpace(step?.Cmd),
                        $"Step do comando '{cmd.Gun} {cmd.Aim}' tem 'cmd' vazio");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Validação da tabela gerada
        // ═══════════════════════════════════════════════════════════════

        [Fact]
        public void BuildLimidetTable_DeveGerarTabelaComCabecalho()
        {
            // Arrange
            var wolfishCommand = new WolfishCommand(JsonPath);
            var commands = wolfishCommand.LoadFromJson();

            // Act
            var table = wolfishCommand.BuildLimidetTable(commands);

            // Assert
            Assert.NotNull(table);
            Assert.Contains("GUN", table);
            Assert.Contains("AIM", table);
            Assert.Contains("MESSAGE", table);
            Assert.Contains("CMD", table);
            Assert.Contains("ARG", table);
            Assert.Contains("+", table);  // separadores da tabela
        }

        [Fact]
        public void BuildTable_DeveGerarTabelaCompleta()
        {
            // Arrange
            var wolfishCommand = new WolfishCommand(JsonPath);
            var commands = wolfishCommand.LoadFromJson();

            // Act
            var table = wolfishCommand.BuildTable(commands);

            // Assert
            Assert.NotNull(table);
            Assert.Contains("GUN", table);
            Assert.Contains("download", table);
            Assert.Contains("chrome", table);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Validação de ListCommandNames
        // ═══════════════════════════════════════════════════════════════

        [Fact]
        public async Task ListCommandNames_DeveRetornarNomesDeComandos()
        {
            // Arrange
            var wolfishCommand = new WolfishCommand(JsonPath);

            // Act
            var names = await wolfishCommand.ListCommandNames();

            // Assert
            Assert.NotNull(names);
            Assert.NotEmpty(names);
            Assert.Contains("download", names);
            Assert.Contains("install", names);
            Assert.Contains("update", names);
        }
    }
}
