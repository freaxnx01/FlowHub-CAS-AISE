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

Umgesetzt in `docs/design/data-flow.md` — zwei Mermaid-Diagramme mit Legende (Werner-Vorgabe):

- **A. Trust-Boundary-Übersicht** (flowchart): Homelab-Boundary umfasst Web + Classifier + DB + Ollama + Vikunja + Wallabag. Cloud-LLM ist als gestrichelter Opt-in-Pfad ausserhalb der Boundary markiert. Outbound-Pfade zu Vikunja/Wallabag transportieren nur Tag + URL, nicht den Capture-Body.
- **B. Capture-Lebenszyklus** (sequenceDiagram): zeigt zwei Boundary-Crossings (Eingang vom externen Source, Ausgang zum Skill-Target) und markiert den Punkt, an dem `ClassificationSource = "AI"` persistiert wird (NfA-P2-Hook).

Beide Diagramme haben Legenden + Invarianten-Liste und sind direkt im Submission-PDF einsetzbar.

### 3.3 Risiko-Tabelle

| Datenkategorie | Regime | Mitigation |
|---|---|---|
| Eigene Captures (Text, URL) | Haushaltsausnahme | self-hosted Storage, lokales LLM |
| Telegram-Peer-Namen | GDPR/DSG | nicht persistieren oder Hash; Pseudonymisierung im Capture-Parser |
| URLs externer Websites | – | Outbound-Fetch ist Eigeninitiative, kein Datenschutz-Trigger |
| Log-Inhalte (Serilog) | GDPR/DSG | keine Capture-Bodies loggen; nur Capture-IDs + Klassifikation |
| LLM-Prompt-Inhalte | GDPR/DSG/AI-Act | lokales Modell bevorzugt; falls Cloud → DPA-Hinweis |

### 3.4 ADRs (zwei genügen)

- **ADR-XXXX — LLM-Hosting:** Lokal (Ollama) vs. Cloud — Kompromiss Datenschutz ↔ Klassifikationsqualität.
- **ADR-XXXX — Logging-Policy:** Kein PII / Capture-Body in Serilog-Output (Definition + Code-Beispiel).

### 3.5 Reflexions-Absatz

Kurzer Text in der KI-Reflexion: AI-Act-Einstufung benennen (Art. 50 minimal risk), Umsetzungs-Nachweis verlinken (UI-Badge + Datenfluss-Diagramm), eine Lessons-Learned-Zeile ("Compliance war früh als NfA fixiert → vermied späte Architektur-Umbauten").

## 4. Was bewusst NICHT gemacht wird

- ❌ Vollständige DSFA / DPIA — Overkill für Single-User-Tool.
- ❌ Verzeichnis von Verarbeitungstätigkeiten (Art. 30 GDPR) — gilt nicht für Privatperson.
- ❌ Eigene Datenschutzerklärung — FlowHub hat keine Nutzer Dritter.
- ❌ Stack-Cross-Walk Java/Quarkus → .NET im Compliance-Kapitel — Stack-Neutralität (siehe `Projektarbeit/`-Konvention).

## 5. Verweise

- NfA-Spec: `docs/spec/nfa.md` → `NfA-P1`, `NfA-P2`
- Data-Flow-Diagramm: `docs/design/data-flow.md`
- Rubrik: `Organisation/Bewertungskriterien.md`
- Werner-Vorgaben PVA #4 (2026-05-23): Diagramm-Legenden, Roter Faden, Reflexion CAS→Projektarbeit, Moodle ≤20 MB
- Block 5 (Deployment) ist der natürliche Ort, dieses Material ins Submission-PDF zu überführen
