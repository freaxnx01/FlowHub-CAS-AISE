# Vault ↔ Repo Link Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a discoverable, read-only reference link between this repo and the CAS Obsidian vault — repo points at vault, vault points back at repo.

**Architecture:** Documentation-only. Two files touched: a new section in this repo's `CLAUDE.md`, and a new page `Projektarbeit/Repository.md` in the vault. No sync, no symlinks, no automation.

**Tech Stack:** Markdown, git (two repos: this one and the vault repo at `gitlab.freaxnx01.ch/freax/obsidian-cas-aise`).

---

## File Structure

- **Modify:** `/home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/CLAUDE.md` — add new section between line 16 (end of Project Overview) and line 17 (`---`).
- **Create:** `/mnt/c/Users/freax/Documents/Obsidian/cas-aise/Projektarbeit/Repository.md` — new vault page with frontmatter and back-link content.
- **Already exists:** `docs/superpowers/specs/2026-04-09-vault-repo-link-design.md` (committed separately as part of brainstorming).

---

## Pre-flight Constraints

- The vault working tree has **unrelated dirty changes**. The vault commit MUST add the new file by exact path (`git add Projektarbeit/Repository.md`). **Never** use `git add -A` or `git add .` in the vault during this task.
- This repo's working tree is clean at the start of the task (verify with `git status`).
- Both commits use Conventional Commits style. Vault commit uses the `docs(<scope>)` style required by the vault's `CLAUDE.md`.

---

## Task 1: Add vault pointer section to repo `CLAUDE.md`

**Files:**
- Modify: `/home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/CLAUDE.md` (insert after line 16, before line 17)

- [ ] **Step 1: Verify the insertion point**

Run: read lines 9–18 of `CLAUDE.md` and confirm the structure is:
```
## Project Overview
... 5 bullet lines ending with "Course context: ..."

---

## Essential Commands
```

Expected: matches exactly. If not, stop and re-read the file.

- [ ] **Step 2: Insert the new section**

Use Edit tool to replace this exact block:

```
**Course context:** See [`.ai/cas-instructions.md`](.ai/cas-instructions.md) for block schedule, implementation rhythm, and grading criteria

---

## Essential Commands
```

with:

```
**Course context:** See [`.ai/cas-instructions.md`](.ai/cas-instructions.md) for block schedule, implementation rhythm, and grading criteria

---

## Knowledge Base (Obsidian Vault)

The CAS coursework, FlowHub concept, and project decisions live in a separate Obsidian vault. This vault is the source of truth for everything **except** code.

- **Path:** `/mnt/c/Users/freax/Documents/Obsidian/cas-aise/`
- **Mode:** Read-only from this repo. Writes happen only when CWD is the vault (per the global vault convention in `~/.claude/CLAUDE.md`).
- **Back-link:** `Projektarbeit/Repository.md` in the vault points back here.

**Primary focus paths** — grep/read these first when project background is needed:

- `Projektarbeit/` — thesis & FlowHub concept (`Idee FlowHub.md`, `Dev.md`, `Skills.md`, `External Services.md`)
- `Blöcke/` — course block notes that drive the incremental build

**Secondary** (search on demand): `Knowledge/`, `Allgemein/`, `Organisation/`, `Notes.md`, `TODO.md`.

**Read trigger:** Before answering questions about CAS scope, modules, deadlines, project decisions, or the FlowHub concept, grep the vault first.

---

## Essential Commands
```

- [ ] **Step 3: Verify the edit**

Run: read lines 9–40 of `CLAUDE.md` and confirm the new "Knowledge Base (Obsidian Vault)" section is present, the bullets are intact, and the surrounding `---` separators are correct.

- [ ] **Step 4: Commit**

