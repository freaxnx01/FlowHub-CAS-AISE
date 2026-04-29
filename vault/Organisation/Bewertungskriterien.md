---
tags:
  - claude-generated
updated: 2026-04-29
---

# CAS AISE — Bewertungskriterien (Projektarbeit)

Offizielle Moodle-Rubrik für die Projektarbeit. **Source of truth** für alle Block-Nachbereitungen — die Skala (`0 / 1 / 3 / 5`, `0 / 1 / 4 / 7`, `0 / 3 / 7 / 10`, `0 / 2 / 6`, `0 / 1 / 7 / 12`, `0 / 2`) zeigt die Gewichtung jedes Kriteriums.

> **Anwendung:** Am Ende jeder Block-Nachbereitung diese Liste durchgehen. Pro Kriterium prüfen, welche Stufe heute realistisch ist, und fehlende Deliverables proaktiv erzeugen — nicht erst zum Schlussabgabe-Zeitpunkt.

---

## Spezifikation

**Spezifikation: Sind die wichtigsten Use-Cases und fachlichen Anforderungen benannt**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 3 — überwiegend bzw. mehrheitlich
- 5 — vollständig bzw. korrekt

**Spezifikation: Sind die Qualitätsanforderungen (NfA) nach SMART spezifiziert**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 3 — überwiegend bzw. mehrheitlich
- 5 — vollständig bzw. korrekt

**Spezifikation: Ist die grundsätzliche Vision der Lösung beschrieben**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 3 — überwiegend bzw. mehrheitlich
- 5 — vollständig bzw. korrekt

## Entwurf

**Entwurf: Ist der Lösungsansatz und die Architektur beschrieben (bildlich wie textuell)**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 4 — überwiegend bzw. mehrheitlich
- 7 — vollständig bzw. korrekt

**Entwurf: Ist der Entwurf aus den verschiedenen Perspektiven (Struktur, Verhalten, Interaktion) beschrieben**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 4 — überwiegend bzw. mehrheitlich
- 7 — vollständig bzw. korrekt

**Entwurf: Ist das DB-Modell spezifiziert**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 2 — überwiegend bzw. mehrheitlich
- 3 — vollständig bzw. korrekt

## Programmierung

**Programmierung: Ist der Code lesbar, dokumentiert und nach Layer, Modulen und Sub-Systemen strukturiert**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 4 — überwiegend bzw. mehrheitlich
- 7 — vollständig bzw. korrekt

**Programmierung: Wurden die Konzepte von Quarkus, Jakarta EE und modernen Java Applikationen berücksichtigt**
- 0 — nicht bzw. kaum
- 3 — teilweise bzw. ansatzweise
- 7 — überwiegend bzw. mehrheitlich
- 10 — vollständig bzw. korrekt

> **FlowHub-Kontext:** Stack-Mismatch — FlowHub ist .NET 10 / ASP.NET Core / Blazor / EF Core. Dieses Kriterium wird **nicht aktiv verfolgt**. Die generischen "modernen Konzepte" (DI, Configuration, OpenAPI, async/await, Testbarkeit) sind über das Stack-äquivalente .NET-Ökosystem ohnehin abgedeckt.

**Programmierung: Sind die Erkenntnisse aus der Programmierung dokumentiert**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 2 — überwiegend bzw. mehrheitlich
- 3 — vollständig bzw. korrekt

**Programmierung: Ist der Source-Code in einem Git-Repository verfügbar**
- 0 — nicht bzw. kaum
- 2 — vollständig bzw. korrekt

## Validierung

**Validierung: Ist definiert, welches die Abnahmekriterien sind**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 3 — überwiegend bzw. mehrheitlich
- 5 — vollständig bzw. korrekt

**Validierung: Ist spezifiziert, wie die Applikation getestet wird und welche Technologien dazu verwendet werden**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 3 — überwiegend bzw. mehrheitlich
- 5 — vollständig bzw. korrekt

**Validierung: Sind Unit-Tests programmiert**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 3 — vollständig bzw. korrekt

**Validierung: Sind die Test-Ergebnisse dokumentiert**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 3 — vollständig bzw. korrekt

## KI, Sub-Systeme & Reflexion

**Wurden KI-unterstützende Werkzeuge verwendet und deren Nutzung beschrieben**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 7 — überwiegend bzw. mehrheitlich
- 12 — vollständig bzw. korrekt

**Wurden mit Hilfe der KI intelligente und flexible Services gebaut**
- 0 — nicht bzw. kaum
- 2 — teilweise bzw. ansatzweise
- 6 — vollständig bzw. korrekt

**Wurde die Lösung in verschiedene Sub-Systeme aufgeteilt, die unabhängig voneinander als Container verteilt und betrieben werden können**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 3 — überwiegend bzw. mehrheitlich
- 5 — vollständig bzw. korrekt

**Sind die Erfahrungen während der Projektarbeit mit KI-unterstützenden Werkzeugen als Fazit reflektiert**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 4 — überwiegend bzw. mehrheitlich
- 7 — vollständig bzw. korrekt

---

## Punkte-Total

| Bereich | Max-Punkte |
|---|---:|
| Spezifikation (3 Items × 5) | 15 |
| Entwurf (7 + 7 + 3) | 17 |
| Programmierung (7 + 10 Quarkus + 3 + 2) | 22 |
| Validierung (5 + 5 + 3 + 3) | 16 |
| KI / Sub-Systeme / Reflexion (12 + 6 + 5 + 7) | 30 |
| **Total** | **100** |

Mit Quarkus-Item ausgeklammert (Stack-Mismatch .NET): erreichbar 90 / 90.

---

## Verweise

- Repo-Mirror dieser Dimensionen (Kurz-Summary): `.ai/cas-instructions.md` → Section "Grading"
- Block-Nachbereitungen sollen die Kriterien aktiv abhaken (siehe `Blöcke/<NN ...>/<NN ...> - c) Nachbereitung.md`)
