export const meta = {
  name: 'examiner-sim',
  description: 'Simulate the CAS-AISE examiner: rebuild the real submission PDFs, grade them against the Moodle rubric with a multi-agent panel, exercise the live public demo, and produce a dated grade sheet.',
  whenToUse: 'Run before a real exam/submission checkpoint to get an honest, rubric-anchored grade prediction against the freshly-built artifacts and the live demo. Repeatable.',
  phases: [
    { title: 'Build', detail: 'regenerate the 5 uploaded PDFs (package-submission) + the convenience bundle, extract text per uploaded PDF' },
    { title: 'Examine', detail: '5 rubric-bucket examiners + 1 live-demo examiner, in parallel' },
    { title: 'Architecture', detail: 'deep architecture lenses (focus=architecture only): ADRs, structure fidelity, behavior/interaction views, deployment topology, NFR alignment' },
    { title: 'Skeptic', detail: 'adversarial pass — challenge over-generous scores per bucket' },
    { title: 'Verdict', detail: 'aggregate to a grade sheet (/100), defense questions, ranked gaps' },
  ],
}

// ── Inputs (from the /examiner-sim slash command, or sensible defaults) ───────
const stamp   = (args && args.stamp)  || 'latest'           // e.g. "2026-06-07T0758"
const dateStr = (args && args.date)   || 'unknown-date'     // e.g. "2026-06-07"
const commit  = (args && args.commit) || 'unknown-commit'
const demoUrl = (args && args.demoUrl) || 'https://demo.flowhub.freaxnx01.ch'
// focus: 'balanced' (default) grades all buckets evenly; 'architecture' adds a
// deep architecture-lens phase and tells the design/structure examiners to be
// extra rigorous. Always still produces the full /100 grade sheet (comparable).
const focus   = (args && args.focus)  || 'balanced'
const archFocus = focus === 'architecture'
// repoDir: the checkout to grade. Default '.' = the workflow's current cwd.
// Pass an absolute path (e.g. another worktree) to grade a different branch.
const REPO    = (args && args.repoDir) || '.'

// agent-dev runs on a memory-capped LXC (shared Proxmox node). Under current
// Claude Code, bare `grep`/`find` are shimmed to a bundled ugrep/bfs engine that
// can balloon to many GB and OOM the host (see CLAUDE.md "Search discipline").
// Every examiner prompt carries this guard so agents search with ripgrep only.
const SEARCH_GUARD = 'SEARCH SAFETY (shared host): when you search files, ALWAYS use `rg` (ripgrep) — never `grep`, `ugrep`, or `grep -r`. Scope `rg` to the specific file or dir you need. ripgrep is memory-bounded; bare `grep` is shimmed here to an engine that has OOM-ed the host.'

// ── Progress (overall %) ──────────────────────────────────────────────────────
// Coarse but honest: emit a running percentage at each phase boundary that
// actually resolves. Architecture only counts on a focus==='architecture' run.
const TOTAL_STEPS = archFocus ? 4 : 3
let stepDone = 0
const progress = (label) => {
  stepDone++
  const pct = Math.round((stepDone / TOTAL_STEPS) * 100)
  log('[' + pct + '% · step ' + stepDone + '/' + TOTAL_STEPS + '] ' + label)
}

