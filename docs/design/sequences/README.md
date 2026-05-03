# Sequence Diagrams — Capture Pipeline

This folder contains Mermaid sequence diagrams for the two async pipeline flows in FlowHub.
They document the **Verhalten** dimension of the Block-3 Nachbereitung rubric item
*"Struktur / Verhalten / Interaktion — Sequence-/Activity-Diagramme"*.

## Diagrams

| File | Flows documented |
|---|---|
| [`capture-enrichment.md`](capture-enrichment.md) | A. AI classifier success path · B. AI fallback to `KeywordClassifier` |
| [`skill-routing.md`](skill-routing.md) | A. Integration write success · B. Retry exhaustion → `LifecycleFaultObserver` |

## Reading guide

Read `capture-enrichment.md` first — it covers the first pipeline hop (`CaptureCreated →
CaptureEnrichmentConsumer → CaptureClassified`) and explains the `IClassifier` port and its
Slice-C AI adapter. Then read `skill-routing.md` for the second hop (`CaptureClassified →
SkillRoutingConsumer → Routed | Unhandled`) including the fault-observer recovery path.

Both files include invariant callouts that complement the structural view in
[ADR 0003](../../adr/0003-async-pipeline.md) (retry policies, fault routing, lifecycle stages)
and [ADR 0004](../../adr/0004-ai-integration-in-services.md) (AI fallback semantics, provider
abstraction). The Slice-C AI adapter design is specified in
[`docs/superpowers/specs/2026-05-03-slice-c-ai-integration-design.md`](../../superpowers/specs/2026-05-03-slice-c-ai-integration-design.md).
