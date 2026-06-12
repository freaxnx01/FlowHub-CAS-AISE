---
marp: true
theme: flowhub
paginate: true
footer: 'FlowHub · CAS AI-Assisted Software Engineering · Andreas Imboden'
math: false
---

<!--
Sprechnotizen stehen in HTML-Kommentaren wie diesem und erscheinen im
Marp-Presenter-View (P drücken) bzw. im PDF-Notes-Export – nicht auf der Folie.
Zielzeit: ~9 Minuten. Richtwert pro Folie steht oben in der Notiz.
-->

<!-- _class: title -->
<!-- _paginate: false -->
<!-- _footer: '' -->

# FlowHub

## Ein KI-gestützter persönlicher Eingangskorb

**Andreas Imboden** · CAS AI-Assisted Software Engineering · FFHS FS26

*Das Projekt — und die Erfahrung, es mit KI zu bauen*

<!--
[~20 s] Begrüssung.
"FlowHub – mein CAS-Projekt. Ich zeige euch in den nächsten Minuten zwei Dinge:
erstens WAS ich gebaut habe, zweitens – und das ist der interessantere Teil –
WIE es war, das fast vollständig mit KI zu bauen."
-->

---

## Das Problem: Capture without friction

Der digitale Alltag produziert ständig kleine Informationsschnipsel:
ein Film den man schauen will, ein Artikel zum Lesen, das Foto einer Quittung.

**Heute landen sie überall — oder werden vergessen:**

> Idee → Welche App? → App öffnen → Kategorisieren → Ablegen
> **= 5+ Schritte, Kontextwechsel, oft vergessen**

Der Nutzer will *festhalten*, ohne im Moment zu entscheiden, **wohin** es gehört.

<!--
[~50 s] Das Problem konkret machen.
"Jeder kennt das: schnell etwas merken wollen, aber dann – welche App? Notizen?
Lesezeichen? Task-Liste? Bis man sich entschieden hat, ist der Gedanke weg.
Das Kernbedürfnis: capture without friction. Erfassen ohne Reibung."
-->

---

## Die Idee: ein Eingang, KI macht den Rest

<div class="cols">
<div>

### Heute
Idee → welche App? → öffnen →
kategorisieren → ablegen
**5+ Schritte**

</div>
<div>

### Mit FlowHub
Idee → **Telegram** → fertig
**1 Schritt — KI übernimmt**

</div>
</div>

Ein **Telegram-Bot** als einziger Eingang.
FlowHub **erkennt** den Input, **kategorisiert** ihn und **routet** ihn
automatisch an den richtigen Self-Hosted-Service im eigenen Homelab.

<!--
[~45 s] Die Lösung in einem Satz.
"FlowHub dreht das um. Ein einziger Eingang – ein Telegram-Bot. Ich schicke etwas
hin, die KI erkennt was es ist und legt es am richtigen Ort ab. Aus fünf Schritten
wird einer. Und alles läuft self-hosted in meinem eigenen Homelab – kein Cloud-SaaS."
-->

---

## Konkret: das Skill-System

Jeder Input wird einem **Skill** zugewiesen — Erkennung über Keywords,
URL-Muster und, wenn nötig, ein **lokales LLM**.

| Input | Skill | landet in |
|---|---|---|
| heise.de-Artikel | ArticleSkill | **Wallabag** (read-later) |
| „The Imitation Game", share.google/… | MovieSkill | **Vikunja** (Watchlist) |
| jellyfin.org „ausprobieren" | HomelabSkill | **Wekan** (Kanban) |
| Foto einer Quittung | DocumentSkill | **paperless-ngx** (DMS) |
| nichts passt | GenericSkill | **Inbox** (PostgreSQL) |

Unklar? → Der Bot **fragt mit 2–3 Optionen zurück** (Confidence-Score).

