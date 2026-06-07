export const meta = {
  name: 'examiner-sim',
  description: 'Simulate the CAS-AISE examiner: rebuild the real submission PDFs, grade them against the Moodle rubric with a multi-agent panel, exercise the live public demo, and produce a dated grade sheet.',
  whenToUse: 'Run before a real exam/submission checkpoint to get an honest, rubric-anchored grade prediction against the freshly-built artifacts and the live demo. Repeatable.',
  phases: [
    { title: 'Build', detail: 'regenerate SUBMISSION + bundle PDFs, extract text from the real PDFs' },
    { title: 'Examine', detail: '5 rubric-bucket examiners + 1 live-demo examiner, in parallel' },
    { title: 'Skeptic', detail: 'adversarial pass — challenge over-generous scores per bucket' },
    { title: 'Verdict', detail: 'aggregate to a grade sheet (/90), defense questions, ranked gaps' },
  ],
}

// ── Inputs (from the /examiner-sim slash command, or sensible defaults) ───────
const stamp   = (args && args.stamp)  || 'latest'           // e.g. "2026-06-07T0758"
const dateStr = (args && args.date)   || 'unknown-date'     // e.g. "2026-06-07"
const commit  = (args && args.commit) || 'unknown-commit'
const demoUrl = (args && args.demoUrl) || 'https://demo.flowhub.freaxnx01.ch'

const RUBRIC      = 'vault/Organisation/Bewertungskriterien.md'
const WORK        = 'tools/build/examiner-sim'
const BUNDLE_TXT  = WORK + '/bundle.txt'
const SUB_TXT     = WORK + '/submission.txt'
const SHOTS       = 'nachbereitung/examiner-sim/screenshots'
// REPORT path + effective metadata are resolved AFTER the build agent runs,
// so they use the freshly-derived commit/stamp/date even when args don't propagate.

// ── Schemas ──────────────────────────────────────────────────────────────────
const ITEM = {
  type: 'object',
  required: ['name', 'scale', 'awarded', 'max', 'justification', 'evidence', 'gaps'],
  properties: {
    name:          { type: 'string' },
    scale:         { type: 'string', description: 'the rubric scale, e.g. "0/1/3/5"' },
    awarded:       { type: 'number', description: 'one of the discrete scale values' },
    max:           { type: 'number' },
    justification: { type: 'string', description: 'why this exact level, in the examiner voice' },
    evidence:      { type: 'array', items: { type: 'string' }, description: 'concrete citations: file/section/page in the bundle PDF that justify the score' },
    gaps:          { type: 'array', items: { type: 'string' }, description: 'what is missing to reach the next level' },
  },
}
const BUCKET_SCHEMA = {
  type: 'object',
  required: ['bucket', 'items', 'awarded', 'max', 'summary'],
  properties: {
    bucket:  { type: 'string' },
    items:   { type: 'array', items: ITEM },
    awarded: { type: 'number' },
    max:     { type: 'number' },
    summary: { type: 'string' },
  },
}
const SKEPTIC_SCHEMA = {
  type: 'object',
  required: ['bucket', 'disputes', 'adjustedAwarded', 'verdict'],
  properties: {
    bucket: { type: 'string' },
    disputes: {
      type: 'array',
      items: {
        type: 'object',
        required: ['item', 'claimed', 'recommended', 'reason'],
        properties: {
          item:        { type: 'string' },
          claimed:     { type: 'number' },
          recommended: { type: 'number' },
          reason:      { type: 'string' },
        },
      },
    },
    adjustedAwarded: { type: 'number', description: 'bucket total after applying recommended adjustments' },
    verdict:         { type: 'string', description: 'one line: was the examiner too generous, too harsh, or fair?' },
  },
}
const DEMO_SCHEMA = {
  type: 'object',
  required: ['reachable', 'roundTrip', 'rateLimit', 'observations', 'rubricImplications', 'screenshots', 'issues'],
  properties: {
    reachable: { type: 'boolean' },
    roundTrip: {
      type: 'object',
      required: ['submitted', 'classified', 'matchedSkill', 'titleEnriched', 'notes'],
      properties: {
        submitted:     { type: 'boolean' },
        captureId:     { type: 'string' },
        classified:    { type: 'boolean' },
        matchedSkill:  { type: 'string' },
        finalStage:    { type: 'string' },
        titleEnriched: { type: 'boolean' },
        notes:         { type: 'string' },
      },
    },
    rateLimit:    { type: 'object', properties: { tested: { type: 'boolean' }, observed: { type: 'string' } } },
    embeddings503: { type: 'boolean', description: 'did /api/v1/captures/search return 503 ProblemDetails as documented' },
    resetPosture:  { type: 'string', description: 'evidence of the demo banner / 15-min reset posture' },
    observations:       { type: 'array', items: { type: 'string' } },
    rubricImplications: { type: 'array', items: { type: 'string' }, description: 'which rubric items the live demo strengthens, esp. intelligent services & containerized sub-systems' },
    screenshots:        { type: 'array', items: { type: 'string' } },
    issues:             { type: 'array', items: { type: 'string' }, description: 'anything an examiner would dock points for or ask about' },
  },
}
const BUILD_SCHEMA = {
  type: 'object',
  required: ['built', 'bundlePages', 'bundleTxt', 'submissionTxt', 'commit', 'stamp', 'date', 'notes'],
  properties: {
    built:         { type: 'boolean' },
    bundlePages:   { type: 'number' },
    bundleBytes:   { type: 'number' },
    bundleTxt:     { type: 'string' },
    submissionTxt: { type: 'string' },
    commit:        { type: 'string', description: 'git rev-parse --short HEAD' },
    stamp:         { type: 'string', description: 'filesystem-safe timestamp, e.g. 2026-06-07T0758 from `date +%Y-%m-%dT%H%M`' },
    date:          { type: 'string', description: 'YYYY-MM-DD from `date +%F`' },
    freshness:     { type: 'string', description: 'evidence the PDFs were rebuilt this run (mtime)' },
    warnings:      { type: 'array', items: { type: 'string' } },
    notes:         { type: 'string' },
  },
}

