# Release Notes

User-friendly summary of changes in each version.

---

## Version 0.2.0

_Released 2026-06-20_

This release grows FlowHub from two live destinations to three, adds document archiving, and makes the public demo richer and easier to explore.

### New Features
- **Document archiving** — captures with an attached file are now sent to paperless-ngx, joining Wallabag (read-later) and Vikunja (tasks) as a third live integration.
- **File-upload API** — submit a capture with an attached file directly through the REST API.
- **Quote captures** — quotes are recognised, enriched with author and source details, and filed onto a dedicated "Zitate" board in Vikunja.
- **Classification insights** — open a capture to see how it was classified, including the model used and an estimated cost (available on the demo).
- **Try-me examples** — one-click example chips in Quick Capture let you see routing in action without thinking up your own input.
- **Live status page** — an uptime monitor lets you check the health of the demo services at a glance.
- **Explainer & walkthrough videos** — short videos walk you through what FlowHub does and how the live demo works.

### Improvements
- The public demo now exposes all three live services with quick-access links and a writable inbox board you can interact with directly.
- Captured links are enriched with title and source information automatically.
- More resilient demo: self-healing service restarts and added monitoring keep the live demo available.

### Bug Fixes
- Quick-Capture controls are now legible and easy to tap on the dark app bar.
- Captures that couldn't be matched now read clearly instead of looking like an error.
- More reliable link saving to Wallabag, including automatic token refresh.
- Various demo stability fixes for file uploads, service startup, and scheduled resets.

---

## Version 0.1.0

_Released 2026-06-05_

The first public release of **FlowHub** — a personal capture hub that takes whatever you throw at it (a link, a note, or a file) and automatically files it in the right place.

### New Features
- **Quick Capture** — drop in a URL, a snippet of text, or a small file from a single field and FlowHub takes it from there.
- **Smart routing** — captures are automatically classified and sent to the right destination, with a clear title and tags filled in for you.
- **Read-later integration** — articles and links are saved to your Wallabag reading list.
- **Task & project integration** — to-dos and project items are created in Vikunja and sorted into the right project.
- **File uploads** — attach a file to a capture (demo limit 2 MB) for later document handling.
- **Dashboard & overview pages** — see recent captures at a glance, browse the full capture list with filters, and open any capture for full detail and status.
- **Lifecycle tracking** — every capture shows where it is in the journey (submitted → classified → routed → completed), and anything that couldn't be matched stays visible in your inbox instead of disappearing.
- **REST API** — submit, list, retrieve, and retry captures programmatically.

### Improvements
- A live public demo with automatic reset every 15 minutes so you can try FlowHub without setup.
- Refreshed branding with a new logo and browser-tab icon.
- Health and status endpoints for reliable monitoring.

---