const RUBRIC      = REPO + '/vault/Organisation/Bewertungskriterien.md'
const WORK        = REPO + '/tools/build/examiner-sim'
const BUNDLE_TXT  = WORK + '/bundle.txt'        // convenience superset (repo-only safety net, NOT uploaded)
const UPLOAD_TXT  = WORK + '/upload.txt'        // concatenated text of the 5 PDFs actually uploaded to Moodle
const SUB_TXT     = WORK + '/submission.txt'    // Übersicht only (== uploaded PDF #00)
const SHOTS       = REPO + '/nachbereitung/examiner-sim/screenshots'
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
    evidence:      { type: 'array', items: { type: 'string' }, description: 'concrete citations: file/section/page in the uploaded PDFs (or bundle) that justify the score' },
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
    embeddings503: { type: 'boolean', description: 'did /api/v1/captures/search return 503 ProblemDetails (embeddings deliberately disabled on the demo; transparent, not hidden)' },
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
    uploadTxt:     { type: 'string', description: 'path to concatenated text of the 5 uploaded PDFs (what Moodle receives)' },
    uploadFiles:   { type: 'array', items: { type: 'string' }, description: 'uploaded PDFs with page counts, e.g. "02_FlowHub_Projektbeschreibung.pdf (27p)"' },
    commit:        { type: 'string', description: 'git rev-parse --short HEAD' },
    stamp:         { type: 'string', description: 'filesystem-safe timestamp, e.g. 2026-06-07T0758 from `date +%Y-%m-%dT%H%M`' },
    date:          { type: 'string', description: 'YYYY-MM-DD from `date +%F`' },
    freshness:     { type: 'string', description: 'evidence the PDFs were rebuilt this run (mtime)' },
    warnings:      { type: 'array', items: { type: 'string' } },
    notes:         { type: 'string' },
  },
}

// ── Rubric buckets (mirrors vault/Organisation/Bewertungskriterien.md) ────────
// Rubric update June 2026: framework-concepts item is framework-neutral and in scope → /100.
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
      '"Ist der Entwurf aus den verschiedenen Perspektiven (Struktur, Verhalten, Interaction) beschrieben" (0/1/4/7)',
      '"Ist das DB-Model spezifiziert" (0/1/2/3)',
    ],
    read: ['docs/adr', 'docs/architektur', 'docs/projektbeschreibung', 'docs/design/db/entities.md', 'docs/design/db/er.md'],
  },
  {
    key: 'Programmierung', max: 22,
    note: 'Rubric update (June 2026): the framework-concepts item is now framework-NEUTRAL (was Quarkus/Jakarta-EE) and fully in scope for .NET — no exclusion; total is /100.',
    items: [
      '"Ist der Code lesbar, dokumentiert und nach Schichten und Modulen mit klaren Verantwortlichkeiten strukturiert" (0/1/4/7)',
      '"Wurden die Konzepte des gewählten Frameworks und moderner Applikationsentwicklung sachgerecht eingesetzt (z. B. Dependency Injection, REST-Schnittstellen, Configuration, Fehlerbehandlung)" (0/3/7/10) — framework-neutral; grade the .NET stack (ASP.NET Core) on its own terms, evidence in docs/spec/modern-app-concepts.md + source/',
      '"Sind die Erkenntnisse aus der Programmierung dokumentiert" (0/1/2/3)',
      '"Ist der Source-Code in einem Git-Repository verfügbar" (0/2)',
    ],
    read: ['source', 'docs/spec/modern-app-concepts.md', 'docs/insights', 'docs/ci-cd.md', 'README.md', 'CLAUDE.md'],
  },
  {
    key: 'Validierung', max: 16,
    items: [
      '"Ist definiert, welches die Abnahmekriterien sind" (0/1/3/5)',
      '"Ist spezifiziert, wie die Application getestet wird und welche Technologien dazu verwendet werden" (0/1/3/5)',
      '"Sind Unit-Tests programmiert" (0/1/3)',
      '"Sind die Test-Ergebnisse dokumentiert" (0/1/3)',
    ],
    read: ['docs/spec/acceptance-criteria.md', 'docs/spec/testing-strategy.md', 'tests', 'docs/insights'],
  },
  {
    key: 'KI und Architektur', max: 30,
    items: [
      '"Wurden KI-unterstützende Werkzeuge verwendet und deren Nutzung beschrieben" (0/1/7/12)',
      '"Wurden mit Hilfe der KI intelligence und flexible Services gebaut" (0/2/6)',
      '"Ist die Lösung in klar abgegrenzte Module bzw. Sub-Systeme strukturiert (modularer Monolith ODER verteilte Services) und also Container lauffähig betrieben" (0/1/3/5) — rubric update June 2026: a modular monolith run as a container now explicitly qualifies for full marks; do NOT require independently-deployable per-subsystem containers',
      '"Sind die Erfahrungen während der Projektarbeit mit KI-unterstützenden Werkzeugen also Fazit reflektiert" (0/1/4/7)',
    ],
    read: ['docs/ai-usage.md', 'vault/Projektarbeit/Learnings.md', 'docs/insights', 'docker-compose.yml', 'demo', 'source/FlowHub.AI', '.ai', '.claude'],
  },
]

