# FlowHub – Reflexion: KI-gestützte Entwicklung

**CAS AI-Assisted Software Engineering (AISE)** · W4B-C-AS001 · ZH-Sa-1 · FS26
**Student:** Andreas Imboden · **Stand:** Juni 2026

> Dieses Dokument reflektiert den **KI-Einsatz** über alle fünf CAS-Blöcke: welche
> Werkzeuge wie eingesetzt wurden, wie sich der Workflow entwickelt hat, was
> verlässlich funktioniert hat und was nicht — und welche Disziplinen als
> tragfähig über das Projekt hinaus erscheinen. Die detaillierte, blockweise
> Aufschlüsselung (generated-vs-handwritten-Anteile, Korrektur-Geschichten) liegt
> in `docs/ai-usage.md`.

---

## 1. KI-Werkzeuge im Entwicklungsprozess

| Werkzeug | Einsatzbereich |
|---|---|
| Claude Code (Opus, 1M-Kontext) | Brainstorming, Spec-/Plan-Erstellung, ADR-Drafts, Controller für Subagent-Dispatches |
| Claude Sonnet (Subagents) | Implementer + Spec-Reviewer + Code-Quality-Reviewer unter dem `subagent-driven-development`-Workflow |
| Claude Haiku (Subagents) | Mechanische Tasks (csproj/Markdown/YAML), ca. 60 % Token-Ersparnis gegenüber Sonnet |
| `/ultrareview` (Multi-Agent-Branch-Review) | Architektonisches Review über einen ganzen Feature-Branch — fängt System-Invarianten ab, die Per-Task-Review nicht sieht |
| Microsoft.Extensions.AI + Anthropic/OpenRouter | KI **im Produkt** (Classifier, Enrichment, Embeddings) |
| Eigene CAS-Skills | `cas-aise-todo-list`, `cas-aise-grade-self-check`, `sync-ai-instructions` — Rubrik-Verankerung, Block-/Phasen-Steuerung, Cross-Project-Reuse |

---

## 2. Workflow-Entwicklung (Block 1 → Block 5)

Mit jedem Block wurde ad-hoc-Chat gegen strukturierteren, isolierteren
Subagent-Dispatch getauscht. Der Mehraufwand für Struktur hat sich durch weniger
Mid-Task-Eskalationen und seltenere „Agent-ist-abgedriftet"-Momente bezahlt
gemacht.

| Block | Workflow-Verschiebung |
|---|---|
| 1 — Einführung | Ad-hoc-Chat für Architektur; Copilot inline für Code. |
| 2 — Frontend | Erster sustained CLI-Einsatz mit phasenbasierter Disziplin (`/ui-brainstorm` → `/ui-flow` → `/ui-build` → `/ui-review`). |
| 3 — Service | Erster vollständiger `brainstorming` → `writing-plans` → `subagent-driven-development`-Slice; zweistufiges Review (Spec + Quality) pro Task. |
| 4 — Persistence | Sonnet als Default, Haiku für Mechanik; Per-Task-Quality-Review zugunsten einer Branch-Review am Slice-Ende fallen gelassen — ca. 60 % Token-Burn reduziert ohne Qualitätsverlust. |
| 5 — Deployment | `/ultrareview` und rubrik-gegroundetes `cas-aise-grade-self-check` ergänzt; jede Block-Abschluss-Prüfung läuft durch die Skill. |

Bis Block 5 verantworten Implementer-Subagents ganze Slices end-to-end. Die
menschliche Arbeit konzentriert sich an zwei Stellen: **Spec** (wo Verträge
festgenagelt werden) und **Review** (wo System-Invarianten geprüft werden). Der
Engpass wandert vom Tippen zu diesen beiden Punkten.

---

## 3. Was funktioniert hat

1. **Der Plan ist der Vertrag.** Enthält der Plan exakte Pfade, exakten Code,
   exakte Commit-Messages und exakte Verifikationskommandos, operiert der
   Implementer mit engem Spielraum und meldet DONE statt NEEDS_CONTEXT. Die
   95 %+ „AI-drafted"-Anteile im Produktionscode sind nur deshalb erreichbar.
2. **Review-Kadenz muss zur Fehlerklasse passen.** Per-Task-Spec + Per-Task-
   Quality war für den ersten SDD-Lauf (Block 3) richtig. Ab Block 4 fing
   Per-Task-Quality nichts Neues mehr, während ein branchweites `/ultrareview` am
   Slice-Ende genau die Architektur-Fehler fand, die Per-Task-Review nicht sehen
   konnte.
3. **Eigene Skills als Memory-Layer des Projekts.** `cas-aise-grade-self-check`,
   `cas-aise-todo-list`, `sync-ai-instructions` decken Concerns ab, die das
   Upstream-Plugin nicht behandelt: Rubrik-Verankerung, kalendergetriebene
   Priorisierung, Cross-Project-Reuse. Ohne `cas-aise-grade-self-check` driftete
   die Rubrik-Abdeckung in langen Sessions.
