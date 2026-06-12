# Moderne Applikationskonzepte — Nachweis im .NET-Framework

> **Bezug:** Bewertungskriterium *„Wurden die Konzepte des gewählten Frameworks und
> moderner Applikationsentwicklung sachgerecht eingesetzt (z. B. Dependency
> Injection, REST-Schnittstellen, Konfiguration, Fehlerbehandlung)"* (max. 10 Pkt.,
> Skala 0 / 3 / 7 / 10). *(Rubrik-Update Juni 2026 — vorher Quarkus-/Jakarta-EE-spezifisch.)*

## Einordnung

Das Kriterium ist **framework-neutral**: das **gewählte Framework** ist hier
.NET 10 / ASP.NET Core (Stack-Entscheid: ADR 0001; freie Stackwahl in der PVA
bestätigt). Es ist damit **direkt erfüllt** — die ausdrücklich genannten Konzepte
(Dependency Injection, REST-Schnittstellen, Konfiguration, Fehlerbehandlung) sowie
die weiteren Konzepte moderner Enterprise-Applikationen (ORM-Abstraktion,
Health-/Metrics-Observability, asynchrone & ereignisbasierte Kommunikation,
Resilienz, schlanke Container, Testbarkeit) sind in FlowHub vorhanden und im Code
nachgewiesen.

Die folgende Tabelle weist jedes Konzept an konkreten Stellen nach. Die linke
Spalte nennt zusätzlich die jeweilige Jakarta-EE-/Quarkus-Entsprechung — nicht weil
sie behauptet würde, sondern als gängige Bezeichnung des Konzepts (Referenz-Stack
des Moduls).

Ehrlich abgegrenzt: die rein **Java-/Quarkus-runtime-spezifischen** Mechanismen
(CDI-Annotationen, Quarkus-Build-time-DI, echtes GraalVM-Native-Image) werden
**nicht** verwendet und nicht behauptet — sie sind für jeden Nicht-JVM-Stack
gegenstandslos.

## Konzept-Mapping Jakarta EE / Quarkus → FlowHub (.NET)

| Jakarta-EE / Quarkus / MicroProfile-Konzept | Zweck | FlowHub .NET-Äquivalent | Nachweis |
|---|---|---|---|
| **CDI** (Dependency Injection) | Lose Kopplung, Inversion of Control | Eingebaute DI (`IServiceCollection`), pro Modul registriert via `*ServiceCollectionExtensions` | `source/FlowHub.Web/Program.cs`; `FlowHub.Api/ServiceCollectionExtensions.cs`, `FlowHub.AI/AiServiceCollectionExtensions.cs`, `FlowHub.Skills/SkillsServiceCollectionExtensions.cs` |
| **JAX-RS / RESTEasy Reactive** | Deklarative REST-Endpunkte | Minimal API + `ProblemDetails` (RFC 9457) + OpenAPI/Scalar | `source/FlowHub.Api/Endpoints/*`; ADR 0001 |
| **Bean Validation** (Jakarta Validation) | Eingabevalidierung am Rand | FluentValidation am Boundary, Domäne bleibt rein | `source/FlowHub.Api/Validation/CreateCaptureRequestValidator.cs` |
| **JPA / Hibernate ORM** | Objekt-relationales Mapping | EF Core 10 + Repository-Pattern + `IEntityTypeConfiguration<T>` (statt Annotationen) | `source/FlowHub.Persistence/` (6 Repositories, `Entities/*EntityTypeConfiguration.cs`); ADR 0005 |
| **MicroProfile Config** | Externalisierte Konfiguration (12-Factor) | `IConfiguration` + Options-Pattern + Umgebungsvariablen, keine Secrets im Code | `appsettings.json` + `*Options.cs`; NfA-Doku, 12-Factor-Abschnitt |
| **MicroProfile Health** | Liveness/Readiness-Probes | ASP.NET Health Checks (`/health/live`) für den Docker-Healthcheck | `source/FlowHub.Web/Program.cs`; NfA-D3 |
| **MicroProfile Metrics / OpenTelemetry** | Observability (Metriken, Traces) | OpenTelemetry-**Metriken** + Prometheus-Export (`/metrics`, live) + Grafana-Provisioning; Tracing-Exporter geplant (ADR 0004 „As built") | `Program.cs`; NfA-O1 |
| **Mutiny / Reactive** | Nicht-blockierende Verarbeitung | Durchgängig `async`/`await`, `Task`-basiert, `CancellationToken` an externen Calls | gesamtes `source/` |
| **SmallRye Reactive Messaging / Kafka** | Asynchrone, ereignisbasierte Kommunikation | MassTransit (In-Memory in Dev/Test, RabbitMQ in Prod), 2 Events / 5 Consumer | ADR 0003; `source/FlowHub.Web/Pipeline/*` |
| **MicroProfile Fault Tolerance** (Retry, Fallback, Timeout) | Resilienz | MassTransit Retry-Policies pro Consumer + deterministischer Klassifikator-Fallback (`AiClassifier`→`KeywordClassifier`, `EventId 3010`) + Cost-Guards | ADR 0003 §5; ADR 0004 |
| **GraalVM Native Image** | Schlanke, schnell startende Container | Schlanker Multi-Stage-Container auf `aspnet:10.0-alpine`, non-root, schneller Start (NfA-D3) — **kein** echtes Native-Image/AOT (für Blazor Server nicht genutzt; .NET Native AOT wäre die Entsprechung) | `Dockerfile`; NfA-D1/D2/D3 |
| **Testcontainers** (Sprach-übergreifend) | Integrationstests gegen echte Backing-Services | Testcontainers .NET gegen echtes PostgreSQL | `tests/FlowHub.Persistence.Tests/`; Testing-Strategie |
| **Maven / Gradle** | Reproduzierbarer Build | .NET SDK (`global.json`-Pin), `FlowHub.slnx`, zentrale Paketverwaltung, Warnings-as-Errors | `Directory.Build.props`, `Directory.Packages.props` |
| **Quarkus Dev Mode** (Live Reload) | Schneller Inner-Dev-Loop | `dotnet watch` (`just watch`) mit Hot Reload | `justfile` |

## Selbsteinschätzung

Das Kriterium ist framework-neutral und für FlowHub **direkt erfüllt**. Die vier
ausdrücklich genannten Konzepte sind belegt: **Dependency Injection**,
**REST-Schnittstellen**, **Konfiguration**, **Fehlerbehandlung** — ebenso die
weiteren Konzepte moderner Applikationsentwicklung (ORM, Health/Metrics, async &
Messaging, Resilienz, Container, Testbarkeit). Angestrebte Stufe: **„vollständig /
korrekt" (10)**.

Ehrlich abgegrenzt bleibt nur, dass FlowHub **kein echtes GraalVM-Native-Image/AOT**
nutzt (schlanker Alpine-Container statt Native Image) — ein einzelnes
Runtime-Detail, kein Konzept-Defizit. Es gibt **kein ausgeklammertes Item mehr**;
alle **100 Punkte** sind erreichbar.
