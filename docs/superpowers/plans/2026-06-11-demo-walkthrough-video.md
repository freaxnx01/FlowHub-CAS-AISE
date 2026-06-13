# Demo Walkthrough Video Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a silent, screenshot-driven walkthrough video of the live VPS-DE public demo — every sample captured, classified, and landing in its downstream service — with an animated cursor, built in the existing `video/` Remotion subproject.

**Architecture:** Two stages mirroring the existing capture→compose split. A Playwright script (`capture-demo.mjs`) drives the live demo, writes screenshots + a data-driven `manifest.json`, and records each click's target coordinates. A new silent Remotion composition (`demo-walkthrough`) renders those screenshots full-frame with Ken-Burns zoom, caption strips, and an SVG cursor that glides+clicks per the manifest. Pure logic (manifest validation, captions, timeline/cursor math) lives in dependency-free ESM modules unit-tested with Node's built-in test runner; the capture script and the rendered composition are verified by a live dry run + frame inspection.

**Tech Stack:** Node 24 (`node:test`, no extra test dep), Playwright (Chromium), Remotion 4 / React 18 (existing), `just`, ffmpeg (vendored).

---

## File Structure

| File | Responsibility | Action |
|---|---|---|
| `video/package.json` | add `playwright` devDep + `capture:demo` / `render:demo` / `test` scripts | Modify |
| `video/tools/setup.sh` | also `npx playwright install chromium` | Modify |
| `video/src/demoManifest.mjs` | manifest shape + `validateManifest()` (shared by capture script & tests) | Create |
| `video/tools/demoCaptions.mjs` | `captionFor(kind, key)` lookup used by the capture script | Create |
| `video/src/demoTimeline.mjs` | pure `scaleCursor()`, `buildTimeline()`, `cursorAt()` | Create |
| `video/src/demoTimeline.d.ts` | ambient TS types for the `.mjs` helpers (so TSX imports are typed without `@ts-expect-error`) | Create |
| `video/tools/__tests__/demoManifest.test.mjs` | tests for validation | Create |
| `video/tools/__tests__/demoCaptions.test.mjs` | tests for captions | Create |
| `video/tools/__tests__/demoTimeline.test.mjs` | tests for timeline + cursor math | Create |
| `video/tools/capture-demo.mjs` | Playwright capture of the live demo → PNGs + manifest | Create |
| `video/src/components/Cursor.tsx` | animated SVG pointer overlay | Create |
| `video/src/components/DemoShot.tsx` | one screenshot full-frame + Ken-Burns + caption strip | Create |
| `video/src/components/SectionCard.tsx` | section title card | Create |
| `video/src/DemoWalkthrough.tsx` | the composition assembling the timeline | Create |
| `video/src/Root.tsx` | register `demo-walkthrough` | Modify |
| `video/public/demo/*.png` + `manifest.json` | committed capture output (reproducible render) | Create (by capture) |
| `justfile` | `video-capture` recipe; `render:demo` wiring | Modify |
| `video/README.md` | document the demo video flow | Modify |
| `README.md` | third entry in "Explainer videos" | Modify |

---

## Task 1: Tooling scaffold — Playwright dep, scripts, setup, test runner

**Files:**
- Modify: `video/package.json`
- Modify: `video/tools/setup.sh`

- [ ] **Step 1: Add scripts + devDependency to `video/package.json`**

In `"scripts"`, add three entries; in `"devDependencies"`, add `playwright`:

```json
    "test": "node --test tools/__tests__/",
    "capture:demo": "node tools/capture-demo.mjs",
    "render:demo": "remotion render src/index.ts demo-walkthrough out/flowhub-demo.en.mp4",
```
```json
  "devDependencies": {
    "@types/react": "^18.3.0",
    "@types/react-dom": "^18.3.0",
    "playwright": "^1.48.0",
    "typescript": "^5.5.0"
  }
```

- [ ] **Step 2: Install the dep**

Run: `cd video && npm install`
Expected: `playwright` added, no errors.

- [ ] **Step 3: Teach `setup.sh` to install Chromium for Playwright**

In `video/tools/setup.sh`, immediately before the final `echo` block, add:

```bash
# ── Playwright Chromium (for the demo-walkthrough capture script) ─────────────
if [[ -d "$here/../node_modules/playwright" ]]; then
  echo "↓ ensuring Playwright Chromium is installed ..."
  (cd "$here/.." && npx --yes playwright install chromium) >/dev/null 2>&1 \
    && echo "✓ Playwright Chromium ready" \
    || echo "⚠ Playwright Chromium install failed — run 'npx playwright install chromium' in video/ manually"
fi
```

- [ ] **Step 4: Run the Chromium install once**

Run: `cd video && npx playwright install chromium`
Expected: downloads/﹣or﹣confirms Chromium present.

- [ ] **Step 5: Verify the empty test runner wiring**

Run: `cd video && mkdir -p tools/__tests__ && npm test`
Expected: node test runner runs with `tests 0` (no failures). 

- [ ] **Step 6: Commit**

```bash
git add video/package.json video/package-lock.json video/tools/setup.sh
git commit -m "build(video): add Playwright + node:test wiring for the demo walkthrough"
```

---

## Task 2: Manifest schema + validation (TDD)

**Files:**
- Create: `video/src/demoManifest.mjs`
- Test: `video/tools/__tests__/demoManifest.test.mjs`

The manifest is the contract between the capture script and the composition. A `shot.kind` (`context` | `result` | `action`) drives timing; `cursor` is present on `action` shots.

- [ ] **Step 1: Write the failing test**

