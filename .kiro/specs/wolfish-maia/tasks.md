# Implementation Plan: wolfish-maia

## Overview

Plano de implementação para consolidar, robustecer e testar o Wolfish.Maia. As tarefas cobrem refatoração interna, melhorias de robustez, suporte multi-plataforma, segurança na distribuição, testes com property-based testing, pipeline CI, melhorias de UX e documentação.

## Tasks

- [ ] 1. Consolidar Estrutura de Comandos
  - Garantir que todos os Quick Shot commands existentes estejam devidamente implementados e registrados.
  - [ ] 1.1 Verificar que `WelcomeCommand`, `HelpCommand`, `ListCommand`, `InfoCommand`, `HomeCommand`, `ConfigCommand` implementam `ICliCommand` corretamente
  - [ ] 1.2 Confirmar que todos estão registrados em `CommandRegistry.CreateDefault()`
  - [ ] 1.3 Remover o `Program3.cs` duplicado (ou documentar seu propósito)
  - [ ] 1.4 Garantir que nenhum comando lança exceção não tratada
  - Arquivos afetados: `Wolfish.Maia/Commands/QuickShotCommands/*.cs`, `Wolfish.Maia/Commands/CommandRegistry.cs`, `Wolfish.Maia/Program.cs`
  - _Requirements: 1.1, 1.2, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [ ] 2. Hardening do AskCommand
  - Tornar o `AskCommand` mais robusto e testável. Depende de T1.
  - [ ] 2.1 Extrair `GetAllAgents()` e `ConfigProvider()` para um serviço `AgentConfigService` (facilita testes)
  - [ ] 2.2 Validar que `cloudagents.json` existe antes de tentar carregar — erro informativo se ausente
  - [ ] 2.3 Validar que `appsettings.json` existe antes de tentar carregar — erro informativo se ausente
  - [ ] 2.4 Garantir que `History: "none"` não cria nem lê arquivos de histórico (corrigir se necessário)
  - [ ] 2.5 Capturar `HttpRequestException` separadamente de outras exceções para mensagem mais clara
  - Arquivos afetados: `Wolfish.Maia/Commands/BurstCommands/AskCommand.cs`, `Wolfish.Shared/CloudAgent.cs`, `LlmProvider.cs`
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.8, 3.11, 4.1, 4.2_

- [ ] 3. Suporte a TerminalCommands por Plataforma
  - Garantir que o Clean Shot carregue o arquivo JSON correto baseado na plataforma atual. Depende de T1.
  - [ ] 3.1 Verificar lógica de seleção de arquivo em `WolfishCommand` (Linux vs Windows)
  - [ ] 3.2 Confirmar que `TerminalCommands.Linux.json` e `TerminalCommands.Windows.json` estão populados com comandos relevantes
  - [ ] 3.3 Usar `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` para detecção de plataforma
  - [ ]* 3.4 Adicionar testes de integração básicos para os Clean Shots mais comuns (opcional)
  - Arquivos afetados: `Wolfish.Commands/WolfishCommand.cs`, `Wolfish.Maia/TerminalCommands.Linux.json`, `Wolfish.Maia/TerminalCommands.Windows.json`
  - _Requirements: 1.3, 5.2, 5.3, 5.4_

- [ ] 4. Templates de Configuração Seguros
  - Garantir que nenhuma informação sensível seja distribuída com o pacote NuGet. Depende de T2.
  - [ ] 4.1 Auditar `cloudagents.json` — substituir endpoints e valores reais por placeholders
  - [ ] 4.2 Auditar `appsettings.json` — garantir que API keys são placeholders
  - [ ] 4.3 Adicionar `appsettings.json` ao `.gitignore` local (já no `.gitignore` global? verificar)
  - [ ] 4.4 Criar `appsettings.template.json` documentando a estrutura esperada
  - [ ] 4.5 Verificar que o `.nupkg` gerado não contém segredos (inspecionar com `dotnet nuget` ou `zip`)
  - Arquivos afetados: `Wolfish.Maia/cloudagents.json`, `Wolfish.Maia/appsettings.json`, `.gitignore`
  - _Requirements: 4.3, 4.5, 4.6, 6.4_