// ── Rubric buckets (mirrors vault/Organisation/Bewertungskriterien.md) ────────
// Quarkus/Jakarta-EE item (max 10) is N/A for the .NET stack and excluded → /90.
const BUCKETS = [
  {
    key: 'Spezifikation', max: 15,
    items: [
      '"Sind die wichtigsten Use-Cases und fachlichen Anforderungen benannt" (0/1/3/5)',
      '"Sind die Qualitätsanforderungen (NfA) nach SMART spezifiziert" (0/1/3/5)',
      '"Ist die grundsätzliche Vision der Lösung beschrieben" (0/1/3/5)',
    ],
    read: ['docs/spec/use-cases.md', 'docs/spec/nfa.md', 'docs/spec/system-context.md', 'docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md'],
  },
  {
    key: 'Entwurf', max: 17,
    items: [
      '"Ist der Lösungsansatz und die Architektur beschrieben (bildlich wie textuell)" (0/1/4/7)',
      '"Ist der Entwurf aus den verschiedenen Perspektiven (Struktur, Verhalten, Interaktion) beschrieben" (0/1/4/7)',
      '"Ist das DB-Modell spezifiziert" (0/1/2/3)',
    ],
    read: ['docs/adr', 'docs/architektur', 'docs/projektbeschreibung', 'docs/design/db/entities.md', 'docs/design/db/er.md'],
  },
  {
    key: 'Programmierung', max: 12,
    note: 'The Quarkus/Jakarta-EE item (max 10) is consciously excluded (stack-mismatch, .NET). Effective max = 7 + 3 + 2 = 12.',
    items: [
      '"Ist der Code lesbar, dokumentiert und nach Layer, Modulen und Sub-Systemen strukturiert" (0/1/4/7)',
      '"Sind die Erkenntnisse aus der Programmierung dokumentiert" (0/1/2/3)',
      '"Ist der Source-Code in einem Git-Repository verfügbar" (0/2)',
    ],
    read: ['source', 'docs/insights', 'docs/ci-cd.md', 'README.md', 'CLAUDE.md'],
  },
  {
    key: 'Validierung', max: 16,
    items: [
      '"Ist definiert, welches die Abnahmekriterien sind" (0/1/3/5)',
      '"Ist spezifiziert, wie die Applikation getestet wird und welche Technologien dazu verwendet werden" (0/1/3/5)',
      '"Sind Unit-Tests programmiert" (0/1/3)',
      '"Sind die Test-Ergebnisse dokumentiert" (0/1/3)',
    ],
    read: ['docs/spec/acceptance-criteria.md', 'docs/spec/testing-strategy.md', 'tests', 'docs/insights'],
  },
  {
    key: 'KI, Sub-Systeme & Reflexion', max: 30,
    items: [
      '"Wurden KI-unterstützende Werkzeuge verwendet und deren Nutzung beschrieben" (0/1/7/12)',
      '"Wurden mit Hilfe der KI intelligente und flexible Services gebaut" (0/2/6)',
      '"Wurde die Lösung in verschiedene Sub-Systeme aufgeteilt, die unabhängig voneinander als Container verteilt und betrieben werden können" (0/1/3/5)',
      '"Sind die Erfahrungen während der Projektarbeit mit KI-unterstützenden Werkzeugen als Fazit reflektiert" (0/1/4/7)',
    ],
    read: ['docs/ai-usage.md', 'vault/Projektarbeit/Learnings.md', 'docs/insights', 'docker-compose.yml', 'demo', 'source/FlowHub.AI', '.ai', '.claude'],
  },
]

