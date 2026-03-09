# AI Classification PoC Design

**Date:** 2026-03-09
**Status:** Approved
**Goal:** Validate LLM-based message classification quality using Microsoft.Extensions.AI and OpenRouter with multiple models.

## Overview

Standalone console app in `poc/FlowHub.AI.Classification/` that classifies user messages into one of 8 FlowHub skills using different LLMs via OpenRouter. Parallel execution with comparison table output.

## Project Structure

```
poc/
└── FlowHub.AI.Classification/
    ├── FlowHub.AI.Classification.csproj
    ├── Program.cs                     # Entry point, CLI handling
    ├── Models/
    │   ├── SkillDefinition.cs         # Skill name, description, example triggers
    │   └── ClassificationResult.cs    # Skill, confidence, reasoning, model, latency
    ├── Services/
    │   ├── ClassificationService.cs   # Builds prompt, calls IChatClient, parses response
    │   └── OpenRouterClientFactory.cs # Creates M.E.AI IChatClient per model
    ├── Config/
    │   ├── SkillCatalog.cs            # Static list of 8 skill definitions
    │   └── ModelCatalog.cs            # Model IDs + display names (read from config)
    ├── Demo/
    │   └── DemoMessages.cs            # ~16 predefined test messages with expected skills
    └── appsettings.json               # OpenRouter API key, base URL, model list
```

Not part of the main solution file. Run standalone via `dotnet run`.

## Core Flow

```
User input (or demo message)
        │
        ▼
ClassificationService.ClassifyAsync(message)
        │
        ├──► Task 1: IChatClient (gemma-3-4b) ──► ClassificationResult
        ├──► Task 2: IChatClient (llama-4-scout) ──► ClassificationResult
        └──► Task 3: IChatClient (qwen3-4b) ──► ClassificationResult
        │
        ▼
   Task.WhenAll (timeout per model, individual error handling)
        │
        ▼
   Comparison table output to console
```

## Skill Definitions

| # | Skill | Description |
|---|-------|-------------|
| 1 | ArticleSkill | News articles, blog posts, tech articles, read-later content |
| 2 | HomelabSkill | Self-hosted software, homelab services, server tools, Docker images |
| 3 | BookSkill | Books, e-books, audiobooks, reading recommendations |
| 4 | MovieSkill | Movies, TV series, streaming content, watchlists |
| 5 | DocumentSkill | PDFs, scanned documents, receipts, photos of paperwork |
| 6 | KnowledgeSkill | Facts, explanations, how-things-work, learning topics |
| 7 | QuoteSkill | Quotes, sayings, citations, memorable phrases |
| 8 | GenericSkill | Anything that doesn't fit the above categories |

## System Prompt

```
You are a message classifier for FlowHub, a personal inbox system.
Classify the user's message into exactly one of these skills:

1. ArticleSkill — News articles, blog posts, tech articles, read-later content
2. HomelabSkill — Self-hosted software, homelab services, server tools, Docker images
3. BookSkill — Books, e-books, audiobooks, reading recommendations
4. MovieSkill — Movies, TV series, streaming content, watchlists
5. DocumentSkill — PDFs, scanned documents, receipts, photos of paperwork
6. KnowledgeSkill — Facts, explanations, how-things-work, learning topics
7. QuoteSkill — Quotes, sayings, citations, memorable phrases
8. GenericSkill — Anything that doesn't fit the above categories

Respond ONLY with valid JSON:
{
  "skill": "<skill name>",
  "confidence": <0.0-1.0>,
  "reasoning": "<1-2 sentences why>"
}
```

## LLM Response Format

```json
{
  "skill": "MovieSkill",
  "confidence": 0.95,
  "reasoning": "IMDB is a movie and TV series database, this URL points to a title page."
}
```

## CLI Modes

```bash
# Interactive mode
dotnet run --project poc/FlowHub.AI.Classification

# Demo mode — runs all predefined messages
dotnet run --project poc/FlowHub.AI.Classification -- --demo
```

## Console Output

```
Message: https://www.imdb.com/de/title/tt2543312/

┌──────────────────┬───────────┬────────────┬──────────────────────────────────────┬─────────┐
│ Model            │ Skill     │ Confidence │ Reasoning                            │ Latency │
├──────────────────┼───────────┼────────────┼──────────────────────────────────────┼─────────┤
│ Gemma 3 4B       │ MovieSkill│ 0.95       │ IMDB is a movie/TV database          │ 1.2s    │
│ Llama 4 Scout    │ MovieSkill│ 0.90       │ URL points to IMDB title page        │ 0.8s    │
│ Qwen3 4B         │ MovieSkill│ 0.92       │ IMDB link indicates movie/TV content │ 1.5s    │
└──────────────────┴───────────┴────────────┴──────────────────────────────────────┴─────────┘
```

