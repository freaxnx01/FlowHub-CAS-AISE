# Update AI Instructions

Sync local AI agent configuration files with the latest versions from the upstream template repository.

**Source:** `https://github.com/freaxnx01/dotnet-ai-instructions` (branch: `main`)

---

## Files to Sync

These files map 1:1 between the template repo and this project:

| Template path | Local path | Notes |
|---|---|---|
| `.ai/base-instructions.md` | `.ai/base-instructions.md` | Canonical conventions — source of truth |
| `.ai/skills/commit.md` | `.ai/skills/commit.md` | |
| `.ai/skills/push.md` | `.ai/skills/push.md` | |
| `.ai/skills/ui-brainstorm.md` | `.ai/skills/ui-brainstorm.md` | |
| `.ai/skills/ui-build.md` | `.ai/skills/ui-build.md` | |
| `.ai/skills/ui-flow.md` | `.ai/skills/ui-flow.md` | |
| `.ai/skills/ui-review.md` | `.ai/skills/ui-review.md` | |
| `.claude/commands/commit.md` | `.claude/commands/commit.md` | |
| `.claude/commands/push.md` | `.claude/commands/push.md` | |
| `.claude/commands/ui-brainstorm.md` | `.claude/commands/ui-brainstorm.md` | |
| `.claude/commands/ui-build.md` | `.claude/commands/ui-build.md` | |
| `.claude/commands/ui-flow.md` | `.claude/commands/ui-flow.md` | |
| `.claude/commands/ui-review.md` | `.claude/commands/ui-review.md` | |
| `.github/copilot-instructions.md` | `.github/copilot-instructions.md` | |
| `SKILL.md` | `SKILL.md` | OpenClaw skill definition |
| `CLAUDE.md` | `CLAUDE.md` | **Has local customizations — merge carefully** |

## Steps

### Step 1 — Fetch upstream files

For each file in the table above, fetch the raw content from GitHub:

```
https://raw.githubusercontent.com/freaxnx01/dotnet-ai-instructions/main/<path>
```

Use `WebFetch` or `curl` to download each file. Fetch all files in parallel where possible.

### Step 2 — Compare and categorize

For each file, compare the fetched content with the local version. Categorize into:

- **Identical** — no changes needed
- **Updated upstream** — template has changes the local file doesn't
- **Missing locally** — template has a file that doesn't exist locally yet (new skill or command added to template)

Report a summary to the user:
```
Sync summary:
  ✓ 12 files already up to date
  ↑ 3 files have upstream changes: .ai/base-instructions.md, .ai/skills/commit.md, SKILL.md
  + 1 new file: .ai/skills/new-skill.md
```

If all files are identical, say so and stop.

### Step 3 — Handle CLAUDE.md separately

`CLAUDE.md` contains project-specific sections (project name, purpose, environment variables, etc.) that are filled in locally. Do NOT overwrite it blindly.

If CLAUDE.md has upstream changes:
1. Show a diff of the structural/template changes (new sections, updated commands, changed conventions)
2. Identify which parts are template boilerplate vs. local customizations
3. Ask the user whether to:
   - **Merge** — apply only the structural changes while preserving local content
   - **Overwrite** — replace entirely with template version (user will re-fill customizations)
   - **Skip** — leave CLAUDE.md as-is

### Step 4 — Apply updates

For all non-CLAUDE.md files with upstream changes:
1. Show the list of files that will be updated
2. Ask the user to confirm before overwriting
3. Write each updated file using the fetched content

For any new files (missing locally), create them.

### Step 5 — Summary

Report what was updated:
```
Updated 3 files from dotnet-ai-instructions@main:
  .ai/base-instructions.md
  .ai/skills/commit.md
  SKILL.md
```

Suggest the user review the changes with `git diff` and commit when satisfied.
