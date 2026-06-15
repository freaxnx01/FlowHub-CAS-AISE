# AISE – Projektarbeiten: Übersicht aller Aufträge

> **Eigene Zusammenfassung.** Diese Datei fasst die fünf Block-Aufträge der
> Projektarbeit in eigenen Worten zusammen — sie enthält **nicht** den
> wörtlichen Moodle-Text (Instruktoren-IP). Der Java/Quarkus-Stack wird im
> Original nur als Beispiel-Stack genannt; FlowHub setzt die Lernziele
> stack-neutral mit .NET 10 / ASP.NET Core / Blazor um.

---

## Block 1 — Konzeption

**Aufwand:** ~28 h

### Lernziel

- KI-gestützte IDE-Werkzeuge für automatisierte Entwicklungsaufgaben nutzen.
- Mit diesen Werkzeugen ein lauffähiges Anwendungsskelett erzeugen.

### Auftrag (Zusammenfassung)

- Anforderungen dokumentieren, Architekturentwurf erstellen, Lösung konzeptionell aufsetzen.
- Lösung in Module gliedern und Paketstruktur definieren (Arc42 als empfohlene Doku-Vorlage).
- Projektstruktur im gewählten Stack anlegen.
- Architekturstil bewusst wählen (modularer Monolith, Hexagonal, DDD-Microservices, Schichten oder Mix) und den Entscheid per ADR begründen.

**Termin:** bis zur nächsten PVA.

---

## Block 2 — Frontend

**Aufwand:** ~26 h

### Lernziel

- Verschiedene Technologien/Frameworks für Web-Apps einsetzen.
- SSR- und CSR-Konzepte erklären und anwenden.
- Ein Framework für Web-Formulare nutzen.
- Unit-Tests generieren und Services testen.

### Auftrag (Zusammenfassung)

- Technologie für die Präsentationsschicht wählen und begründen (CSR via JS-Framework, SSR oder Mix).
- Frontend per Wireframes spezifizieren und den Seiten-Ablauf beschreiben.
- Frontend gegen Stub-Services mit statischen Testdaten implementieren.
- KI-generierte Unit-Tests schreiben, alle grün.

**Termin:** bis zur nächsten PVA.

---

## Block 3 — Services

**Aufwand:** ~22 h

### Lernziel

- Microservice- und Service-based-Architekturen entwerfen.
- Protokolle wie SOAP, REST und gRPC nutzen.
- Service-Discovery und Service-Mesh für GenAI entwickeln.
- Mit KI flexible Microservice-Architekturen bauen.
- KI-Agenten bauen.

### Auftrag (Zusammenfassung)

- Services schrittweise in unabhängigere Einheiten auftrennen und resilienter machen (z. B. asynchrone Kommunikation über eine Queue).
- KI in den Services selbst einsetzen, um sie intelligenter zu machen.
- Konsistenz zwischen Server und Client über OpenAPI / typisierte REST-Clients sicherstellen.
- Ergebnis: verteilte Lösung, deren Services noch fiktive/statische Daten liefern.

**Termin:** bis zur nächsten PVA.

---

## Block 4 — Persistence

**Aufwand:** ~22 h

### Lernziel

- Geeignete Persistenzform pro Problemstellung bestimmen.
- DB-Zugriff über ORM / JPA-äquivalente Spezifikationen abstrahieren.
- Dynamische Abfragen effizient programmieren.
- Datenzugriffe auf verschiedene Datenbankmodelle realisieren.

### Auftrag (Zusammenfassung)

- Geeignetes Datenmodell entwerfen — mit Bedacht, da Daten jede Technologie überleben und Migrationen teuer sind; das Modell soll künftige Bedürfnisse antizipieren.
- Datenzugriff über ein ORM abstrahieren.
- Dynamische Abfragen umsetzen (Criteria-/Query-API-Äquivalent).

**Termin:** bis zur nächsten PVA (Grundlage für die Arbeit in der PVA).

---

## Block 5 — Deployment & Abgabe

**Aufwand:** ~22 h | **Abgabe:** Montag, 6. Juli 2026, 00:00

### Lernziel

- Applikation containerisieren und in Docker/Kubernetes betreiben.
- Mit Git-Host-Tooling (GitHub/Copilot, GitLab-Agent) CI/CD-Pipelines aufsetzen und das Deployment automatisieren.
- Monitoring/Observability aufsetzen und Systeme überwachen/optimieren.
- KI-gestützte Applikationen bauen.

### Auftrag (Zusammenfassung)

- Lösung containerisieren und für den Betrieb verteilen.
- Deployment über das Git-Host-Tooling weitgehend automatisieren.
- Applikation um KI-basierte Suche und Workflows erweitern.
- Arbeit abschliessen und als PDF hochladen; das PDF enthält die URL des Git-Repositorys.

**Termin:** bis zwei Wochen nach der letzten PVA.
