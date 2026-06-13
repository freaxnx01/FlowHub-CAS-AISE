---
tags:
  - claude-generated
updated: 2026-06-12
---

# CAS AISE — Bewertungskriterien (Projektarbeit)

Offizielle Moodle-Rubrik für die Projektarbeit. **Source of truth** für alle Block-Nachbereitungen — die Skala (`0 / 1 / 3 / 5`, `0 / 1 / 4 / 7`, `0 / 3 / 7 / 10`, `0 / 2 / 6`, `0 / 1 / 7 / 12`, `0 / 2`) zeigt die Gewichtung jedes Kriteriums.

> **Rubrik-Update Juni 2026 (an die aktuelle Moodle-Fassung angeglichen):** Zwei Kriterien wurden umformuliert — beide zugunsten von FlowHub: (1) das Programmier­kriterium ist jetzt **framework-neutral** (vorher „Quarkus/Jakarta EE/Java") und damit für den .NET-Stack **direkt erfüllt**; (2) das Sub-System-Kriterium akzeptiert nun **explizit den modularen Monolithen** „als Container lauffähig betrieben". Es gibt **kein ausgeklammertes Item mehr** — alle **100 Punkte** sind erreichbar.

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

**Ist der Code lesbar, dokumentiert und nach Schichten und Modulen mit klaren Verantwortlichkeiten strukturiert**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 4 — überwiegend bzw. mehrheitlich
- 7 — vollständig bzw. korrekt

**Programmierung: Wurden die Konzepte des gewählten Frameworks und moderner Applikationsentwicklung sachgerecht eingesetzt (z. B. Dependency Injection, REST-Schnittstellen, Konfiguration, Fehlerbehandlung)**
- 0 — nicht bzw. kaum
- 3 — teilweise bzw. ansatzweise
- 7 — überwiegend bzw. mehrheitlich
- 10 — vollständig bzw. korrekt

> **FlowHub-Kontext (Rubrik-Update Juni 2026):** Dieses Kriterium ist **framework-neutral** formuliert (vorher Quarkus/Jakarta EE-spezifisch). Es ist für FlowHub **direkt und vollständig erfüllt** — das gewählte Framework ist .NET 10 / ASP.NET Core, und die genannten Konzepte sind im Code nachgewiesen: **Dependency Injection** (`IServiceCollection`, per-Modul `*ServiceCollectionExtensions`), **REST-Schnittstellen** (Minimal API + RFC 9457 ProblemDetails in `FlowHub.Api`), **Konfiguration** (`IConfiguration`/Options, 12-Factor), **Fehlerbehandlung** (ProblemDetails, MassTransit-Retry + deterministischer Fallback). Konzept-Belege: **`docs/spec/modern-app-concepts.md`**. Kein Stack-Mismatch, keine Ausklammerung mehr — angestrebte Stufe **„vollständig/korrekt" (10)**.

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

**Ist die Lösung in klar abgegrenzte Module bzw. Sub-Systeme strukturiert (modularer Monolith oder verteilte Services) und als Container lauffähig betrieben**
- 0 — nicht bzw. kaum
- 1 — teilweise bzw. ansatzweise
- 3 — überwiegend bzw. mehrheitlich
- 5 — vollständig bzw. korrekt

> **FlowHub-Kontext (Rubrik-Update Juni 2026):** Das Kriterium akzeptiert nun **explizit den modularen Monolithen** (statt nur verteilter, unabhängig deploybarer Container). FlowHub ist genau das: klar abgegrenzte Module (`FlowHub.Core/Api/AI/Persistence/Skills/Web`) mit sauberen Verantwortlichkeiten (ADR 0002), **als Container lauffähig betrieben** (Docker-Compose-Stack + Live-Demo). Damit ist **„vollständig/korrekt" (5)** erreichbar — die frühere Lücke „kein unabhängig deploybarer Sub-System-Container" entfällt durch die neue Formulierung.

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
| Programmierung (7 + 10 + 3 + 2) | 22 |
| Validierung (5 + 5 + 3 + 3) | 16 |
| KI / Sub-Systeme / Reflexion (12 + 6 + 5 + 7) | 30 |
| **Total** | **100** |

**Rubrik-Update (Juni 2026):** Das frühere Java-/Quarkus-spezifische Programmier­kriterium wurde **framework-neutral** umformuliert und das Sub-System-Kriterium akzeptiert nun explizit den **modularen Monolithen**. Es gibt damit **kein ausgeklammertes Item mehr** — alle **100 Punkte sind für FlowHub erreichbar** (kein „/90"-Sonderfall).

---

## Verweise

- Repo-Mirror dieser Dimensionen (Kurz-Summary): `.ai/cas-instructions.md` → Section "Grading"
- Block-Nachbereitungen sollen die Kriterien aktiv abhaken (siehe `Blöcke/<NN ...>/<NN ...> - c) Nachbereitung.md`)
