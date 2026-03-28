# AISE – Projektarbeiten: Übersicht aller Aufträge

---

## Projektarbeit: Konzeption

**Aufwand:** ~28 h

### Lernziel

- Ich bin in der Lage, KI-unterstützende Werkzeuge in meiner IDE für verschiedene, automatisierte Aufgaben zu nutzen.
- Ich kann mit den entsprechenden Technologien und KI ein Skelett einer solchen Anwendung erstellen.

### Auftrag

In der Nachbearbeitung wenden Sie jeweils das Gelernte aus der Vorarbeit und der Präsenzveranstaltung an Ihrer Projektarbeit an. Dies bedeutet für den ersten Block, dass Sie die Anforderungen an die Arbeit dokumentieren, den Architekturentwurf erstellen und die Lösung konzeptionell erstellen. Dazu gehört auch, die Lösung in entsprechende Module zu gliedern und die Paketstruktur zu definieren.

Für den Lösungsentwurf und die Dokumentation ist das Arc42-Template empfehlenswert. Erstellen Sie anschliessend in Quarkus Ihre Projektstruktur. Überlegen Sie sich dabei, ob Sie einen modularen Monolithen, eine am Hexagonal-Architektur orientierten Struktur, eine nach DDD in Service aufgetrennte Microservice-Architektur, eine klassische Schichtenarchitektur oder ein Mix davon verwenden wollen. Begründen Sie Ihren Architekturentscheid (ADR).

### Termin

Bis zur nächsten Präsenzveranstaltung.

### Reflexion & Auswertung

Auf Basis dieses Lösungsentwurfs werden Sie in den nachfolgenden Blöcken Ihre Lösung implementieren.

---

## Projektarbeit: Frontend

**Aufwand:** ~26 h

### Lernziel

- Ich bin in der Lage, verschiedene Technologien und Frameworks für die Entwicklung von Web-Apps einzusetzen.
- Ich kann die Konzepte von SSR und CSR erklären und anwenden.
- Ich bin in der Lage, Quarkus für Web-Formulare zu nutzen.
- Ich bin in der Lage, Unit-Tests zu generieren und Services zu testen.

### Auftrag

Überlegen und definieren Sie, welche Technologie Sie für Ihre Präsentationsschicht einsetzen wollen. Soll es ein Client-seitiges Rendering mit JavaScript-Frameworks wie React oder Vue sein, ein Server-seitiges Rendering mit Qute oder ein Mix von beiden? Spezifizieren Sie mit Wireframes, wie Ihr Frontend aussehen sollte. Beschreiben Sie den Ablauf der Web-Seiten. Dies vereinfacht die anschliessende Implementation.

Implementieren Sie das Frontend und nutzen Sie dabei Services, die statische Testdaten zurückgeben. Generieren Sie mithilfe Ihrer KI Unit-Tests und überprüfen Sie damit die korrekte Funktionsweise Ihrer Lösung. Am Ende haben Sie ein fertiges Frontend, das auf statischen oder generierten Testdaten beruht.

### Termin

Bis zur nächsten Präsenzveranstaltung.

### Reflexion & Auswertung

Stellen Sie in der Gruppe sicher, dass das Frontend vollständig ist und alle Unit-Tests funktionieren. Im nächsten Block werden basierend auf dem Frontend die Backend-Services implementiert.

---

## Projektarbeit: Services

**Aufwand:** ~22 h

### Lernziel

- Ich kann Microservices- und Service-based Architekturen entwerfen.
- Ich bin fähig, die verschiedenen Protokolle wie SOAP, REST und gRPC zu nutzen.
- Ich kann Service-Discovery und Service-Mesh für GenAI entwickeln.
- Ich kann mit KI flexible Microservice-Architekturen bauen.
- Ich kann mit Spring-AI und Koog AI Agenten bauen.

### Auftrag