```js
// video/tools/__tests__/demoManifest.test.mjs
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {validateManifest, SECTIONS, KINDS} from '../../src/demoManifest.mjs';

const good = {
  capturedAt: '2026-06-11T10:00:00Z',
  viewport: {width: 1920, height: 1080},
  shots: [
    {id: 'home', file: 'demo/home.png', section: 'intro', kind: 'context', caption: 'The public demo'},
    {id: 'cap-todo-list', file: 'demo/cap-todo-list.png', section: 'capture', kind: 'result',
     caption: 'AI → todo → Vikunja', sample: 'todo'},
    {id: 'svc-vikunja', file: 'demo/svc-vikunja.png', section: 'service', kind: 'action',
     caption: 'Vikunja board', cursor: {x: 100, y: 200, click: true}},
  ],
};

test('valid manifest passes', () => {
  assert.deepEqual(validateManifest(good), {ok: true, errors: []});
});

test('rejects wrong viewport', () => {
  const m = {...good, viewport: {width: 1280, height: 720}};
  assert.equal(validateManifest(m).ok, false);
});

test('rejects empty shots', () => {
  assert.equal(validateManifest({...good, shots: []}).ok, false);
});

test('rejects bad section/kind and missing caption', () => {
  const m = {...good, shots: [{id: 'x', file: 'f', section: 'nope', kind: 'huh'}]};
  const r = validateManifest(m);
  assert.equal(r.ok, false);
  assert.ok(r.errors.some((e) => e.includes('section')));
  assert.ok(r.errors.some((e) => e.includes('kind')));
  assert.ok(r.errors.some((e) => e.includes('caption')));
});

test('rejects cursor without numeric x/y', () => {
  const m = {...good, shots: [{...good.shots[2], cursor: {x: 'a', y: 1}}]};
  assert.equal(validateManifest(m).ok, false);
});

test('exports the section/kind vocabularies', () => {
  assert.deepEqual(SECTIONS, ['intro', 'capture', 'service', 'outro']);
  assert.deepEqual(KINDS, ['context', 'result', 'action']);
});
```

- [ ] **Step 2: Run it to verify it fails**

Run: `cd video && npm test`
Expected: FAIL — cannot find module `../../src/demoManifest.mjs`.

- [ ] **Step 3: Implement `demoManifest.mjs`**

```js
// video/src/demoManifest.mjs
// Shape of video/public/demo/manifest.json — the contract between
// tools/capture-demo.mjs (writer) and src/DemoWalkthrough.tsx (reader).

export const SECTIONS = ['intro', 'capture', 'service', 'outro'];
export const KINDS = ['context', 'result', 'action'];

/**
 * @param {any} m
 * @returns {{ok: boolean, errors: string[]}}
 */
export function validateManifest(m) {
  const errors = [];
  if (!m || typeof m !== 'object') return {ok: false, errors: ['manifest is not an object']};
  if (!m.viewport || m.viewport.width !== 1920 || m.viewport.height !== 1080) {
    errors.push('viewport must be 1920x1080');
  }
  if (!Array.isArray(m.shots) || m.shots.length === 0) {
    errors.push('shots must be a non-empty array');
    return {ok: false, errors};
  }
  m.shots.forEach((s, i) => {
    if (!s || typeof s !== 'object') return errors.push(`shot[${i}] is not an object`);
    if (!s.id) errors.push(`shot[${i}].id missing`);
    if (!s.file) errors.push(`shot[${i}].file missing`);
    if (!SECTIONS.includes(s.section)) errors.push(`shot[${i}].section invalid: ${s.section}`);
    if (!KINDS.includes(s.kind)) errors.push(`shot[${i}].kind invalid: ${s.kind}`);
    if (typeof s.caption !== 'string' || !s.caption) errors.push(`shot[${i}].caption missing`);
    if (s.cursor !== undefined) {
      const c = s.cursor;
      if (!c || typeof c.x !== 'number' || typeof c.y !== 'number') {
        errors.push(`shot[${i}].cursor x/y must be numbers`);
      }
    }
  });
  return {ok: errors.length === 0, errors};
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `cd video && npm test`
Expected: PASS (all `demoManifest` tests).

- [ ] **Step 5: Commit**

```bash
git add video/src/demoManifest.mjs video/tools/__tests__/demoManifest.test.mjs
git commit -m "feat(video): demo manifest schema + validation"
```

---

## Task 3: Caption lookup (TDD)

**Files:**
- Create: `video/tools/demoCaptions.mjs`
- Test: `video/tools/__tests__/demoCaptions.test.mjs`

Captions are authored at capture time (so they can't drift from the UI). `captionFor` maps a sample key or service name to its caption; unknown keys fall back to a generic string rather than throwing.

- [ ] **Step 1: Write the failing test**

```js
// video/tools/__tests__/demoCaptions.test.mjs
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {captionFor} from '../demoCaptions.mjs';

test('known sample captions', () => {
  assert.equal(captionFor('sample', 'todo'), 'Sample: a to-do');
  assert.equal(captionFor('sample', 'movie'), 'Sample: a movie tip');
  assert.equal(captionFor('sample', 'zitat'), 'Sample: a quote');
  assert.equal(captionFor('sample', 'url'), 'Sample: a link');
});

test('known service captions', () => {
  assert.equal(captionFor('service', 'Vikunja'), 'Lands in Vikunja');
  assert.equal(captionFor('service', 'Zitate'), 'Lands in Vikunja · Zitate');
  assert.equal(captionFor('service', 'Wallabag'), 'Lands in Wallabag');
  assert.equal(captionFor('service', 'paperless-ngx'), 'Lands in paperless-ngx');
});

test('unknown key falls back, never throws', () => {
  assert.equal(captionFor('service', 'Nextcloud'), 'Lands in Nextcloud');
  assert.equal(captionFor('sample', 'weird'), 'Sample');
});
```

- [ ] **Step 2: Run it to verify it fails**

Run: `cd video && npm test`
Expected: FAIL — cannot find module `../demoCaptions.mjs`.

- [ ] **Step 3: Implement `demoCaptions.mjs`**

```js
// video/tools/demoCaptions.mjs
const SAMPLE = {
  movie: 'Sample: a movie tip',
  zitat: 'Sample: a quote',
  todo: 'Sample: a to-do',
  url: 'Sample: a link',
};
const SERVICE = {
  Vikunja: 'Lands in Vikunja',
  Zitate: 'Lands in Vikunja · Zitate',
  Wallabag: 'Lands in Wallabag',
  'paperless-ngx': 'Lands in paperless-ngx',
};

