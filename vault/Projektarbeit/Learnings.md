---
tags:
  - claude-generated
  - claude-updated
updated: 2026-05-15
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

## UI bauen in vier Phasen: Brainstorm → Flow → Build → Review

Die wohl wichtigste prozessuale Erkenntnis bei UI-Arbeit mit Coding-Agents war, dass der Agent **nicht** sofort Komponenten-Code generieren darf. Modelle wie Claude oder Codex spüren bei einer UI-Aufgabe einen sofortigen "Schreib-Reflex" — die schnellste sichtbare Antwort ist eine `.razor`-Datei mit MudBlazor-Markup, also tippen sie diese auch. Das Ergebnis sind funktionierende, aber konzeptlose Oberflächen: Layout-Entscheidungen ohne Begründung, Inkonsistenzen quer durch verschiedene Pages, kein nachvollziehbarer Übergang vom Use-Case zur konkreten Komponente. Genau dieses Problem adressieren die vier UI-Skills, die in FlowHub als feste Pipeline etabliert sind.

**Phase 1 — `/ui-brainstorm`** erzeugt eine **ASCII-Wireframe** der geplanten Page. Kein PNG, kein Figma, sondern bewusst eine Textdarstellung in `docs/design/<feature>/wireframe.md` mit Boxen aus `+`, `-` und `|`. Der Aufwand ist niedrig, die Iterationskosten sind nahe null (eine Zeile umsortieren, eine Spalte löschen), und das Wireframe ist sofort Diff-bar in Git, kommentierbar im PR und vom Agent in einer späteren Phase wieder lesbar. Wichtiger noch: ASCII zwingt zur Konzentration auf **Struktur und Hierarchie** statt auf Farben, Typographie oder Animationen — genau das, was zu diesem Zeitpunkt entschieden werden muss.

**Phase 2 — `/ui-flow`** ergänzt das Wireframe um einen **Mermaid-User-Flow** (`docs/design/<feature>/flow.md`). Welche Aktion führt von welchem Zustand wohin, welche Fehlerpfade existieren, was passiert beim Klick auf "Retry"? Der Flow ist die zweite Gate-Bedingung: erst wenn Wireframe und Flow vom Menschen freigegeben sind, beginnt überhaupt Code-Generierung. Auch hier ist Mermaid bewusst gewählt — Text-as-source-of-truth, ein PR-Review entscheidet über Akzeptanz, keine externen Tools im Loop. Die Disziplin, beide Phasen *separat* zu durchlaufen (nicht parallel), erzwingt zweimaliges Nachdenken: einmal über Struktur, einmal über Verhalten.

**Phase 3 — `/ui-build`** ist die einzige Phase, in der Code entsteht — und sie wird intern noch einmal in vier Mikro-Schritte zerlegt: erst die *Shell* (statisches Layout ohne Logik), dann *Logic* (DI, Services, Bindings), dann *Interactions* (Event-Handler, Validierung, Loading-States), zuletzt *Polish* (Empty-States, Skeleton-Loaders, A11y). Diese Reihenfolge verhindert, dass der Agent Polish und Logic vermischt — ein häufiger Fail-Modus, der dazu führt, dass Edge-Cases halb in der Komponente und halb im Service landen.

**Phase 4 — `/ui-review`** schliesslich ist eine deterministische Checkliste: Rendert die Komponente leer, mit Daten, mit Fehler? Sind `EventCallback`s sauber? Existiert eine bUnit-Testabdeckung? Sind MudBlazor-Konventionen eingehalten (`MudDataGrid` statt `MudTable`, `MudDialog` statt Custom-Overlay)? Erst wenn Review grün ist, gilt das Feature als fertig — die Reihenfolge ist nicht verhandelbar, weder für den Menschen noch für den Agent.

Die Investition in dieses 4-Phasen-Pattern hat sich messbar in Block 2 amortisiert (vier Pages, alle Tests grün beim ersten Commit) und blieb in Block 4/5 die Default-Pipeline für jede neue UI-Arbeit. Die generalisierbare Lektion ist breiter als UI-Design selbst: **wenn Code-Generierung der schnellste Default für einen Agent ist, dann muss der Prozess explizite Gates *vor* dem Code einbauen** — sonst wird die Geschwindigkeit zum strukturellen Schaden.

---

## Bewertungs-Selbstcheck als kontinuierliche Disziplin

Eine spezifische Lektion aus dem CAS-Kontext, die generischer ist als sie zunächst wirkt: die Moodle-Bewertungskriterien (18 Items, 100 Punkte) wurden bewusst nicht als Endkontrolle behandelt, sondern als **fortlaufend prüfbares Gerüst**. Konkret entstand dafür das Skill `cas-aise-grade-self-check`, das die Rubrik aus `vault/Organisation/Bewertungskriterien.md` einliest, jedes Item gegen reale Repo-Evidenz (ADRs, Tests, Docker-Compose, Insights-Files, …) klassifiziert und einen Punkte-Forecast plus priorisierte Lücken-Liste produziert. Das Skill ist read-only, klassifiziert jedes Item auf seiner echten Punkte-Leiter (`0/1/3/5`, `0/1/4/7`, `0/3/7/10`, …) und liefert eine Top-3-bis-5-Liste der grössten Lücken sortiert nach `(max_pts − estimated_pts)`. Am Ende jeder Block-Nachbereitung lief der Selfcheck — nicht erst kurz vor der Abgabe.

