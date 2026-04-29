# Obsidian Vault: cas-aise

This directory **is** the project. It is the Obsidian vault for the CAS AI/SE coursework (Certificate of Advanced Studies — AI & Software Engineering), backed by git repo `gitlab.freaxnx01.ch/freax/obsidian-cas-aise`.

When Claude is started with this directory as CWD, the vault is the workspace: read, create, and edit pages as the primary task.

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

After creating or editing pages, automatically commit and push:

```bash
cd /mnt/c/Users/freax/Documents/Obsidian/cas-aise
git add -A
git commit -m "docs(<scope>): <description>"
git push
```

Conventional commit style with `docs()` prefix. Scope = folder or topic name (e.g. `docs(knowledge): add UML sequence diagram notes`).