// ── Architecture deep-dive lenses (focus === 'architecture' only) ─────────────
const ARCH_SCHEMA = {
  type: 'object',
  required: ['lens', 'rating', 'strengths', 'weaknesses', 'risks', 'evidence', 'recommendations'],
  properties: {
    lens:            { type: 'string' },
    rating:          { type: 'string', description: 'strong | adequate | weak — the examiner verdict for this lens' },
    strengths:       { type: 'array', items: { type: 'string' } },
    weaknesses:      { type: 'array', items: { type: 'string' } },
    risks:           { type: 'array', items: { type: 'string' }, description: 'architectural risks an examiner would probe' },
    evidence:        { type: 'array', items: { type: 'string' }, description: 'concrete citations: ADR id, diagram, source path, bundle page' },
    recommendations: { type: 'array', items: { type: 'string' } },
    rubricImpact:    { type: 'string', description: 'which rubric items this lens informs (Entwurf, Programmierung-structure, KI und Architektur)' },
  },
}
const ARCH_LENSES = [
  {
    key: 'adr-quality',
    title: 'ADR coherence & decision quality',
    prompt: 'Read every ADR (docs/adr/0001..0006). For each: is the Context/Decision/Consequences/Alternatives structure complete, are the alternatives real and weighed, and — critically — is the decision actually reflected in the code and the rest of the docs (no drift)? Flag any ADR that is aspirational vs implemented. Assess the modular-monolith decision (ADR 0002) and the async-pipeline decision (ADR 0003) hardest.',
    read: ['docs/adr', 'source/FlowHub.slnx', 'docker-compose.yml'],
  },
  {
    key: 'structure-fidelity',
    title: 'Documented architecture vs actual code structure',
    prompt: 'Compare the documented architecture (C4 diagrams, hexagonal layering, modular monolith, the FlowHub.<Capability> split) against the ACTUAL source tree. Open FlowHub.slnx and the source/ projects. Verify: driving/driven ports live in FlowHub.Core, adapters in Persistence/Skills/AI, no cross-module project refs, DI registration per module. Call out placeholder/empty projects (e.g. FlowHub.Telegram, FlowHub.Integrations) that are advertised as layers but not implemented, and any layer-leakage (EF types in Core, etc.).',
    read: ['source', 'source/FlowHub.Core', 'source/FlowHub.Persistence', 'CLAUDE.md'],
  },
  {
    key: 'behavior-interaction',
    title: 'Behavioral & interaction perspectives',
    prompt: 'The Entwurf rubric demands Struktur AND Verhalten AND Interaction. Structure is well covered; scrutinize the other two. Is there a rendered sequence diagram (Capture→Classify→Route), a state machine for the capture lifecycle (Raw→Classified→Routed/Unhandled/Orphan), and an activity/async view (ADR 0003 pipeline)? Distinguish prose/tables from actual rendered diagrams IN THE BUNDLE. Identify exactly what behavioral/interaction artifact is missing and what it would take to render it into the bundle.',
    read: ['docs/adr/0003-async-pipeline.md', 'docs/projektbeschreibung', 'docs/design', 'docs/spec/use-cases.md'],
  },
  {
    key: 'deployment-topology',
    title: 'Deployment topology & sub-system independence',
    prompt: 'Assess the runtime topology against "Sub-Systeme unabhängig also Container verteilt und betrieben". Read docker-compose.yml, the demo overlay, the Dockerfile, and the release CI. Which images are first-party and independently built/pushed/scaled vs which are attached backing services (postgres/rabbitmq/prometheus/grafana) vs which are the same codebase (web + migrations init-job)? Be precise about what genuine decomposition exists today, and what changing it to true microservices would cost — and whether that is even the right call given the deliberate modular-monolith decision. This is the lens where the submission is weakest; be exact.',
    read: ['docker-compose.yml', 'demo', '.github/workflows', 'docs/adr/0002-service-architecture-and-async-communication.md'],
  },
  {
    key: 'nfr-crosscutting',
    title: 'NFR ↔ architecture alignment & cross-cutting concerns',
    prompt: 'Do the architecture decisions actually satisfy the SMART non-functional requirements, and are cross-cutting concerns architected (not bolted on)? Check: observability (OpenTelemetry/Prometheus/Grafana, health endpoints), error contract (RFC 9457 ProblemDetails consistency), resilience/fallback (AiClassifier→KeywordClassifier, cost guards), config/secrets (12-factor, env vars), data residency / AI-transparency NFRs. Map each major NFR to the architectural mechanism that delivers it, and flag NFRs with no architectural backing.',
    read: ['docs/spec/nfa.md', 'docs/adr/0004-ai-integration-in-services.md', 'source/FlowHub.AI', 'docs/ci-cd.md'],
  },
]
const archPrompt = (L) => [
  'You are a senior software architect acting as the CAS-AISE examiner, doing a DEEP architecture review of the FlowHub Projektarbeit. Your single lens: ' + L.title + '.',
  '',
  L.prompt,
  '',
  SEARCH_GUARD,
  'The repository checkout you are grading is at: ' + REPO + ' (cd there for shell tools; use absolute paths). Ground every claim in evidence. Primary source is the real rendered bundle text at ' + BUNDLE_TXT + ' (search it with `rg`), but for architecture you SHOULD also open the actual repo artifacts: ' + L.read.map((p) => REPO + '/' + p).join(', ') + '. Distinguish what is documented from what is implemented from what is merely asserted. Be rigorous and specific — this is an architecture deep-dive, not a checklist. Return the structured finding.',
].join('\n')