4. **KI für rigide Struktur-Artefakte, Mensch für architektonisches Urteil.**
   GitHub-Actions-YAML, ADR-Scaffolds, ProblemDetails, EF-Migrationen — rigide
   Strukturen, in denen der 90 %-korrekte AI-Erstwurf reale Zeit spart.
   Architektur-Entscheidungen (hexagonaler Split, In-Process vs. RabbitMQ)
   bleiben beim Menschen; die KI listet Alternativen, sie wählt nicht.
5. **Korrektur-Geschichten sind die Rubrik-Evidenz, die zählt.** „KI produzierte
   X, Smoke fing Y, Fix landete in Commit Z" schliesst einen nachvollziehbaren
   Loop — pauschale „KI hat geholfen"-Aussagen nicht.

---

## 4. Wiederkehrende Fehlerquellen

1. **Single-Pass-AI-Review übersieht System-Invarianten.** Block-5-Beispiel:
   AI-generierter Code, der Embedding-Generation in `EfCaptureService.SubmitAsync`
   integrierte, war lokal korrekt (eine Transaktion), global aber falsch — ein
   langsamer Provider sprengt das Submit-Latenz-Budget. Per-Task-Quality-Review
   fand das nicht; `/ultrareview` fand es beim ersten Lauf.
2. **Deployment-Shape-Failures sind systematisch unter-getestet.** Compose-
   `${X:-}`-Interpolation substituiert leere Strings (nicht null), wodurch
   `??`-Defaults still no-op'en; `.editorconfig` fehlte im Docker-Build-Kontext;
   ein Casing-Mismatch (`EMBEDDINGS__APIKEY` vs. `Embeddings__ApiKey`) schaltete
   ein ganzes Feature aus. Fünf solcher Defekte fing `just smoke-prod` an einem
   Nachmittag.
3. **Training-Data-Lag bei aktuellen Libraries.** `Pgvector.EntityFrameworkCore`
   wurde als in-the-box-Feature angenommen (ist ein separates Paket);
   `MassTransit.Testing` umgekehrt als separates Paket (ist Teil der Haupt-
   Assembly). API-Behauptungen der KI müssen gegen das installierte Paket
   verifiziert werden.
4. **Offene Prompts inflationieren den Scope.** „Füge X hinzu" ohne Plan
   produziert eine Feature-Suite. Gegenmittel: kleinteilige, TDD-geordnete Tasks
   mit Inline-Code, exakten Pfaden und Commit-Messages.
5. **Pläne aus unvollständigem Repo-Wissen.** Ein Slice-Plan übersah eine
   bestehende Test-Datei und vorhandene Enum-Werte; seitdem liest Planning jede
   referenzierte Datei, *bevor* der Plan eingefroren wird.

---

## 5. Tragfähige Disziplinen (über das Projekt hinaus)

Die wertvollsten Erkenntnisse sind weniger einzelne Tools als wiederholbare
Arbeitsweisen:

- **AI-Instructions wie ein Onboarding-Dokument pflegen.** `CLAUDE.md` für harte
  Regeln, `.ai/`-Instructions als kanonische Konventionsreferenz. Ohne diese
  Schichten driftet jeder Agent in seine Defaults; mit ihnen werden Instruktionen
  einmal geschrieben und von Claude Code, Codex, Copilot gleichermassen befolgt.
- **Eigene Skills schreiben.** Kleine, scharf umrissene Markdown-Definitionen mit
  Trigger-Description und geprüftem Vorgehen — der Agent erkennt selbst, *wann*
  ein Skill anzuwenden ist, und folgt einer Checkliste statt zu improvisieren.
- **Skills thematisch in Plugins aufteilen.** Jede Skill-Description kostet
  System-Prompt-Tokens und verwässert die Trigger-Schärfe. Nur das Plugin
  aktivieren, das zur aktuellen Arbeit passt.
- **Context-Hygiene: Logs via Datei.** Console-/Build-/Test-Output in eine Datei
  schreiben und gezielt mit `Read`/`grep` lesen, statt ganze Streams ins
  Kontextfenster zu kippen — subjektiv Faktor 5–10 weniger Token pro
  Debug-Session.
- **Code-Exploration bewusst eskalieren.** Erst `rg`; wenn drei Suchanläufe
  nichts liefern, ist die Frage semantisch (LSP / semantische Suche), nicht
  lexikalisch.
- **Spec → Plan → Implement mit harten `/clear`-Schnitten.** Jede Phase
  produziert ein Artefakt auf Disk, das die nächste Phase als reinen Input
  zurückliest; der vorherige Dialog wird verworfen. Das ist die strukturelle
  Variante des Logs-via-Datei-Patterns.

---

## 6. Was ich anders machen würde

