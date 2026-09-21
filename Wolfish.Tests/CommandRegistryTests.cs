using Wolfish.Maia.Commands;
using Xunit;

namespace Wolfish.Tests
{
    /// <summary>
    /// Testes que validam o CommandRegistry — garante que todos os comandos esperados
    /// estão registrados e que o TryExecuteAsync retorna true/false corretamente.
    /// 
    /// NOTA: Os testes que executam comandos reais do CreateDefault() usam apenas
    /// comandos "seguros" que não chamam Console.ReadLine() nem acessam APIs externas.
    /// Comandos como "home" e "config" bloqueiam o test runner por pedirem input.
    /// </summary>
    public class CommandRegistryTests
    {
        // ═══════════════════════════════════════════════════════════════
        //  Validação de comandos registrados — apenas comandos seguros
        //  (que não pedem input do console nem fazem I/O destrutivo)
        // ═══════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("welcome")]
        [InlineData("help")]
        [InlineData("info")]
        [InlineData("ask")]
        public async Task TryExecuteAsync_ComandoSeguroRegistrado_DeveRetornarTrue(string commandName)
        {
            // Arrange
            var registry = CommandRegistry.CreateDefault();

            // Act — "ask" com args insuficientes apenas imprime uso e retorna
            var args = commandName == "ask"
                ? new[] { "ask" }
                : new[] { commandName };

            var found = await registry.TryExecuteAsync(commandName, args);

            // Assert
            Assert.True(found,
                $"Comando '{commandName}' deveria estar registrado no CommandRegistry");
        }


        [Theory]
        [InlineData("xpto")]
        [InlineData("inexistente")]
        [InlineData("install")]
        [InlineData("download")]
        [InlineData("merge")]
        [InlineData("")]
        public async Task TryExecuteAsync_ComandoNaoRegistrado_DeveRetornarFalse(string commandName)
        {
            // Arrange
            var registry = CommandRegistry.CreateDefault();

            // Act
            var found = await registry.TryExecuteAsync(commandName, [commandName]);

            // Assert
            Assert.False(found,
                $"Comando '{commandName}' não deveria estar registrado no CommandRegistry");
        }

        // ═══════════════════════════════════════════════════════════════
        //  Case Insensitivity — apenas comandos seguros
        // ═══════════════════════════════════════════════════════════════

        [Theory]
        [InlineData("WELCOME")]
        [InlineData("Welcome")]
        [InlineData("wElCoMe")]
        [InlineData("HELP")]
        [InlineData("Help")]
        [InlineData("INFO")]
        [InlineData("Info")]
        [InlineData("ASK")]
        [InlineData("Ask")]
        public async Task TryExecuteAsync_CaseInsensitive_ComandoSeguro_DeveEncontrar(string commandName)
        {
            // Arrange
            var registry = CommandRegistry.CreateDefault();
            var lowerName = commandName.ToLowerInvariant();
            var args = lowerName == "ask"
                ? new[] { commandName }
                : new[] { commandName };

            // Act
            var found = await registry.TryExecuteAsync(commandName, args);

            // Assert
            Assert.True(found,
                $"Comando '{commandName}' (case insensitive) deveria ser encontrado");
        }


        // ═══════════════════════════════════════════════════════════════
        //  Validação de registro customizado (usa mocks, sempre seguro)
        // ═══════════════════════════════════════════════════════════════

        [Fact]
        public async Task Register_NovoComando_DeveSerEncontradoNoRegistry()
        {
            // Arrange
            var registry = new CommandRegistry();
            var mockCommand = new MockCliCommand("test-cmd");
            registry.Register(mockCommand);

            // Act
            var found = await registry.TryExecuteAsync("test-cmd", ["test-cmd"]);

            // Assert
            Assert.True(found);
            Assert.True(mockCommand.WasExecuted, "O comando mock deveria ter sido executado");
        }

        [Fact]
        public async Task Register_SubstituirComando_DeveUsarUltimoRegistrado()
        {
            // Arrange
            var registry = new CommandRegistry();
            var firstCommand = new MockCliCommand("duplicate");
            var secondCommand = new MockCliCommand("duplicate");
            registry.Register(firstCommand);
            registry.Register(secondCommand);

            // Act
            await registry.TryExecuteAsync("duplicate", ["duplicate"]);

            // Assert — somente o segundo (último registrado) deve ter sido executado
            Assert.False(firstCommand.WasExecuted);
            Assert.True(secondCommand.WasExecuted);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Validação do CreateDefault
        // ═══════════════════════════════════════════════════════════════

        [Fact]
        public void CreateDefault_DeveRetornarRegistryNaoNulo()
        {
            // Act
            var registry = CommandRegistry.CreateDefault();

            // Assert
            Assert.NotNull(registry);
        }

        [Fact]
        public async Task CreateDefault_DeveConterComandosSeguros()
        {
            // Arrange — testa apenas os comandos que não bloqueiam o runner
            var registry = CommandRegistry.CreateDefault();
            var safeCommands = new[] { "welcome", "help", "info", "ask" };

            // Act & Assert
            foreach (var cmdName in safeCommands)
            {
                var args = cmdName == "ask" ? new[] { "ask" } : new[] { cmdName };
                var found = await registry.TryExecuteAsync(cmdName, args);

                Assert.True(found,
                    $"Comando '{cmdName}' deveria estar no CreateDefault()");
            }
        }


        /// <summary>
        /// Comando mock para testes de registro customizado.
        /// </summary>
        private class MockCliCommand : ICliCommand
        {
            public string Name { get; }
            public bool WasExecuted { get; private set; }

            public MockCliCommand(string name)
            {
                Name = name;
                WasExecuted = false;
            }

            public Task ExecuteAsync(string[] args)
            {
                WasExecuted = true;
                return Task.CompletedTask;
            }
        }
    }
}
