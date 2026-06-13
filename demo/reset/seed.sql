-- demo/reset/seed.sql — fixture Captures reseeded after every demo reset.
--
-- Curated to showcase the full FlowHub feature surface the moment a visitor lands:
--   * all 6 lifecycle stages: Raw, Classified, Routed, Completed, Orphan, Unhandled
--   * both skills (Wallabag, Vikunja) incl. completed writes (ExternalRef, VikunjaProject)
--   * all 3 channels: Web, Api, Telegram
--   * keyword-style classification (URL -> Wallabag, "todo" -> Vikunja) AND
--     AI-style classification (free-text -> meaningful skill + generated Title)
--   * Tags and a file Attachment (paperless-ngx prep)
--
-- These are illustrative fixtures inserted directly into the DB. In the public demo,
-- *live* submissions stop at Unhandled (skill writes disabled) and /search returns 503
-- (embeddings disabled) — the fixtures show the outcomes a full deployment produces.
-- Fixed UUIDs so Tags/attachments can reference rows and detail URLs stay stable.

INSERT INTO "Captures"
  ("Id","Content","Source","Stage","CreatedAt","MatchedSkill","Title","ExternalRef","FailureReason","VikunjaProject",
   "Attachment_FileName","Attachment_ContentType","Attachment_SizeBytes","Attachment_RelativePath","Attachment_UploadedAt")
VALUES
  -- 1. URL -> Wallabag, completed write (keyword: URL rule)
  ('11111111-1111-4111-8111-111111111111',
   'https://en.wikipedia.org/wiki/Hexagonal_architecture','Api','Completed', NOW() - INTERVAL '16 minutes',
   'Wallabag','Hexagonal Architecture — Wikipedia','wb-demo-001',NULL,NULL, NULL,NULL,NULL,NULL,NULL),

  -- 2. "todo" -> Vikunja, completed write + VikunjaProject (keyword: todo rule)
  ('22222222-2222-4222-8222-222222222222',
   'todo: prepare slides for the team meeting','Web','Completed', NOW() - INTERVAL '14 minutes',
   'Vikunja','Prepare slides for the team meeting','vk-demo-002',NULL,'Inbox', NULL,NULL,NULL,NULL,NULL),

  -- 3. free-text movie -> Vikunja, completed (AI-style: generated Title + project routing)
  ('33333333-3333-4333-8333-333333333333',
   'Inception is a mind-bending sci-fi film about dreams within dreams','Web','Completed', NOW() - INTERVAL '11 minutes',
   'Vikunja','Watch: Inception (sci-fi film)','vk-demo-003',NULL,'Movies', NULL,NULL,NULL,NULL,NULL),

  -- 4. free-text book -> Wallabag, ROUTED (in-flight) via Telegram (AI-style)
  ('44444444-4444-4444-8444-444444444444',
   'Sapiens by Yuval Noah Harari — add to my reading list','Telegram','Routed', NOW() - INTERVAL '3 minutes',
   'Wallabag','Read: Sapiens — Yuval Noah Harari',NULL,NULL,NULL, NULL,NULL,NULL,NULL,NULL),

  -- 5. URL -> Wallabag, CLASSIFIED (pipeline mid-flight, not yet routed)
  ('55555555-5555-4555-8555-555555555555',
   'https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/','Api','Classified', NOW() - INTERVAL '5 minutes',
   'Wallabag','Modern Web Apps with .NET',NULL,NULL,NULL, NULL,NULL,NULL,NULL,NULL),

  -- 6. "todo" -> Vikunja, ORPHAN (integration failed, retryable)
  ('66666666-6666-4666-8666-666666666666',
   'todo: book a dentist appointment','Web','Orphan', NOW() - INTERVAL '8 minutes',
   'Vikunja','Book a dentist appointment',NULL,'Vikunja API returned 503 (simulated for demo)',NULL, NULL,NULL,NULL,NULL,NULL),

  -- 7. free-text -> UNHANDLED (no matching skill) via Telegram
  ('77777777-7777-4777-8777-777777777777',
   'just a thought I want to remember later','Telegram','Unhandled', NOW() - INTERVAL '6 minutes',
   NULL,'Just a thought I want to remember later',NULL,NULL,NULL, NULL,NULL,NULL,NULL,NULL),

  -- 8. URL -> RAW (just landed, awaiting classification)
  ('88888888-8888-4888-8888-888888888888',
   'https://martinfowler.com/articles/cant-buy-integration.html','Api','Raw', NOW() - INTERVAL '25 seconds',
   NULL,NULL,NULL,NULL,NULL, NULL,NULL,NULL,NULL,NULL),

  -- 9. FILE UPLOAD (paperless-ngx prep) -> Unhandled (no paperless skill yet) + Attachment
  ('99999999-9999-4999-8999-999999999999',
   'Scanned receipt — Migros 2026-05-31','Web','Unhandled', NOW() - INTERVAL '2 minutes',
   NULL,'Receipt — Migros 2026-05-31',NULL,NULL,NULL,
   'receipt-migros-2026-05-31.pdf','application/pdf',184320,'uploads/demo/receipt-migros-2026-05-31.pdf', NOW() - INTERVAL '2 minutes'),

  -- 10. free-text quote -> Vikunja 'Zitate', completed + enriched (AI-style: classify -> Zitate route)
  ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
   'Save this quote: "Simplicity is the soul of efficiency." — Austin Freeman','Web','Completed', NOW() - INTERVAL '12 minutes',
   'Vikunja','Quote — Austin Freeman','vk-demo-010',NULL,'Zitate', NULL,NULL,NULL,NULL,NULL);

-- Tags (CaptureId, Value) — demonstrates the tagging feature + the Captures-grid tag display
INSERT INTO "Tags" ("CaptureId","Value") VALUES
  ('11111111-1111-4111-8111-111111111111','article'),
  ('11111111-1111-4111-8111-111111111111','architecture'),
  ('22222222-2222-4222-8222-222222222222','task'),
  ('33333333-3333-4333-8333-333333333333','movie'),
  ('33333333-3333-4333-8333-333333333333','sci-fi'),
  ('44444444-4444-4444-8444-444444444444','book'),
  ('55555555-5555-4555-8555-555555555555','article'),
  ('55555555-5555-4555-8555-555555555555','dotnet'),
  ('99999999-9999-4999-8999-999999999999','document'),
  ('99999999-9999-4999-8999-999999999999','receipt'),
  ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa','quote');
