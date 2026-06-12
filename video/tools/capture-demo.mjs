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

// Step 1 confirmed: all chips are role=button; all services are role=link.
// Terminal states: completed, routed, unhandled, failed, orphan.
// Classification takes ~15-17s; list does NOT update live in headless — reload required.
// Row click navigates to /captures/<id> (not a dialog).
// Login hint is a SPAN with text "flowhub / flowhub-demo".
const CHIPS = [
  {key: 'movie', name: /Matrix/i},
  {key: 'zitat', name: /Zitat/i},
  {key: 'todo', name: /todo/i},
  {key: 'url', name: /URL/i},
];
const SERVICES = ['Vikunja', 'Zitate', 'Wallabag', 'paperless-ngx'];

// Time budget for classification to settle before reload-check.
const CLASSIFY_WAIT_MS = 20_000;
// Max polling rounds after reload (1s each).
const CLASSIFY_POLL_ROUNDS = 10;

const shots = [];

async function shot(page, {id, section, kind, caption, sample, cursor}) {
  await page.screenshot({path: join(outDir, `${id}.png`)});
  shots.push({
    id,
    file: `demo/${id}.png`,
    section,
    kind,
    caption,
    ...(sample ? {sample} : {}),
    ...(cursor ? {cursor} : {}),
  });
}

async function centerOf(locator) {
  const box = await locator.boundingBox();
  if (!box) return undefined;
  return {x: Math.round(box.x + box.width / 2), y: Math.round(box.y + box.height / 2), click: true};
}

/**
 * Wait for row 1 (the newest capture) to reach a terminal lifecycle state.
 * Strategy: wait a fixed period then reload the page (SignalR updates don't
 * propagate to headless), then poll row 1 briefly.
 */
async function waitForFirstRowTerminal(page) {
  await page.waitForTimeout(CLASSIFY_WAIT_MS);
  await page.reload({waitUntil: 'networkidle'});
  for (let i = 0; i < CLASSIFY_POLL_ROUNDS; i++) {
    const rowText = await page.getByRole('row').nth(1).textContent().catch(() => '');
    if (/completed|routed|unhandled|failed|orphan/i.test(rowText)) return;
    await page.waitForTimeout(1000);
    await page.reload({waitUntil: 'networkidle'});
  }
  // Not terminal yet — proceed anyway; faithfully capture whatever state is shown.
}