Der Effekt war praktisch und psychologisch zugleich. Praktisch, weil Lücken früh sichtbar wurden, solange der Block-Kontext noch frisch im Kopf war — eine fehlende NfA-Spezifikation oder ein dünn geratenes Insight-Dokument lassen sich Tage nach dem PVA mit deutlich weniger Aufwand schliessen als nach drei Wochen. Psychologisch, weil das Skill den Druck aus der finalen Abgabe nimmt: statt zwei Wochen vor der Deadline herauszufinden, dass das höchstgewichtete 12-Punkte-Item (KI-Nutzung) noch undokumentiert ist, war jeder Block-Nachbereitungs-Endstand bereits ein bewertungsfähiges Mini-Submission. Die übertragbare Erkenntnis: **wenn es ein definiertes Erfolgskriterium gibt, lohnt es sich, dieses Kriterium in Code zu giessen** — als Linter, als Skill, als CI-Check — und kontinuierlich laufen zu lassen, nicht punktuell zu prüfen. Das gilt für Bewertungsrubriken genauso wie für Architekturkonventionen, Test-Coverage-Schwellen oder Compliance-Anforderungen in einem Berufskontext.

---

## Kontextsensitive Todo-Listen: `cas-aise-todo-list`

Komplementär zum Grading-Selfcheck steht ein zweites schmales Skill: `cas-aise-todo-list`. Aufgabe: aus dem aktuellen Datum **automatisch den richtigen Block (1–5) und die richtige Phase (Vorbereitung / PVA / Nachbereitung) berechnen**, die zugehörige Vault-Datei (`vault/Blöcke/<NN ...>/<NN ...> - <a|b|c>) <Phase>.md`) lesen und die offenen `- [ ]`-Punkte zurückliefern. Default-Ausgabe ist eine kompakte 5×3-Block-×-Phase-Matrix mit Prozent-Erledigung pro Zelle plus Gesamtfortschritt; ein `full`-Switch erweitert auf die volle Schedule-Tabelle, den aktuellen Block-/Phase-Einzeiler und die offenen Items gruppiert nach Section.

Der Wert liegt nicht in der Komplexität des Skills — die Logik ist trivial, ein Datums-Check plus Markdown-Parsing — sondern darin, dass es **die richtige Frage zur richtigen Zeit beantwortet**, ohne dass man jedes Mal selbst durch die Block-Schedule navigieren muss. "Was ist heute dran?" ist eine erstaunlich oft gestellte Frage, und je länger man sie manuell beantworten muss, desto häufiger fällt sie aus dem Workflow heraus. Die generalisierbare Lektion: **Skills sind nicht nur für schwere Arbeit gedacht** — gerade kleine, hochfrequent benötigte Lookups (Status, Termin, "wo bin ich gerade?") sind ideal als Skill, weil sie die kognitive Last reduzieren und den Agent in dieselbe Antwort-Struktur zwingen, die der Mensch ohnehin erwartet. In Kombination mit `cas-aise-grade-self-check` ergibt sich ein praktisches Disziplin-Paar: `todo-list` zeigt *was offen ist*, `grade-self-check` zeigt *was die Bewertung dazu sagen würde* — und beide laufen über das Vault als gemeinsame Single-Source-of-Truth.

---

## Plan-vs-Execute strikt trennen

Verwandt mit dem Phasen-Pattern, aber eine eigene Erkenntnis: Agents sind im **Planen** und im **Ausführen** sehr gut — aber schwach, wenn sie beides gleichzeitig sollen. Die typische Falle ist, einen Agent mit "Build feature X" zu beauftragen und dabei zu erwarten, dass er erst denkt und dann tippt. In der Praxis springt das Modell oft sofort in Code, weil die nächste Edit-Aktion eine niedrige Latenz hat — Planung kostet Tokens, Ausführen erzeugt sichtbaren Fortschritt. Das Resultat sind Implementierungen, die nach drei Iterationen plötzlich in eine Sackgasse laufen, weil eine grundlegende Designentscheidung übersprungen wurde.

Die Gegenstrategie ist Werkzeug-gestützt: der `superpowers:writing-plans` Skill produziert in einer dedizierten Plan-Session einen Markdown-Plan mit nummerierten Schritten, Risiken und Reviewpunkten — ohne eine einzige Code-Edit. Erst danach, in einer frischen Session (`/clear`), wird der Plan via `subagent-driven-development` ausgeführt; der ausführende Agent sieht nur den Plan, nicht die Plan-Diskussion. Dieser harte Schnitt verhindert, dass Planung "im Vorbeigehen" passiert und liefert nebenbei ein durchsuchbares Artefakt unter `docs/superpowers/plans/`, das später als Begründung für Designentscheidungen taugt — der ADR-Light fürs eigene Tooling. Die übertragbare Lektion ist banal, aber zählt: **wer Plan und Execute mischt, kriegt am Ende beides schlechter**.
