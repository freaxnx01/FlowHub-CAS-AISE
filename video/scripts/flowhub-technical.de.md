# FlowHub — Erklärvideo (Technisch)

<!-- scene: hook -->
FlowHub ist ein Integrations-Hub, der deine selbst gehosteten Dienste orchestriert — statt dass du sie alle einzeln bedienst.

<!-- scene: architecture -->
Im Kern ein modularer Monolith: getrennte Module für Domäne, KI, Persistenz, Integrationen und das Blazor-Web-Frontend.

<!-- scene: airouting -->
Jeder Capture durchläuft einen KI-Klassifikator auf Basis von Microsoft Extensions AI. Er erkennt die Kategorie und routet an die passende Skill-Integration.

<!-- scene: integrations -->
Über Ports und Adapter sprechen austauschbare Integrationen an: Wallabag für Lesezeichen, Vikunja für Aufgaben, Telegram als Eingangskanal.

<!-- scene: stack -->
Gebaut auf .NET 10 und Blazor, mit Entity Framework Core, OpenTelemetry, Health-Endpoints und einer durchgehenden Testabdeckung.

<!-- scene: close -->
FlowHub — modular, testbar, erweiterbar.