// ════════════════════════════════════════════════════════════════════════════
// Phase 0 — Build the real artifacts and extract their text
// ════════════════════════════════════════════════════════════════════════════
phase('Build')
const build = await agent(
  [
    'You are the build step of an examiner simulation. The examiner must grade the REAL rendered submission PDFs, not the markdown sources.',
    'FIRST: cd into the repository checkout you are grading: ' + REPO + '  (all steps below run there; the extracted-text and report paths are given as absolute paths).',
    'Then, from that repo root:',
    '',
    '1. Regenerate the REAL Moodle upload set — the 5 separately-uploaded PDFs (puppeteer renderer):',
    '   just package-submission',
    '   This builds + numbers ./upload/00_FlowHub_Uebersicht.pdf, 01_FlowHub_Arc42.pdf, 02_FlowHub_Projektbeschreibung.pdf, 03_FlowHub_Reflexion.pdf, 04_FlowHub_Eigenstaendigkeitserklaerung.pdf. NOTE: the Präsentation is intentionally NOT uploaded (it is linked from the Übersicht) — do not expect it in ./upload/.',
    '   Also build the convenience superset: just pdf-submission-bundle  (the repo-only safety net, NOT uploaded — it inlines repo-only artifacts too). Both use `set -euo pipefail`; a non-zero exit means a referenced/built file was missing — capture that as a warning and continue.',
    '   If `just` is unavailable, read those justfile targets and run the equivalent renderer commands.',
    '2. mkdir -p ' + WORK,
    '3. Extract the REAL PDF text so the examiners read EXACTLY what the examiner receives:',
    '   - Upload set (PRIMARY): for each ./upload/0*_*.pdf in 00..04 order, append a "===== <filename> =====" header then its `pdftotext -layout` output, concatenated into ' + UPLOAD_TXT + '.',
    '   - Übersicht alone into ' + SUB_TXT + ' (pdftotext -layout ./upload/00_FlowHub_Uebersicht.pdf).',
    '   - Convenience bundle into ' + BUNDLE_TXT + ' (pdftotext -layout SUBMISSION-bundle.pdf).',
    '4. Record freshness: `ls -l --time-style=full-iso upload/*.pdf SUBMISSION-bundle.pdf` and `pdfinfo` page counts per upload PDF + bundle. Confirm mtimes are from this run; populate uploadFiles (name + page count) and bundlePages.',
    '5. Sanity-check ' + UPLOAD_TXT + ' is non-empty and contains the cover title "FlowHub", the Arc42 architecture content, and the Projektbeschreibung.',
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
const focusSuffix = archFocus ? '-architecture' : ''
const REPORT    = REPO + '/nachbereitung/examiner-sim/report-' + effStamp + focusSuffix + '.md'

log('Build: ' + (build && build.built ? 'PDFs rebuilt (' + build.bundlePages + 'p) @ ' + effCommit : 'BUILD ISSUE — see report') + ' [focus=' + focus + ']')
progress('Build complete — ' + (build && build.built ? build.bundlePages + 'p PDFs @ ' + effCommit : 'build issue, see report'))

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
    '   Capture the returned id, then GET ' + demoUrl + '/api/v1/captures/{id} a few seconds later (poll up to ~10s). Expect stage to advance from Raw to "Completed" with a real externalRef, a matchedSkill (e.g. Wallabag for a read-later URL), and an AI-enriched title. The demo runs THREE live skill integrations (Vikunja, Wallabag, Paperless), each writing to a self-contained, sandboxed demo instance (NOT the real homelab services), wiped every 15-min reset and bounded by a per-IP rate limit — so a real downstream write reaching "Completed" is EXPECTED, not a defect.',
    '3. Submit a second, different capture (e.g. a task-like text "todo: buy milk" or a film tip) to show classification variety / flexibility.',
    '4. Embeddings posture: GET ' + demoUrl + '/api/v1/captures/search?q=paper → expect HTTP 503 with a ProblemDetails body (embeddings are deliberately disabled in the demo profile; transparent, not hidden — the feature is demonstrated via integration tests + ADR 0006).',
    '5. Rate-limit posture: fire ~25 rapid GETs to / and report the status-code progression (expect 200s rolling into 429s after the burst). Example:',
    '   for i in $(seq 1 25); do curl -s -o /dev/null -w "%{http_code} " ' + demoUrl + '/; done',
    '6. Reset/demo posture: note the demo banner / 15-min self-reset behavior if observable (the home HTML contains the demo banner text).',
    '',
    'Visual evidence (best-effort, must not block):',
    '7. Try to screenshot the live UI with the cached Playwright Chromium. Write a tiny node script using playwright (try the project node_modules or `npx playwright`); navigate to ' + demoUrl + '/ and the Captures list page, save ONGs into ' + SHOTS + '/ (e.g. home-' + effStamp + '.png and captures-list-' + effStamp + '.png). If Playwright is not usable, skip screenshots, set screenshots=[] and add an issue noting visual capture was unavailable — do NOT fail the run.',
    '',
    'Then judge what the demo proves for the rubric, especially: "Wurden mit Hilfe der KI intelligence und flexible Services gebaut" (live AI classification + fallback) and "Sub-Systeme ... unabhängig also Container verteilt und betrieben" (it is deployed and running, containerized). Return the structured result with concrete observations and any issues.',
  ].join('\n'),
  { label: 'examine:demo', phase: 'Examine', schema: DEMO_SCHEMA },
)

