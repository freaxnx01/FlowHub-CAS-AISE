---
tags:
  - claude-generated
updated: 2026-05-24
---

# Datenschutz & AI-Act — FlowHub im CAS-AISE-Kontext

## TL;DR

- **Rubrik-Status:** Kein eigenes Bewertungskriterium in `Organisation/Bewertungskriterien.md`. Wirkt **indirekt** auf drei Buckets: Spezifikation (NfA/SMART), Entwurf (Datenfluss/Residenz), KI-Reflexion (AI-Act-Klassifikation als Teil der KI-Nutzungsbeschreibung).
- **FlowHub-Realität:** Single-User-Homelab-App. Haushaltsausnahme (GDPR Art. 2(2)(c)) und das CH-DSG-Äquivalent greifen, **solange** keine Personendaten Dritter verarbeitet werden. Trigger sind: Telegram-Peers, E-Mail-Absender in Captures, externe Skill-Provider-Daten.
- **AI-Act-Einstufung:** **Minimal risk / Art. 50 (Transparenzpflicht)**. LLM-Klassifikation von Captures ist weder Annex-III high-risk noch Art. 5 prohibited. Umsetzung: UI markiert KI-klassifizierte Items sichtbar.
- **Cloud-LLM ist die einzige echte Compliance-Entscheidung:** Lokales Ollama → kein Datenexport, trivial. OpenAI/Anthropic → Auftragsverarbeitung + SCCs / CH-US-DPF dokumentieren.
- **Pflichten, die NICHT gelten:** keine DSFA, kein Verarbeitungsverzeichnis (Art. 30), keine Datenschutzerklärung im Submission-PDF.
- **In der Projektarbeit unterbringen:** 1 NfA-Eintrag (SMART), 1 Datenfluss-Diagramm mit Legende, 1 Absatz AI-Act-Klassifikation in KI-Reflexion, 1 Risiko-Tabelle (3–5 Zeilen), 2 ADRs (LLM-Hosting, Logging-PII-Policy). Insgesamt ≤2 Seiten.

---

## 1. Geltungsbereich für FlowHub

| Regime | Trifft zu? | Trigger |
|---|---|---|
| EU GDPR | nur falls Daten Dritter im System | Telegram-Captures von anderen Personen, Mail-Sender, Kontakte in Capture-Text |
| CH revDSG (09/2023) | analog GDPR | dieselben Trigger; "Bearbeitung Personendaten Dritter" durch eine Privatperson |
| EU AI Act | ja, aber niedrigste Stufe | Art. 50 — Transparenzpflicht für KI-Interaktion |

**Haushaltsausnahme:** Solange FlowHub nur eigene Daten des Betreibers verarbeitet, greift die persönliche/familiäre Nutzungsausnahme. Sobald ein Capture die Daten einer dritten Person enthält (Name, E-Mail, Telefonnummer, Telegram-Handle), verlässt der Use-Case diese Ausnahme.

## 2. AI-Act-Klassifikation (Kurzfassung)

- **Art. 5 — Verbotene Praktiken:** ❌ trifft nicht zu (kein Social Scoring, keine Biometrie, keine subliminale Manipulation).
- **Annex III — High-Risk:** ❌ trifft nicht zu (kein Employment, Credit, Education, Critical Infrastructure, Law Enforcement).
- **Art. 50 — Transparenzpflicht:** ✅ greift. FlowHub klassifiziert Captures via LLM → Nutzer muss erkennen können, dass es sich um KI-Output handelt.
  - **Umsetzung:** `LifecycleBadge` markiert KI-klassifizierte Captures; Reflexions-Text im Submission-PDF benennt die Klassifikation explizit.
- **GPAI-Pflichten:** ❌ FlowHub *baut* kein GPAI-Modell, sondern *nutzt* eines. Anbieter-Pflichten liegen beim LLM-Provider.

## 3. Konkrete Deliverables für die Projektarbeit

### 3.1 NfA (SMART) in der Spezifikation

Konkret als `NfA-P1` (Personendaten-Residenz) und `NfA-P2` (KI-Transparenz / AI Act Art. 50) in `docs/spec/nfa.md` hinterlegt — Format konsistent zu `NfA-D*` und `NfA-O*` (Category / Statement / Measurable / Achievable / Relevant / Time-bound).

