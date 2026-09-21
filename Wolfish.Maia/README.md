# 🐺 Wolfish.Maia

[![NuGet](https://img.shields.io/nuget/v/wolfish.maia.svg)](https://www.nuget.org/packages/wolfish.maia)
[![Downloads](https://img.shields.io/nuget/dt/wolfish.maia.svg)](https://www.nuget.org/packages/wolfish.maia)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](../LICENSE.txt)

**MAIA** — *MAIA Automated Integrated Assistant*

Na tradição dos acrônimos recursivos da computação (GNU, PHP, WINE), MAIA se define a si mesma: um assistente de terminal leve, extensível e integrado a LLMs, construído em .NET.

---

## Instalação

Requer .NET 8, 10 ou 11 SDK.

```bash
dotnet tool install --global wolfish.maia
```

Para atualizar:

```bash
dotnet tool update --global wolfish.maia
```

---

## Comandos

### Informação e navegação

```bash
maia help          # lista todos os comandos disponíveis
maia welcome       # exibe mensagem de boas-vindas com a versão
maia list          # lista comandos e descrições
maia info          # OS, runtime .NET, diretório base
maia home          # diretório de instalação da ferramenta
maia config        # exibe/guia configuração de agentes e providers
```

### Automação de sistema

```bash
maia install <pacote>       # instala um pacote via gerenciador do SO
maia uninstall <pacote>     # remove um pacote
maia download <alvo>        # baixa um recurso (browser, modelo LLM, etc.)
maia platform               # informações da plataforma/OS
maia directory              # diretório corrente
```

Exemplos:

```bash
maia download chrome
maia download qwen
maia download gemma
maia install git
```

### Conversa com agentes LLM

```bash
maia ask <agente> <pergunta...>    # envia pergunta para um agente específico
maia ask all <pergunta...>         # envia para todos os agentes configurados
```

Exemplos:

```bash
maia ask principal como faço um loop em bash
maia ask fulano explica ponteiros em C
maia ask all quais são os atalhos do vim mais usados
```

A resposta é salva em `ask-{agente}-{timestamp}.md` no diretório corrente.

---

## Configuração de Agentes

### 1. Configurar providers em `appsettings.json`

Crie ou edite `appsettings.json` no diretório de instalação:

```json
{
  "LLMProviders": [
    {
      "Name": "OpenRouter",
      "Endpoint": "https://openrouter.ai/api/v1/chat/completions",
      "ApiKey": "sua-chave-aqui"
    },
    {
      "Name": "LMStudio",
      "Endpoint": "http://localhost:1234/v1/chat/completions",
      "ApiKey": ""
    },
    {
      "Name": "Gemini",
      "Endpoint": "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
      "ApiKey": "sua-chave-aqui"
    }
  ]
}
```

> **Importante:** nunca commite API keys. O `appsettings.json` com valores reais é local.

### 2. Configurar agentes em `cloudagents.json`

```json
{
  "CloudAgents": [
    {
      "Name": "principal",
      "SystemMessage": "Você é um assistente direto. Responda de forma concisa.",
      "ProviderName": "OpenRouter",
      "Model": "openrouter/auto",
      "History": "none"
    },
    {
      "Name": "local",
      "SystemMessage": "You are a helpful assistant.",
      "ProviderName": "LMStudio",
      "Model": "local-model",
      "History": "self"
    }
  ]
}
```

### Modos de histórico

| Modo | Comportamento |
|------|---------------|
| `none` | Sem memória entre execuções |
| `self` | Persiste histórico em `history-{agente}.json` no diretório corrente |
| `global` | Carrega histórico de todos os agentes (`history-*.json`) antes de responder |

---

## Providers suportados

Qualquer endpoint compatível com a API OpenAI (`/chat/completions`) funciona:

- [OpenRouter](https://openrouter.ai) — acesso a centenas de modelos
- [LM Studio](https://lmstudio.ai) — modelos locais via interface gráfica
- [GitHub Models](https://github.com/marketplace/models) — modelos via GitHub Copilot
- [Google Gemini](https://ai.google.dev) — via endpoint OpenAI-compatible
- [Ollama](https://ollama.com) — modelos locais via API

---

## Modelos locais (download)

```bash
maia download qwen     # Qwen2.5 1.5B — rápido, leve
maia download gemma    # Gemma — Google, eficiente
```

---

## Extensão

Para adicionar novos comandos ao Maia, basta:

**Quick Shot** (1 argumento):
1. Crie uma classe implementando `ICliCommand` em `Commands/QuickShotCommands/`
2. Registre em `CommandRegistry.CreateDefault()`

**Clean Shot** (2 argumentos):
1. Adicione uma entrada em `TerminalCommands.json` (e variante Linux/Windows)

---

## Site e links

- [Site oficial](https://wolfishstudio.github.io/tools/pages/home.html)
- [NuGet](https://www.nuget.org/packages/Wolfish.Maia)
- [Repositório](https://github.com/wolfishstudio/tools)

---

## Licença

MIT — veja [LICENSE.txt](../LICENSE.txt).
