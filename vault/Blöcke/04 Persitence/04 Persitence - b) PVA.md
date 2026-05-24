---
tags:
  - claude-updated
updated: 2026-05-24
---

## Recherche

## Feedback / Notizen aus PVA 4 (2026-05-23)

- [x] **Capture-Eingabe erweitern:** Nicht nur Freitext erlauben, sondern auch *Datei-Browse / Upload* — z. B. als Input für eine künftige **paperless-ngx**-Skill/-Integration (Dokumente direkt aus FlowHub einreichen). *(implementiert in `worktree-upload`; Spec & Plan unter `docs/superpowers/specs/2026-05-24-capture-file-upload-design.md` bzw. `…/plans/2026-05-24-capture-file-upload.md`)*
- [x] **Upload-Limit:** Maximale Datei-Grösse für das Demo-Environment auf **2 MB** setzen (Konfiguration über `appsettings` / ENV-Var, Validierung am Boundary). *(implementiert: `FlowHub:Uploads:MaxBytes=2097152` in `appsettings.json`, override über `FlowHub__Uploads__MaxBytes`, Server-Validation in `EfCaptureService.SubmitAsync` + Kestrel/FormOptions limits)*
