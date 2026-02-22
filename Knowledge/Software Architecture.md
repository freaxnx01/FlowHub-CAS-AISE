
## Misc

| Elasticity | The system’s ability to automatically scale resources up or down in response to actual demand, quickly and efficiently, without manual intervention. |
| ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
|            |                                                                                                                                                      |

## 01 Einführung PVA

Architektur

Monolith: Eine Deployment Einheit

Microservice:
- Daten sind beim Service (im Bauch)
- Customer -> REST sync -> Sales: Nach Definition kein Microservice mehr
- Wenn über eine Message Queue (async) ist es Microservice
- Netzwerk dazwischen: nicht deterministisch

Nicht funktionale Anforderungen bestimmen die Architektur

- Sicherheit
- Verfügbarkeit
- ...