// Architecture deep-dive lenses run concurrently with the bucket pipeline,
// only when focus === 'architecture'. They feed the verdict, not the /100 totals.
const archPromise = archFocus
  ? parallel(ARCH_LENSES.map((L) => () => agent(archPrompt(L), { label: 'arch:' + L.key, phase: 'Architecture', schema: ARCH_SCHEMA })))
  : Promise.resolve([])

const examinePrompt = (b) => [
  'You are a strict, fair CAS-AISE examiner grading the FlowHub Projektarbeit submission. You are responsible ONLY for the rubric bucket: "' + b.key + '" (max ' + b.max + ' points).',
  b.note ? ('NOTE: ' + b.note) : '',
  '',
  'The repository checkout you are grading is at: ' + REPO + ' — use these absolute paths (cd there for shell tools).',
  SEARCH_GUARD,
  'The canonical rubric is ' + RUBRIC + ' — open it and use the EXACT scales. Your items in this bucket:',
  b.items.map((i) => '  - ' + i).join('\n'),
  '',
  'ANKER-REGEL (rubric update June 2026): every level has concrete Bewertungsanker in ' + RUBRIC + '. A level counts as reached ONLY when ALL anchors of that level are satisfied; if the anchors are only partially met, award the NEXT-LOWER level. Read the per-level anchors for each item, choose the highest level whose anchors are fully met, and justify the choice explicitly against that level\'s anchor (and against the next level\'s unmet anchor).',
  '',
  'Grade against the REAL rendered submission, exactly as the examiner receives it:',
  '  - PRIMARY source: the text of the 5 uploaded PDFs at ' + UPLOAD_TXT + ' (search it with `rg`, or Read it) — Übersicht, Arc42, Projektbeschreibung, Reflexion, Eigenständigkeitserklärung. This is literally what Moodle receives.',
  '  - The examiner also has the GitHub repo, linked from the Übersicht. The bundle text at ' + BUNDLE_TXT + ' inlines those repo-only artifacts (full use-cases, design docs, all ADRs, Block-Nachbereitungen) as a SUPERSET. Content found ONLY in the bundle (not in ' + UPLOAD_TXT + ') is reachable by the real examiner only by following a repo link — treat it as weaker/secondary evidence and say so when a score leans on it.',
  '  - You MAY also open the underlying repo files for depth/cross-check: ' + b.read.map((p) => REPO + '/' + p).join(', ') + '.',
  '',
  (archFocus && (b.key === 'Entwurf' || b.key === 'Programmierung' || b.key.startsWith('KI')))
    ? 'ARCHITECTURE-FOCUS RUN: be extra rigorous on architectural substance. Separate documented-from-implemented; distinguish rendered diagrams from prose; verify the C4/hexagonal/modular-monolith claims against the actual source tree; do not award structure points for placeholder/empty projects. A dedicated architecture panel is also reviewing — your bucket scoring must be defensible against it.'
    : '',
  'For each item: choose the single best-supported discrete level on its scale, justify it in the examiner voice, cite concrete evidence (section/heading/page in the bundle), and list what is missing to reach the next level. Do not invent evidence. Do not be a grade-inflator: award the top level only if the criterion is genuinely "vollständig bzw. korrekt". Sum the bucket. Return the structured result.',
].filter(Boolean).join('\n')

