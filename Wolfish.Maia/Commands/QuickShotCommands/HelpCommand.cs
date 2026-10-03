namespace Wolfish.Maia.Commands
{
    public class HelpCommand : ICliCommand
    {
        public string Name => "help";

        public Task ExecuteAsync(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("🐺 Wolfish.Maia — Assistente de terminal integrado com LLMs");
            Console.WriteLine();
            Console.WriteLine("Comandos disponíveis:");
            Console.WriteLine();
            Console.WriteLine("  📋 Informação");
            Console.WriteLine("    maia welcome                     Mensagem de boas-vindas");
            Console.WriteLine("    maia list                        Lista todos os comandos");
            Console.WriteLine("    maia info                        Informações do sistema");
            Console.WriteLine("    maia home                        Diretório de instalação");
            Console.WriteLine("    maia help                        Esta ajuda");
            Console.WriteLine();
            Console.WriteLine("  ⚙️  Configuração");
            Console.WriteLine("    maia config                      Guia de configuração");
            Console.WriteLine("    maia provider list               Lista providers LLM");
            Console.WriteLine("    maia provider add                Adiciona novo provider");
            Console.WriteLine("    maia provider edit               Edita provider existente");
            Console.WriteLine();
            Console.WriteLine("  🤖 Agentes LLM");
            Console.WriteLine("    maia ask <agente> <pergunta>     Pergunta para um agente");
            Console.WriteLine("    maia ask all <pergunta>          Pergunta para todos");
            Console.WriteLine();
            Console.WriteLine("  📦 Sistema");
            Console.WriteLine("    maia install <pacote>            Instala pacote");
            Console.WriteLine("    maia uninstall <pacote>          Remove pacote");
            Console.WriteLine("    maia download <alvo>             Baixa recurso");
            Console.WriteLine("    maia platform                    Info da plataforma");
            Console.WriteLine("    maia directory                   Diretório corrente");
            Console.WriteLine();
            Console.WriteLine("Para mais informações: https://github.com/wolfishstudio/tools");
            Console.WriteLine();
            return Task.CompletedTask;
        }
    }
}