<!--
[~55 s] Ein, zwei Zeilen der Tabelle vorlesen, nicht alle.
"Ein paar Beispiele: Ein heise-Artikel geht automatisch nach Wallabag, meinem
Read-Later-Dienst. Ein Filmtitel landet in meiner Vikunja-Watchlist. Ein Quittungsfoto
geht ins Dokumentenmanagement. Wenn die KI unsicher ist, fragt der Bot kurz nach –
zwei, drei Optionen, ein Tap. Das ist der Confidence-Score in Aktion."
-->

---

## Architektur

![h:520](../projektbeschreibung/FlowHub_Architecture-v2.svg)

<!--
[~50 s] Nicht jede Box erklären – den Fluss zeigen.
"Von oben nach unten: Telegram-Input kommt rein. Das Skill-System in der Mitte
entscheidet – unterstützt vom AI-Layer, der über Microsoft.Extensions.AI ein lokales
Ollama-Modell oder als Fallback die Anthropic-API anspricht. Typsichere REST-Clients
schreiben dann ins Homelab. Persistenz unten: PostgreSQL plus Redis. Sauber getrennte
Schichten – das war von Anfang an das Ziel: Komplexität durch Architektur beherrschen,
nicht durch Featuremenge."
Falls das SVG nicht rendert: Pfad in flowhub-praesentation.md auf das exportierte PNG zeigen.
-->

---

## Tech-Stack

| Schicht | Technologie |
|---|---|
| Backend | **.NET 10** / C# / ASP.NET Core (LTS, Nov 2025) |
| Web-UI | **Blazor SSR** — .NET-native, kein JS-Framework |
| KI-Integration | **Microsoft.Extensions.AI** + Ollama (lokal) / Anthropic (Fallback) |
| Pipeline | **MassTransit** — In-Memory (dev) / RabbitMQ (prod) |
| Persistenz | **PostgreSQL 17** + **pgvector** (semantische Suche) · EF Core 10 |
| REST-Clients | **Refit** — typsicher, deklarativ |
| Deployment | **Docker Compose** — 6 Services, Migrations als Init-Container |

*Inkrementell über 5 Blöcke gebaut: Konzept → UI → Services/KI → Persistenz → Deployment.*

<!--
[~40 s] Stack schnell, nicht vorlesen. Highlights setzen.
"Durchgehend .NET 10. Frontend Blazor – ich brauchte kein separates JS-Framework.
KI über Microsoft.Extensions.AI, das abstrahiert den Provider weg: lokales Ollama oder
Anthropic, umschaltbar per Config. Persistenz Postgres mit pgvector für semantische
Suche. Alles in Docker Compose. Gebaut wurde das inkrementell – ein Block pro Thema."
-->

---

## Betrieb: beobachten & selbst heilen

| Was | Wie |
|---|---|
| Metriken | OpenTelemetry → **Prometheus** (`/metrics`) → **Grafana** |
| Health | `/health/live` · In-App-Integration-Health (Vikunja/Wallabag/Paperless) |
| Demo-Status | öffentliche **Uptime-Kuma**-Statusseite (`status.demo.flowhub…`) — prüft auch LLM-Erreichbarkeit |
| Self-Healing | Container-`restart`-Policies + Healthchecks · KeywordClassifier-Fallback bei LLM-Ausfall |

*Block-5-Lernziel „Systeme überwachen und optimieren" — von der App bis zur Demo.*

<!--
[~30 s] Monitoring ist nicht nachträglich angeklebt: Metriken via OpenTelemetry,
Prometheus + Grafana im Stack. Die öffentliche Demo hat eine Uptime-Kuma-Statusseite,
die auch die LLM-Erreichbarkeit prüft. Fällt das LLM aus, greift der KeywordClassifier —
die App bleibt funktionsfähig. Container heilen sich per restart-Policy selbst.
-->

---

<!-- _class: divider -->
<!-- _paginate: true -->

# Teil 2: Bauen mit KI

## Wie war es wirklich?

<!--
[~10 s] Übergang. Tempo wechseln.
"Soviel zum Produkt. Jetzt der Teil, um den es in diesem CAS eigentlich geht:
Wie war es, das mit KI zu bauen?"
-->

