Sync local AI agent configuration files with the latest versions from the upstream template repo.

Context: $ARGUMENTS

## Steps

Follow the instructions in `.ai/skills/update-ai-instructions.md` to:

1. Fetch all AI instruction files from `https://github.com/freaxnx01/dotnet-ai-instructions` (main branch)
2. Compare each file with the local version and report a summary
3. Handle CLAUDE.md carefully — it has local customizations that must be preserved
4. Apply updates with user confirmation
5. Report what changed
