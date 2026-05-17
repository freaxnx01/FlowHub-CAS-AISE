import { test, expect, request, type APIRequestContext } from '@playwright/test';

const VIKUNJA_BASE = process.env.VIKUNJA_BASE_URL
  ?? 'https://todo.flowhub-test-services.home.freaxnx01.ch';
const VIKUNJA_PROJECT_ID = Number.parseInt(
  process.env.VIKUNJA_PROJECT_ID ?? '1', 10);
const VIKUNJA_TOKEN = process.env.VIKUNJA_API_TOKEN ?? '';

type VikunjaTask = { id: number; title: string; created: string };

async function listTasks(api: APIRequestContext): Promise<VikunjaTask[]> {
  const res = await api.get(
    `${VIKUNJA_BASE}/api/v1/projects/${VIKUNJA_PROJECT_ID}/tasks?per_page=200`,
    { headers: { Authorization: `Bearer ${VIKUNJA_TOKEN}` } });
  expect(res.ok(), `Vikunja GET failed: ${res.status()} ${res.statusText()}`)
    .toBeTruthy();
  return (await res.json()) as VikunjaTask[];
}

test.describe('FlowHub full cycle: capture -> Vikunja', () => {
  test.skip(!VIKUNJA_TOKEN,
    'VIKUNJA_API_TOKEN not set — skipping live Vikunja assertion.');

  test('quick capture lands as Vikunja task', async ({ page, playwright }) => {
    const api = await request.newContext({ ignoreHTTPSErrors: true });

    const stamp = new Date().toISOString().replace(/[:.]/g, '-');
    const captureText = `todo: e2e-demo full-cycle ${stamp}`;

    const tasksBefore = await listTasks(api);
    const titlesBefore = new Set(tasksBefore.map(t => t.title));

    await test.step('open the app', async () => {
      await page.goto('/');
      await expect(page).toHaveTitle(/FlowHub/i);
    });

    await test.step('submit capture via AppBar QuickCapture', async () => {
      const quick = page.getByPlaceholder(/quick capture/i);
      await quick.click();
      await quick.fill(captureText);
      await quick.press('Enter');

      await expect(page.getByText(/Captured/i)).toBeVisible({ timeout: 10_000 });
    });

    await test.step('verify task appears in Vikunja', async () => {
      await expect.poll(
        async () => {
          const tasks = await listTasks(api);
          return tasks.some(t =>
            !titlesBefore.has(t.title) && t.title === captureText);
        },
        {
          message: `task with title "${captureText}" did not appear in Vikunja`,
          timeout: 30_000,
          intervals: [500, 1_000, 2_000],
        },
      ).toBe(true);
    });

    await api.dispose();
  });
});
