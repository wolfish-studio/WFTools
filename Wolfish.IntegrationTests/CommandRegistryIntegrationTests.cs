using Wolfish.Maia.Commands;
using Xunit;

namespace Wolfish.IntegrationTests
{
    /// <summary>
    /// Testes de integração do CommandRegistry que executam comandos reais do CreateDefault()
    /// e que acessam o filesystem ou pedem input do console (ex: Console.ReadLine).
    /// </summary>
    public class CommandRegistryIntegrationTests
    {
        [Theory]
        [InlineData("list")]
        [InlineData("config")]
        [InlineData("home")]
        public async Task TryExecuteAsync_ComandoComSideEffect_DeveRetornarTrue(string commandName)
        {
            // Arrange
            var reader = new StringReader("Y\n");
            Console.SetIn(reader);
            var registry = CommandRegistry.CreateDefault();
            var found = await registry.TryExecuteAsync(commandName, [commandName]);

            // Assert
            Assert.True(found,
                $"Comando '{commandName}' deveria estar registrado no CommandRegistry");
        }

        [Theory]
        [InlineData("LIST")]
        [InlineData("List")]
        [InlineData("CONFIG")]
        [InlineData("Config")]
        [InlineData("HOME")]
        [InlineData("Home")]
        public async Task TryExecuteAsync_CaseInsensitive_ComandoComSideEffect_DeveEncontrar(string commandName)
        {
            var reader = new StringReader("Y\n");
            Console.SetIn(reader);
            var registry = CommandRegistry.CreateDefault();
            var found = await registry.TryExecuteAsync(commandName, [commandName]);
            Assert.True(found,
                $"Comando '{commandName}' (case insensitive) deveria ser encontrado");
        }

        [Fact]
        public async Task CreateDefault_DeveConterTodosOsComandos()
        {
            // Arrange
            var reader = new StringReader("Y\n");
            Console.SetIn(reader);
            var registry = CommandRegistry.CreateDefault();
            var allCommands = new[] { "welcome", "list", "config", "home", "help", "info", "ask" };

            // Act & Assert
            foreach (var cmdName in allCommands)
            {
                var args = cmdName == "ask" ? new[] { "ask" } : new[] { cmdName };
                var found = await registry.TryExecuteAsync(cmdName, args);

                Assert.True(found,
                    $"Comando '{cmdName}' deveria estar no CreateDefault()");
            }
        }
    }
}
