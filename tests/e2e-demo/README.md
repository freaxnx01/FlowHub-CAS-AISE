# FlowHub headed slow-motion full-cycle demo

A single Playwright test that drives the **AppBar QuickCapture** in a real
Chromium window, then verifies the task landed in Vikunja via its REST API.

Designed to be run **from Win11** against an app instance running on
**claude-dev** at `http://192.168.1.108:5070`. The test does **not** require
the FlowHub repo to be checked out on Win11 in full — just this folder.

## One-time setup (Win11)

Install Node.js 20+ (https://nodejs.org), then in PowerShell:

```powershell
cd path\to\FlowHub-CAS-AISE\tests\e2e-demo
npm install
npx playwright install chromium
```

## Get the Vikunja API token into an env var

The test asserts the new task arrives in Vikunja's `Inbox` project. It needs
a bearer token. From the claude-dev box (where Passbolt CLI is configured):

```bash
passbolt get resource --id e235b4a5-1a1e-49f5-82f4-1effb8d02b77 -j \
  | jq -r .password
```

Copy the value, then on Win11 set it for the current shell:

```powershell
$env:VIKUNJA_API_TOKEN = "tk_xxxxxxxxxxxxxxxxxxxxxxxxxxxx"
```

## Run

```powershell
npm run test:demo                       # headed Chromium, slow-mo 500ms
```

Or with custom pacing / target:

```powershell
$env:SLOW_MO = "1000"                   # 1s between actions
$env:BASE_URL = "http://192.168.1.108:5070"
npm run test:demo
```

After the run, `playwright-report/index.html` and `test-results/<test>/`
contain trace, video, and screenshot artefacts.

## What it does

1. Snapshot Vikunja `Inbox` task titles.
2. Open the FlowHub root page in a fresh headed Chromium.
3. Click into the AppBar QuickCapture field, type a unique
   `todo: e2e-demo …` line, press **Enter**.
4. Wait for the green "Captured ✓" snackbar (proves Blazor Server round-trip
   completed — covers the bug we hit during manual testing on 2026-05-17
   where keyboard events sometimes didn't dispatch).
5. Poll Vikunja's API for ~30s until a task with the exact captured title
   appears, then assert it does.

## Skipping when no token is set

If `VIKUNJA_API_TOKEN` is unset, the test self-skips with a clear message
rather than failing on auth. Lets you run the UI half of the demo without
Passbolt access.