---

## Nicht „Prompt rein, Code raus" — eine Pipeline

Jeder grössere Baustein lief durch denselben strukturierten Ablauf:

**Brainstorm → Spec → Plan → Subagent-Implementierung → 2-stufiges Review**

1. **Brainstorming-Skill** — Design als A/B/C-Entscheidungen (z. B. 13 Entscheide für die Async-Pipeline), jede mit Begründung festgehalten
2. **Spec + Plan** — schriftliches Design, dann TDD-geordneter Aufgabenplan
3. **Subagenten** — pro Task ein frischer Implementierer (Test-First)
4. **Review ×2** — Spec-Konformität, dann Code-Qualität — *bevor* etwas in `main` geht

<!--
[~55 s] Das ist der wichtigste konzeptionelle Punkt der Präsentation.
"Der grösste Lerneffekt: Gute KI-Entwicklung ist NICHT 'Prompt rein, Code raus'.
Es ist eine Pipeline. Erst brainstorme ich das Design – die KI zwingt mich, jede
Entscheidung als A/B/C-Wahl explizit zu treffen statt zu schwafeln. Dann ein
schriftliches Spec, dann ein Plan in test-first-Reihenfolge. Erst dann implementiert
ein Subagent – pro Aufgabe ein frischer Kontext. Und nichts geht in main ohne zwei
Reviews: stimmt es mit dem Spec, und ist die Qualität gut. Diese Struktur hat die KI
davon abgehalten, unkontrolliert in die falsche Richtung zu laufen."
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

Über alle Blöcke: **~85–95 % des Codes KI-generiert.**
Der Mensch-Anteil ist klein — aber **hochwertig**.

<!--
[~45 s] Die Zahl wirken lassen, dann sofort relativieren.
"In Zahlen: rund 89 Prozent des Persistenz-Codes kamen von der KI. Über das ganze
Projekt 85 bis 95 Prozent. Klingt nach 'die KI hat's gemacht'. Aber – und das ist der
Punkt der nächsten zwei Folien – die verbleibenden 10 bis 15 Prozent Mensch waren genau
die, die über Erfolg oder Desaster entscheiden."
-->

---

## Wo die KI glänzt

### Boilerplate
7 strukturgleiche `IEntityTypeConfiguration<T>`-Klassen — komplett generiert.
Von Hand zeitintensiv und fehleranfällig.

### Migrations & Infrastruktur
EF-Core-Migrations, Refit-Interfaces, GitHub-Actions-YAML — Muster auf Anhieb korrekt.

### Tests
16 Integrationstests gegen echtes PostgreSQL (Testcontainers) —
**alle grün beim ersten Durchlauf.** Konsistente Arrange/Act/Assert-Struktur.

> KI als **Accelerator**: was repetitiv und gut spezifiziert ist, entsteht in Minuten.

<!--
[~45 s] Positiv, konkret.
"Wo die KI brilliert: alles Repetitive und gut Spezifizierte. Sieben fast identische
EF-Core-Konfigurationsklassen – generiert in Sekunden, fehlerfrei. Migrations, typsichere
REST-Clients, CI-Pipelines. Und Tests: 16 Integrationstests gegen eine echte Postgres-
Datenbank, alle grün beim ersten Lauf. Das ist der Beschleuniger-Effekt."
-->

---

## Wo die KI scheitert

Genau dort, wo **Domänenverständnis** oder **Performance-Gespür** nötig ist:

- **N+1-Blindheit** — `ListAsync` ohne `.Include(c => c.Tags)`. Kein implizites Performance-Bewusstsein für Navigation Properties.
- **CASCADE überall** — alle Fremdschlüssel auf `CASCADE DELETE`. Die Unterscheidung *owned* vs. *referenced* (Soft-FK für Audit-Trail) war eine **menschliche Domänen-Entscheidung**.
- **Feldlängen** — `varchar(128)` statt `(64)`. KI weicht ohne expliziten Spec-Verweis auf „sichere" Defaults aus.
- **Veraltete Paket-Versionen** — Plan pinnte Npgsql 9; real war 10 nötig. **Trainingsdaten hinken neuen Releases hinterher.**
- **Feature-Drift** — KI schlägt ständig mehr Features vor; bewusste MVP-Eingrenzung war nötig.