const skepticPrompt = (b, examined) => [
  'You are an adversarial second examiner (the skeptical co-grader) for the CAS-AISE bucket "' + b.key + '". A first examiner produced these scores:',
  JSON.stringify(examined, null, 2),
  '',
  SEARCH_GUARD,
  'Your job is to challenge over-generous scoring. For each item, verify the cited evidence actually exists at the claimed strength in the real uploaded-PDF text (' + UPLOAD_TXT + ', search for the cited terms with `rg`; the bundle ' + BUNDLE_TXT + ' is only a repo-link superset — push back on any score that leans on bundle-only content). Where the first examiner awarded a level the evidence does not fully support, recommend a lower (or, rarely, higher) value with a crisp reason. Default to challenging: if evidence is vague, hand-wavy, or merely asserted, push the score down. Compute the adjusted bucket total. Be specific and cite what you checked.',
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
const archFindings = (await archPromise).filter(Boolean)
const buckets = graded.filter(Boolean)
progress('Examine + Skeptic complete — ' + buckets.length + '/' + BUCKETS.length + ' rubric buckets graded & challenged; demo ' + (demo && demo.reachable ? 'reached' : 'unreached'))
if (archFocus) progress('Architecture lenses complete — ' + archFindings.length + '/' + ARCH_LENSES.length)

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
    archFocus
      ? 'ARCHITECTURE DEEP-DIVE findings (this is an architecture-focus run — weight these heavily and devote a dedicated report section to them):\n' + JSON.stringify(archFindings, null, 2)
      : '',
    'Rules:',
    '- Max achievable is 100 (rubric update June 2026: the framework-concepts item is framework-neutral and fully in scope for .NET; no item is excluded — state this explicitly).',
    '- For each item, choose a FINAL awarded value: start from the first examiner, and where the skeptic raised a well-founded dispute, move toward the skeptic. Show both the first-pass and final value.',
    '- Fold the live-demo findings into the KI/Sub-Systeme bucket items ("intelligence und flexible Services" and "Container/Sub-Systeme") — the working live demo is first-hand evidence.',
    '- If the build step reported warnings or built=false, reflect that as real risk (broken/incomplete uploaded PDFs — or content reachable only via repo links — are what the examiner would actually receive).',
    '',
    'Write a Markdown report to ' + REPORT + ' (mkdir -p its directory first) with these sections:',
    '  1. Title (note focus=' + focus + ') + run metadata (date, commit, demo URL, bundle pages).',
    '  2. Overall result: FINAL score X / 100, plus a one-line grade band and a 3-4 sentence examiner summary.',
    '  3. Per-bucket table: bucket | first-pass | final | max.',
    '  4. Per-item detail table for every rubric item: item | scale | first-pass | final | justification | key evidence | gap-to-next-level.',
    '  5. Live demo walkthrough: what was submitted, how it was classified, rate-limit/embeddings/reset posture, screenshot links (' + SHOTS + '), and issues.',
    archFocus
      ? '  5b. ARCHITECTURE DEEP-DIVE (the centerpiece of this run): one subsection per lens (ADR quality, structure fidelity, behavior/interaction, deployment topology, NFR alignment) with rating, key strengths, weaknesses, risks, evidence and recommendations. Then an "Architecture verdict" paragraph and a prioritized architecture-improvement roadmap.'
      : '',
    '  6. Top gaps ranked by point-leverage (where the cheapest points are).' + (archFocus ? ' Call out which are architectural.' : ''),
    '  7. Defense questions: the 8-12 sharpest questions a real examiner would ask in the oral defense, grouped by bucket' + (archFocus ? ' — with an extra architecture-defense block (modular-monolith trade-off, behavioral views, sub-system decomposition, NFR backing).' : '.'),
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
progress('Verdict complete — ' + (verdict ? verdict.totalAwarded + '/' + verdict.max + ' (' + verdict.band + ')' : 'no verdict'))

return {
  focus,
  stamp: effStamp,
  date: effDate,
  commit: effCommit,
  report: (verdict && verdict.reportPath) || REPORT,
  score: verdict && (verdict.totalAwarded + '/' + verdict.max),
  band: verdict && verdict.band,
  perBucket: verdict && verdict.perBucket,
  topGaps: verdict && verdict.topGaps,
  archLenses: archFindings.length,
  buildOk: build && build.built,
  demoReachable: demo && demo.reachable,
}
