# Hilfsmittelverzeichnis & Eigenständigkeitserklärung

**Modul:** CAS AI-Assisted Software Engineering (AISE) · W4B-C-AS001 · ZH-Sa-1 · FS26
**Projektarbeit:** FlowHub — AI-assisted personal inbox
**Studierender:** Andreas Imboden
**Repository:** <https://github.com/freaxnx01/FlowHub-CAS-AISE>
**Abgabe-Termin:** vor 2026-07-04 24:00

Dieses Dokument ist gemäss FFHS-Richtlinie *"Hinweise und Anforderungen zum Umgang mit generativer künstlicher Intelligenz"* (Stand 01.08.2025) der Projektarbeit beizulegen. Es gehört zwingend zur Abgabe jedes seit dem 1.8.2023 begonnenen unbeaufsichtigten schriftlichen Leistungsnachweises.

---

## 1 — Hilfsmittelverzeichnis

Die folgende Tabelle deklariert sämtliche Hilfsmittel, die bei der Erarbeitung dieser Projektarbeit eingesetzt wurden — für Text, Code und Dokumentation. Eine ausführliche, narrativ-strukturierte Beschreibung des KI-Workflows liegt unter [`docs/ai-usage.md`](../ai-usage.md) im Repository; die per-Block-Reflexion findet sich in den jeweiligen Block-Nachbereitungen unter `vault/Blöcke/`.

| Welches Hilfsmittel wurde eingesetzt? | Wozu wurde das Hilfsmittel eingesetzt? | Betroffene Stellen |
|---|---|---|
| Claude Code (Opus 4.7, 1M context) | Brainstorming, ADR-Entwürfe, Plan-Schreiben, Controller für Subagent-Dispatches, Code-Reviews, Submission Document. | `docs/superpowers/specs/`, `docs/superpowers/plans/`, `docs/adr/`, `docs/spec/`, `docs/insights/`, `docs/ai-usage.md`, `SUBMISSION.md`, grosse Teile von `source/` und `tests/` über Subagent-Dispatches. |
| Claude Sonnet 4.6 (Subagents) | Implementer-, Spec-Reviewer- und Code-Quality-Reviewer-Subagents im `superpowers` subagent-driven-development Workflow. TDD-Ausführung, urteilsintensive Refactorings. | Code-Änderungen in `source/` und `tests/` ab Block 3 — konkret: Async-Pipeline (Slice B), REST-API (Slice A), AI-Classifier (Slice C), EF-Persistenz (Block 4). |
| Claude Haiku 4.5 | Mechanische Tasks im SDD-Workflow — Projekt-Scaffolding, Datei-Moves, repetitive Registrierungen, Dokumentations-Stubs. | Diverse Scaffolding- und Registrierungs-Tasks in Block 3–5; pro-Block-Details in `docs/ai-usage.md`. |
| GitHub Copilot | Inline-Code-Vorschläge während des Editierens in VS Code. | Sparsam — Claude Code steuert ganze Sessions; Copilot nur für kurze Completions ausserhalb dedizierter Sessions. |
| ChatGPT (GPT-4-Familie) | Ad-hoc-Klärung von Konzepten, Quervergleiche, während Claude Code an einem anderen Thema arbeitete. | Punktuelle Cross-Checks; nicht zur Generierung abgegebenen Codes oder Texts eingesetzt. |
| DeepL / Google Translate | Übersetzung einzelner Passagen zwischen Deutsch und Englisch (z. B. Moodle-Originalwortlaut in §2 des Submission Document, deutsche Vault-Notizen, die in englischen Dokumenten referenziert werden). | Ausgewählte DE↔EN-Passagen in `SUBMISSION.md`, `docs/ai-usage.md`, `vault/Projektarbeit/Learnings.md`. |
| Mistral `mistral-embed` (via API) | **Laufzeit-Bestandteil des Produkts** — Embeddings für die FlowHub-Semantic-Search-Funktion (`Captures.Embedding`, ADR 0006). **Nicht** zur Erzeugung von Abgabe-Texten verwendet. | Nur Produktions-Code: `FlowHub.AI` Embedding-Adapter, `Captures`-Tabellen-Spalte. |
| OpenRouter (Gemma free tier) | **Laufzeit-Bestandteil der öffentlichen Demo** — Klassifikations-Fallback, solange das $1-Budget der Demo nicht erschöpft ist. **Nicht** zur Erzeugung von Abgabe-Texten verwendet. | Nur Demo-Umgebung (`demo.flowhub.freaxnx01.ch`). |

**Anteil generiert vs. handgeschrieben (Schätzung):** Für Implementierungs-Code (Block 3–5) ca. 70–80 % KI-erzeugt via Subagent-Workflow, danach manuell reviewed, angepasst oder verworfen. Für Dokumentation (ADRs, Specs, Runbooks, Submission Document) liegt der Anteil ähnlich, jedes Artefakt durchlief jedoch manuelle Nachbearbeitung in Bezug auf inhaltliche Korrektheit und Stil. Pro-Slice-Aufschlüsselung mit konkreten Zahlen in `docs/ai-usage.md`. Alle Architekturentscheidungen, Scope-Entscheidungen und die finale Annahme jedes Artefakts liegen beim Autor.

---

## 2 — Selbständigkeitserklärung (Eigenständigkeitserklärung)

Hiermit erkläre ich,

- dass ich die vorliegende Arbeit selbstständig verfasst habe,
- dass alle sinngemäss und wörtlich übernommenen Textstellen aus fremden Quellen kenntlich gemacht wurden,
- dass alle mit Hilfsmitteln erbrachten Teile der Arbeit präzise deklariert wurden (siehe §1 Hilfsmittelverzeichnis oben),
- dass keine anderen als die im Hilfsmittelverzeichnis aufgeführten Hilfsmittel verwendet wurden,
- dass das Thema, die Arbeit oder Teile davon nicht bereits Gegenstand eines Leistungsnachweises eines anderen Moduls waren, sofern dies nicht ausdrücklich mit der Dozentin oder dem Dozenten im Voraus vereinbart wurde,
- dass ich mir bewusst bin, dass meine Arbeit elektronisch auf Plagiate und auf Drittautorschaft menschlichen oder technischen Ursprungs überprüft werden kann und ich hiermit der FFHS das Nutzungsrecht so weit einräume, wie es für diese Verwaltungshandlungen notwendig ist.

---

**Ort, Datum:** Sisseln, _________________________

**Name:** Andreas Imboden

**Unterschrift:**


_____________________________________________