<!--
[~55 s] Ehrlich und spezifisch – das überzeugt die Dozenten am meisten.
"Und wo scheitert sie? Überall, wo Domänenwissen oder Performance-Gespür zählt. Die KI
schrieb eine Datenbankabfrage mit dem klassischen N+1-Problem – fällt nur im Review auf.
Sie setzte alle Fremdschlüssel auf CASCADE DELETE; dass ein Audit-Trail erhalten bleiben
muss, ist eine Domänen-Entscheidung, die sie nicht treffen konnte. Sie nahm veraltete
Paket-Versionen, weil ihre Trainingsdaten den neuen Releases hinterherhinken. Und sie
will ständig mehr Features bauen – Scope-Disziplin musste ich liefern."
-->

---

## Der Smoke-Test-Moment

Die KI schrieb den ganzen Deployment-Stack. Dann lief **ein** Befehl:
`make smoke-prod` — End-to-End-Probe des laufenden Stacks.

**An einem Nachmittag fand er 5 reale, latente Bugs:**

- `.editorconfig` nicht ins Build-Image kopiert → Build bricht mit `TreatWarningsAsErrors` ab
- Compose-Env-Casing `${EMBEDDINGS__APIKEY}` ≠ `Embeddings__ApiKey` → Embeddings still no-op
- Leerstring-Modellname triggert `AssertNotNullOrEmpty` → Crash beim Start
- Mistral lehnt das `dimensions`-Feld ab → 422
- Passbolt-Refs vom Makefile überschattet → KI-Call erreichte nie den Provider

> KI schrieb den Code. Eine **KI-gestützte Prüfung** fand, was die KI übersah.
> **Der Mensch bleibt im Loop — als Reviewer.**

<!--
[~50 s] Die beste Geschichte des Decks. Mit etwas Spannung erzählen.
"Mein liebster Moment: Die KI hatte den kompletten Deployment-Stack geschrieben. Sah gut
aus. Dann schrieb ich – auch mit KI – einen Smoke-Test, der den echten Stack hochfährt
und durchprobt. Erster Lauf: fünf echte Bugs. Ein fehlendes File, das den Build sprengt.
Ein Casing-Fehler, durch den die Embeddings still nichts taten. Ein Crash beim Start.
Alles latent, alles hätte die Abgabe blockiert. Die Lektion: KI schreibt den Code, aber
eine Prüfung – idealerweise auch KI-gestützt – muss finden, was die KI übersieht.
Der Mensch bleibt im Loop, als Reviewer."
-->

---

<!-- _class: lead -->

## Fazit

**KI ist ein starker Accelerator für Infrastruktur-Code** —
Boilerplate, Migrations, Tests entstehen in Minuten.

**Sie braucht menschliche Führung bei Architektur & Domäne** —
FK-Strategie, Performance, Scope, aktuelle Versionen.

### Der Mensch bleibt Architekt und Reviewer.

<span class="small">Code & Doku: github.com/freaxnx01/FlowHub-CAS-AISE · Danke — Fragen?</span>

<!--
[~35 s] Klar landen, dann Q&A öffnen.
"Mein Fazit in einem Satz: KI ist ein starker Beschleuniger für Infrastruktur-Code, aber
sie braucht menschliche Führung bei allem, was Architektur und Domäne berührt. Die Rolle
hat sich verschoben – weg vom Zeile-für-Zeile-Tippen, hin zu Entwerfen, Lenken und
Reviewen. Der Mensch bleibt Architekt und Reviewer. Danke – ich freue mich auf eure Fragen."
-->