Demo mode additionally shows expected skill and pass/fail per model.

## Demo Messages

| # | Message | Expected Skill |
|---|---------|---------------|
| 1 | `https://www.imdb.com/de/title/tt2543312/` | MovieSkill |
| 2 | `Muss ich unbedingt schauen!` | MovieSkill |
| 3 | `https://heise.de/news/some-article-12345` | ArticleSkill |
| 4 | `Interesting blog post about Rust memory safety` | ArticleSkill |
| 5 | `https://www.thalia.ch/shop/some-book` | BookSkill |
| 6 | `Dieses Buch muss ich unbedingt lesen` | BookSkill |
| 7 | `Check out Portainer for managing Docker containers` | HomelabSkill |
| 8 | `https://github.com/louislam/uptime-kuma` | HomelabSkill |
| 9 | `Photo of a receipt from the dentist` | DocumentSkill |
| 10 | `PDF: Steuererklaerung_2025.pdf` | DocumentSkill |
| 11 | `Wie funktioniert eigentlich DNS?` | KnowledgeSkill |
| 12 | `I want to understand how transformers work in ML` | KnowledgeSkill |
| 13 | `"Be the change you wish to see in the world"` | QuoteSkill |
| 14 | `Zitat: "Der Weg ist das Ziel"` | QuoteSkill |
| 15 | `Remind me to buy milk tomorrow` | GenericSkill |
| 16 | `Call mom on Sunday` | GenericSkill |

## Models

### Phase 1 — Free tier
| Model ID | Display Name |
|----------|-------------|
| `google/gemma-3-4b-it:free` | Gemma 3 4B |
| `meta-llama/llama-4-scout:free` | Llama 4 Scout |
| `qwen/qwen3-4b:free` | Qwen3 4B |

### Phase 2 — Low cost (if free tier quality is insufficient)
| Model ID | Display Name |
|----------|-------------|
| `moonshotai/kimi-k2.5` | Kimi K2.5 |
| `google/gemini-2.0-flash-lite-001` | Gemini 2.0 Flash Lite |

### Phase 3 — Reference quality
| Model ID | Display Name |
|----------|-------------|
| Anthropic Claude models via OpenRouter | Claude |

## Configuration

`appsettings.json`:
```json
{
  "OpenRouter": {
    "ApiKey": "YOUR_KEY_HERE",
    "BaseUrl": "https://openrouter.ai/api/v1"
  },
  "Models": [
    { "Id": "google/gemma-3-4b-it:free", "DisplayName": "Gemma 3 4B" },
    { "Id": "meta-llama/llama-4-scout:free", "DisplayName": "Llama 4 Scout" },
    { "Id": "qwen/qwen3-4b:free", "DisplayName": "Qwen3 4B" }
  ]
}
```

- `OPENROUTER_API_KEY` env variable takes precedence over appsettings
- `appsettings.Development.json` in `.gitignore` for local secrets

## Error Handling

- Each model call wrapped individually — one failure doesn't block others
- Failed models show `ERROR` in the table with the error message
- JSON parse failures: retry once, then mark as `PARSE_ERROR`
- Global timeout: 60s for `Task.WhenAll`
- Per-model timeout: 30s

## NuGet Packages

- `Microsoft.Extensions.AI.OpenAI` — M.E.AI with OpenAI-compatible provider
- `Microsoft.Extensions.Configuration.Json` — appsettings loading
- `Microsoft.Extensions.Configuration.EnvironmentVariables` — env var override
- `System.Text.Json` — LLM response parsing

## Future: Phase b) Test Suite

The demo messages with expected skills prepare the ground for a parameterized test suite that asserts classification correctness across LLMs. This will be a separate project in `poc/` or `tests/`.

## Technology Decisions

- **OpenRouter over direct provider APIs**: Single API key, single integration, access to dozens of models. OpenAI-compatible = works with M.E.AI out of the box.
- **Standalone PoC over integration into FlowHub.AI**: Faster iteration, no coupling to unfinished domain code, easy to throw away or graduate into production.
- **Parallel calls**: Real-world comparison needs identical inputs at roughly the same time. `Task.WhenAll` with per-model error isolation.
