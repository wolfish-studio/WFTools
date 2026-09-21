# Design — Wolfish.Maia

## Overview

O Maia é uma ferramenta CLI .NET multi-target empacotada como `dotnet tool`. Sua arquitetura é intencionalmente simples: um despachador por camadas que resolve comandos do mais específico para o mais genérico, sem frameworks de DI ou IoC.

## Architecture

### Architecture Diagram

```mermaid
flowchart TD
    CLI[maia args] --> Dispatcher[Program.cs — Layer Dispatcher]

    Dispatcher -->|1 arg| QS[Quick Shot\nCommandRegistry]
    Dispatcher -->|2 args| CS[Clean Shot\nWolfishCommand + JSON]
    Dispatcher -->|3+ args\nask| Burst[Burst\nAskCommand]
    Dispatcher -->|nenhum match| Unknown[Unknown command!]

    QS --> WelcomeCmd[WelcomeCommand]
    QS --> HelpCmd[HelpCommand]
    QS --> ListCmd[ListCommand]
    QS --> InfoCmd[InfoCommand]
    QS --> HomeCmd[HomeCommand]
    QS --> ConfigCmd[ConfigCommand]

    CS --> TermJson[TerminalCommands.json\n+ Linux/Windows variants]

    Burst --> AskCmd[AskCommand]
    AskCmd --> AgentLoader[GetAllAgents\ncloudagents.json]
    AskCmd --> ProviderLoader[ConfigProvider\nappsettings.json]
    AskCmd --> HistoryMgr[AgentHistory\nhistory-*.json]
    AskCmd --> LLMClient[OpenAiAgent\nstreaming]
    AskCmd --> OutputFile[ask-{name}-{ts}.md]
```

### Project Dependencies

```
Wolfish.Maia
├── Wolfish.ChatAgent
│   ├── OpenAiAgent          ← implementa envio streaming OpenAI-compatible
│   └── AgentHistory         ← carrega/salva histórico JSON
├── Wolfish.Commands
│   └── WolfishCommand       ← resolução de Clean Shots via JSON
└── Wolfish.Shared
    ├── CloudAgent            ← DTO do agente
    └── LlmProvider           ← DTO do provider
```

## Components and Interfaces

### Program.cs — Layer Dispatcher

Ponto de entrada. Despacha por `args.Length` sem framework:

```csharp
if (args.Length == 0)               → CommandRegistry.TryExecuteAsync("help")
if (args.Length == 1)               → CommandRegistry.TryExecuteAsync(args[0])
if (!found && args.Length == 2)     → WolfishCommand.SeekAndExecute(args[0], args[1])
if (!found && args[0] == "ask")     → CommandRegistry.TryExecuteAsync("ask", args)
if (!found)                         → "Unknown command!"
```

### CommandRegistry

Dicionário `Dictionary<string, ICliCommand>` (case-insensitive). Registro explícito em `CreateDefault()`. Extensível sem modificar o dispatcher.

### ICliCommand

```csharp
public interface ICliCommand
{
    string Name { get; }
    Task ExecuteAsync(string[] args);
}
```

### AskCommand — Fluxo Detalhado

```
1. Validar args (>= 3)
2. Resolver agente(s) via GetAllAgents() + SearchAgentByName()
3. Resolver provider via ConfigProvider(agent.ProviderName)
4. Instanciar AgentHistory com arquivo history-{name}.json
5. Carregar histórico conforme History mode
6. AddSystem(SystemMessage) + AddUser(question)
7. Criar OpenAiAgent(model, endpoint, apiKey, history)
8. Streaming via SendMessageStreamingAndGetModelAsync()
9. Salvar histórico se mode = self
10. Escrever resposta em ask-{name}-{timestamp}.md (UTF-8)
```

### AgentHistory (Wolfish.ChatAgent)

- Persistência em JSON local
- Métodos: `Load()`, `Save()`, `AddSystem()`, `AddUser()`, `AddAssistant()`
- `LoadGlobalHistories(directory, pattern)` — carrega múltiplos arquivos

### OpenAiAgent (Wolfish.ChatAgent)

- Implementa chamada à API OpenAI-compatible via `HttpClient`
- Retorna `IAsyncEnumerable<(string Text, string ModelId)>` para streaming
- Funciona com qualquer endpoint compatível (OpenRouter, LMStudio, GitHub Copilot, Gemini via proxy)

### File Layout

