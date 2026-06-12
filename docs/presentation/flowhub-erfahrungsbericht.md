---
marp: true
theme: flowhub
paginate: true
footer: 'FlowHub · CAS AI-Assisted Software Engineering'
math: false
---

<!--
Erfahrungsbericht-Deck: Learnings & Harness aus dem Bauen von FlowHub mit KI.
Quellen: vault/Projektarbeit/Learnings.md, docs/ai-usage.md, docs/CLAUDE-PIPELINE.md.
Sprechnotizen stehen in HTML-Kommentaren (Marp Presenter-View / PDF-Notes).
Zielzeit: ~10 Minuten.
-->

<!-- _class: title -->
<!-- _paginate: false -->
<!-- _footer: '' -->

# Erfahrungsbericht

## FlowHub mit KI bauen — Learnings & Harness

**Andreas Imboden** · CAS AI-Assisted Software Engineering · FFHS FS26

*Wie war es wirklich, ein Full-Stack-Projekt fast vollständig mit KI zu bauen?*

<!--
[~20 s] Begrüssung & Rahmen.
"Diese Präsentation handelt nicht vom Produkt FlowHub, sondern von der Erfahrung,
es mit KI zu bauen: was funktioniert hat, was nicht, und vor allem mit welchem
Werkzeug-Setup – dem Harness – ich gearbeitet habe."
-->

---

## Worum es geht

Nicht das Produkt — die **Arbeitsweise**.

- **~85–95 %** des Codes KI-generiert — aber das ist nicht der eigentliche Punkt
- **Kernthese:** Gute KI-Entwicklung ist kein „Prompt rein, Code raus", sondern ein **Harness** aus Instruktionen, Skills, Pipeline und Disziplin
- Die Rolle des Menschen verschiebt sich vom Tippen zum **Architekten & Reviewer**

<!--
[~30 s] Die These setzen.
"Ja, 85 bis 95 Prozent des Codes kamen von der KI. Aber die Zahl allein führt in die
Irre. Der Punkt ist: Diese Quote erreicht man nur mit Struktur – einem Harness. Ohne
den driftet die KI ab. Mit ihm wird der Mensch zum Architekten und Reviewer."
-->

---

## Der Harness — Überblick

Die Werkzeugkette, die die KI gesteuert hat:

| Ebene | Werkzeug |
|---|---|
| **Agent** | Claude Code (interaktiv) · Codex / Copilot ergänzend |
| **Konventionen** | `ai-instructions` (base + `dotnet-blazor`) → `CLAUDE.md` |
| **Workflows** | eigene Skills: `/ui-*`, `/flowhub-*`, `/commit`, `/push` |
| **Methode** | Superpowers: Brainstorm → Spec → Plan → Subagent → Review |
| **Automatisierung** | `claude-pipeline` (Issue→PR) · `examiner-sim` (Grading) |
| **Disziplin** | Context-Hygiene: Logs-via-File · `/clear`-Schnitte |

<!--
[~35 s] Landkarte, nicht vorlesen.
"Das ist der ganze Harness auf einen Blick – von oben nach unten: der Agent selbst,
die Konventionen, die ihn steuern, eigene Workflows als Skills, die Arbeitsmethode,
zwei Automatisierungen und ganz unten die Disziplin, die alles zusammenhält. Die
nächsten Folien gehen die wichtigsten Ebenen durch."
-->

---

## Geschichtetes Agent-Onboarding

KI-Agents brauchen ein gepflegtes Onboarding-Dokument — wie ein neuer Teamkollege.

- `CLAUDE.md` — harte Projektregeln
- `.ai/base-instructions.md` — kanonische Konventionen (stack-agnostisch)
- `.ai/cas-instructions.md` — CAS-Kontext (Blöcke, Bewertungskriterien)
- **+ Stack-Overlay `dotnet-blazor`** — z. B. **SemVer** · **Conventional Commits** · **12-Factor** · **TDD**

**Eigene Skills > Prompt-Templates:** der Agent erkennt selbst, *wann* anzuwenden, und folgt einer geprüften Checkliste statt zu improvisieren — befolgt von Claude Code, Codex, Copilot **und** Gemini gleichermassen.

<!--
[~45 s] Der strukturelle Kern-Learning.
"Die wichtigste Erkenntnis: Ein KI-Agent braucht dasselbe wie ein neuer Kollege – ein
gepflegtes Onboarding. Bei mir in Schichten: harte Regeln im CLAUDE.md, stack-agnostische
Konventionen in den base-instructions, der CAS-Kontext separat, plus ein .NET-Blazor-
Overlay mit konkreten Regeln wie SemVer, Conventional Commits, TDD. Und: eigene Skills
schlagen Prompt-Templates, weil der Agent selbst erkennt, wann er sie anwenden muss."
-->

---

## Nicht „Prompt rein, Code raus" — eine Pipeline

Jeder grössere Baustein lief durch denselben strukturierten Ablauf:

**Brainstorm → Spec → Plan → Subagent-Implementierung → Review ×2**

