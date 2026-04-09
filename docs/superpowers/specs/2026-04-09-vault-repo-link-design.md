# Vault ↔ Repo Connection — Design

**Date:** 2026-04-09
**Status:** Approved
**Type:** Reference-only link (no sync, no symlinks)

## Goal

Make the CAS Obsidian vault (`/mnt/c/Users/freax/Documents/Obsidian/cas-aise/`) discoverable from this repo, and make this repo discoverable from the vault, so that any future Claude Code session in either CWD knows where the other half of the project lives.

## Non-Goals

- No file sync, copy, or symlink between the two locations.
- No changes to `.ai/cas-instructions.md` (course rhythm stays as-is).
- No automation or hooks — the link is documentation, not infrastructure.

## Design

### Repo → Vault pointer

Add a new section **"Knowledge base (Obsidian vault)"** to `CLAUDE.md`, placed immediately after the **Project Overview** section. The section states:

- **Path:** `/mnt/c/Users/freax/Documents/Obsidian/cas-aise/`
- **Mode:** Read-only from this repo. Writes happen only when CWD is the vault (per the global vault convention in `~/.claude/CLAUDE.md`).
- **Primary focus paths** (grep/read first when project background is needed):
  - `Projektarbeit/` — thesis & FlowHub concept (`Idee FlowHub.md`, `Dev.md`, `Skills.md`, `External Services.md`)
  - `Blöcke/` — course block notes that drive the incremental build
- **Secondary** (search on demand): rest of the vault (`Knowledge/`, `Allgemein/`, `Organisation/`, `Notes.md`, `TODO.md`).
- **Read trigger:** Before answering questions about CAS scope, modules, deadlines, project decisions, or the FlowHub concept, grep the vault first.

### Vault → Repo back-link

Create a new page `Projektarbeit/Repository.md` in the vault with:

- YAML frontmatter per vault convention: `tags: [claude-generated]`, `updated: 2026-04-09`.
- Local clone path: `/home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/`
- GitHub URL: `https://github.com/freaxnx01/FlowHub-CAS-AISE`
- One-line description: code repository for the FlowHub project work, built incrementally across the 5 CAS blocks.
- Note that code lives only in the repo; the vault holds concept, decisions, and course notes.

### Commits

- Repo: one commit on `main` editing `CLAUDE.md` and adding this spec file.
- Vault: one commit on the vault repo (`gitlab.freaxnx01.ch/freax/obsidian-cas-aise`) adding `Projektarbeit/Repository.md`, following the vault's `docs(<scope>): <description>` convention. Cross-CWD commit performed in this session.

## Acceptance

- A fresh Claude Code session started in this repo can find the vault path from `CLAUDE.md` without prompting.
- A fresh Claude Code session started in the vault can find the repo path and GitHub URL from `Projektarbeit/Repository.md`.
- Both changes are committed to their respective git repos.