// ════════════════════════════════════════════════════════════════════════════
// Phase 0 — Build the real artifacts and extract their text
// ════════════════════════════════════════════════════════════════════════════
phase('Build')
const build = await agent(
  [
    'You are the build step of an examiner simulation. The examiner must grade the REAL rendered submission PDFs, not the markdown sources. Do this from the repo root:',
    '',
    '1. Regenerate the real submission PDFs (these call the project puppeteer renderer):',
    '   just pdf-submission',
    '   just pdf-submission-bundle',
    '   If `just` is unavailable, read the justfile targets `pdf-submission` / `pdf-submission-bundle` and run the equivalent commands. The bundle build (tools/submission-bundle.sh) uses `set -euo pipefail`, so a successful exit means every referenced artifact was found and inlined; a failure means a referenced file is missing — capture that as a warning (an examiner would see a broken bundle).',
    '2. mkdir -p ' + WORK,
    '3. Extract the REAL PDF text so the examiners read what the examiner sees:',
    '   pdftotext -layout SUBMISSION-bundle.pdf ' + BUNDLE_TXT,
    '   pdftotext -layout SUBMISSION.pdf ' + SUB_TXT,
    '4. Record evidence of freshness: run `ls -l --time-style=full-iso SUBMISSION-bundle.pdf SUBMISSION.pdf` and `pdfinfo SUBMISSION-bundle.pdf` (page count + byte size). Confirm the mtimes are from this run.',
    '5. Sanity-check the extracted text is non-empty and contains the cover title "FlowHub" and the table-of-contents.',
    '6. Capture run metadata so the report can be dated independently of any caller-supplied args: `git rev-parse --short HEAD` (→ commit), `date +%Y-%m-%dT%H%M` (→ stamp, used in the report filename), and `date +%F` (→ date). If args provided different values, prefer the freshly-derived ones.',
    '',
    'Return the structured result. If a build command genuinely cannot run, set built=false, explain in notes, and still attempt pdftotext on any existing PDFs so the run can degrade gracefully.',
  ].join('\n'),
  { label: 'build:pdfs', phase: 'Build', schema: BUILD_SCHEMA },
)

// Resolve effective run metadata: prefer freshly-derived values from the build
// step, fall back to caller args, then to the top-of-file defaults.
const effStamp  = (build && build.stamp)  || stamp
const effDate   = (build && build.date)   || dateStr
const effCommit = (build && build.commit) || commit
const REPORT    = 'nachbereitung/examiner-sim/report-' + effStamp + '.md'

log('Build: ' + (build && build.built ? 'PDFs rebuilt (' + build.bundlePages + 'p) @ ' + effCommit : 'BUILD ISSUE — see report'))

// ════════════════════════════════════════════════════════════════════════════
// Phase 1 — Examine (5 rubric buckets in a verify-pipeline) + live demo (parallel)
// ════════════════════════════════════════════════════════════════════════════
phase('Examine')