- **Rubrik-Verankerung ab Block 1, nicht Block 3.** `cas-aise-grade-self-check`
  entstand mittendrin; Block 1–2 hatten keine formalen `use-cases.md` /
  `nfa.md` / `acceptance-criteria.md`. Retroaktive Doku-Arbeit kostete Zeit.
- **`/ultrareview` ab Block 3, nicht Block 5.** Branchweites Multi-Agent-Review
  fängt die Fehlerklasse, die Per-Task-Review nicht sieht — günstiger nach Slice B
  als nach einem 21-Task-MVP.
- **Ein `just smoke`-Rezept pro Block, nicht nur Block 5.** Ein einfaches Smoke
  (App booten, Feature-Pfad curlen) hätte mindestens drei der fünf späten Bugs
  früher gefangen.
- **Mechanische Patterns ins Plan-Template kodifizieren** (`InternalsVisibleTo`,
  EF-Core-Test-Host) — einmal ausgeschrieben verschwindet die Friktion.

---

## 7. Hat die Ausgangshypothese gestimmt?

Die Block-1-Annahme war: Claude ist „ein schneller Tipper mit guter
Library-Kenntnis" — nützlich für Boilerplate, suspekt bei Architektur, im Kern
ein Produktivitäts-Multiplikator und keine Workflow-Änderung. Bis Block 5 ist das
in **beide** Richtungen weit daneben.

**Boilerplate ist schneller als erwartet.** Der 95 %+ AI-drafted-Anteil im
Produktionscode heisst nicht „KI hat 95 % der Arbeit gemacht" — die 5 %
Mensch-Input konzentrieren sich auf High-Judgment-Punkte (Scope, Verträge,
Trade-offs). Das eigentliche Tippen ist ein kleiner Bruchteil der Zeit.

**Architektur ist gefährlicher als erwartet.** Der Embedding-on-Submit-Fall ist
ein Ein-Absatz-Rewrite, der im Nachhinein offensichtlich ist: lokal korrekter
Code, der eine System-Invariante brach, den ein Single-Pass-Review ausgeliefert
hätte. Die menschliche Rolle an der Architektur-Grenze ist nicht geschrumpft, sie
ist **gewachsen** — weil der Agent genug Code schnell genug produziert, dass nur
noch der Filter an Design und Review zählt.

---

## 8. Fazit

KI hat keine Rolle im Workflow ersetzt — sie hat **verschoben, welche Rolle der
Engpass ist**. Implementierungs-Durchsatz ist nicht mehr die Beschränkung;
Design- und Review-Durchsatz sind es. Für das nächste Projekt: zuerst die
Spec-Dokumente, `/ultrareview` ab Tag eins, ein Smoke-Target vor dem ersten
Feature.

### Transfer CAS → Projektarbeit

| CAS-Block | Mitgenommener Inhalt | Konkrete FlowHub-Entscheidung |
|---|---|---|
| 1 — Einführung | Architekturoptionen begründet abwägen; KI-Tooling aufsetzen | **Modular Monolith mit hexagonaler Schichtung** (ADR 0001) + die ADR-Praxis selbst; Tooling-Fundament (`CLAUDE.md`, `.ai/`, eigene Skills) |
| 2 — Frontend | Render-Modelle, Frontend-Architektur | **Blazor Interactive Server** statt WASM-SPA (ADR 0001); vierstufige UI-Pipeline, die Entwurf vor Code erzwingt |
| 3 — Service | Service-Architekturen, Protokolle, KI-Services | **Asynchrone In-Process-Pipeline** (RabbitMQ + MassTransit, ADR 0002/0003) statt synchronem Microservice-Geflecht; KI-Klassifikation als erster „intelligenter Service" (ADR 0004) |
| 4 — Persistence | Persistenzform wählen, ORM-Abstraktion | **EF Core + PostgreSQL** mit Repository-Abstraktion (ADR 0005); KI-Suche über **pgvector auf derselben Postgres** (ADR 0006) |
| 5 — Deployment | Containerisierung, CI/CD, Observability | Compose-Stack, drei GitHub-Actions-Workflows, **OpenTelemetry + Prometheus + Grafana**; Policy-ADRs 0007–0009 |

Das eigentliche Querschnittsthema des CAS — **KI-assistierte
Software-Entwicklung** — war zugleich die Arbeitsweise der gesamten
Projektarbeit. Der direkteste Transfer sind die oben beschriebenen Disziplinen:
gepflegte Agent-Instructions, eigene Skills, Context-Hygiene über Datei-Artefakte
und der Spec→Plan→Implement-Rhythmus.

---

*Reflexion, Juni 2026. Erstellt mit Unterstützung von Claude (Anthropic) gemäss
den FFHS-Richtlinien für KI-Einsatz in Projektarbeiten. Volltext der blockweisen
KI-Nutzung: `docs/ai-usage.md`.*
