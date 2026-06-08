# Moderne Applikationskonzepte — Quarkus / Jakarta-EE-Kriterium im .NET-Mapping

> **Bezug:** Bewertungskriterium *„Wurden die Konzepte von Quarkus, Jakarta EE und
> modernen Java-Applikationen berücksichtigt"* (max. 10 Pkt., Skala 0 / 3 / 7 / 10).

## Einordnung (ehrlich)

FlowHub ist bewusst in **.NET 10 / ASP.NET Core** umgesetzt (Stack-Entscheid: siehe
ADR 0001 und Projektbeschreibung). Die **Java-spezifischen** Ausprägungen des
Kriteriums — die Quarkus-Runtime, CDI-Annotationen, GraalVM-Build-time-DI — sind
damit **nicht anwendbar** und werden nicht behauptet.

Das Kriterium zielt jedoch ausdrücklich auch auf die **Konzepte moderner
Applikationen**. Genau diese Konzepte sind in FlowHub vollständig vorhanden — in
der .NET-äquivalenten Ausprägung. Die folgende Tabelle bildet jedes Konzept auf
sein FlowHub-Äquivalent ab, mit konkretem Nachweis. Sie dient als Grundlage für
eine etwaige **Teilbewertung** dieses Kriteriums; die konservative Punktebasis
bleibt 90 (Kriterium ausgeklammert).

## Konzept-Mapping Jakarta EE / Quarkus → FlowHub (.NET)

| Jakarta-EE / Quarkus / MicroProfile-Konzept | Zweck | FlowHub .NET-Äquivalent | Nachweis |
|---|---|---|---|
| **CDI** (Dependency Injection) | Lose Kopplung, Inversion of Control | Eingebaute DI (`IServiceCollection`), pro Modul registriert via `*ServiceCollectionExtensions` | `source/FlowHub.Web/Program.cs`; `FlowHub.Api/ServiceCollectionExtensions.cs`, `FlowHub.AI/AiServiceCollectionExtensions.cs`, `FlowHub.Skills/SkillsServiceCollectionExtensions.cs` |
| **JAX-RS / RESTEasy Reactive** | Deklarative REST-Endpunkte | Minimal API + `ProblemDetails` (RFC 9457) + OpenAPI/Scalar | `source/FlowHub.Api/Endpoints/*`; ADR 0001 |
| **Bean Validation** (Jakarta Validation) | Eingabevalidierung am Rand | FluentValidation am Boundary, Domäne bleibt rein | `source/FlowHub.Api/Validation/CreateCaptureRequestValidator.cs` |
| **JPA / Hibernate ORM** | Objekt-relationales Mapping | EF Core 10 + Repository-Pattern + `IEntityTypeConfiguration<T>` (statt Annotationen) | `source/FlowHub.Persistence/` (6 Repositories, `Entities/*EntityTypeConfiguration.cs`); ADR 0005 |
| **MicroProfile Config** | Externalisierte Konfiguration (12-Factor) | `IConfiguration` + Options-Pattern + Umgebungsvariablen, keine Secrets im Code | `appsettings.json` + `*Options.cs`; NfA-Doku, 12-Factor-Abschnitt |
| **MicroProfile Health** | Liveness/Readiness-Probes | ASP.NET Health Checks (`/health/live`) für den Docker-Healthcheck | `source/FlowHub.Web/Program.cs`; NfA-D3 |
| **MicroProfile Metrics / OpenTelemetry** | Observability (Metriken, Traces) | OpenTelemetry + Prometheus-Export (`/metrics`), Grafana-Dashboards | `Program.cs`; ADR 0009; NfA-O1 |
| **Mutiny / Reactive** | Nicht-blockierende Verarbeitung | Durchgängig `async`/`await`, `Task`-basiert, `CancellationToken` an externen Calls | gesamtes `source/` |
| **SmallRye Reactive Messaging / Kafka** | Asynchrone, ereignisbasierte Kommunikation | MassTransit (In-Memory in Dev/Test, RabbitMQ in Prod), 2 Events / 5 Consumer | ADR 0003; `source/FlowHub.Web/Pipeline/*` |
| **MicroProfile Fault Tolerance** (Retry, Fallback, Timeout) | Resilienz | MassTransit Retry-Policies pro Consumer + deterministischer Klassifikator-Fallback (`AiClassifier`→`KeywordClassifier`, `EventId 3010`) + Cost-Guards | ADR 0003 §5; ADR 0004 |
| **GraalVM Native Image** | Schlanke, schnell startende Container | Multi-Stage-Build auf `aspnet:10.0-alpine`, getrimmtes Self-Contained-Publish, non-root | `Dockerfile`; NfA-D1/D2/D3 |
| **Testcontainers** (Sprach-übergreifend) | Integrationstests gegen echte Backing-Services | Testcontainers .NET gegen echtes PostgreSQL | `tests/FlowHub.Persistence.Tests/`; Testing-Strategie |
| **Maven / Gradle** | Reproduzierbarer Build | .NET SDK (`global.json`-Pin), `FlowHub.slnx`, zentrale Paketverwaltung, Warnings-as-Errors | `Directory.Build.props`, `Directory.Packages.props` |
| **Quarkus Dev Mode** (Live Reload) | Schneller Inner-Dev-Loop | `dotnet watch` (`just watch`) mit Hot Reload | `justfile` |

## Selbsteinschätzung

- **Java-/Quarkus-spezifische Laufzeitkonzepte** (CDI-Annotationen, Quarkus
  Build-time-DI, GraalVM-AOT-Compilation in der Quarkus-Toolchain): **nicht
  verwendet** — Stack ist .NET. Hierfür wird kein Punkt beansprucht.
- **Konzepte moderner Applikationen** (DI, deklaratives REST, ORM-Abstraktion,
  externalisierte Konfiguration, Health/Metrics-Observability, asynchrone &
  ereignisbasierte Kommunikation, Fehlertoleranz, schlanke Container, Testbarkeit):
  **vollständig und nachgewiesen** in der .NET-äquivalenten Form (Tabelle oben).

Daraus ergibt sich eine ehrliche Grundlage für eine **Teilbewertung** (Stufe
*„teilweise/überwiegend"*) dieses Kriteriums, ohne eine Java-Nutzung zu behaupten.
Wird das Kriterium vom Prüfer strikt Java-gebunden ausgelegt, bleibt die
konservative Bewertung bei 90 erreichbaren Punkten (Kriterium ausgeklammert) — die
übrigen Punkte sind davon unberührt.
