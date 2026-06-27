# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-06-20

### Added

- **demo:** publish public demo on VPS-DE
- **demo:** richer seed fixtures showcasing the full feature set
- **demo:** add try-me example prompts to the demo banner
- **web:** demo-only one-click example chips in quick-capture
- **demo:** example chips auto-open Captures list; scrub seed samples
- **demo:** publish capture notifications to ntfy.sh (dormant until configured)
- **demo:** clickable 'Source on GitHub' link in demo banner
- **enrichment:** wire citation enrichment end-to-end (demo showcase)
- **examiner-sim:** repeatable multi-agent examiner simulation (#21)
- **demo:** add live Vikunja routing to the public demo
- **video:** code-based explainer videos (technical + end-user) (#32)
- **persistence:** add OpenReadAsync to attachment storage
- **core:** flag attachment captures on CaptureCreated
- **pipeline:** route attachment captures to Paperless without LLM
- **skills:** add PaperlessOptions
- **skills:** add Paperless document-upload skill
- **skills:** register Paperless integration when configured
- **skills:** log Wallabag token-refresh failures
- **demo:** add paperless-ngx bootstrap sidecar
- **demo:** add wallabag bootstrap sidecar
- **demo:** run live wallabag + paperless-ngx services
- **demo:** VPS Traefik routes for wallabag + paperless
- **demo:** clear wallabag + paperless on each reset cycle
- **video:** generate explainer videos (English voice, logo) + embed in README (#41)
- route quote captures to Vikunja 'Zitate' (rename Quotes->Zitate) (#39)
- **core:** add ClassifierTrace to classification result
- **core:** keyword classifier records a trace
- **ai:** AiClassifier records provider/model/token trace
- **core:** carry ClassifierTrace on Capture
- thread ClassifierTrace through MarkClassifiedAsync
- **persistence:** persist ClassifierTrace as owned entity (+migration)
- **ai:** config-backed classification cost estimator
- **web:** DemoTraceOptions gate + enable on demo VPS
- **web:** classification trace panel on capture detail (gated)
- **demo:** quick-access links to all three live services in the banner
- **api:** add multipart file-upload capture endpoint
- **demo:** cleaner banner — service links as a tidy action row
- **demo:** use an on-theme German Zitat as the quote Try-example
- **demo:** live Zitate board + richer quote enrichment
- **demo:** use an Einstein quote for the Zitat Try-example
- **video:** demo walkthrough — screenshot the live VPS demo, silent + animated cursor (#45)
- **demo:** add Walkthrough link to the demo banner (#48)
- **demo:** add Uptime Kuma monitoring to the VPS demo (#52)
- **monitoring:** self-healing restart policies + surface observability in submission/deck (#54)
- **demo:** link the Uptime Kuma status page from the demo banner (#55)
- **search:** live semantic search on the demo via self-hosted embedder + Search UI
- **pdf:** guard against unrendered mermaid diagrams + refresh stale Projektbeschreibung PDF (#71)
- **pdf:** offline mermaid + bundle bookmarks + trim to 267pp (#72)
- **docs:** as-built Arc42 v2 + Reflexion (two-document submission) (#75)

### CI/CD

- render test report + coverage summary in the run summary
- bump actions to Node24 majors; reconcile test count to 253

### Changed

- **persistence:** honor CancellationToken in OpenReadAsync
- **skills:** single-owner disposal + partial + stronger Paperless test
- **skills:** drive Wallabag with self-refreshing OAuth token
- **ai:** document trace int-casts + assert keyword-trace nulls
- **web:** tidy trace-panel injects + explicit chip variant

### Documentation

- **vault:** add 'examiner' glossary term; replace examiner prename with role
- add FEATURES.md; fix dangling demo-limitations reference in README
- **demo:** add GitHub repo URL to the demo banner
- reconcile living docs with post-v0.1.0 product/demo work
- honesty pass — reconcile documented-but-not-built claims (#23)
- **prep:** add PVA defense prep + gap-fill plan (#24)
- map Quarkus/Jakarta-EE criterion to .NET equivalents (potential +3–7) (#26)
- **readme:** add Features section linking FEATURES.md
- final accuracy pass — fix doc-vs-code residues (~+3-6) (#27)
- **demo:** document live Vikunja routing (FEATURES, DEMO, runbook)
- embed verified test run + relabel observability (micro-pass) (#28)
- **presentation:** add Marp CAS slide deck (project + AI-build reflection) (#30)
- claim Quarkus/Jakarta-EE criterion via .NET equivalents (toward /100) (#31)
- **prep:** add Quarkus-criterion defense answer + refresh numbers (#33)
- **demo:** spec + plan for three live services (#37)
- **demo:** document wallabag + paperless env + banner
- **readme:** use GitHub native players for explainer videos (#43)
- **spec:** design for routing quote captures to Vikunja 'Zitate' (#39)
- **plan:** implementation plan for Zitate routing (#39)
- spec for classification trace mode (#35)
- implementation plan for classification trace mode (#35)
- **ai:** note demo-model sync requirement for free pricing
- **readme:** link related repos (ai-instructions, agent-pipeline) (#44)
- **demo:** correct stale read-only-share comment in compose overlay
- **roadmap:** add USER.md personal-context idea for skill generation
- align with June-2026 rubric update (neutralize Quarkus/Java, /100) (#47)
- move loose project docs into docs/project/ (#46)
- **roadmap:** add "Now Playing on SRF 3" skill idea (#49)
- **ci-cd:** clarify GitHub-vs-GitLab platform choice + deployment scope (#50)
- add /metrics uptime monitor + LLMeter benchmarking roadmap idea (#53)
- **vault/block2:** add Lernziel section to Frontend Nachbereitung (#56)
- **presentation:** product+harness deck, skill-system docs, agent-pipeline rename (#40)
- add Lernziele coverage crosswalk + deferred-scope notes (#59)
- **readme:** fix stale repo-tree note for purged rubric (#61)
- **submission:** cite concrete test results (CI run, 294 green) (#62)
- **roadmap:** add extensibility-showcase items + surface roadmap in SUBMISSION (#64)
- **submission:** correct header facts + classification/target wording (#65)
- GitHub-safe §6.1 diagram (SVG→Mermaid) + readability lists (#66)
- **submission:** readability sweep — break §4 run-on bullets into lists (#67)
- **blocks:** paraphrase verbatim Moodle-Aufträge + align rubric self-checks (#68)
- **nachbereitung:** paraphrase verbatim Moodle-Aufträge into own words (#69)
- **submission:** document the semantic-search product use case
- **adr-0006:** record demo index-drop + small-model near-tie limitation honestly
- reconcile default-suite test count to 294 (#70)
- **blocks:** paraphrase verbatim Moodle Lernziele + neutralize Quarkus (#73)
- **roadmap:** add "Terminzettel → Calendar" skill idea (#76)
- add Obsidian vault as 2nd brain (AI read & write) (#77)
- **next:** correct stale test count 234 → 294 in the authoritative header (#78)
- **presentation:** give "Vault also 2nd Brain" its own slide (fix overflow) (#79)
- **reflexion:** clarify wording per review feedback (#80)
- **submission:** add Dokumentenübersicht; include presentation in upload set (#81)
- **arc42:** clarity + renderer layout fixes; add live-demo URL to hub (#82)
- **submission:** fold Dokumentenübersicht into hub; rename hub PDF → FlowHub_Uebersicht.pdf (#83)
- **submission:** land hub rename + overview-fold (missed by #83) + cleanup (#84)
- **submission:** numbered upload set (00–04) + package-submission recipe (#85)
- **submission:** use underscore prefix (00_) for upload set — survives download (#86)
- **reflexion:** add three Veto-Entscheidungen (rubric Krit. 18, June-2026) (#87)
- align to June-2026 rubric anchors (Krit. 3, 12, 16) (#88)
- apply FFHS Semesterarbeit-template structure to Arc42/Reflexion + align declaration (#91)
- note that the TOC is in the PDF bookmarks/outline (#92)
- rename Wekan→Vikunja Kanban and GitLab→Git Forge in submission docs
- **submission:** upload Projektbeschreibung, link Präsentation; add Kurzformel glossary
- **arc42:** add use-case overview table (§1.4) so RE is self-contained in the SAD
- **entwurf:** inline a representative migration + fix stale migration citation
- **submission:** land submission-doc content + Vision KI-Nutzen win; regenerate all PDFs
- **db:** correct embedding dim/migration lineage in er.md + record examiner-sim grade sheet
- **presentation:** add pipeline/prompt/services/agent-pipeline slides + demo QR
- **presentation:** add repo QR code on the Fazit slide
- **demo:** reconcile write-posture to the as-built three live integrations
- **release:** add RELEASENOTES.md with v0.1.0 entry
- **changelog:** regenerate CHANGELOG.md from Conventional Commits via git-cliff

### Fixed

- **submission:** close examiner-sim doc/packaging gaps (+~8 pts) (#22)
- **submission:** the four submission-legal gap-fills (~+5-6) (#25)
- **tools:** emit Wallabag OAuth creds (not removed ApiToken key) from --export
- **persistence:** renumber trace migration to 0011 after #39 (Zitate) rebase
- **demo:** create Wallabag user Config + make wallabag-client idempotent
- **demo:** writable upload dir in image + resilient wallabag-bootstrap
- **demo:** pin paperless worker counts (max_workers must be >0)
- **web:** dark app bar in light mode for legible Quick-Capture
- **demo:** make Vikunja Inbox board writable so visitors can tick todos
- **persistence:** seed Zitate skill as healthy
- **web:** make Quick-Capture controls legible on the dark app bar
- **web:** make Quick-Capture icon buttons visible on the dark app bar
- **video:** show a real paperless document in the demo walkthrough (#58)
- **skills:** Wallabag URL extraction + XML-doc the orchestration seam (#51)
- **submission:** encode TOC link parens + purge PVA instructor materials (#60)
- **demo:** give the embedder 2.5G + trimmed batch buffers (e5-small OOM'd at 1.5G)
- **demo:** embed reseeded fixtures atomically so search never shows a partial set
- **demo:** drop HNSW index in reset so semantic search is exact (correct ranking)
- **demo:** bigger click target for the 'Try:' example chips
- **demo:** grow the two-row app bar so the Quick-Capture input stays visible
- **demo:** widen Quick-Capture field so the Try chips stay on one row
- **web:** remove static <title> that collided with HeadOutlet (malformed tab title)
- **web:** soften Unhandled capture copy so it doesn't read as a crash (#89)

### Miscellaneous

- **gitignore:** ignore local lernziele.md (FFHS course material) (#57)
- **submission:** purge Moodle/instructor IP from public repo + harden pre-submission checklist
- **examiner-sim:** rename bucket-5 key to "KI und Architektur" to match the rubric PDF
- **examiner-sim:** grade the real 5-PDF upload set, not the bundle
- **release:** add cliff.toml and implement release-notes recipe

### Reverted

- remove temporary EMBED-DEBUG logging (umlaut/ranking investigation done)
- **demo:** remove Search UI + self-hosted embedder from the demo

### Testing

- **web:** cover trace-panel env gate on capture detail
## [0.1.0] - 2026-06-05

### Added

- **poc:** add Program.cs with interactive and demo CLI modes
- add project overview and evaluation criteria for AISE project work
- **ai:** sync AI instructions from template and add CAS course context
- **web:** scaffold FlowHub.Web Blazor app + FlowHub.Core domain (Phase 3 Step 1)
- **web:** wire Dashboard to Bogus stub services (Phase 3 Step 2)
- **web:** wire interactions, dev auth, and bUnit tests (Phase 3 Step 3)
- **web:** polish AppBar tooltips (Phase 3 Step 4)
- **web:** add NotFound template and stub pages for all planned routes
- **web:** implement New Capture page (Phase 3+4, all steps)
- **web:** implement Captures list page with filters (Phase 3+4)
- **skills:** add flowhub-capture and flowhub-triage CC-skills
- **skills:** add flowhub-issue and flowhub dispatcher CC-skills
- **core:** add Completed lifecycle stage for successful processing
- **web:** implement Capture detail page with stubbed actions (Phase 1-4)
- **web:** implement Skills and Integrations list pages
- **skills:** add flowhub-triaged label tracking to triage skill
- **poc:** add RESTful API playground for Block 3 Vorbereitung
- **skills:** expand flowhub-triage with simulate, image enrich, issue action
- **skills:** add enrichment interview, anonymization, claude-ready integration to flowhub-issue (+ triage handoff)
- **skills:** add Telegram channel drain to flowhub-triage
- **core:** add CaptureCreated and CaptureClassified events
- **core:** add IClassifier port and ClassificationResult
- **core:** add ISkillIntegration port and LoggingSkillIntegration stub
- **core:** add KeywordClassifier with URL/todo heuristics
- **core:** publish CaptureCreated on submit; add mark-methods to ICaptureService
- **pipeline:** add CaptureEnrichmentConsumer with Orphan branch
- **pipeline:** add SkillRoutingConsumer with Unknown-skill→Unhandled branch
- **pipeline:** add LifecycleFaultObserver mapping Fault<T> to Orphan/Unhandled
- **web:** wire MassTransit pipeline + classifier + skill integrations
- **api:** scaffold FlowHub.Api project + AddFlowHubApi / MapFlowHubApi extensions
- **core:** add ChannelKind.Api for REST-submitted captures
- **core:** add CaptureCursor with Base64Url JSON encode/decode
- **core:** add ListAsync + CaptureFilter + CapturePage to ICaptureService
- **core:** add ResetForRetryAsync to ICaptureService
- **api:** add POST /api/v1/captures with FluentValidation
- **api:** add GET /api/v1/captures with cursor pagination + stage/source filters
- **api:** add GET /api/v1/captures/{id} with capture-not-found problem
- **api:** add POST /api/v1/captures/{id}/retry with stage validation + republish
- **core:** extend ClassificationResult with optional Title (Slice C prep)
- **ai:** scaffold FlowHub.AI csproj with MEAI + provider packages
- **ai:** add AiPrompts with English system prompt + BuildMessages helper
- **ai:** add AiClassificationResponse DTO with JSON-schema annotations
- **ai:** add AiClassifier with structured-output + keyword fallback floor
- **ai:** add AddFlowHubAi extension with D8 registration matrix + boot logger
- **ai:** wire AddFlowHubAi into Program.cs (Slice C)
- **core:** extend Capture record with optional Title + ExternalRef (Beta MVP prep)
- **core:** add MarkCompletedAsync + Title parameter on MarkClassifiedAsync
- **pipeline:** forward classifier Title from enrichment consumer to capture service
- **skills:** refactor ISkillIntegration to HandleAsync→SkillResult; consumer marks Completed on success
- **persistence:** scaffold FlowHub.Persistence csproj with EF Core packages
- **persistence:** add CaptureEntity + FlowHubDbContext with Stage and CreatedAt indexes
- **persistence:** add EfCaptureService against EF Core (13 unit tests, InMemory provider)
- **persistence:** add AddFlowHubPersistence extension + Initial migration
- **web:** wire EF persistence in Program.cs; integration tests use InMemory provider
- **skills:** add WallabagSkillIntegration with bearer-token auth (7 unit tests)
- **skills:** add AddFlowHubSkills + SkillsBootLogger with Wallabag registration (4 tests)
- **web:** wire AddFlowHubSkills; remove LoggingSkillIntegration (Slice-B placeholder)
- **skills:** add VikunjaSkillIntegration with title fallback (6 unit tests)
- **skills:** add Vikunja branch to AddFlowHubSkills (3 extension tests)
- **web:** surface classifier Title + ExternalRef in Recent Captures + Capture Detail
- **tools:** add FlowHub.AiPing console + make ai-ping/classify/embed
- **ai-ping:** also smoke-test embeddings provider when configured
- **test-infra:** add backend/frontend/e2e make targets, Playwright happy-flow, Bruno, db-ping
- **make:** add smoke-prod target — end-to-end smoke of the docker-compose stack
- **tools/md-to-pdf:** replicate VS Code Markdown PDF extension headlessly
- **make:** split skill tests — test-contract (WireMock) + test-services (live CT 128)
- **make,tools:** wire live Wallabag tests via on-demand OAuth2 token exchange
- **demo:** public demo overlay — VPS-DE + 15-min reset + Traefik rate-limit + Gemma 4 free
- **persistence:** seed baseline Skills + Integrations via EF migration
- **web:** opt-in IFaultInjector for E2E negative-path coverage — 28/28 green
- **web:** add FlowHub logo + favicon, show in README and AppBar
- **make:** add rabbit-ping target to verify RabbitMQ reachability
- per-Vikunja-project routing + capture enrichment (#18)
- **core:** add Attachment value object
- **core:** allow optional Attachment on Capture
- **core:** add upload port, options, policy, and input DTO
- **core:** add ICaptureService.SubmitAsync overload for attachments
- **web:** add UploadPolicy adapter over IOptionsMonitor<UploadOptions>
- **persistence:** add FilesystemAttachmentStorage adapter
- **persistence:** map AttachmentEntity as owned entity of CaptureEntity
- **persistence:** add 0009_AddCaptureAttachment migration
- **persistence:** wire EfCaptureService attachment orchestration with rollback
- **web:** wire upload options, storage, policy + Kestrel limits
- **web:** add file picker to QuickCaptureField
- **web:** add file upload to NewCapture page
- capture file upload (paperless-ngx prep, 2 MB demo limit) (#20)

### CI/CD

- add claude-pipeline consumer stub
- **release:** bump orhun/git-cliff-action v3 -> v4
- exclude AI / BetaSmoke / E2E tests to match make test (#17)
- auto-add new issues to freaxnx01 Backlog project

### Changed

- drop default IClassifier from test base + EventId namespacing
- **web:** drop redundant using; annotate fault-observer + Block-4 TODO
- **skills:** move LoggingSkillIntegration to FlowHub.Skills project
- **api:** drop unused Scalar dep from FlowHub.Api; centralize problem-type URIs
- **persistence:** keep CaptureEntity + Captures DbSet internal; expose to tests via InternalsVisibleTo
- drop Wekan integration, Vikunja absorbs kanban role
- drop Obsidian from seeded Integrations catalog

### Documentation

- **poc:** add AI classification PoC design and implementation plan
- add CAS AISE preparation materials (Frontend)
- link repo to CAS Obsidian vault
- **adr:** accept ADR 0001 — frontend render mode and architecture
- **adr:** clarify dev auth bypass approach in ADR 0001
- **design:** add Dashboard wireframe (Phase 1)
- **design:** add Dashboard flow diagrams (Phase 2)
- add Make targets and update Repository Structure to current reality
- **design:** add New Capture wireframe (Phase 1)
- **design:** add New Capture flow diagrams (Phase 2)
- **design:** add Captures list wireframe (Phase 1)
- **design:** add Captures list flow diagrams (Phase 2)
- **design:** add Capture detail wireframe (Phase 1)
- add CHANGELOG.md with Block 2 deliverables
- fix Mermaid syntax + add spec docs for Bewertungskriterien
- render all Mermaid diagrams as PNG images
- **adr:** accept ADR 0002 — service architecture & async communication
- **poc:** expand REFLECTION.md with Block 3 KI-Reflexion answers
- **api:** add Block 3 API-surface sketch with 6 accepted decisions
- **flowhub:** document claude-ready bot workflow
- **projektarbeit:** add Repository back-link to FlowHub repo
- **projektarbeit:** add FlowHub Glossary and link from Repository
- **knowledge:** add PKM and GTD to acronyms
- **blöcke:** fill Block 2 Nachbearbeitung TODO with current state
- **projektarbeit:** add Completed lifecycle stage to Glossary
- **blöcke:** tick off all Block 2 Nachbearbeitung items
- **blöcke:** tick off manual walkthrough — replaced by smoke tests
- **blöcke:** mark Block 3 reading assignments done (Beyond Vibe Coding 5+6, Coding with AI 5)
- **blöcke:** cross-reference ADR 0002 draft + POC reflection in Block 3 Vorbereitung
- **blöcke:** mark ADR 0002 accepted in Block 3 Vorbereitung
- rename Nachbearbeitung → Nachbereitung to match official Moodle term
- **blöcke:** mark Leseauftrag + E2E test done in Block 3 Vorbereitung
- **blöcke:** mark Block 3 KI-Reflexion done (REFLECTION.md expanded)
- **blöcke:** mark async-communication-candidates done (covered in ADR 0002 + REFLECTION.md)
- **vault:** update path references after subtree merge
- **vault/blöcke:** scaffold Block 3 Nachbereitung
- **vault:** canonical Bewertungskriterien rubric + scaffold Block 4 & 5 Nachbereitung
- **cas:** always-on Bewertungskriterien guardrail + /grade-self-check skill
- **cas:** point Bewertungskriterien guardrail at cas-aise-grade-self-check plugin
- **superpowers:** brainstorm async-pipeline spec for Block 3 Slice B
- **superpowers:** add async-pipeline implementation plan for Slice B
- **vault/projektarbeit:** add Learnings page for CAS submission
- add SUBMISSION.md as central Moodle submission entry point
- **adr:** accept ADR 0003 — async pipeline (MassTransit topology + retry/fault)
- **ai-usage:** bootstrap living doc for KI-Werkzeug-Nutzung rubric
- **vault/blöcke:** tick off Slice-B items in Block 3 Nachbereitung
- **adr:** align EventId range allocation in ADR 0003 with code
- **superpowers:** brainstorm REST API spec for Block 3 Slice A
- **superpowers:** add Slice A REST API implementation plan
- **api:** add 3 ProblemDetails type docs (validation, capture-not-found, capture-not-retryable)
- **vault/blöcke:** tick off Slice-A items in Block 3 Nachbereitung
- **vault/blöcke:** refocus Block 3 KI-reflection on .NET stack
- **superpowers:** brainstorm AI-integration spec for Block 3 Slice C
- **vault/blöcke:** tick off completed items across Blocks 1-3
- **spec:** update testing-strategy.md to Block-3 reality (Slice A/B/C)
- **design:** add Block-4 DB-model sketch (entities ER + indexes)
- **design:** add Mermaid sequence diagrams for Capture-Enrichment + Skill-Routing
- **superpowers:** add Slice C AI-integration implementation plan
- **ai-usage:** scaffold Slice-C section (Block 3 AI integration)
- **adr:** add ADR 0004 — AI integration in services (Slice C)
- **claude.md:** mark FlowHub.AI as active (Slice C landed)
- **vault/blöcke:** tick off Slice-C items in Block 3 Nachbereitung + CHANGELOG
- **ai:** mark Anthropic prompt-cache as deferred to Slice D
- **spec,design:** correct Slice-C framing — shipped, not planned
- **spec:** add Block-3 use cases (REST API, async pipeline, AI fallback) + SMART NfAs
- **vault/blöcke:** tick off Block-3 rubric-gap items in Nachbereitung
- **vault/blöcke:** close out Block 3 Nachbereitung (87% → 100%)
- **superpowers:** brainstorm Beta-MVP spec (Web → AI → Wallabag/Vikunja)
- **superpowers:** write Beta-MVP implementation plan (21 tasks)
- mark Persistence/Skills active; CHANGELOG + ai-usage retrospective for Beta MVP
- **vault/blöcke:** tick Block-4 Nachbereitung items earned by Beta MVP (0% → 16%)
- **ai-usage:** fill Beta-MVP retrospective (adaptations, AI/human share, reflexion)
- **adr:** add ADR 0005 — Persistence (provider, ORM, repository, migrations)
- **vault/blöcke:** tick Block-4 items earned by ADR 0005 + ai-usage retrospective (16% → 22%)
- **changelog:** note ADR 0005 + Beta-MVP review fixups + filled retrospective
- **design/db:** align entities.md with shipped Capture entity (Beta MVP)
- **runbooks:** add Beta-MVP operator acceptance runbook (Task 21)
- **superpowers/specs:** add Block 4 Nachbereitung scope design
- **superpowers/plans:** add Block 4 Nachbereitung implementation plan (5 slices)
- **vault:** add Superpowers /clear workflow to context-hygiene notes
- **vault/block3:** add PVA 3 session notes
- **ai-usage:** document self-authored CAS AISE skills and source repo
- add CLAUDE-PIPELINE.md describing the autonomous pipeline
- add ROADMAP.md (enrichment, web search, AI providers) (#14)
- **next:** refresh NEXT.md to reflect 2026-05-11 clean state
- **superpowers/plans:** add Block 5 Nachbereitung implementation plan
- **env:** add .env.example documenting Ai__* and Embeddings__* vars
- **block5:** close 6 rubric gaps surfaced by cas-aise-grade-self-check
- **submission:** finalize Block-5 CHANGELOG, ai-usage Block-5 reflections, NEXT for tag/PDF
- **vault/block4:** tick the 18 demonstrably-done items in Nachbereitung
- close the last 5 Block-4 Nachbereitung items (49/49)
- **vault:** close Block-5 Nachbereitung, clean 1-PVA, add model inventory
- **vault:** tick Quarkus chapters as N/A in Block-4/5 Vorbereitung
- **runbooks:** add test-services runbook for cirrus-pve CT 128
- add DEMO.md — top-level recap of the public demo posture
- **journeys:** refresh status snapshot — 23/29 green
- **submission:** operator tooling for the CAS AISE submission
- **next:** log CI failure + supersede stale 2026-05-12 snapshot
- **submission:** consolidate acceptance criteria + fix bundle TOC
- **vault:** track Coursework-Glossary EN↔DE vocabulary
- **determine:** add Block 5 / final submission deadline
- **submission:** flesh out 5 short stubs flagged by preflight
- **next:** announce cas-aise-submission-preflight v0.1.1
- **env:** document Skills__Vikunja__/Wallabag__ keys in .env.example
- **submission:** correct stale language note (SUBMISSION.md is German)
- **vault/blöcke:** correct Block 5 Abgabe-Deadline to 2026-07-04 24:00
- **vault/blöcke:** backfill Bewertungskriterien sections for Block 1 & 2
- **submission:** regenerate SUBMISSION-bundle.pdf
- add repo-root TODO.md with manual-test follow-ups
- **ai-usage:** add Fazit synthesizing AI workflow across all five blocks
- **projektbeschreibung:** expand §9 KI-Reflexion with full Fazit (DE)
- add Block-4 test-results table + persistence-layer coverage cross-ref
- **submission:** regenerate PDFs after Fazit + Block-4 docs updates
- **todo:** mark Vikunja test-projects item done — covered by VikunjaCatalogLiveTests
- **block-4:** close grade self-check gaps (Werner W3/W4 + Solution Vision)
- **vault/knowledge:** add Datenschutz & AI-Act stub
- **spec:** add NfA-P1/P2 for privacy & AI Act + cross-link vault stub
- **design:** add data-flow diagram for NfA-P1/P2 + link vault stub
- **vault/block-4:** log PVA 4 feedback — file upload + 2 MB demo limit
- **vault/knowledge:** expand privacy risk table + add ADR-0007/0008 placeholders
- **adr:** add ADR-0007 LLM-Hosting + renumber vault stub refs
- **spec:** capture file upload — design for paperless-ngx prep + 2 MB demo limit
- **adr:** add ADR-0008 Logging-Policy
- **plan:** implementation plan for capture file upload
- **adr:** add ADR-0009 Telemetry-PII-Policy
- **vault/knowledge:** formulate Reflexions-Absatz for AI-Act/privacy
- record capture file upload feature (CHANGELOG, ai-usage, PVA notes)
- add SCOPE-FREEZE.md — lock CAS-AISE submission scope
- **vault/projektarbeit:** add CAS→Projektarbeit reflexion bridge (W4)
- rebuild SUBMISSION-bundle.pdf with W4 reflexion bridge
- **vault:** mark Block 1/2 Nachbereitung forward-refs as delivered

### Fixed

- **poc:** fix config loading and remove API key from appsettings
- **pipeline:** narrow LifecycleFaultObserver catch to exclude OperationCanceledException
- **ai:** use typed GetResponseAsync<T> so DTO schema reaches providers
- **ai:** wire 10s HttpClient timeout in AddFlowHubAi (Slice C cost guard)
- **test:** skip live AI tests properly when API keys are unset
- **web:** surface FailureReason on Unhandled CaptureDetail; type-match MigrationRunner removal
- **make:** use grep -h in help so .env in MAKEFILE_LIST doesn't break output
- **docker:** copy .editorconfig into build context
- **compose:** align env interpolation with .env.example casing + map Ai__* keys
- **ai,make:** resolve passbolt:// refs at recipe time; treat empty model strings as null
- **web:** serialize CaptureDetail loads — shared EF context can't run them in parallel
- **web:** serialize Dashboard data load + tighten 5 journey selectors
- **web:** tighter favicon (orange F glyph) for readable browser-tab icon
- **web:** strip navy fill from logo + favicons so they sit cleanly on AppBar
- **env:** quote Skills__Vikunja__ApiToken placeholder so .env sources cleanly
- **web:** keep MudSelect label shrunk and trim placeholder tooltip
- **web:** enforce upload allowlist even when empty (security)

### Miscellaneous

- move solution file to poc/ and harden .gitignore
- update project files and fix Zone.Identifier filenames
- ignore .worktrees directory
- add Makefile with run/watch/build/test/format targets
- **vault:** ignore syncthing markers and personal PII assets
- **web:** untrack appsettings.Development.json
- **ai-instructions:** sync base + dotnet-blazor overlay from ai-instructions@1071724
- **make:** auto-load gitignored .env for ai-* targets
- untrack accidentally embedded .claude/worktrees + refresh journeys.md
- **gitignore:** also ignore .claude/worktrees/
- **vault:** untrack Moodle source content — copyright FFHS
- **ai-instructions:** refresh base-instructions from upstream
- ignore App_Data/uploads/ for demo attachment blobs
- gitignore Claude Code scheduled-tasks lock file

### Testing

- **web:** add smoke tests covering full walkthrough against Bogus stubs
- **api:** scaffold FlowHub.Api.IntegrationTests with WebApplicationFactory
- **api:** assert RFC 9457 wire format on validation problems
- **ai:** scaffold FlowHub.AI.IntegrationTests project for live provider tests
- **ai:** add 4 live-provider integration tests (trait-gated Category=AI)
- **skills:** add Beta-smoke live tests for Wallabag + Vikunja (trait-gated)
- **skills:** add WireMock.Net contract tests for Vikunja and Wallabag
- **web:** add bUnit specs for the 8 untested user-facing components
- **e2e:** scaffold journey-driven Playwright suite + J20 spec
- **e2e:** mark J20 passes=true — spec verified green against live web
- **e2e:** mark J20 passes=true — verified green against rebuilt container
- **e2e:** scaffold all 27 remaining journeys (J01–J19, J21–J28)
- **e2e:** close 2 reds — Immediate binding + circuit-ready sentinel
- **e2e:** close J15 via direct-DB upsert helper — 27/29 green
- **e2e:** remove legacy HappyFlowTests — flake source, redundant with journeys
- **persistence:** opt out of catalog seed for empty-DB scenarios (#16)
- **e2e:** add headed Playwright full-cycle demo (capture -> Vikunja)
- **skills:** add WireMock contract tests for Vikunja routing + enrichment
- **skills:** live Vikunja integration tests for routing buckets + Quotes route

### build

- **deps:** add MassTransit + RabbitMQ packages
- **deploy:** add docker-compose.yml sketch for Block-5 topology
- **deps:** add FluentValidation, OpenAPI, Scalar, Mvc.Testing for Slice A
- **deps:** add Microsoft.Extensions.AI + provider packages for Slice C
- **make:** split AI live tests behind test-ai target (Category!=AI default)
- **deps:** add EF Core + skill-test packages for Beta MVP
- **skills:** scaffold FlowHub.Skills.Tests; pull HTTP/Options packages into FlowHub.Skills
- add make test-beta; exclude BetaSmoke from default suite
- replace Makefile with justfile and sweep docs

### release

- **v0.1.0:** regenerate v4 PDF + finalize CHANGELOG release section
