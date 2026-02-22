# Tool-Übersicht & Setup

> **Kern-Takeaway:** Alle Tools nutzen dieselben Modelle. Der Unterschied liegt im Kontext und im Workflow. Wählt das Tool, mit dem ihr am produktivsten seid.

## Tool-Vergleich

| Tool | Typ | IDE | Kosten | Kontextdatei |
|------|-----|-----|--------|--------------|
| **GitHub Copilot** | Copilot + Agent | VS Code, JetBrains | Gratis (Basis) | `.github/copilot-instructions.md` |
| **Cursor** | Agent | Cursor (VS Code Fork) | Gratis (Basis) | `.cursorrules` |
| **Claude Code** | Agent (CLI) | Terminal / VS Code | API-Kosten | `CLAUDE.md` |
| **Junie (JetBrains)** | Agent | IntelliJ, PyCharm | JetBrains AI-Abo | `.junie/guidelines.md` |
| **Windsurf** | Agent | Windsurf (VS Code Fork) | Gratis (Basis) | `.windsurfrules` |

### Einordnung nach Stufe

| Stufe | Tools | Stärke |
|-------|-------|--------|
| **Chat** | ChatGPT, Claude.ai, Gemini | Erklärungen, Konzeptfragen, Recherche |
| **Copilot** | GitHub Copilot, Codeium, Tabnine | Inline-Completion, schnelle Snippets |
| **Agent** | Claude Code, Cursor Agent, Junie, Windsurf | Projektweite Änderungen, Workflows |

## Installation

| Tool | Link |
|------|------|
| GitHub Copilot | <https://github.com/features/copilot> |
| Cursor | <https://cursor.com> |
| Claude Code | <https://docs.anthropic.com/en/docs/claude-code> |
| Junie | <https://www.jetbrains.com/junie/> |
| Windsurf | <https://windsurf.com> |

## Entwicklungsumgebung (Kurzanleitung)

Für das Mietwagen-Projekt werden folgende Werkzeuge benötigt:

| Werkzeug | Version | Zweck |
|----------|---------|-------|
| **Python** | 3.13 | Programmiersprache |
| **uv** | aktuell | Paketmanager und Virtualenv |
| **Docker Desktop** | aktuell | Container für Datenbank und Services |
| **Git** | aktuell | Versionskontrolle |
| **VS Code / JetBrains** | aktuell | IDE nach Wahl |

### Schnellstart

```bash
# Python und uv installieren (macOS)
brew install python@3.13 uv

# Projekt klonen und Abhängigkeiten installieren
git clone <repo-url>
cd ffhs-aise
uv sync

# Docker starten (für Datenbank)
docker compose up -d
```

## Empfehlung

Wählt **ein** Tool und lernt es richtig kennen. Die Konzepte (Kontextdateien, Workflows) sind bei allen Tools gleich -- nur die Bedienung unterscheidet sich. Wer das Prinzip einmal verstanden hat, kann jederzeit wechseln.

---

*AISE Modul 1 -- PVA 1 | FFHS*
