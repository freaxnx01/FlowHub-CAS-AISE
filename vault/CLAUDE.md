# Obsidian Vault: cas-aise

This directory is the Obsidian vault for the CAS AI/SE coursework (Certificate of Advanced Studies — AI & Software Engineering). It used to be a standalone git repo at `gitlab.freaxnx01.ch/freax/obsidian-cas-aise`, but has been merged into the FlowHub project repo (`github.com/freaxnx01/FlowHub-CAS-AISE`) as a git subtree. Vault edits ship through the FlowHub repo's normal commit/push workflow.

Within this folder, treat the workspace as the vault: read, create, and edit pages following the conventions below.

## Structure

```
Allgemein/       # General coursework notes
Blöcke/          # Course blocks / modules
Knowledge/       # Distilled knowledge (Software Architecture, UML, Akronyme, …)
Organisation/    # Admin (Anmeldung, Termine, Kosten, Zertifikate)
Projektarbeit/   # Thesis / project work (FlowHub idea, Dev, Skills, …)
_files/          # Attachments (PDFs, docs)
_images/         # Image attachments
_misc/           # Miscellaneous
Notes.md         # Scratchpad
TODO.md          # Open todos
```

Folders prefixed with `_` are asset/support folders — don't create top-level notes there.

## Read triggers

- Before answering any question about the CAS coursework, modules, project, or related topics, grep/glob the vault first.
- When the user references a module, lecturer, deadline, or concept, look for an existing page before assuming.

## Write triggers

- **New topic / concept** → create in `Knowledge/<Topic>.md`
- **Module or block content** → `Blöcke/<Block>.md` or sub-folder
- **Admin / organisation change** → update `Organisation/`
- **Project work (thesis)** → `Projektarbeit/`
- **Open task** → add to `TODO.md`

Prefer updating an existing page over creating a new one.

## Tagging convention

Pages created or updated by Claude Code get YAML frontmatter:

```yaml
---
tags:
  - claude-generated    # for new pages created by Claude
  - claude-updated      # for existing pages edited by Claude
updated: YYYY-MM-DD     # date of last Claude edit
---
```

Do NOT add frontmatter to pages that are only being read, not modified.

## Auto-commit

After creating or editing vault pages, commit and push from the FlowHub repo root:

```bash
cd /home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE
git add vault/
git commit -m "docs(vault/<scope>): <description>"
git push
```

Conventional commit style with `docs()` prefix and a `vault/` scope prefix to keep vault-only edits visible in the log. Inner scope = folder or topic name (e.g. `docs(vault/knowledge): add UML sequence diagram notes`).
