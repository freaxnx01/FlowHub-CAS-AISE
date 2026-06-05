---
tags:
  - claude-generated
  - claude-updated
updated: 2026-06-05
---

# Learnings CAS AISE

Persönliche Lessons Learned aus der Arbeit am FlowHub-Projekt mit Claude Code, Codex und weiteren AI-Coding-Agents. Fokus liegt auf den praktischen Erkenntnissen rund um Agent-Tooling, die sich über alle fünf Blöcke hinweg als wirklich tragfähig erwiesen haben — nicht auf einer vollständigen Tool-Übersicht.

---

## AI-Instructions & Write Skills

Die wichtigste strukturelle Erkenntnis war: AI-Agents brauchen genauso ein gepflegtes "Onboarding-Dokument" wie ein neuer Teamkollege. In FlowHub wurde das in mehrere Ebenen aufgeteilt — `CLAUDE.md` für die unmittelbar harten Regeln, `.ai/base-instructions.md` als kanonische Konventionsreferenz und `.ai/cas-instructions.md` für den CAS-spezifischen Kontext (Block-Schedule, Bewertungskriterien, Implementierungs-Rhythmus). Ohne diese Schichten driftet jeder Agent in seine eigenen Defaults ab; mit ihnen werden Instruktionen einmal geschrieben und von Claude Code, Codex, Copilot und Gemini gleichermassen befolgt. Das `update-ai-instructions` Skill hält diese Files synchron mit dem Upstream-Template, damit Verbesserungen aus anderen Projekten zurückfliessen.

Parallel dazu hat sich das Schreiben **eigener** Skills als stärkster Hebel herausgestellt, um wiederkehrende Workflows verlässlich zu machen. Skills sind kleine, scharf umrissene Markdown-Definitionen mit einer Trigger-Description und einem expliziten Vorgehen — und genau das macht den Unterschied zu blossen Prompt-Templates: der Agent erkennt selbst, *wann* ein Skill anzuwenden ist, und folgt dann einer geprüften Checkliste statt zu improvisieren. Das `superpowers:writing-skills` Skill diente als Meta-Werkzeug für genau diesen Prozess (Verifikation vor Deployment, klare Triggerbedingungen). Konkret entstanden so für FlowHub u.a. die `/ui-brainstorm → /ui-flow → /ui-build → /ui-review` Pipeline, der `/commit` und `/push` Flow sowie das `cas-aise-grade-self-check` Skill, das jede Block-Nachbereitung gegen die Moodle-Bewertungskriterien prüft.

---

## Skills in Plugins nach Thema aufteilen

Skills landen — zumindest mit ihrer Description — im System Prompt des Agents. Das wird unterschätzt: jeder zusätzliche Skill kostet Tokens *bevor* die erste User-Nachricht überhaupt gelesen wird, und je mehr Skills geladen sind, desto schwerer fällt es dem Modell, den jeweils richtigen zu triggern. Im FlowHub-Setup zeigte sich das, sobald die persönliche Skill-Sammlung über ein gutes Dutzend hinauswuchs: Trigger wurden unschärfer, Kontext-Budget wurde knapper, und Skills für komplett fremde Domänen (Homelab-Routing, Movie-Sync, …) belasteten die CAS-Sessions ohne jeden Mehrwert.

Die Lösung war, Skills konsequent in **thematische Plugins** zu zerlegen und jeweils nur das Plugin zu aktivieren, das zur aktuellen Arbeit passt. Für FlowHub-CAS-Sessions sind das z.B. `cas-aise-grade-self-check`, `cas-aise-todo-list`, `flowhub` (mit den UI-/Commit-Sub-Skills) sowie das schmale `superpowers`-Set; alles andere bleibt ausserhalb der Session unsichtbar. Das hält den System-Prompt-Footprint klein, schärft die Triggerwahrscheinlichkeit der wirklich relevanten Skills und macht es zudem möglich, Plugins einzeln zu versionieren und über Marketplace-Repos mit anderen zu teilen, ohne ständig die persönliche Default-Konfiguration mitzuschleppen.

---

## Context Hygiene: Logs via File

