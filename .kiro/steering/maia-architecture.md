# Wolfish.Maia — Arquitetura e Convenções

## Modelo de Comandos

O Maia usa três camadas de despacho de comandos:

```
maia <verb>                  → "Quick Shot" (1 arg) — CommandRegistry
maia <verb> <alvo>           → "Clean Shot" (2 args) — WolfishCommand + TerminalCommands.json
maia ask <agente> <texto...> → "Burst" (3+ args) — AskCommand via CommandRegistry
```

### Como adicionar um novo comando Quick Shot

1. Crie uma classe em `Wolfish.Maia/Commands/QuickShotCommands/` implementando `ICliCommand`
2. Implemente `string Name { get; }` com o verbo do comando
3. Registre no `CommandRegistry.CreateDefault()` com `.Register(new SeuComando())`

### Como adicionar um novo comando Clean Shot

1. Adicione uma entrada em `Wolfish.Maia/TerminalCommands.json` (e variantes Linux/Windows)
2. O `WolfishCommand.SeekAndExecute()` resolve automaticamente

## Configuração de Agentes

### cloudagents.json
Define os agentes disponíveis. Cada agente tem:
- `Name` — identificador usado em `maia ask <name>`
- `SystemMessage` — prompt de sistema
- `ProviderName` — referência a um provider em `appsettings.json`
- `Model` — nome do modelo no provider
- `History` — modo: `"none"` | `"self"` | `"global"`

### appsettings.json
Define os provedores LLM. **Nunca commitar API keys.** O arquivo template deve usar placeholders.

### Modos de Histórico
| Modo | Comportamento |
|------|---------------|
| `none` | Sem memória entre execuções |
| `self` | Persiste histórico do agente em `history-{name}.json` |
| `global` | Carrega histórico de todos os agentes (`history-*.json`) |

## Dependências do Maia (diretas)

```
Wolfish.Maia
├── Wolfish.ChatAgent   (OpenAiAgent, AgentHistory)
├── Wolfish.Commands    (WolfishCommand, TerminalCommandDto)
└── Wolfish.Shared      (CloudAgent, LlmProvider)
```

**NÃO** adicionar dependências de LLamaSharp, EF Core ou Rita diretamente no Maia.

## Versionamento

- Versão controlada via `Version.props` na raiz (`WolfishMaiaVersion`)
- Seguir semver: MAJOR.MINOR.PATCH
- Breaking changes no CLI (renomear/remover comandos) = bump MAJOR
- Novos comandos = bump MINOR
- Bugfixes = bump PATCH

## Build e Publicação

```bash
# Build multi-target
dotnet build -c Release

# Empacotar como ferramenta NuGet
dotnet pack -c Release

# Instalar localmente para teste
dotnet tool install --global --add-source ./nupkg wolfish.maia

# Publicar no NuGet.org
dotnet nuget push ./nupkg/wolfish.maia.*.nupkg --api-key <KEY> --source https://api.nuget.org/v3/index.json
```

## Convenções de Código

- Nullable reference types habilitado — sempre checar nulos
- Async/await para operações de I/O e chamadas a LLMs
- Streaming preferido para respostas longas dos agentes (`SendMessageStreamingAndGetModelAsync`)
- Saída de respostas escrita em arquivo `.md` no diretório corrente

## Classes Relevantes em Wolfish.Commands

| Classe | Responsabilidade |
|--------|-----------------|
| `WolfishCommand` | Carrega definições de comandos do `TerminalCommands.json` e executa via `SeekAndExecute()` |
| `AgentCommand` | Helper estático para comandos relacionados a agentes |
| `IssueManagerCommand` | Utilitário estático para criar GitHub Issues (contém classe interna `Issue`) |
| `TerminalCommandDto` / `StepCommand` | DTOs que descrevem um comando de terminal (nome, descrição, argumentos, exemplos) |

### IssueManagerCommand

Permite criar issues no GitHub diretamente do terminal. Útil para reportar bugs ou tarefas sem sair do ambiente de desenvolvimento. A classe interna `Issue` representa o payload do issue (título, body, labels).

## Classes Relevantes em Wolfish.CloudAgents

| Classe | Responsabilidade |
|--------|-----------------|
| `CloudAgent` | Representa um agente LLM (nome, modelo, provider) — apenas propriedades |
| `LlmProvider` | Configuração do provider (endpoint, API key) — apenas propriedades |
| `OpenAiAgent` | Implementa `IAgent` para streaming OpenAI-compatible — método principal: `SendMessageStreamingAsync()` |
| `AgentSettings` | Configurações do agente (modo de histórico, system prompts) |

> Nota: `Wolfish.CloudAgents` não é referenciado diretamente pelo Maia — o Maia usa `Wolfish.Shared` que espelha os DTOs. O `CloudAgents` pode vir a ser a fonte única de verdade futuramente.

## Classes Relevantes em Wolfish.Shared

| Classe | Responsabilidade |
|--------|-----------------|
| `CloudAgent` | Espelho do DTO do CloudAgents, usado diretamente pelo Maia |
| `LlmProvider` | Espelho do DTO de provider |
| `LlamaSettings` | Schema de configuração para integração Llama (usado pelos projetos experimentais) |

## Comandos CLI Completos do Maia

```
maia welcome              # Exibe mensagem de boas-vindas com versão
maia list                 # Lista todos os comandos disponíveis
maia platform             # Exibe informações da plataforma/OS
maia directory            # Exibe o diretório base da instalação
maia help                 # Exibe ajuda
maia install <pacote>     # Instala um pacote via gerenciador de pacotes do SO
maia uninstall <pacote>   # Remove um pacote
maia download <alvo>      # Baixa um recurso (ex: maia download chrome, maia download qwen)
maia ask <agente> <texto> # Envia pergunta para o agente LLM configurado
maia ask all <texto>      # Envia para todos os agentes configurados
```

`platform` e `directory` são Clean Shots definidos em `TerminalCommands.json`, não Quick Shots registrados no `CommandRegistry`.