// Kick off the live-demo examiner concurrently with the bucket pipeline.
const demoPromise = agent(
  [
    'You are the CAS-AISE examiner exercising the LIVE public demo of FlowHub at ' + demoUrl + '. Do a full interactive round-trip and gather evidence as a grader would. Be rigorous and skeptical; note anything you would dock points for.',
    '',
    'Functional round-trip (primary evidence — use curl):',
    '1. GET / and GET /health/live → confirm reachable (HTTP 200).',
    '2. Submit a "read-later" capture and watch the AI classify it:',
    "   curl -s -X POST " + demoUrl + "/api/v1/captures -H 'Content-Type: application/json' -d '{\"content\":\"Examiner: save https://arxiv.org/abs/1706.03762 to read later\",\"source\":\"Api\"}'",
    '   Capture the returned id, then GET ' + demoUrl + '/api/v1/captures/{id} a few seconds later (poll up to ~10s). Expect stage to advance from Raw, a matchedSkill (e.g. Wallabag for a read-later URL), and an AI-enriched title. Skill writes are intentionally disabled in the demo so the final stage may be "Unhandled" with a "no integration registered" failureReason — that is EXPECTED and documented, not a defect.',
    '3. Submit a second, different capture (e.g. a task-like text "todo: buy milk" or a film tip) to show classification variety / flexibility.',
    '4. Embeddings posture: GET ' + demoUrl + '/api/v1/captures/search?q=paper → expect HTTP 503 with a ProblemDetails body (embeddings are deliberately disabled in the demo profile; transparent, not hidden).',
    '5. Rate-limit posture: fire ~25 rapid GETs to / and report the status-code progression (expect 200s rolling into 429s after the burst). Example:',
    '   for i in $(seq 1 25); do curl -s -o /dev/null -w "%{http_code} " ' + demoUrl + '/; done',
    '6. Reset/demo posture: note the demo banner / 15-min self-reset behavior if observable (the home HTML contains the demo banner text).',
    '',
    'Visual evidence (best-effort, must not block):',
    '7. Try to screenshot the live UI with the cached Playwright Chromium. Write a tiny node script using playwright (try the project node_modules or `npx playwright`); navigate to ' + demoUrl + '/ and the Captures list page, save PNGs into ' + SHOTS + '/ (e.g. home-' + effStamp + '.png and captures-list-' + effStamp + '.png). If Playwright is not usable, skip screenshots, set screenshots=[] and add an issue noting visual capture was unavailable — do NOT fail the run.',
    '',
    'Then judge what the demo proves for the rubric, especially: "Wurden mit Hilfe der KI intelligente und flexible Services gebaut" (live AI classification + fallback) and "Sub-Systeme ... unabhängig als Container verteilt und betrieben" (it is deployed and running, containerized). Return the structured result with concrete observations and any issues.',
  ].join('\n'),
  { label: 'examine:demo', phase: 'Examine', schema: DEMO_SCHEMA },
)

const examinePrompt = (b) => [
  'You are a strict, fair CAS-AISE examiner grading the FlowHub Projektarbeit submission. You are responsible ONLY for the rubric bucket: "' + b.key + '" (max ' + b.max + ' points).',
  b.note ? ('NOTE: ' + b.note) : '',
  '',
  'The canonical rubric is ' + RUBRIC + ' — open it and use the EXACT scales. Your items in this bucket:',
  b.items.map((i) => '  - ' + i).join('\n'),
  '',
  'Grade against the REAL rendered submission, exactly as the examiner receives it:',
  '  - Primary source: the extracted text of the real bundle PDF at ' + BUNDLE_TXT + ' (grep/Read it). This is the single PDF uploaded to Moodle; if content is absent from the bundle it effectively does not count, even if it exists elsewhere in the repo.',
  '  - You MAY also open the underlying repo files for depth/cross-check: ' + b.read.join(', ') + '.',
  '',
  'For each item: choose the single best-supported discrete level on its scale, justify it in the examiner voice, cite concrete evidence (section/heading/page in the bundle), and list what is missing to reach the next level. Do not invent evidence. Do not be a grade-inflator: award the top level only if the criterion is genuinely "vollständig bzw. korrekt". Sum the bucket. Return the structured result.',
].filter(Boolean).join('\n')

const skepticPrompt = (b, examined) => [
  'You are an adversarial second examiner (the skeptical co-grader) for the CAS-AISE bucket "' + b.key + '". A first examiner produced these scores:',
  JSON.stringify(examined, null, 2),
  '',
  'Your job is to challenge over-generous scoring. For each item, verify the cited evidence actually exists at the claimed strength in the real bundle text (' + BUNDLE_TXT + ', grep for the cited terms). Where the first examiner awarded a level the evidence does not fully support, recommend a lower (or, rarely, higher) value with a crisp reason. Default to challenging: if evidence is vague, hand-wavy, or merely asserted, push the score down. Compute the adjusted bucket total. Be specific and cite what you checked.',
].join('\n')