```
Wolfish.Maia/
├── Program.cs
├── Commands/
│   ├── ICliCommand.cs
│   ├── CommandRegistry.cs
│   ├── QuickShotCommands/
│   │   ├── WelcomeCommand.cs
│   │   ├── HelpCommand.cs
│   │   ├── ListCommand.cs
│   │   ├── InfoCommand.cs
│   │   ├── HomeCommand.cs
│   │   └── ConfigCommand.cs
│   └── BurstCommands/
│       └── AskCommand.cs
└── Examples/
    ├── TerminalCommands.Linux.json
    ├── TerminalCommands.Windows.json
    ├── AgentCommands.json
    └── AutomatedScripts.json
```

## Data Models

### cloudagents.json
```json
{
  "CloudAgents": [
    {
      "Name": "string",
      "SystemMessage": "string",
      "ProviderName": "string",
      "Model": "string",
      "History": "none|self|global"
    }
  ]
}
```

### appsettings.json
```json
{
  "LLMProviders": [
    {
      "Name": "string",
      "Endpoint": "https://...",
      "ApiKey": "your-api-key-here"
    }
  ]
}
```

### CloudAgent DTO

Mapeado diretamente de `cloudagents.json`. Campos:

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Name` | `string` | Identificador do agente, usado em `maia ask <name>` |
| `SystemMessage` | `string` | Prompt de sistema enviado antes da pergunta do usuário |
| `ProviderName` | `string` | Referência ao provider em `appsettings.json` |
| `Model` | `string` | Nome do modelo no provider (ex: `gpt-4o`, `gemini-pro`) |
| `History` | `string` | Modo de memória: `none`, `self`, ou `global` |

### LlmProvider DTO

Mapeado de `appsettings.json`. Campos:

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Name` | `string` | Identificador do provider, referenciado por `CloudAgent.ProviderName` |
| `Endpoint` | `string` | URL base da API OpenAI-compatible |
| `ApiKey` | `string` | Chave de autenticação (nunca commitada em valores reais) |

### AgentHistory (modelo de persistência)

Arquivo `history-{agentName}.json` no diretório corrente. Estrutura:

```json
[
  { "role": "system",    "content": "string" },
  { "role": "user",      "content": "string" },
  { "role": "assistant", "content": "string" }
]
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

| Propriedade | Abordagem de Teste |
|-------------|-------------------|
| P1.2 (No-Throw) | Fuzz `args` com arrays arbitrários — nunca deve lançar exceção não tratada |
| P3.1 (Agent Resolution) | Gerar listas de agentes aleatórias, verificar que `SearchAgentByName` é equivalente a LINQ `FirstOrDefault` case-insensitive |
| P3.2 (History Isolation) | Com modo `none`, verificar que nenhum arquivo `history-*.json` é criado/modificado |
| P3.3 (Output Persistence) | Mockar `OpenAiAgent`, verificar que cada agente processado gera exatamente 1 arquivo `.md` |
| P4.2 (No Secret Leak) | Capturar stdout/stderr e verificar que nenhuma string de `ApiKey` aparece na saída |
| P5.1 (Cross-platform paths) | Verificar que todos os caminhos construídos passam por `Path.Combine` |

### Property 1: No-Throw

*For any* array of command-line arguments (including empty, null-like, or malformed inputs), the Maia dispatcher SHALL complete without throwing an unhandled exception.

**Validates: Requirements 1.2, 1.5**

### Property 2: Agent Resolution Correctness

*For any* list of agents and any agent name string, `SearchAgentByName(name, agents)` returns `null` if and only if no agent in the list has a `Name` equal to `name` under case-insensitive comparison.

**Validates: Requirements 3.1, 3.4**

### Property 3: History Isolation

*For any* agent configured with `History: "none"`, executing `maia ask` SHALL not create or modify any `history-*.json` file in the current directory.

**Validates: Requirements 3.8**

### Property 4: Output Persistence

*For any* successful LLM response (mocked), the `AskCommand` SHALL produce exactly one `.md` output file per agent invoked.

**Validates: Requirements 3.9**

### Property 5: No Secret Leak

*For any* `appsettings.json` containing an `ApiKey` value, that value SHALL never appear in stdout or stderr during any command execution.

**Validates: Requirements 4.2, 4.5**

### Property 6: Cross-platform Paths

*For any* file path constructed during execution, no path separator is hardcoded as `\` or `/` — all paths are built via `Path.Combine` or equivalent cross-platform API.

**Validates: Requirements 5.4**

## Error Handling

### Layer Dispatcher (Program.cs)

O dispatcher não lança exceções ao usuário final. Qualquer verbo não reconhecido resulta em saída amigável:

```
Unknown command! Try Again!
```

O tratamento é por omissão: se nenhuma camada resolve, a mensagem padrão é exibida e o processo encerra com código `0`.

### AskCommand

| Situação de Erro | Comportamento |
|-----------------|---------------|
| Agente não encontrado em `cloudagents.json` | Exibe `"Agent '<name>' not found."` e retorna |
| Provider não encontrado em `appsettings.json` | Exibe mensagem indicando o provider ausente e retorna |
| `cloudagents.json` ausente | Lança erro informativo com o caminho esperado |
| `appsettings.json` ausente | Lança erro informativo com o caminho esperado |
| `HttpRequestException` na chamada ao LLM | Exibe `"Error: {mensagem}"` sem encerrar o processo com código diferente de zero |
| Streaming interrompido | Escreve o conteúdo parcial recebido no arquivo `.md` e exibe o erro |

### Quick Shot Commands

Comandos Quick Shot não realizam I/O externo (sem HTTP, sem escrita em disco além do stdout). O escopo de erro é mínimo — falhas são praticamente impossíveis em condições normais. Se um arquivo de lista JSON (ex: `TerminalCommands.json`) estiver ausente, o comando afetado exibe uma mensagem informativa sem lançar exceção.

### Princípios Gerais

- O processo **nunca** encerra com código de saída não-zero em resposta a erros de usuário ou de configuração.
- Erros de programação (bugs reais) podem ainda lançar exceções — estes são tratados no nível do runtime .NET.
- Nenhuma API key ou dado sensível é incluído em mensagens de erro.

## Testing Strategy

### Abordagem Dual

A estratégia combina testes de exemplo com testes baseados em propriedades (PBT):

- **Testes de exemplo (xUnit):** cobrem cenários concretos, casos de borda e fluxos de erro específicos.
- **Testes de propriedade (CsCheck ou FsCheck):** verificam invariantes universais com entradas geradas aleatoriamente (mínimo 100 iterações por propriedade).

### Biblioteca PBT

Usar **CsCheck** (C#-native, integra com xUnit) ou **FsCheck** com bindings para C#. Cada propriedade é anotada com um comentário referenciando a propriedade do design:

```
// Feature: wolfish-maia, Property 1: No-Throw
```

### Propriedades a Implementar

| Propriedade do Design | Teste PBT | Estratégia de Mock |
|-----------------------|-----------|-------------------|
| P1: No-Throw | Gerar `string[]` arbitrários, invocar dispatcher | Nenhum (execução real sem I/O externo) |
| P2: Agent Resolution | Gerar `List<CloudAgent>` e `string` aleatórios | Nenhum (lógica pura) |
| P3: History Isolation | Gerar agentes com `History: "none"`, executar `AskCommand` | Mockar `OpenAiAgent` e filesystem |
| P4: Output Persistence | Gerar perguntas e agentes aleatórios, executar `AskCommand` | Mockar `OpenAiAgent` |
| P5: No Secret Leak | Gerar `appsettings.json` com ApiKeys aleatórias, capturar output | Mockar HTTP |
| P6: Cross-platform Paths | Inspecionar todos os paths construídos em runtime | Nenhum |

### Testes de Exemplo (xUnit)

Cobrir especificamente:

- `maia` sem argumentos → exibe ajuda
- `maia help` → lista comandos
- `maia ask unknownAgent question` → mensagem de erro correta
- `maia ask <agent> <question>` com provider ausente → mensagem de erro correta
- `cloudagents.json` ausente → erro informativo
- Agente com `History: "self"` → arquivo `history-{name}.json` criado/atualizado
- Agente com `History: "global"` → todos os `history-*.json` são carregados
- Saída `.md` contém nome do modelo no formato correto

### Configuração

```xml
<!-- Wolfish.Maia.Tests.csproj -->
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="CsCheck" Version="3.*" />
<PackageReference Include="NSubstitute" Version="5.*" />
```

## Evolution Path

| Fase | Evolução planejada |
|------|--------------------|
| v2 | Suporte a `maia ask <agent> --file <path>` para enviar arquivos como contexto |
| v3 | Plugin system — comandos externos via assemblies dinâmicos |
| v4 | Integração opcional com Wolfish.Rita para contexto RAG no `ask` |
