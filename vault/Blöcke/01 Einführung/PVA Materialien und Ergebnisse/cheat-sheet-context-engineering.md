---
tags:
  - claude-updated
updated: 2026-05-03
---

# Cheat Sheet: Context Engineering

> **Kern-Takeaway:** Kontext ist wichtiger als Prompts.

## Drei Stufen der KI-gestützten Entwicklung

| Stufe | Interaktion | Kontext | Beispiel |
|-------|-------------|---------|----------|
| **Chat** | Frage → Antwort | Einzelne Konversation | ChatGPT, Gemini, Claude.ai |
| **Copilot** | Code-Completion in IDE | Aktuelle Datei | GitHub Copilot, Codeium |
| **Agent** | Autonome Multi-Step-Aktionen | Gesamtes Projekt | Claude Code, Cursor Agent |

Die Modelle sind nicht der Unterschied -- entscheidend ist, **wie viel Kontext** das Tool dem Modell zur Verfügung stellt.

## Was gehört in eine Kontextdatei?

Eine Kontextdatei ist das *Onboarding-Dokument* für den KI-Agenten:

| Bereich | Inhalt | Beispiel |
|---------|--------|----------|
| **Projektarchitektur** | Schichten, Services, Abhängigkeiten | Service-based, 3-tier |
| **Technologie-Stack** | Sprache, Frameworks, Versionen | Python 3.13, FastAPI, SQLAlchemy |
| **Coding-Konventionen** | Linting, Formatierung, Namensgebung | ruff, Pydantic v2 |
| **Testing-Ansatz** | Framework, Abdeckungsziele, Muster | pytest, TDD |
| **Build & Run** | Wie das Projekt gebaut und gestartet wird | uv, Docker Compose |

## Kontextdatei-Formate pro Tool

| Tool | Dateiname | Ort |
|------|-----------|-----|
| **Claude Code** | `CLAUDE.md` | Projektwurzel |
| **Cursor** | `.cursorrules` | Projektwurzel |
| **GitHub Copilot** | `.github/copilot-instructions.md` | `.github/`-Verzeichnis |
| **Windsurf** | `.windsurfrules` | Projektwurzel |

Das Konzept ist bei allen Tools identisch: Eine Datei im Projekt beschreibt den Kontext. Die Dateinamen unterscheiden sich, der Inhalt und Zweck sind gleich.

## Vorher/Nachher: Gleicher Prompt, anderer Kontext

**Prompt:** *"Erstelle einen FastAPI-Endpunkt für die Fahrzeugliste."*

### Ohne Kontextdatei

```python
from fastapi import FastAPI

app = FastAPI()
vehicles = []

@app.get("/vehicles")
def get_vehicles():
    return vehicles
```

Generisch, kein ORM, keine Schemas, keine Projektstruktur.

### Mit Kontextdatei (CLAUDE.md vorhanden)

```python
from fastapi import APIRouter, Depends
from sqlalchemy.ext.asyncio import AsyncSession

from src.core.database import get_session
from src.models.vehicle import Vehicle
from src.schemas.vehicle import VehicleResponse
from src.services.vehicle_service import VehicleService

router = APIRouter(prefix="/vehicles", tags=["vehicles"])

@router.get("/", response_model=list[VehicleResponse])
async def list_vehicles(
    session: AsyncSession = Depends(get_session),
) -> list[VehicleResponse]:
    service = VehicleService(session)
    return await service.get_all()
```

Async, Service-Layer, Pydantic-Schemas, korrekte Imports, Router statt App-Instanz -- der Agent kennt die Architektur.

## Kontexthygiene mit Superpowers

Drei-Phasen-Workflow, der den Kontext zwischen den Phasen bewusst leert (`/clear`), damit jede Phase mit frischem, fokussiertem Kontext startet:

### 1. Specs / Design

Anforderungen und Design erarbeiten (z. B. via `superpowers:brainstorming`).

```text
/clear
```

### 2. Plan

```text
> Read docs/superpowers/specs/xyz.md and run superpowers:writing-plans to produce the implementation plan.
```

```text
/clear
```

### 3. Implement

```text
> Execute the xyz plan via subagent-driven-development
```

**Warum `/clear` zwischen den Phasen?** Jede Phase produziert ein Artefakt (Spec → Plan → Code), das in der nächsten Phase als reiner Input dient. Den vorherigen Hin-und-her-Dialog im Kontext zu behalten verwässert den Fokus und kostet Tokens, ohne Mehrwert zu liefern.

---

*AISE Modul 1 -- PVA 1 | FFHS*