Der grösste single Token-Fresser in interaktiven Sessions war anfangs der Browser-Console-Output. Wenn der Agent einen Bug im Blazor-UI nachverfolgt und dafür komplette Console-Streams ins Conversation-Window kippt — Fetch-Logs, MudBlazor-Render-Warnings, Hot-Reload-Meldungen, Stack-Traces — sind nach wenigen Iterationen mehrere zehntausend Tokens verbraucht, die anschliessend bis zur Compaction im Kontext kleben bleiben. Das verlangsamt die Antworten, treibt die Kosten hoch und drückt ältere, oft noch relevante Information aus dem Fenster.

Die deutlich tragfähigere Variante: den Console-Output (oder `dotnet watch`-Logs, Test-Output, OTel-Traces) in eine **Datei** schreiben und den Agent diese Datei gezielt mit `Read` plus `offset`/`limit` lesen lassen — oder per `grep`/`rg` nur die wirklich relevanten Zeilen extrahieren. Damit landet im Kontext exakt der Ausschnitt, der für die nächste Entscheidung gebraucht wird, statt des gesamten Streams. Dieselbe Logik gilt für Build-Output, Migration-SQL oder Telemetrie-Dumps: alles, was potenziell gross werden kann, gehört zuerst auf die Disk und wird dann selektiv in den Kontext geholt. Diese eine Gewohnheit hat den Token-Verbrauch pro Debug-Session subjektiv um den Faktor 5–10 reduziert.

---

## Code-Exploration: LSP / ripgrep / grepai

Es gibt nicht *das eine* richtige Werkzeug, um sich im Code zu orientieren — die Wahl hängt von Projektgrösse und Frage-Typ ab, und das macht der Agent erst gut, wenn man ihm das explizit beibringt. Bei kleinen Repos (FlowHub ist mit wenigen tausend LOC noch handlich) reicht oft ein direktes `Read` oder ein gezielter `rg`-Aufruf; das ist schnell, deterministisch und kostet praktisch keinen Overhead. Sobald aber strukturelle Fragen ins Spiel kommen ("wo wird `IClassificationPort` implementiert?", "welche Aufrufer hat diese Methode?"), wird ein **LSP**-basierter Zugriff überlegen, weil er Symbole statt Strings auflöst und Renames, Inheritance oder Partial Classes korrekt versteht — etwas, das `grep` prinzipiell nicht leisten kann.

Bei grösseren Codebases oder unscharfen Suchen lohnt sich dann der nächste Sprung: **grepai** (oder vergleichbare semantische Such-Tools), die nicht nach exakten Tokens, sondern nach Bedeutung suchen ("wo behandeln wir Validierungsfehler an der API-Boundary?"). Die praktische Heuristik, die sich bewährt hat: erst `rg` versuchen — wenn drei Suchanläufe nichts Brauchbares liefern, ist die Frage vermutlich nicht lexikalisch, sondern semantisch, und es ist Zeit für LSP oder grepai. Diese Hierarchie bewusst zu wählen — statt reflexartig das mächtigste Tool zu nehmen — spart Tokens, hält Antworten schnell und vermeidet das typische "Agent versinkt in Suchergebnissen"-Pattern.

---

## Context Hygiene: Superpowers-Workflow mit `/clear`-Schnitten

Eng verwandt mit der Logs-via-File-Disziplin ist die Frage, wie man **mehrphasige Aufgaben** sauber durch Spec → Plan → Implementierung führt, ohne dass sich der Kontext bis zur Unkenntlichkeit aufbläht. Das `superpowers`-Plugin hat dafür einen Drei-Phasen-Workflow etabliert, der zwischen den Phasen jeweils ein hartes `/clear` setzt — also den Conversation-Kontext bewusst auf null zurücksetzt:

1. **Specs / Design** — Anforderungen und Entwurf erarbeiten (z. B. via `superpowers:brainstorming`), Ergebnis als `docs/superpowers/specs/xyz.md` ablegen.
   `/clear`
2. **Plan** — neue Session: `> Read docs/superpowers/specs/xyz.md and run superpowers:writing-plans to produce the implementation plan.`
   `/clear`
3. **Implement** — wieder neue Session: `> Execute the xyz plan via subagent-driven-development`

Der Trick liegt im `/clear`: jede Phase produziert ein **Artefakt auf Disk** (Spec-MD, Plan-MD, Code-Diff), das in der nächsten Phase als reiner Input zurückgelesen wird. Den vorherigen Hin-und-her-Dialog mitzuschleppen liefert keinen Mehrwert — die Entscheidungen sind im Artefakt festgehalten — kostet aber konstant Tokens und lenkt das Modell mit veralteten Zwischenstands-Diskussionen ab. Diese Disziplin ist die strukturelle Variante des Logs-via-File-Patterns: was zwischen Phasen weiterleben muss, gehört in eine Datei; was nur Gesprächs-Begleitmaterial war, darf gelöscht werden. Praktisch macht das bei nicht-trivialen Features den Unterschied zwischen einer fokussierten Implementierung und einem zähen, immer langsamer werdenden Mega-Thread.