/**
 * @param {'sample'|'service'} kind
 * @param {string} key
 * @returns {string}
 */
export function captionFor(kind, key) {
  if (kind === 'sample') return SAMPLE[key] ?? 'Sample';
  if (kind === 'service') return SERVICE[key] ?? `Lands in ${key}`;
  return key;
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `cd video && npm test`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add video/tools/demoCaptions.mjs video/tools/__tests__/demoCaptions.test.mjs
git commit -m "feat(video): demo caption lookup"
```

---

## Task 4: Timeline + cursor math (TDD)

**Files:**
- Create: `video/src/demoTimeline.mjs`
- Test: `video/tools/__tests__/demoTimeline.test.mjs`

Three pure functions:
- `scaleCursor(cursor, from, to)` — map manifest viewport coords to composition coords.
- `buildTimeline(shots, fps, opts)` — flat scene list with absolute `startFrame`s, inserting a logo intro, section cards on `capture`/`service` entry, and a logo outro. Per-shot duration from `opts` keyed by `kind`. Frames summed (never `round(totalSeconds*fps)`) to avoid tail drift — the lesson learned on the explainer videos.
- `cursorAt(frame, scenes, opts)` — cursor `{x, y, visible, clickT}` at an absolute frame: glides from the previous target (or a rest point) to the current scene's target over `glideFrames`, then a click pulse `clickT` ∈ [0,1] in the scene's last `clickFrames`.

- [ ] **Step 1: Write the failing test**

```js
// video/tools/__tests__/demoTimeline.test.mjs
import {test} from 'node:test';
import assert from 'node:assert/strict';
import {scaleCursor, buildTimeline, cursorAt} from '../../src/demoTimeline.mjs';

const FPS = 30;
const OPTS = {
  intro: 2, outro: 2, section: 1.5,
  context: 2.5, result: 3.5, action: 3, glide: 0.5,
  sectionTitles: {capture: 'Capture an inbox item', service: 'Where it lands'},
};

const shots = [
  {id: 'home', section: 'intro', kind: 'context', caption: 'demo'},
  {id: 'cap-todo-list', section: 'capture', kind: 'result', caption: 'a',
   cursor: {x: 960, y: 540, click: true}},
  {id: 'svc-vikunja', section: 'service', kind: 'result', caption: 'b'},
];

test('scaleCursor maps proportionally and rounds', () => {
  assert.deepEqual(
    scaleCursor({x: 480, y: 270, click: true}, {width: 1920, height: 1080}, {width: 960, height: 540}),
    {x: 240, y: 135, click: true},
  );
});

test('buildTimeline brackets with intro/outro and inserts section cards', () => {
  const tl = buildTimeline(shots, FPS, OPTS);
  const types = tl.map((s) => s.type);
  assert.deepEqual(types, ['intro', 'shot', 'section', 'shot', 'section', 'shot', 'outro']);
  assert.equal(tl.find((s) => s.type === 'section').title, 'Capture an inbox item');
});

test('buildTimeline start frames are contiguous and summed', () => {
  const tl = buildTimeline(shots, FPS, OPTS);
  let cursor = 0;
  for (const s of tl) {
    assert.equal(s.startFrame, cursor);
    assert.ok(s.durationInFrames >= 1);
    cursor += s.durationInFrames;
  }
  // intro(2s) + home context(2.5) + section(1.5) + todo result(3.5) + section(1.5) + vikunja result(3.5) + outro(2)
  assert.equal(cursor, Math.round((2 + 2.5 + 1.5 + 3.5 + 1.5 + 3.5 + 2) * FPS));
});

test('cursorAt rests before first target then reaches target after glide', () => {
  const tl = buildTimeline(shots, FPS, OPTS);
  const target = tl.find((s) => s.cursorTarget);
  const rest = {x: 1700, y: 1000};
  const opts = {glideFrames: Math.round(OPTS.glide * FPS), clickFrames: 10, rest};
  // very first frame → at rest
  const start = cursorAt(0, tl, opts);
  assert.deepEqual({x: start.x, y: start.y}, rest);
  // end of the target scene's glide → at the target
  const atTarget = cursorAt(target.startFrame + opts.glideFrames, tl, opts);
  assert.equal(atTarget.x, target.cursorTarget.x);
  assert.equal(atTarget.y, target.cursorTarget.y);
  // within the last clickFrames of the target scene → clickT > 0
  const clicking = cursorAt(target.startFrame + target.durationInFrames - 1, tl, opts);
  assert.ok(clicking.clickT > 0);
});
```

- [ ] **Step 2: Run it to verify it fails**

Run: `cd video && npm test`
Expected: FAIL — cannot find module `../../src/demoTimeline.mjs`.

- [ ] **Step 3: Implement `demoTimeline.mjs`**

```js
// video/src/demoTimeline.mjs
// Pure layout/timeline math for the demo-walkthrough composition.

/**
 * @param {{x:number,y:number,click?:boolean}} cursor
 * @param {{width:number,height:number}} from
 * @param {{width:number,height:number}} to
 */
export function scaleCursor(cursor, from, to) {
  return {
    x: Math.round((cursor.x * to.width) / from.width),
    y: Math.round((cursor.y * to.height) / from.height),
    click: !!cursor.click,
  };
}

/**
 * Flat scene list with absolute start frames. Scene types:
 *  intro | outro | section (has `title`) | shot (has `shot`, maybe `cursorTarget`).
 * @param {Array} shots manifest shots (already in display order)
 * @param {number} fps
 * @param {{intro:number,outro:number,section:number,context:number,result:number,action:number,glide:number,sectionTitles:Record<string,string>}} opts seconds
 */
export function buildTimeline(shots, fps, opts) {
  const frames = (sec) => Math.max(1, Math.round(sec * fps));
  const scenes = [];
  let start = 0;
  const push = (scene) => {
    scene.startFrame = start;
    start += scene.durationInFrames;
    scenes.push(scene);
  };

  push({type: 'intro', durationInFrames: frames(opts.intro)});

  let prevSection = 'intro';
  for (const shot of shots) {
    if (shot.section !== prevSection && opts.sectionTitles[shot.section]) {
      push({type: 'section', title: opts.sectionTitles[shot.section], durationInFrames: frames(opts.section)});
    }
    prevSection = shot.section;
    const sec = shot.kind === 'result' ? opts.result : shot.kind === 'action' ? opts.action : opts.context;
    const glide = shot.cursor ? opts.glide : 0;
    push({
      type: 'shot',
      shot,
      cursorTarget: shot.cursor ?? null,
      durationInFrames: frames(sec + glide),
    });
  }

  push({type: 'outro', durationInFrames: frames(opts.outro)});
  return scenes;
}

function lerp(a, b, t) {
  return a + (b - a) * t;
}
function clamp01(t) {
  return Math.max(0, Math.min(1, t));
}

/**
 * Cursor position at an absolute frame.
 * @returns {{x:number,y:number,visible:boolean,clickT:number}}
 */
export function cursorAt(frame, scenes, opts) {
  const targets = scenes.filter((s) => s.cursorTarget);
  if (targets.length === 0) return {x: opts.rest.x, y: opts.rest.y, visible: false, clickT: 0};

  // current = the last target scene whose start is <= frame; else none yet.
  let curIdx = -1;
  for (let i = 0; i < targets.length; i++) {
    if (targets[i].startFrame <= frame) curIdx = i;
  }
  if (curIdx === -1) {
    return {x: opts.rest.x, y: opts.rest.y, visible: true, clickT: 0};
  }
  const cur = targets[curIdx];
  const prevPos = curIdx === 0 ? opts.rest : targets[curIdx - 1].cursorTarget;
  const glideStart = cur.startFrame;
  const glideEnd = cur.startFrame + opts.glideFrames;
  const gt = clamp01((frame - glideStart) / Math.max(1, glideEnd - glideStart));
  const x = Math.round(lerp(prevPos.x, cur.cursorTarget.x, gt));
  const y = Math.round(lerp(prevPos.y, cur.cursorTarget.y, gt));

  const clickStart = cur.startFrame + cur.durationInFrames - opts.clickFrames;
  let clickT = 0;
  if (cur.cursorTarget.click && frame >= clickStart) {
    clickT = clamp01((frame - clickStart) / Math.max(1, opts.clickFrames));
  }
  return {x, y, visible: true, clickT};
}
```

- [ ] **Step 4: Add ambient TS types for the `.mjs` helper**

The composition (`.tsx`) imports these helpers. A wildcard ambient module declaration types them regardless of the tsconfig's `moduleResolution`, so the TSX imports stay clean (no `@ts-expect-error`, which would be fragile). Create `video/src/demoTimeline.d.ts`:

```ts
// Ambient types for the plain-ESM helper imported by the TSX composition.
declare module '*demoTimeline.mjs' {
  export interface CursorTarget {
    x: number;
    y: number;
    click: boolean;
  }
  export interface TimelineScene {
    type: 'intro' | 'outro' | 'section' | 'shot';
    startFrame: number;
    durationInFrames: number;
    title?: string;
    shot?: {file: string; caption: string; [k: string]: unknown};
    cursorTarget?: CursorTarget | null;
  }
  export function scaleCursor(
    cursor: CursorTarget,
    from: {width: number; height: number},
    to: {width: number; height: number},
  ): CursorTarget;
  export function buildTimeline(
    shots: Array<Record<string, unknown>>,
    fps: number,
    opts: {
      intro: number; outro: number; section: number;
      context: number; result: number; action: number; glide: number;
      sectionTitles: Record<string, string>;
    },
  ): TimelineScene[];
  export function cursorAt(
    frame: number,
    scenes: Array<{startFrame: number; durationInFrames: number; cursorTarget?: CursorTarget | null}>,
    opts: {glideFrames: number; clickFrames: number; rest: {x: number; y: number}},
  ): {x: number; y: number; visible: boolean; clickT: number};
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `cd video && npm test`
Expected: PASS (all three test files green).

- [ ] **Step 6: Commit**

```bash
git add video/src/demoTimeline.mjs video/src/demoTimeline.d.ts video/tools/__tests__/demoTimeline.test.mjs
git commit -m "feat(video): timeline + cursor math for the demo walkthrough"
```

---

## Task 5: Capture script (live integration)

**Files:**
- Create: `video/tools/capture-demo.mjs`

> This script drives the **live** demo, so exact selectors must be confirmed against the running DOM. Step 1 is a deliberate inspection step; adjust the selector constants in Step 2 if they differ. The script is read-only against production.

- [ ] **Step 1: Inspect the live DOM to confirm selectors**

Run:
```bash
cd video && node -e "
const {chromium} = require('playwright');
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({viewport:{width:1920,height:1080}});
  await p.goto('https://demo.flowhub.freaxnx01.ch', {waitUntil:'networkidle'});
  // example chips
  for (const t of ['Matrix','Zitat','todo','URL'])
    console.log('chip', t, await p.getByRole('button', {name: new RegExp(t,'i')}).count());
  // banner service links
  for (const t of ['Vikunja','Zitate','Wallabag','paperless'])
    console.log('svc', t, await p.getByRole('link', {name: new RegExp(t,'i')}).count(),
                          await p.getByRole('button', {name: new RegExp(t,'i')}).count());
  await b.close();
})();
"
```
Expected: a non-zero count for each chip and for the services the live banner exposes. Note which roles match (MudBlazor renders `MudButton Href=…` as an `<a>` → `link` role). Record the actual accessible names/roles and adjust `CHIPS` / service selectors in Step 2 accordingly.

- [ ] **Step 2: Write `capture-demo.mjs`**

```js
// video/tools/capture-demo.mjs
// Headless capture of the live FlowHub demo → video/public/demo/*.png + manifest.json.
// Read-only against production. Adaptive: screenshots whatever "Live services" links
// the banner currently exposes.
import {chromium} from 'playwright';
import {mkdirSync, writeFileSync, rmSync} from 'node:fs';
import {dirname, join} from 'node:path';
import {fileURLToPath} from 'node:url';
import {validateManifest} from '../src/demoManifest.mjs';
import {captionFor} from './demoCaptions.mjs';

const here = dirname(fileURLToPath(import.meta.url));
const outDir = join(here, '..', 'public', 'demo');
const DEMO = process.env.DEMO_URL || 'https://demo.flowhub.freaxnx01.ch';
const VIEWPORT = {width: 1920, height: 1080};
const TERMINAL = /Routed|Unhandled|Failed/i;

// Confirmed in Step 1; label = caption key, name = accessible-name regex.
const CHIPS = [
  {key: 'movie', name: /Matrix/i},
  {key: 'zitat', name: /Zitat/i},
  {key: 'todo', name: /todo/i},
  {key: 'url', name: /URL/i},
];
const SERVICES = ['Vikunja', 'Zitate', 'Wallabag', 'paperless-ngx'];

const shots = [];
async function shot(page, {id, section, kind, caption, sample, cursor}) {
  const file = `demo/${id}.png`;
  await page.screenshot({path: join(outDir, `${id}.png`)});
  shots.push({id, file, section, kind, caption, ...(sample ? {sample} : {}), ...(cursor ? {cursor} : {})});
}
async function centerOf(locator) {
  const box = await locator.boundingBox();
  if (!box) return undefined;
  return {x: Math.round(box.x + box.width / 2), y: Math.round(box.y + box.height / 2), click: true};
}

async function run() {
  rmSync(outDir, {recursive: true, force: true});
  mkdirSync(outDir, {recursive: true});
  const browser = await chromium.launch();
  const ctx = await browser.newContext({viewport: VIEWPORT});
  const page = await ctx.newPage();

  await page.goto(DEMO, {waitUntil: 'networkidle'});
  await page.waitForTimeout(800);
  await shot(page, {id: 'home', section: 'intro', kind: 'context', caption: 'The public demo — drop a sample in'});

  // 1) Capture each sample.
  for (const chip of CHIPS) {
    await page.goto(DEMO, {waitUntil: 'networkidle'});
    const btn = page.getByRole('button', {name: chip.name}).first();
    const cursor = await centerOf(btn);
    await shot(page, {id: `cap-${chip.key}-chip`, section: 'capture', kind: 'action',
      caption: captionFor('sample', chip.key), sample: chip.key, cursor});
    await btn.click();
    await page.waitForURL(/captures/i, {timeout: 15000}).catch(() => {});
    // poll the list until this row reaches a terminal stage (best-effort).
    await page.waitForFunction(
      (re) => /Routed|Unhandled|Failed/i.test(document.body.innerText) || re,
      false, {timeout: 25000},
    ).catch(() => {});
    await page.waitForTimeout(500);
    await shot(page, {id: `cap-${chip.key}-list`, section: 'capture', kind: 'result',
      caption: captionFor('sample', chip.key), sample: chip.key});
    // open the first/detail row → classifier trace.
    const firstRow = page.getByRole('row').nth(1);
    await firstRow.click().catch(() => {});
    await page.waitForTimeout(800);
    await shot(page, {id: `cap-${chip.key}-detail`, section: 'capture', kind: 'result',
      caption: `${captionFor('sample', chip.key)} — classified`, sample: chip.key});
  }

  // 2) Each live service from the banner.
  await page.goto(DEMO, {waitUntil: 'networkidle'});
  const loginChip = await page.getByText(/\//).filter({hasText: /\w+\s*\/\s*\w+/}).first()
    .textContent().catch(() => null);
  const creds = parseLogin(loginChip);
  for (const name of SERVICES) {
    const link = page.getByRole('link', {name: new RegExp(name.replace('-ngx', ''), 'i')}).first();
    if ((await link.count()) === 0) continue;
    const cursor = await centerOf(link);
    await shot(page, {id: `svc-${slug(name)}-link`, section: 'service', kind: 'action',
      caption: captionFor('service', name), cursor});
    const href = await link.getAttribute('href');
    const svc = await ctx.newPage();
    await svc.setViewportSize(VIEWPORT);
    await svc.goto(href, {waitUntil: 'networkidle'}).catch(() => {});
    await maybeLogin(svc, creds);
    await svc.waitForTimeout(1200);
    await svc.screenshot({path: join(outDir, `svc-${slug(name)}.png`)});
    shots.push({id: `svc-${slug(name)}`, file: `demo/svc-${slug(name)}.png`,
      section: 'service', kind: 'result', caption: captionFor('service', name)});
    await svc.close();
  }

  await browser.close();

  const manifest = {capturedAt: process.env.CAPTURED_AT || '', viewport: VIEWPORT, shots};
  const {ok, errors} = validateManifest(manifest);
  if (!ok) throw new Error('manifest invalid:\n' + errors.join('\n'));
  if (shots.length < 1 + CHIPS.length) throw new Error(`too few shots captured: ${shots.length}`);
  writeFileSync(join(outDir, 'manifest.json'), JSON.stringify(manifest, null, 2) + '\n');
  console.log(`captured ${shots.length} shots → ${outDir}`);
}

function slug(s) {
  return s.toLowerCase().replace(/[^a-z0-9]+/g, '-');
}
function parseLogin(text) {
  if (!text) return null;
  const m = text.match(/([\w.-]+)\s*\/\s*(\S+)/);
  return m ? {user: m[1], pass: m[2]} : null;
}
async function maybeLogin(page, creds) {
  if (!creds) return;
  const user = page.locator('input[name="username"], input[type="email"], #username').first();
  const pass = page.locator('input[type="password"]').first();
  if ((await pass.count()) === 0) return;
  await user.fill(creds.user).catch(() => {});
  await pass.fill(creds.pass).catch(() => {});
  await page.locator('button[type="submit"], input[type="submit"]').first().click().catch(() => {});
  await page.waitForTimeout(1500);
}

run().catch((e) => {
  console.error(e);
  process.exit(1);
});
```

- [ ] **Step 3: Run a live capture dry run**

Run: `cd video && CAPTURED_AT="$(date -u +%FT%TZ)" npm run capture:demo`
Expected: `captured N shots → …/public/demo`, no throw, `manifest.json` written. If a service needs different login selectors, adjust `maybeLogin` and re-run.

- [ ] **Step 4: Verify the screenshots are real (non-blank)**

Run:
```bash
cd video && node -e "
const fs=require('fs'),d='public/demo';
const m=JSON.parse(fs.readFileSync(d+'/manifest.json'));
let bad=0;
for(const s of m.shots){const b=fs.statSync(d+'/'+s.id+'.png').size; if(b<5000){console.log('SUSPECT',s.id,b);bad++;}}
console.log('shots',m.shots.length,'suspect',bad); process.exit(bad?1:0);"
```
Expected: `suspect 0` (each PNG > 5 KB ⇒ not blank).

- [ ] **Step 5: Eyeball one frame**

Run: `cd video && cp public/demo/cap-todo-list.png /tmp/check-todo.png`
Then open `/tmp/check-todo.png` (or use the Read tool on it) and confirm it shows the Captures list with the todo. Adjust selectors/waits if not.

- [ ] **Step 6: Commit the script (assets committed later in Task 10)**

```bash
git add video/tools/capture-demo.mjs
git commit -m "feat(video): Playwright capture of the live demo → screenshots + manifest"
```

---

## Task 6: Cursor overlay component

**Files:**
- Create: `video/src/components/Cursor.tsx`

- [ ] **Step 1: Implement `Cursor.tsx`**

```tsx
import React from 'react';
import {useCurrentFrame} from 'remotion';
import {cursorAt} from '../demoTimeline.mjs';

type Scene = {startFrame: number; durationInFrames: number; cursorTarget: {x: number; y: number; click: boolean} | null};

export const Cursor: React.FC<{
  scenes: Scene[];
  glideFrames: number;
  clickFrames: number;
  rest: {x: number; y: number};
}> = ({scenes, glideFrames, clickFrames, rest}) => {
  const frame = useCurrentFrame();
  const {x, y, visible, clickT} = cursorAt(frame, scenes, {glideFrames, clickFrames, rest});
  if (!visible) return null;
  const ringScale = clickT > 0 ? 1 + clickT * 1.6 : 0;
  const ringOpacity = clickT > 0 ? 0.5 * (1 - clickT) : 0;
  const pointerScale = clickT > 0 ? 1 - 0.18 * Math.sin(clickT * Math.PI) : 1;
  return (
    <div style={{position: 'absolute', left: x, top: y, transform: 'translate(-4px,-2px)', pointerEvents: 'none'}}>
      <div
        style={{
          position: 'absolute', left: 0, top: 0, width: 64, height: 64,
          marginLeft: -32, marginTop: -32, borderRadius: '50%',
          border: '4px solid #00C9A7', transform: `scale(${ringScale})`, opacity: ringOpacity,
        }}
      />
      <svg width="40" height="40" viewBox="0 0 24 24" style={{transform: `scale(${pointerScale})`, transformOrigin: '0 0'}}>
        <path d="M4 2 L4 20 L9 15 L12.5 22 L15 21 L11.5 14 L18 14 Z"
          fill="#FFFFFF" stroke="#1A1A2E" strokeWidth="1.5" strokeLinejoin="round" />
      </svg>
    </div>
  );
};
```

- [ ] **Step 2: Typecheck**

Run: `cd video && npm run typecheck`
Expected: PASS (no TS errors).

- [ ] **Step 3: Commit**

```bash
git add video/src/components/Cursor.tsx
git commit -m "feat(video): animated SVG cursor overlay"
```

---

## Task 7: Screenshot + section-card components

**Files:**
- Create: `video/src/components/DemoShot.tsx`
- Create: `video/src/components/SectionCard.tsx`

- [ ] **Step 1: Implement `DemoShot.tsx`**

```tsx
import React from 'react';
import {AbsoluteFill, Img, interpolate, staticFile, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const DemoShot: React.FC<{file: string; caption: string; durationInFrames: number}> = ({
  file,
  caption,
  durationInFrames,
}) => {
  const frame = useCurrentFrame();
  const scale = interpolate(frame, [0, durationInFrames], [1.04, 1.0], {extrapolateRight: 'clamp'});
  const fade = Math.min(8, Math.floor((durationInFrames - 1) / 2));
  const opacity =
    fade < 1 ? 1 : interpolate(frame, [0, fade, durationInFrames - fade, durationInFrames], [0, 1, 1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  return (
    <AbsoluteFill style={{backgroundColor: theme.colors.bg, opacity, alignItems: 'center', justifyContent: 'center'}}>
      <Img src={staticFile(file)} style={{width: '100%', height: '100%', objectFit: 'contain', transform: `scale(${scale})`}} />
      <div
        style={{
          position: 'absolute', bottom: 56, left: '50%', transform: 'translateX(-50%)',
          background: 'rgba(9,24,57,0.85)', color: theme.colors.text, fontFamily: theme.fonts.body,
          fontSize: 40, padding: '16px 36px', borderRadius: 14, border: `2px solid ${theme.colors.accent}`,
        }}
      >
        {caption}
      </div>
    </AbsoluteFill>
  );
};
```

- [ ] **Step 2: Implement `SectionCard.tsx`**

```tsx
import React from 'react';
import {AbsoluteFill, spring, useCurrentFrame, useVideoConfig} from 'remotion';
import {theme} from '../theme';

export const SectionCard: React.FC<{title: string}> = ({title}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const scale = spring({frame, fps, config: {damping: 200}});
  return (
    <AbsoluteFill style={{backgroundColor: theme.colors.logoBg, alignItems: 'center', justifyContent: 'center'}}>
      <div style={{fontFamily: theme.fonts.heading, fontWeight: 800, fontSize: 96, color: theme.colors.text, transform: `scale(${scale})`}}>
        {title}
      </div>
    </AbsoluteFill>
  );
};
```

- [ ] **Step 3: Typecheck**

Run: `cd video && npm run typecheck`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add video/src/components/DemoShot.tsx video/src/components/SectionCard.tsx
git commit -m "feat(video): demo shot + section-card components"
```

---

## Task 8: DemoWalkthrough composition + registration

**Files:**
- Create: `video/src/DemoWalkthrough.tsx`
- Modify: `video/src/Root.tsx`

> **Depends on Task 5:** this composition imports `../public/demo/manifest.json`, so the typecheck (Step 3) and render (Step 4) require a manifest on disk. Run `just video-capture` (Task 5 Step 3) first. If building the composition before a live capture is desired, write a minimal placeholder `video/public/demo/manifest.json` with one `intro` shot to satisfy the import, then replace it via the real capture in Task 10.

- [ ] **Step 1: Implement `DemoWalkthrough.tsx`**

```tsx
import React from 'react';
import {AbsoluteFill, Audio, interpolate, Sequence, staticFile, useVideoConfig} from 'remotion';
import manifest from '../public/demo/manifest.json';
import {theme} from './theme';
import {TitleCard} from './components/TitleCard';
import {SectionCard} from './components/SectionCard';
import {DemoShot} from './components/DemoShot';
import {Cursor} from './components/Cursor';
import {buildTimeline, scaleCursor} from './demoTimeline.mjs';

const SECONDS = {intro: 2, outro: 2.5, section: 1.5, context: 2.6, result: 3.6, action: 2.4, glide: 0.5};
const SECTION_TITLES = {capture: 'Capture an inbox item', service: 'Where it lands'};

export const demoDurationInFrames = (fps: number): number => {
  const tl = buildTimeline(manifest.shots, fps, {...SECONDS, sectionTitles: SECTION_TITLES});
  return tl.reduce((n: number, s: any) => n + s.durationInFrames, 0);
};

export const DemoWalkthrough: React.FC = () => {
  const {fps, width, height} = useVideoConfig();
  const scenes = buildTimeline(manifest.shots, fps, {...SECONDS, sectionTitles: SECTION_TITLES});
  const comp = {width, height};
  const cursorScenes = scenes.map((s: any) => ({
    startFrame: s.startFrame,
    durationInFrames: s.durationInFrames,
    cursorTarget:
      s.cursorTarget && scaleCursor(s.cursorTarget, manifest.viewport, comp),
  }));
  return (
    <AbsoluteFill style={{backgroundColor: theme.colors.bg}}>
      <Audio
        loop
        src={staticFile('audio/music/bed.mp3')}
        volume={(fr) => interpolate(fr, [0, 30], [0, 0.12], {extrapolateRight: 'clamp'})}
      />
      {scenes.map((s: any, i: number) => (
        <Sequence key={i} from={s.startFrame} durationInFrames={s.durationInFrames}>
          {s.type === 'intro' && <TitleCard title="FlowHub" subtitle="See it in action — the live demo" />}
          {s.type === 'outro' && <TitleCard title="FlowHub" subtitle="Try it: demo.flowhub.freaxnx01.ch" />}
          {s.type === 'section' && <SectionCard title={s.title} />}
          {s.type === 'shot' && (
            <DemoShot file={s.shot.file} caption={s.shot.caption} durationInFrames={s.durationInFrames} />
          )}
        </Sequence>
      ))}
      <Cursor scenes={cursorScenes} glideFrames={Math.round(SECONDS.glide * fps)} clickFrames={10} rest={{x: width - 160, y: height - 120}} />
    </AbsoluteFill>
  );
};
```

> The intro/outro `TitleCard` scenes render the logo on `theme.colors.bg`, not `logoBg`. That's acceptable for this composition; if a panel outline shows, wrap the `TitleCard` here in an `AbsoluteFill` with `backgroundColor: theme.colors.logoBg` (the explainers set the scene bg, but this composition uses bare `Sequence`s). Leave as-is unless the smoke frame shows a visible rectangle.

- [ ] **Step 2: Register in `Root.tsx`**

Add the import and a `<Composition>`. The width/height/fps mirror the existing compositions (read the current `Root.tsx` for the exact `<Composition>` props pattern and the per-scene frame summing helper; match it). Use `demoDurationInFrames(fps)` for `durationInFrames`:

```tsx
import {DemoWalkthrough, demoDurationInFrames} from './DemoWalkthrough';
// inside the registrations, fps = 30 (match the others):
<Composition
  id="demo-walkthrough"
  component={DemoWalkthrough}
  durationInFrames={demoDurationInFrames(30)}
  fps={30}
  width={1920}
  height={1080}
/>
```

- [ ] **Step 3: Typecheck**

Run: `cd video && npm run typecheck`
Expected: PASS.

- [ ] **Step 4: Render smoke**

Run: `cd video && npm run render:demo`
Expected: writes `out/flowhub-demo.en.mp4` with no error.

- [ ] **Step 5: Probe the MP4**

Run: `cd video && tools/ffmpeg/ffprobe -v error -show_entries 'format=duration:stream=codec_type,codec_name' -of default=noprint_wrappers=1 out/flowhub-demo.en.mp4`
Expected: an `h264` video stream + an `aac` audio stream; duration ≈ the sum from `demoDurationInFrames`.

- [ ] **Step 6: Commit**

```bash
git add video/src/DemoWalkthrough.tsx video/src/Root.tsx
git commit -m "feat(video): demo-walkthrough composition + registration"
```

---

## Task 9: justfile recipes + video README

**Files:**
- Modify: `justfile`
- Modify: `video/README.md`

- [ ] **Step 1: Add the `video-capture` recipe**

In the `# ── Explainer videos` group in `justfile`, after `video-render`, add:

```just
# Capture the live VPS demo into video/public/demo/ (screenshots + manifest)
[group('video')]
video-capture:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{video_dir}}/node_modules" ]; then just video-setup; fi
    cd {{video_dir}}
    CAPTURED_AT="$(date -u +%FT%TZ)" npm run capture:demo

# Render the demo-walkthrough video → video/out/flowhub-demo.en.mp4
[group('video')]
video-demo:
    #!/usr/bin/env bash
    set -euo pipefail
    if [ ! -d "{{video_dir}}/node_modules" ]; then just video-setup; fi
    cd {{video_dir}}
    PATH="{{video_ffmpeg_dir}}:$PATH" npm run render:demo
```

- [ ] **Step 2: Validate the justfile + run the unit tests as a `video-test` convenience (optional but cheap)**

Run: `just --list | grep video`
Expected: `video-capture` and `video-demo` listed.

- [ ] **Step 3: Document in `video/README.md`**

Add a section after the existing Render section:

```markdown
## Demo walkthrough video

A third, **silent** composition (`demo-walkthrough`) screenshots the live VPS demo
and stitches it into a captioned walkthrough with an animated cursor.

```bash
just video-capture   # drive the live demo → video/public/demo/*.png + manifest.json
just video-demo      # render → video/out/flowhub-demo.en.mp4
```

`video-capture` needs network access to the live demo; the screenshots + `manifest.json`
it writes are committed so `video-demo` renders offline. Pure logic
(`src/demoManifest.mjs`, `src/demoTimeline.mjs`, `tools/demoCaptions.mjs`) is unit-tested
with `npm test`.
```

- [ ] **Step 4: Commit**

```bash
git add justfile video/README.md
git commit -m "build(video): video-capture + video-demo recipes; document the flow"
```

---

## Task 10: Capture live, render, verify, commit assets

**Files:**
- Create (commit): `video/public/demo/*.png`, `video/public/demo/manifest.json`
- Create (local only, gitignored): `video/out/flowhub-demo.en.mp4`

- [ ] **Step 1: Full capture against the live demo**

Run: `just video-capture`
Expected: `captured N shots`, manifest written, exit 0.

- [ ] **Step 2: Render**

Run: `just video-demo`
Expected: `out/flowhub-demo.en.mp4` produced.

- [ ] **Step 3: Verify frames — cursor, captions, service data**

Run:
```bash
cd video && for t in 3 12 22 35 50; do tools/ffmpeg/ffmpeg -hide_banner -loglevel error -ss $t -i out/flowhub-demo.en.mp4 -frames:v 1 /tmp/demo-$t.png; done
```
Then open `/tmp/demo-*.png` (Read tool). Confirm: logo intro, a chip shot with the cursor on the chip, a captures-list result, a service board showing data, and the outro. Adjust timings/selectors and re-capture/re-render if anything is wrong.

- [ ] **Step 4: Commit the captured assets**

```bash
git add video/public/demo
git commit -m "assets(video): committed demo-walkthrough screenshots + manifest"
```

---

## Task 11: Embed in the root README

**Files:**
- Modify: `README.md`

> GitHub's native player needs a `user-attachments` asset URL, which only the operator can mint by uploading the MP4 through the web UI (same as the two explainers). This task renders the MP4 and wires a placeholder; the operator supplies the URL.

- [ ] **Step 1: Add a third entry to the "Explainer videos" section**

After the Technical video block in `README.md`, add:

```markdown
**See it in action — using the live demo** (~60s, no narration)

<!-- replace with the user-attachments asset URL after uploading out/flowhub-demo.en.mp4 -->
https://github.com/freaxnx01/FlowHub-CAS-AISE/raw/main/video/out/flowhub-demo.en.mp4
```

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs(readme): add the demo walkthrough video (URL pending upload)"
```

- [ ] **Step 3: Hand off to the operator**

Tell the user: upload `video/out/flowhub-demo.en.mp4` to a GitHub issue/PR comment, submit it, and paste the `user-attachments/assets/…` URL back. Then replace the placeholder line (Step 1) with that URL and re-commit — matching the two existing explainers.

---

## Notes for the implementer

- **TDD scope:** the three pure modules (`demoManifest`, `demoCaptions`, `demoTimeline`) are unit-tested first (Tasks 2–4). The capture script (Task 5) and the rendered composition (Task 8) are inherently integration work — verified by live dry runs + frame inspection, not unit tests. Do not fake screenshots or stub the live demo to make a check pass.
- **Selectors are the main risk.** Task 5 Step 1 inspects the live DOM before the script is finalized. If the live demo's accessible names differ, fix the `CHIPS`/service regexes and the row/login selectors — don't paper over with long `waitForTimeout`s.
- **Faithfulness over polish:** if a sample lands at `Unhandled` or a service isn't in the banner, capture that truthfully. The caption lookup already phrases each landing neutrally.
- **Frame summing:** `buildTimeline` sums per-scene frames; `demoDurationInFrames` reuses it so `Root.tsx` and the composition never disagree (the tail-clip bug fixed on the explainers).
```
