import { chromium } from 'playwright';

const SS = '/home/freax/projects/repos/github/freaxnx01/public/FlowHub-CAS-AISE/.worktrees/submission-findings/nachbereitung/examiner-sim/screenshots';
const ts = '2026-06-08T1152';
const base = 'https://demo.flowhub.freaxnx01.ch';

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

async function shot(path, name) {
  await page.goto(base + path, { waitUntil: 'networkidle', timeout: 30000 }).catch(() => {});
  await page.waitForTimeout(3500);
  const out = `${SS}/${name}-${ts}.png`;
  await page.screenshot({ path: out, fullPage: true });
  console.log('SAVED', out);
}

await shot('/', 'home');
await shot('/captures', 'captures-list');

await browser.close();
console.log('DONE');