1. **Brainstorming** — Design als A/B/C-Entscheidungen, jede mit Begründung festgehalten
2. **Spec + Plan** — schriftliches Design, dann TDD-geordneter Aufgabenplan (**28 Specs/Plans** in `docs/superpowers/`)
3. **Subagenten** — pro Task ein frischer Implementierer (Test-First)
4. **Review ×2** — Spec-Konformität, dann Code-Qualität — *bevor* etwas in `main` geht

<!--
[~50 s] Der wichtigste konzeptionelle Punkt.
"Gute KI-Entwicklung ist eine Pipeline. Erst zwingt mich die KI, das Design als
A/B/C-Entscheidungen explizit zu treffen. Dann ein schriftliches Spec, dann ein Plan in
test-first-Reihenfolge – im Repo liegen 28 solcher Spec- und Plan-Dateien. Erst dann
implementiert ein Subagent mit frischem Kontext. Und nichts geht in main ohne zwei
Reviews. Diese Struktur hält die KI davon ab, in die falsche Richtung zu laufen."
-->

---

## Context-Hygiene — das unterschätzte Thema

Der Kontext ist das knappste Gut. Zwei Disziplinen brachten am meisten:

**1 · Logs via File** — grösster Token-Fresser waren Console-, Test- und Build-Streams.
Statt alles ins Conversation-Window: in eine **Datei** schreiben, gezielt mit
`Read offset/limit` oder `grep` holen. → **~5–10× weniger Tokens** pro Debug-Session.

**2 · `/clear`-Schnitte** — Spec → `/clear` → Plan → `/clear` → Implement.
Jede Phase hinterlässt ein **Artefakt auf Disk**; der Dialog-Ballast wird verworfen.

> Was zwischen Phasen weiterleben muss, gehört in eine Datei — nicht in den Chat.

<!--
[~50 s] Das praktischste Learning – ruhig betonen.
"Das am meisten unterschätzte Thema: Context-Management. Der grösste Token-Fresser war
anfangs Log-Output – komplette Console-Streams im Chat. Die Lösung: erst in eine Datei,
dann gezielt nur die relevanten Zeilen lesen. Das hat den Verbrauch pro Debug-Session um
das Fünf- bis Zehnfache gesenkt. Zweitens: zwischen Spec, Plan und Implementierung ein
hartes /clear. Jede Phase hinterlässt ein Artefakt auf der Disk – der Gesprächsballast
darf weg. Faustregel: Was weiterleben muss, gehört in eine Datei, nicht in den Chat."
-->

---

## Werkzeug-Disziplin

**Skills kosten System-Prompt-Tokens** — *bevor* die erste Nachricht gelesen wird.
Zu viele geladene Skills → unschärfere Trigger, knapperes Budget.
→ in **thematische Plugins** splitten, nur das Relevante aktivieren
(`flowhub`, `cas-aise-*`, schmales `superpowers`-Set).

**Code-Exploration — das richtige Werkzeug je Frage:**

`rg` (lexikalisch, schnell) → **LSP** (Symbole, Renames, Inheritance) → **grepai** (semantisch)

Heuristik: erst `rg`; liefern **3 Anläufe** nichts, ist die Frage nicht lexikalisch, sondern semantisch.

<!--
[~45 s] Zwei feinere, aber wertvolle Learnings.
"Zwei Detail-Erkenntnisse. Erstens: Jeder Skill kostet Tokens im System-Prompt, noch
bevor ich etwas tippe – und zu viele machen das Triggern unschärfer. Also nach Themen in
Plugins aufteilen und nur das Relevante aktivieren. Zweitens die Code-Suche: nicht
reflexartig das mächtigste Tool nehmen. Erst ripgrep, bei Struktur-Fragen LSP, bei
unscharfen Fragen semantische Suche. Wenn drei ripgrep-Anläufe scheitern, ist die Frage
semantisch."
-->

---

## Automatisierung — KI prüft KI

**`claude-pipeline`** (GitHub Actions) — autonome Issue-Implementierung:
Issue mit `ai-implement` labeln → Branch + Draft-PR, mit Retry-Policy (Rate-Limit / Transient / `max-turns`).

**`examiner-sim`** (Multi-Agent-Workflow) — baut die Abgabe-PDFs, benotet sie
gegen die **Moodle-Rubrik** mit einem Agenten-Panel und übt die Live-Demo.

> Der Mensch schreibt nicht mehr jede Zeile — er definiert die Leitplanken
> und lässt **KI-gestützte Prüfungen** finden, was die KI übersieht.

<!--
[~40 s] Die Meta-Ebene: KI prüft KI.
"Zwei Dinge habe ich automatisiert. Erstens eine Pipeline, die ein gelabeltes Issue
autonom implementiert und einen Draft-PR öffnet. Zweitens einen examiner-sim: ein
Multi-Agenten-Workflow, der die Abgabe-PDFs baut, gegen die Moodle-Rubrik benotet und die
Live-Demo durchspielt. Der Mensch definiert die Leitplanken; KI-gestützte Prüfungen
finden, was die KI selbst übersieht."
-->

---

## KI-Anteil in Zahlen (Block 4: Persistenz)

