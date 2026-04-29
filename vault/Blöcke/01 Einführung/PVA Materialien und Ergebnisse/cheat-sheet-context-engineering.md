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

---

*AISE Modul 1 -- PVA 1 | FFHS*
