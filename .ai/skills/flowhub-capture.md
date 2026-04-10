# flowhub-capture — Slash Command

Capture a free-form input (URL or text) into the user's Vikunja Inbox.

**Input:** $ARGUMENTS

A single positional value. Either a URL (`^https?://`) or arbitrary text. Quote it if it contains spaces.

> **Sibling skill:** `/flowhub-triage` reads the inbox and proposes target projects. This skill only writes; it never classifies or moves.

---

## Steps

### Step 1 — Resolve credentials (Passbolt → Vikunja token)

Retrieve the Passbolt master password from your memory file `passbolt-password.md` (do **not** ask the user). Then shell out to the `passbolt` CLI to fetch the Vikunja API token. Substitute the password directly into the command — never write it to a file or pass it via env var into a logged child process.

```bash
PASSBOLT_PASSWORD='<from memory: passbolt-password.md>'
VIKUNJA_TOKEN=$(passbolt get resource \
  --id c9e732ce-7737-49a7-9879-dd81258083af \
  --serverAddress "https://passbolt.home.freaxnx01.ch" \
  --userPrivateKeyFile ~/.config/passbolt/private.asc \
  --userPassword "$PASSBOLT_PASSWORD" \
  --mfaMode none 2>&1 | awk -F': ' '/Password/ {print $2}')
```

The token must be a non-empty string. If empty, stop and report "Could not retrieve Vikunja API token from Passbolt."

### Step 2 — Resolve Inbox project id

```bash
INBOX_ID=$(curl -s -H "Authorization: Bearer $VIKUNJA_TOKEN" \
  https://todo.home.freaxnx01.ch/api/v1/user \
  | python3 -c 'import json,sys; print(json.load(sys.stdin)["settings"]["default_project_id"])')
```

If `INBOX_ID` is empty or non-numeric, stop and report the failure with the raw response.

### Step 3 — Detect input type and build payload

Inspect `$ARGUMENTS`:

- **If it matches `^https?://`** (URL mode):
  1. Use the WebFetch tool to retrieve the URL with the prompt: *"Return only the page title from the `<title>` element or `og:title` meta. No commentary."*
  2. `title` = the returned page title, trimmed. If WebFetch fails or returns nothing, fall back to the URL itself as the title.
  3. `description` = the URL.
- **Else** (text mode):
  1. `title` = the input, trimmed; if longer than 60 characters, truncate to 60 with a trailing `…`.
  2. `description` = the full input *only if* it's longer than 60 characters; otherwise omit.

### Step 4 — Create the task in Vikunja Inbox

Build a JSON body with `title` and (if present) `description`. Use `python3 -c 'import json,sys; ...'` to construct the JSON safely so quotes/backslashes inside the input are escaped — never `printf`/heredoc string interpolation.

```bash
BODY=$(python3 -c 'import json,sys; print(json.dumps({"title": sys.argv[1], "description": sys.argv[2]}))' "$TITLE" "$DESCRIPTION")

curl -s -w "\n__HTTP_%{http_code}__" \
  -X PUT \
  -H "Authorization: Bearer $VIKUNJA_TOKEN" \
  -H "Content-Type: application/json" \
  -d "$BODY" \
  "https://todo.home.freaxnx01.ch/api/v1/projects/$INBOX_ID/tasks"
```

If the trailing `__HTTP_<code>__` is not `200`, stop and print the response body for the user.

### Step 5 — Confirm to the user

Print exactly one line:

```
captured: #<task_id> <title>  →  Inbox
```

Use the `id` field from the create response.

---

## Rules

- **Never** write the Passbolt password or the Vikunja token to a file, log, env file, or shell history hint.
- **Never** create a task outside the Inbox — this skill is the "raw arrival" stage; classification is the triage skill's job.
- **Never** delete or modify any other task as part of this skill.
- If `$ARGUMENTS` is empty, print: `usage: /flowhub-capture <url-or-text>` and stop.
- If WebFetch is unavailable, fall back to the URL as the title — do not abort.