- **NfA-P1** verankert: Capture-Inhalte bleiben auf eigener Infrastruktur; Cloud-LLM ist Opt-in via Env-Var; Outbound-Audit-Test + Data-Flow-Diagramm als Nachweise.
- **NfA-P2** verankert: KI-klassifizierte Captures tragen `ClassificationSource = "AI"` und werden via `LifecycleBadge` sichtbar markiert; bUnit-Test + EF-Migration als Nachweise.

Beide laufen auf Block 5 als Verifikations-Fenster.

### 3.2 Architektur-Abschnitt "Datenfluss & Residenz"

Umgesetzt in `docs/design/data-flow.md` — zwei Mermaid-Diagramme mit Legende (Prüfer-Vorgabe):

- **A. Trust-Boundary-Übersicht** (flowchart): Homelab-Boundary umfasst Web + Classifier + DB + Ollama + Vikunja + Wallabag. Cloud-LLM ist als gestrichelter Opt-in-Pfad ausserhalb der Boundary markiert. Outbound-Pfade zu Vikunja/Wallabag transportieren nur Tag + URL, nicht den Capture-Body.
- **B. Capture-Lebenszyklus** (sequenceDiagram): zeigt zwei Boundary-Crossings (Eingang vom externen Source, Ausgang zum Skill-Target) und markiert den Punkt, an dem `ClassificationSource = "AI"` persistiert wird (NfA-P2-Hook).

Beide Diagramme haben Legenden + Invarianten-Liste und sind direkt im Submission-PDF einsetzbar.

### 3.3 Risiko-Tabelle

| Datenkategorie | Regime | Mitigation | Nachweis |
|---|---|---|---|
| Eigene Captures (Body, URL) | Haushaltsausnahme — *nur bei lokalem LLM* | Self-hosted Storage; Cloud-LLM nur opt-in via `Embeddings__Provider` | `docs/design/data-flow.md` Abschnitt A; `NfA-P1` |
| Telegram-Peer-Handles | GDPR / revDSG | Capture-Parser pseudonymisiert Handle vor Persistierung; nur Hash in DB | `TelegramCaptureParserTests.Handle_IsHashed_BeforePersist` (Block 5) |
| Mail-Sender-Adressen | GDPR / revDSG | Capture-Parser pseudonymisiert lokalen Teil der Mailadresse (`user@` → Hash, Domain bleibt) | `MailCaptureParserTests.Sender_LocalPart_IsHashed` (Block 5) |
| Externe URLs (Web-Captures) | – | Outbound-Fetch ist Eigeninitiative; keine PII-Last per Definition | n/a |
| Embedding-Vektoren | GDPR / revDSG (grenzwertig — Re-Identifikation theoretisch möglich) | Vektoren bleiben in der lokalen DB; werden nicht an Drittsysteme propagiert | `NfA-P1`; `OutboundCallAuditTests` |
| LLM-Prompt-Inhalte | GDPR / revDSG / AI Act | Default lokales Ollama; Cloud-Pfad erfordert bewusste Konfiguration + DPA-Vermerk | `NfA-P1`; `ADR-0007 LLM-Hosting` |
| Skill-Outbound-Payloads (Vikunja/Wallabag) | GDPR / revDSG | Skill-Adapter senden nur Tag + URL, nicht den Capture-Body | `docs/design/data-flow.md` Abschnitt A Invariante 3; `SkillOutboundContractTests` |
| Log-Inhalte (Serilog) | GDPR / revDSG | Kein Capture-Body in Logs; nur Capture-ID + Stage + Klassifikations-Metadaten | `ADR-0008 Logging-Policy`; `SerilogPiiAuditTests` (Block 5) |
| OpenTelemetry-Span-Tags | GDPR / revDSG | Span-Attribute enthalten nur IDs und Stage-Werte, keine Bodies/Handles | `TracingPiiAuditTests` (Block 5) |
| DB-Backups | GDPR / revDSG | Backups bleiben auf demselben Homelab-Host, identisches Trust-Niveau wie Live-DB; keine Off-Site-Cloud-Backups im Default | Backup-Skript-Pfad in `docs/ops/backup.md`; ADR ggf. nachziehen |