---

## Vom CAS in die Projektarbeit — was konkret eingeflossen ist

Rückblickend lässt sich für jeden der fünf CAS-Blöcke ein konkreter Niederschlag in FlowHub benennen — die Projektarbeit war nicht parallele Spielwiese, sondern der Ort, an dem die jeweiligen Block-Inhalte unmittelbar zur Entscheidung wurden. Der rote Faden ist dabei bewusst, dass fast jede dieser Entscheidungen in einem **ADR** festgehalten ist: Das im CAS gelehrte Abwägen von Optionen wurde so vom Vorlesungsthema zur dokumentierten Projektpraxis.

| CAS-Block | Mitgenommener Inhalt | Konkrete FlowHub-Entscheidung |
|---|---|---|
| **1 — Einführung** | Architekturoptionen (Monolith / Modular Monolith / Microservices) begründet gegeneinander abwägen; KI-Tooling für ein neues Repo aufsetzen | Entscheidung für **Modular Monolith mit hexagonaler Schichtung** statt verteilter Microservices (ADR 0001) — und die ADR-Praxis selbst als Format. Das Tooling-Fundament (`CLAUDE.md`, `.ai/`-Instructions, eigene Skills) entstand direkt aus diesem Block. |
| **2 — Frontend** | Frontend-Architektur und Render-Modelle | **Blazor Interactive Server** statt WASM-SPA (ADR 0001), MudBlazor als einzige Komponenten-Bibliothek, und die vierstufige UI-Pipeline `/ui-brainstorm → /ui-flow → /ui-build → /ui-review`, die Entwurf vor Code erzwingt. |
| **3 — Service** | Service-/Microservice-Architekturen, Protokolle (REST/gRPC), KI-gestützte Services | Bewusste Wahl einer **asynchronen In-Process-Pipeline** (RabbitMQ + MassTransit, ADR 0002/0003) statt eines synchronen Microservice-Geflechts — konsequent zur Modular-Monolith-Entscheidung. REST-Minimal-API für die Aussengrenze; die KI-Klassifikation als erster „intelligenter Service" (ADR 0004). |
| **4 — Persistence** | Geeignete Persistenzform wählen, DB-Zugriff über ORM abstrahieren, dynamische Abfragen | **EF Core + PostgreSQL** mit Port-/Repository-Abstraktion (ADR 0005). Das im Block geschärfte „passende Persistenzform wählen" führte direkt zum Entscheid, die KI-Suche über **pgvector auf derselben Postgres** zu lösen statt eine separate Vector-DB einzuführen (ADR 0006). |
| **5 — Deployment** | Containerisierung, CI/CD-Automatisierung, Monitoring/Observability | Multi-Stage-Dockerfile + Compose-Stack aus **unabhängig deploybaren Containern**, drei GitHub-Actions-Workflows (CI, Release → GHCR, Migrations) und **OpenTelemetry + Prometheus + Grafana** als Observability-Schicht. Die geschärfte Betriebssicht schlug sich zusätzlich in den Policy-ADRs 0007–0009 (LLM-Hosting, Logging-/Telemetry-PII) nieder. |

Über die einzelnen Blöcke hinaus war das eigentliche Querschnittsthema des CAS — **KI-assistierte Software-Entwicklung** — zugleich die Arbeitsweise der gesamten Projektarbeit. Mitgenommen habe ich daraus weniger einzelne Tools als die in diesem Dokument beschriebenen Disziplinen: gepflegte Agent-Instructions, eigene Skills für wiederkehrende Workflows, Context-Hygiene über Datei-Artefakte und den Spec→Plan→Implement-Rhythmus mit harten `/clear`-Schnitten. Diese Praktiken sind der direkteste Transfer aus dem CAS in die Projektarbeit — und zugleich das, was ich über FlowHub hinaus in zukünftige Projekte mitnehme. Die vollständige, blockweise Aufschlüsselung der konkreten KI-Nutzung liegt in [`docs/ai-usage.md`](../../docs/ai-usage.md).