| Artefakt | Zeilen | KI-generiert | Mensch | KI % |
|---|--:|--:|--:|--:|
| Entity-Klassen + Configs (14) | ~250 | ~225 | ~25 | 90 % |
| Repository-Implementierungen (6) | ~350 | ~315 | ~35 | 90 % |
| Integrationstests (16) | ~230 | ~210 | ~20 | 91 % |
| Docker Compose | ~50 | ~40 | ~10 | 80 % |
| … Services · Filter · Refactor | ~130 | ~112 | ~18 | 86 % |
| **Gesamt** | **~1010** | **~902** | **~108** | **~89 %** |

Über alle Blöcke: **~85–95 % des Codes KI-generiert** — der Mensch-Anteil ist klein, aber **hochwertig**.

<!--
[~40 s] Zahl wirken lassen, dann relativieren.
"Konkret für die Persistenz: rund 89 Prozent KI-Anteil, über das ganze Projekt 85 bis 95.
Aber – und das zeigen die nächsten Folien – die verbleibenden 10 bis 15 Prozent Mensch
waren genau die, die über Erfolg oder Desaster entschieden haben."
-->

---

## Wo die KI glänzt — und wo nicht

### Glänzt — repetitiv & gut spezifiziert

7 strukturgleiche `IEntityTypeConfiguration<T>`, EF-Migrations, Refit-Interfaces, CI-YAML;
**16 Integrationstests** gegen echtes PostgreSQL (Testcontainers) — **alle grün beim ersten Lauf.**

### Scheitert — wo Domäne & Performance zählen

- **N+1-Blindheit** — `ListAsync` ohne `.Include(c => c.Tags)`
- **CASCADE überall** — *owned* vs. *referenced* ist eine menschliche Domänen-Entscheidung
- **Veraltete Versionen** — Trainingsdaten hinken neuen Releases hinterher
- **Feature-Drift** — Scope-Disziplin muss vom Menschen kommen

<!--
[~50 s] Ehrlich und konkret – überzeugt die Dozenten.
"Wo die KI brilliert: alles Repetitive und gut Spezifizierte – Konfigurationsklassen,
Migrations, typsichere Clients, und 16 Integrationstests gegen eine echte Datenbank, alle
grün beim ersten Lauf. Wo sie scheitert: überall, wo Domänenwissen oder Performance-Gespür
zählt. N+1-Abfragen, blind gesetzte CASCADE-Löschungen, veraltete Paketversionen, und der
ständige Drang nach mehr Features. Diese Lücke füllt der Mensch."
-->

---

## Der Smoke-Test-Moment

Die KI schrieb den ganzen Deployment-Stack. Dann lief **ein** Befehl:
`make smoke-prod` — End-to-End-Probe des laufenden Stacks.

**An einem Nachmittag fand er 5 reale, latente Bugs:**

- `.editorconfig` fehlt im Build-Image → Build bricht mit `TreatWarningsAsErrors` ab
- Compose-Env-Casing `${EMBEDDINGS__APIKEY}` ≠ `Embeddings__ApiKey` → Embeddings still no-op
- Leerstring-Modellname → `AssertNotNullOrEmpty`-Crash beim Start
- Mistral lehnt das `dimensions`-Feld ab → 422
- Passbolt-Refs vom Makefile überschattet → KI-Call erreichte nie den Provider

> KI schrieb den Code. Eine **KI-gestützte Prüfung** fand, was die KI übersah.

<!--
[~50 s] Die beste Geschichte – mit etwas Spannung.
"Mein liebster Moment: Die KI hatte den kompletten Deployment-Stack geschrieben, sah gut
aus. Dann ein KI-geschriebener Smoke-Test, der den echten Stack hochfährt. Erster Lauf:
fünf latente Bugs – ein fehlendes File, ein Casing-Fehler, ein Crash, ein abgelehntes
API-Feld, eine verschattete Secret-Referenz. Alle hätten die Abgabe blockiert. Lektion:
KI schreibt den Code, aber eine – idealerweise KI-gestützte – Prüfung muss finden, was
die KI übersieht."
-->

---

<!-- _class: lead -->

## Fazit — was bleibt

Nicht einzelne Tools, sondern **Disziplinen**:

- gepflegte, **geschichtete Agent-Instructions** (wiederverwendbar über Projekte)
- **eigene Skills** für wiederkehrende Workflows
- **Context-Hygiene** über Datei-Artefakte + `/clear`-Schnitte
- der **Spec → Plan → Implement**-Rhythmus mit Review-Gates

### Der Mensch bleibt Architekt und Reviewer — die KI wird zum Verstärker.

<span class="small">Details: docs/ai-usage.md · vault/Projektarbeit/Learnings.md · Danke — Fragen?</span>

<!--
[~35 s] Klar landen, Q&A öffnen.
"Was bleibt, sind nicht einzelne Tools, sondern Disziplinen: gepflegte, geschichtete
Instructions, eigene Skills, Context-Hygiene über Datei-Artefakte und der Spec-Plan-
Implement-Rhythmus mit Review-Gates. Genau das nehme ich über FlowHub hinaus mit. Der
Mensch bleibt Architekt und Reviewer – die KI wird zum Verstärker. Danke, Fragen?"
-->