Wahrscheinlich sind Sie mit einer monolithischen oder einer einfachen Schichtenarchitektur im letzten Block gestartet, um das Frontend zu implementieren. Nun möchten wir die Services schrittweise in unabhängige Microservices auftrennen und verschiedene Technologien verwenden, diese flexibler und resilienter zu machen – resilienter beispielsweise durch eine asynchrone Kommunikation mittels einer Queue. Wir wollen aber auch die KI selbst in den Services verwenden, um diese intelligenter zu gestalten. Nutzen Sie OpenAPI und MicroProfile REST Clients, um die Konsistenz zwischen Server und Client sicherzustellen. Am Ende dieser Nacharbeit haben Sie eine verteilte Lösung, in der Ihre Microservices fiktive oder statische Daten zurückgeben.

### Termin

Bis zur nächsten Präsenzveranstaltung.

### Reflexion & Auswertung

Das Ergebnis Ihrer Projektarbeit ist Grundlage für den nächsten Block, wo wir die Lösung um die Persistenzschicht erweitern.

---

## Projektarbeit: Persistence

**Aufwand:** ~22 h

### Lernziel

- Ich kann für die jeweilige Problemstellung die geeignete Persistenzform bestimmen.
- Ich kann mit Spezifikationen wie ORM, JPA und Alternativen den DB-Zugriff abstrahieren.
- Ich kann dynamische Abfragen effizient programmieren.
- Ich kann mit Quarkus Panache Datenzugriffe auf verschiedene Datenbankmodelle realisieren.

### Auftrag

Wir wenden uns nun der dritten und letzten Schicht einer klassischen Enterprise Applikation zu, um Daten effizient zu speichern und für die entsprechenden Geschäftsprozesse flexibel zu nutzen. Da Daten jede Technologie überlebt und den wertvollen Teil Ihrer Applikation darstellt, ist das zugrunde liegende Datenmodell mit Bedacht zu wählen. Daten in ein neues Datenmodell zu migrieren oder fehlende Daten nachzuliefern, ist aufwändig. Entsprechend soll Ihr Datenmodell zukünftige Bedürfnisse antizipieren und daran anpassbar sein.

Entwerfen Sie hier nun das geeignete Datenmodell und implementieren Sie dessen Abstraktion über Hibernate ORM, Panache und Jakarta Data. Nutzen Sie die Möglichkeiten von Criteria API, um dynamische Abfragen zu realisieren.

### Termin

Bis zur nächsten Präsenzveranstaltung. Dieser Auftrag ist Grundlage für die weitere Arbeit in der Präsenzveranstaltung.

### Reflexion & Auswertung

Ihre Applikation speichert nun die Daten persistent.

---

## Projektarbeit: Deployment & Abgabe Projektarbeit

**Aufwand:** ~22 h | **Abgabe:** Montag, 6. Juli 2026, 00:00

### Lernziel

- Ich bin fähig, meine Applikation zu containerisieren und in Docker und Kubernetes zu betreiben.
- Ich kann GitHub und Copilot sowie die GitLab-Agent-Plattform einsetzen, um CI/CD Pipelines aufzusetzen und den Deployment-Prozess zu automatisieren.
- Ich bin fähig, entsprechendes Monitoring und Observation aufzusetzen, Systeme zu überwachen und zu optimieren.
- Ich bin in der Lage, mit Quarkus KI-gestützte Applikationen zu bauen.

### Auftrag

In der letzten Nachbearbeitungsphase geht es nun darum, die Lösung zu containerisieren und für den Betrieb zu verteilen. Nutzen Sie die Möglichkeit Ihrer Git-Host-Lösung, um den Deployment-Prozess weitgehend zu automatisieren. Erweitern Sie Ihre Applikation um KI-basierende Suche und Workflows. Schliessen Sie Ihre Arbeit ab und laden Sie diese als PDF hoch. Die Arbeit enthält die URL auf das Git-Repository Ihrer Lösung.

### Termin

Bis zwei Wochen nach der letzten PVA.

### Reflexion & Auswertung

Reflexion über die anschliessende Bewertung der Arbeit und der Lösung.