### 3.4 ADRs (drei genügen)

- **ADR-0007 — LLM-Hosting:** Lokal (Ollama) vs. Cloud — Kompromiss Datenschutz ↔ Klassifikationsqualität. Geschrieben in `docs/adr/0007-llm-hosting.md`.
- **ADR-0008 — Logging-Policy:** Kein PII / Capture-Body in Serilog-Output (Definition + verbotene Felder + Beispiel-Enricher).
- **ADR-0009 — Telemetry-PII-Policy:** OpenTelemetry-Span-Tags dürfen nur IDs und Lifecycle-Stages enthalten (Liste erlaubter Tag-Keys).

### 3.5 Reflexions-Absatz

> **Datenschutz und AI-Act in der KI-Nutzung von FlowHub.** FlowHub verwendet ein LLM zur Klassifikation von Capture-Inhalten und fällt damit unter den EU AI Act, konkret unter Art. 50 (Transparenzpflicht für KI-Interaktionen). Das System ist weder eine prohibierte Praxis nach Art. 5 noch ein high-risk-Use-Case nach Annex III — entsprechend beschränkt sich die regulatorische Pflicht auf die Erkennbarkeit der KI-Beteiligung für die Nutzer:in. Umgesetzt ist diese Pflicht über das Feld `ClassificationSource` am `Capture` (`None | Heuristic | AI | Manual`) und ein dazu renderndes `LifecycleBadge` im UI; beide werden in NfA-P2 spezifiziert und durch einen bUnit-Test geprüft.
>
> Datenschutzseitig profitiert FlowHub als Single-User-Homelab-Anwendung von der Haushaltsausnahme nach GDPR Art. 2(2)(c) und der entsprechenden Regelung im revidierten CH-DSG — solange Capture-Inhalte das Homelab nicht verlassen. Diese Voraussetzung wird durch NfA-P1 als Default-Verhalten verankert (lokales Ollama, keine Cloud-LLM-Outbound-Calls ausserhalb expliziter Operator-Konfiguration) und durch das Datenfluss-Diagramm in `docs/design/data-flow.md` dokumentiert. ADR-0007 fixiert den Opt-in-Mechanismus für Cloud-LLM-Nutzung; ADR-0008 und ADR-0009 schliessen die zwei weiteren Datenpfade (strukturierte Logs, Telemetry-Spans), über die Capture-Inhalte unbeabsichtigt die Trust-Boundary verlassen könnten.
>
> **Erkenntnis:** Die Compliance-Anforderung früh als messbare NfA zu formulieren — nicht erst gegen Ende der Projektarbeit — hat zwei konkrete Architektur-Umbauten vermieden: einen nachgelagerten Cloud-LLM-Default-Switch und eine spätere PII-Scrubbing-Schicht in der Log-Pipeline. Die Default-Konfiguration war ab Block 3 bereits compliance-konform; spätere Blöcke mussten nur die Verifikations-Werkzeuge (Outbound-Audit-Test, Tracing-Audit-Test) nachziehen.

## 4. Was bewusst NICHT gemacht wird

- ❌ Vollständige DSFA / DPIA — Overkill für Single-User-Tool.
- ❌ Verzeichnis von Verarbeitungstätigkeiten (Art. 30 GDPR) — gilt nicht für Privatperson.
- ❌ Eigene Datenschutzerklärung — FlowHub hat keine Nutzer Dritter.
- ❌ Stack-Cross-Walk Java/Quarkus → .NET im Compliance-Kapitel — Stack-Neutralität (siehe `Projektarbeit/`-Konvention).

## 5. Verweise

- NfA-Spec: `docs/spec/nfa.md` → `NfA-P1`, `NfA-P2`
- Data-Flow-Diagramm: `docs/design/data-flow.md`
- Rubrik: `Organisation/Bewertungskriterien.md`
- Prüfer-Vorgaben PVA #4 (2026-05-23): Diagramm-Legenden, Roter Faden, Reflexion CAS→Projektarbeit, Moodle ≤20 MB
- Block 5 (Deployment) ist der natürliche Ort, dieses Material ins Submission-PDF zu überführen
