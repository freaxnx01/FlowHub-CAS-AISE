Single entry point for all FlowHub CC-skills. Classifies input and routes to the right sub-skill.

Input: $ARGUMENTS

Follow the canonical skill body in `.ai/skills/flowhub.md` exactly.

## Routing (first match wins)

1. **Explicit sub-command:** input starts with `triage`, `issue`, or `capture` → route to that sub-skill
2. **Repo name match:** a word in the input matches a repo in `~/projects/repos/` → route to `/flowhub-issue`
3. **Default:** route to `/flowhub-capture` (Vikunja inbox)

## Sub-skills

| Command | Skill file | Purpose |
|---|---|---|
| `/flowhub-capture` | `.ai/skills/flowhub-capture.md` | Capture → Vikunja Inbox |
| `/flowhub-triage` | `.ai/skills/flowhub-triage.md` | Triage Inbox → classify & move |
| `/flowhub-issue` | `.ai/skills/flowhub-issue.md` | Capture → repo issue on forge |

## Rules

- Always print which sub-skill is being routed to before following its steps.
- No API calls for classification — only local directory scan + string matching.
- If `$ARGUMENTS` is empty → print help listing sub-commands.
- After routing, follow the target skill's canonical body exactly.