- [ ] 5. Suite de Testes com Property-Based Testing
  - Criar projeto de testes cobrindo as propriedades de corretude definidas no design. Depende de T2 e T3.
  - [ ] 5.1 Criar projeto `Wolfish.Maia.Tests` (xUnit + CsCheck + NSubstitute)
  - [ ]* 5.2 Implementar Property 1 (No-Throw): fuzz de `args` arbitrários no dispatcher (opcional)
    - **Property 1: No-Throw**
    - **Validates: Requirements 1.2, 1.5**
  - [ ]* 5.3 Implementar Property 2 (Agent Resolution): gerar listas aleatórias, verificar `SearchAgentByName` (opcional)
    - **Property 2: Agent Resolution Correctness**
    - **Validates: Requirements 3.1, 3.4**
  - [ ]* 5.4 Implementar Property 3 (History Isolation): modo `none` não toca arquivos (opcional)
    - **Property 3: History Isolation**
    - **Validates: Requirements 3.8**
  - [ ]* 5.5 Implementar Property 5 (No Secret Leak): stdout/stderr não vaza ApiKey (opcional)
    - **Property 5: No Secret Leak**
    - **Validates: Requirements 4.2, 4.5**
  - [ ]* 5.6 Implementar testes unitários para `CommandRegistry` (registro, busca, TryExecute) (opcional)
  - [ ] 5.7 Adicionar `Wolfish.Maia.Tests` ao `WolfishTools.slnx`
  - Arquivos novos: `Wolfish.Maia.Tests/Wolfish.Maia.Tests.csproj`, `Wolfish.Maia.Tests/DispatcherTests.cs`, `Wolfish.Maia.Tests/AskCommandTests.cs`, `Wolfish.Maia.Tests/AgentConfigServiceTests.cs`
  - _Requirements: 1.2, 1.5, 3.1, 3.4, 3.8, 4.2_

- [ ] 6. Pipeline CI — Build, Test e Pack
  - Atualizar o workflow de GitHub Actions para incluir testes e validação do pacote. Depende de T5.
  - [ ] 6.1 Atualizar `.github/workflows/release.yml` para rodar `dotnet test` antes de `dotnet pack`
  - [ ] 6.2 Adicionar step de verificação de segredos no pacote gerado
  - [ ] 6.3 Garantir que o build falha se testes falham
  - [ ] 6.4 Adicionar badge de status do CI no `README.md`
  - Arquivos afetados: `.github/workflows/release.yml`, `.github/workflows/pages.yml` (se aplicável), `README.md`
  - _Requirements: 6.1, 6.2, 6.6_

- [ ] 7. Melhorias de UX no Output
  - Melhorar a experiência do usuário na linha de comando. Independente.
  - [ ] 7.1 Adicionar cor no output de `welcome` e `help` (usando `Console.ForegroundColor`)
  - [ ] 7.2 Mostrar spinner ou `...` enquanto aguarda resposta do LLM
  - [ ] 7.3 Mostrar nome do agente sendo consultado antes da resposta
  - [ ] 7.4 Imprimir caminho do arquivo de saída ao final do `ask`
  - Arquivos afetados: `Wolfish.Maia/Commands/QuickShotCommands/WelcomeCommand.cs`, `Wolfish.Maia/Commands/QuickShotCommands/HelpCommand.cs`, `Wolfish.Maia/Commands/BurstCommands/AskCommand.cs`
  - _Requirements: 2.1, 2.2, 3.9, 3.10_

- [ ] 8. Documentação e Release Notes
  - Manter documentação sincronizada com o código. Independente.
  - [ ] 8.1 Atualizar `README.md` raiz com todos os comandos atuais
  - [ ] 8.2 Adicionar seção de configuração de agentes no README
  - [ ] 8.3 Documentar estrutura de `cloudagents.json` e `appsettings.json` com exemplos
  - [ ] 8.4 Criar `CHANGELOG.md` com histórico de versões
  - [ ] 8.5 Atualizar `SPECIFICATION.md` para refletir a arquitetura atual (CommandRegistry, ICliCommand)
  - _Requirements: 6.3_

## Notes

- Tarefas marcadas com `*` são opcionais e podem ser puladas para um MVP mais rápido
- Cada tarefa referencia requisitos específicos para rastreabilidade
- T7 e T8 são independentes e podem ser executadas em paralelo com T1
- T3 e T4 são independentes entre si e podem ser executadas em paralelo após T2
- Property tests (T5) validam invariantes universais com entradas geradas aleatoriamente (mínimo 100 iterações por propriedade)
- Usar **CsCheck** para property-based tests e **NSubstitute** para mocks

## Task Dependency Graph

```json
{
  "waves": [
    { "wave": 1, "tasks": ["T1", "T7", "T8"] },
    { "wave": 2, "tasks": ["T2"] },
    { "wave": 3, "tasks": ["T3", "T4"] },
    { "wave": 4, "tasks": ["T5"] },
    { "wave": 5, "tasks": ["T6"] }
  ]
}
```
