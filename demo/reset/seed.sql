-- demo/reset/seed.sql — fixture Captures inserted after every reset so the
-- demo dashboard has realistic content the moment a visitor lands.
--
-- Spans every LifecycleStage so the "Needs Attention" widget shows non-zero
-- Orphan + Unhandled counts and the Captures grid demonstrates filter chips.
-- Uses NOW() - INTERVAL so the "When" column shows fresh relative times.

INSERT INTO "Captures" ("Id", "Content", "Source", "Stage", "CreatedAt", "MatchedSkill", "Title", "ExternalRef", "FailureReason")
VALUES
  -- 1. Completed Wallabag (an article saved)
  (gen_random_uuid(),
   'https://en.wikipedia.org/wiki/Hexagonal_architecture',
   'Api', 'Completed', NOW() - INTERVAL '13 minutes',
   'Wallabag', 'Hexagonal Architecture — Wikipedia',
   'wb-demo-001', NULL),

  -- 2. Completed Vikunja (a todo routed)
  (gen_random_uuid(),
   'todo: review Block 5 Nachbereitung before submission',
   'Web', 'Completed', NOW() - INTERVAL '10 minutes',
   'Vikunja', 'Review Block 5 Nachbereitung',
   'vk-demo-002', NULL),

  -- 3. Classified but not yet routed (shows the pipeline mid-flight)
  (gen_random_uuid(),
   'https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/',
   'Api', 'Classified', NOW() - INTERVAL '4 minutes',
   'Wallabag', 'Modern Web Apps with .NET',
   NULL, NULL),

  -- 4. Orphan — failed integration, retryable
  (gen_random_uuid(),
   'todo: rotate Passbolt master key',
   'Web', 'Orphan', NOW() - INTERVAL '7 minutes',
   'Vikunja', 'Rotate Passbolt master key',
   NULL, 'Vikunja API returned 503 (simulated for demo)'),

  -- 5. Unhandled — no matching skill
  (gen_random_uuid(),
   'just a thought I want to remember later',
   'Telegram', 'Unhandled', NOW() - INTERVAL '5 minutes',
   NULL, 'Just a thought I want to remember later',
   NULL, NULL),

  -- 6. Raw — just landed, awaiting classification
  (gen_random_uuid(),
   'https://martinfowler.com/articles/cant-buy-integration.html',
   'Api', 'Raw', NOW() - INTERVAL '20 seconds',
   NULL, NULL, NULL, NULL);
