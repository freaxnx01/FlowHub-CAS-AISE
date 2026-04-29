---
tags:
  - claude-generated
  - claude-updated
updated: 2026-04-29
---

# Repository

Code and notes for the **FlowHub** project work — built incrementally across the 5 CAS AISE blocks. As of 2026-04-29 the vault is part of the FlowHub repo (subtree-merged from the retired `gitlab.freaxnx01.ch/freax/obsidian-cas-aise`); code and vault now share one git history.

## Locations

- **GitHub:** https://github.com/freaxnx01/FlowHub-CAS-AISE
- **Local clone:** `/home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/`
- **Vault path within repo:** `vault/`

## Split of concerns

Code and vault live in the same repo but stay in their own top-level folders. Edits are scoped per commit (`docs(vault/...)` for vault-only changes, normal Conventional Commits for code).

| Lives in the repo root | Lives in `vault/` |
|---|---|
| Source code (`source/`, `tests/`) | FlowHub concept & idea (`Projektarbeit/Idee FlowHub.md`) |
| Build/CI/Docker | Course block notes (`Blöcke/`) |
| Architecture decisions (`docs/adr/`) | General CAS knowledge (`Knowledge/`) |
| Implementation specs & plans (`docs/superpowers/`) | Organisation, deadlines, admin (`Organisation/`) |

The repo's top-level `CLAUDE.md` has a **Knowledge Base (Obsidian Vault)** section pointing here.

## See also

- [[Glossary]] — FlowHub project vocabulary (Capture, Skill, Channel, Integration, …) — **read this before naming anything in code or thesis**.