// Pipeline: each bucket is graded, then immediately challenged by the skeptic.
const graded = await pipeline(
  BUCKETS,
  (b) => agent(examinePrompt(b), { label: 'grade:' + b.key, phase: 'Examine', schema: BUCKET_SCHEMA }).then((r) => ({ b, examined: r })),
  ({ b, examined }) => {
    if (!examined) return null
    return agent(skepticPrompt(b, examined), { label: 'skeptic:' + b.key, phase: 'Skeptic', schema: SKEPTIC_SCHEMA })
      .then((skeptic) => ({ bucket: b.key, max: b.max, note: b.note || '', examined, skeptic }))
  },
)

const demo = await demoPromise
const buckets = graded.filter(Boolean)

// ════════════════════════════════════════════════════════════════════════════
// Phase 2 — Verdict: aggregate into the grade sheet and write the report
// ════════════════════════════════════════════════════════════════════════════
phase('Verdict')

const verdict = await agent(
  [
    'You are the lead CAS-AISE examiner writing the final grade sheet for the FlowHub Projektarbeit. Produce an honest, evidence-anchored verdict and WRITE IT to disk.',
    '',
    'Run metadata: date=' + effDate + ', commit=' + effCommit + ', demo=' + demoUrl + '.',
    'Build step result: ' + JSON.stringify(build) + '.',
    '',
    'Per-bucket grading (first examiner + skeptic adjustment):',
    JSON.stringify(buckets, null, 2),
    '',
    'Live demo examination:',
    JSON.stringify(demo, null, 2),
    '',
    'Rules:',
    '- Max achievable is 90 (Quarkus/Jakarta-EE item excluded for the .NET stack — state this explicitly).',
    '- For each item, choose a FINAL awarded value: start from the first examiner, and where the skeptic raised a well-founded dispute, move toward the skeptic. Show both the first-pass and final value.',
    '- Fold the live-demo findings into the KI/Sub-Systeme bucket items ("intelligente und flexible Services" and "Container/Sub-Systeme") — the working live demo is first-hand evidence.',
    '- If the build step reported warnings or built=false, reflect that as real risk (a broken/incomplete bundle PDF is what the examiner would actually receive).',
    '',
    'Write a Markdown report to ' + REPORT + ' (mkdir -p its directory first) with these sections:',
    '  1. Title + run metadata (date, commit, demo URL, bundle pages).',
    '  2. Overall result: FINAL score X / 90, plus a one-line grade band and a 3-4 sentence examiner summary.',
    '  3. Per-bucket table: bucket | first-pass | final | max.',
    '  4. Per-item detail table for every rubric item: item | scale | first-pass | final | justification | key evidence | gap-to-next-level.',
    '  5. Live demo walkthrough: what was submitted, how it was classified, rate-limit/embeddings/reset posture, screenshot links (' + SHOTS + '), and issues.',
    '  6. Top gaps ranked by point-leverage (where the cheapest points are).',
    '  7. Defense questions: the 8-12 sharpest questions a real examiner would ask in the oral defense, grouped by bucket.',
    '  8. Skeptic disputes: a short table of where scores were challenged and the resolution.',
    '',
    'After writing, return the structured summary.',
  ].join('\n'),
  {
    label: 'verdict:grade-sheet',
    phase: 'Verdict',
    schema: {
      type: 'object',
      required: ['totalAwarded', 'max', 'band', 'reportPath', 'perBucket', 'topGaps'],
      properties: {
        totalAwarded: { type: 'number' },
        max:          { type: 'number' },
        band:         { type: 'string' },
        reportPath:   { type: 'string' },
        perBucket: {
          type: 'array',
          items: {
            type: 'object',
            required: ['bucket', 'final', 'max'],
            properties: { bucket: { type: 'string' }, firstPass: { type: 'number' }, final: { type: 'number' }, max: { type: 'number' } },
          },
        },
        topGaps:  { type: 'array', items: { type: 'string' } },
        oneLiner: { type: 'string' },
      },
    },
  },
)

log('Verdict: ' + (verdict ? verdict.totalAwarded + '/' + verdict.max + ' — ' + verdict.band : 'no verdict produced'))

return {
  stamp,
  date: dateStr,
  commit,
  report: verdict && verdict.reportPath,
  score: verdict && (verdict.totalAwarded + '/' + verdict.max),
  band: verdict && verdict.band,
  perBucket: verdict && verdict.perBucket,
  topGaps: verdict && verdict.topGaps,
  buildOk: build && build.built,
  demoReachable: demo && demo.reachable,
}