async function run() {
  rmSync(outDir, {recursive: true, force: true});
  mkdirSync(outDir, {recursive: true});

  const browser = await chromium.launch();
  const ctx = await browser.newContext({viewport: VIEWPORT});
  const page = await ctx.newPage();

  // ── Home / intro ───────────────────────────────────────────────────────────
  await page.goto(DEMO, {waitUntil: 'networkidle'});
  await page.waitForTimeout(800);
  await shot(page, {
    id: 'home',
    section: 'intro',
    kind: 'context',
    caption: 'The public demo — drop a sample in',
  });

  // ── Chip captures ──────────────────────────────────────────────────────────
  for (const chip of CHIPS) {
    await page.goto(DEMO, {waitUntil: 'networkidle'});
    await page.waitForTimeout(400);

    const btn = page.getByRole('button', {name: chip.name}).first();
    if ((await btn.count()) === 0) {
      console.warn(`chip ${chip.key} not found — skipping`);
      continue;
    }

    const cursor = await centerOf(btn);
    await shot(page, {
      id: `cap-${chip.key}-chip`,
      section: 'capture',
      kind: 'action',
      caption: captionFor('sample', chip.key),
      sample: chip.key,
      cursor,
    });

    await btn.click();
    // App navigates to /captures immediately after submit.
    await page.waitForURL(/\/captures/i, {timeout: 15_000}).catch(() => {});
    await page.waitForTimeout(500);

    // Screenshot the list while the capture is still raw (shows the newly submitted entry).
    await shot(page, {
      id: `cap-${chip.key}-list`,
      section: 'capture',
      kind: 'result',
      caption: captionFor('sample', chip.key),
      sample: chip.key,
    });

    // Wait for classification and reload to pick up the updated state.
    await waitForFirstRowTerminal(page);
    await page.waitForTimeout(400);

    // Screenshot list again with the terminal state visible.
    await shot(page, {
      id: `cap-${chip.key}-classified`,
      section: 'capture',
      kind: 'result',
      caption: `${captionFor('sample', chip.key)} — classified`,
      sample: chip.key,
    });

    // Open the detail page for the newest capture (row index 1 = first data row).
    const firstRow = page.getByRole('row').nth(1);
    await firstRow.click().catch(() => {});
    await page.waitForURL(/\/captures\/.+/i, {timeout: 5_000}).catch(() => {});
    await page.waitForTimeout(800);
    await shot(page, {
      id: `cap-${chip.key}-detail`,
      section: 'capture',
      kind: 'result',
      caption: `${captionFor('sample', chip.key)} — detail`,
      sample: chip.key,
    });
  }

  // ── Service screens ────────────────────────────────────────────────────────
  await page.goto(DEMO, {waitUntil: 'networkidle'});
  await page.waitForTimeout(600);

  // Extract credentials from the banner hint span (e.g. "flowhub / flowhub-demo").
  const loginText = await page
    .locator('span')
    .filter({hasText: /\w[\w.-]*\s*\/\s*\S+/})
    .first()
    .textContent()
    .catch(() => null);
  const creds = parseLogin(loginText);
  if (creds) {
    console.log(`found credentials: ${creds.user} / (redacted)`);
  }

  for (const name of SERVICES) {
    // Strip "-ngx" suffix for label matching (banner shows "PAPERLESS-NGX").
    const labelPattern = new RegExp(name.replace(/-ngx$/i, ''), 'i');
    const link = page.getByRole('link', {name: labelPattern}).first();
    if ((await link.count()) === 0) {
      console.warn(`service link "${name}" not found — skipping`);
      continue;
    }

    const cursor = await centerOf(link);
    await shot(page, {
      id: `svc-${slug(name)}-link`,
      section: 'service',
      kind: 'action',
      caption: captionFor('service', name),
      cursor,
    });

    const href = await link.getAttribute('href');
    if (!href) {
      console.warn(`service "${name}" has no href — skipping`);
      continue;
    }

    const svc = await ctx.newPage();
    await svc.setViewportSize(VIEWPORT);
    await svc.goto(href, {waitUntil: 'networkidle'}).catch((e) => {
      console.warn(`service "${name}" navigation error: ${e.message}`);
    });
    await maybeLogin(svc, creds);
    await svc.waitForTimeout(1200);
    const svcId = `svc-${slug(name)}`;
    await svc.screenshot({path: join(outDir, `${svcId}.png`)});
    shots.push({
      id: svcId,
      file: `demo/${svcId}.png`,
      section: 'service',
      kind: 'result',
      caption: captionFor('service', name),
    });
    await svc.close();
  }

  await browser.close();

  // ── Validate and write manifest ────────────────────────────────────────────
  const manifest = {
    capturedAt: process.env.CAPTURED_AT || '',
    viewport: VIEWPORT,
    shots,
  };
  const {ok, errors} = validateManifest(manifest);
  if (!ok) throw new Error('manifest invalid:\n' + errors.join('\n'));
  // Gross-failure floor: home + at least the chip + list shot for every sample.
  // A full run yields ~25 (home + 4 chips × 4 + services); this catches a run
  // where classification/navigation broke for most samples rather than passing
  // a near-empty capture silently. Services are adaptive, so they're not required.
  const minShots = 1 + CHIPS.length * 2;
  if (shots.length < minShots) {
    throw new Error(`too few shots captured: ${shots.length} (expected >= ${minShots})`);
  }
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
  const pass = page.locator('input[type="password"]').first();
  if ((await pass.count()) === 0) return;
  const user = page
    .locator('input[name="username"], input[type="email"], #username, input[name="_username"]')
    .first();
  await user.fill(creds.user).catch(() => {});
  await pass.fill(creds.pass).catch(() => {});
  await page
    .locator('button[type="submit"], input[type="submit"]')
    .first()
    .click()
    .catch(() => {});
  await page.waitForTimeout(2000);
}

run().catch((e) => {
  console.error(e);
  process.exit(1);
});