```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE
git add CLAUDE.md docs/superpowers/specs/2026-04-09-vault-repo-link-design.md docs/superpowers/plans/2026-04-09-vault-repo-link.md
git commit -m "$(cat <<'EOF'
docs: link repo to CAS Obsidian vault

Adds a Knowledge Base section in CLAUDE.md pointing future Claude
sessions at the cas-aise Obsidian vault as the source of truth for
course content, FlowHub concept, and project decisions. Read-only
from the repo side. Includes the brainstorming spec and plan.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

Expected: commit succeeds, `git status` shows clean tree.

---

## Task 2: Create back-link page in the vault

**Files:**
- Create: `/mnt/c/Users/freax/Documents/Obsidian/cas-aise/Projektarbeit/Repository.md`

- [ ] **Step 1: Confirm the target path is free**

Run Glob: `Projektarbeit/Repository.md` in `/mnt/c/Users/freax/Documents/Obsidian/cas-aise/`
Expected: no matches (file does not yet exist).

- [ ] **Step 2: Write the new vault page**

Use Write tool to create `/mnt/c/Users/freax/Documents/Obsidian/cas-aise/Projektarbeit/Repository.md` with this exact content:

```markdown
---
tags:
  - claude-generated
updated: 2026-04-09
---

# Repository

Code repository for the **FlowHub** project work — built incrementally across the 5 CAS AISE blocks.

## Locations

- **GitHub:** https://github.com/freaxnx01/FlowHub-CAS-AISE
- **Local clone:** `/home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/`

## Split of concerns

| Lives in the repo | Lives in this vault |
|---|---|
| Source code (`src/`, `tests/`) | FlowHub concept & idea (`Idee FlowHub.md`) |
| Build/CI/Docker | Course block notes (`Blöcke/`) |
| Architecture decisions (`docs/adr/`) | General CAS knowledge (`Knowledge/`) |
| Implementation specs & plans (`docs/superpowers/`) | Organisation, deadlines, admin |

The repo's `CLAUDE.md` has a **Knowledge Base (Obsidian Vault)** section pointing back here.
```

- [ ] **Step 3: Verify the file was written correctly**

Run: read the new file and confirm frontmatter, headings, table, and back-reference are all present and well-formed.

- [ ] **Step 4: Commit only the new file in the vault**

```bash
cd /mnt/c/Users/freax/Documents/Obsidian/cas-aise
git add Projektarbeit/Repository.md
git status --short Projektarbeit/Repository.md
```

Expected output of the second command: `A  Projektarbeit/Repository.md`

If the staged set contains anything other than `Projektarbeit/Repository.md`, **stop and unstage** — the vault has unrelated dirty changes that must not be swept up.

```bash
git -c commit.gpgsign=false commit -m "docs(projektarbeit): add Repository back-link to FlowHub repo"
```

Note: gpgsign is disabled only if signing is not configured for this vault — if signing **is** configured and works, drop the `-c` flag. Verify by running `git config commit.gpgsign` first; if the value is `true`, omit the flag.

Expected: commit succeeds with one file changed.

- [ ] **Step 5: Do NOT push the vault commit**

The vault has its own auto-push convention, but it only fires when CWD is the vault and is invoked by the user's vault-side workflow. Leave the commit local; the user will push it (along with the unrelated dirty changes) next time they're in the vault. Confirm with the user before pushing from this session.

---

## Task 3: Final verification

- [ ] **Step 1: Verify repo state**

```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE
git log --oneline -1
git status
```

Expected: latest commit is "docs: link repo to CAS Obsidian vault", working tree clean.

- [ ] **Step 2: Verify vault state**

```bash
cd /mnt/c/Users/freax/Documents/Obsidian/cas-aise
git log --oneline -1
git status --short Projektarbeit/Repository.md
```

Expected: latest commit is "docs(projektarbeit): add Repository back-link to FlowHub repo", and `git status --short Projektarbeit/Repository.md` is empty (file is committed).

- [ ] **Step 3: Acceptance check against the spec**

Walk through the spec's Acceptance section:

1. ✅ A fresh Claude Code session in this repo can find the vault path from `CLAUDE.md`.
2. ✅ A fresh Claude Code session in the vault can find the repo path and GitHub URL from `Projektarbeit/Repository.md`.
3. ✅ Both changes are committed to their respective git repos.

If any check fails, stop and report.

---

## Self-Review Notes

- **Spec coverage:** Repo→Vault pointer (Task 1), Vault→Repo back-link (Task 2), commits in both repos (Tasks 1 & 2), acceptance walk-through (Task 3). All spec sections covered.
- **Placeholder scan:** None — every step has exact paths, exact commands, and full file content.
- **Type consistency:** N/A (documentation only, no code symbols).
- **Vault safety:** The vault `git add` is constrained to a single explicit path, with a hard stop if anything else is staged. This protects the unrelated dirty work in the vault.
