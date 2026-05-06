# FlowHub Entity-Relationship Diagram — Block 4 Schema

```mermaid
erDiagram
    Captures {
        uuid Id PK
        text Content
        varchar(32) Source
        varchar(32) Stage
        timestamptz CreatedAt
        varchar(64) MatchedSkill
        text FailureReason
        varchar(512) Title
        varchar(256) ExternalRef
    }

    Channels {
        varchar(64) Name PK
        varchar(32) Kind
        bool IsEnabled
        varchar(16) Status
        timestamptz LastActiveAt
    }

    Skills {
        varchar(64) Name PK
        varchar(16) Status
        int RoutedToday
        timestamptz LastResetAt
    }

    SkillRuns {
        uuid Id PK
        varchar(64) SkillName FK
        uuid CaptureId FK
        timestamptz StartedAt
        timestamptz CompletedAt
        bool Success
        text FailureReason
    }

    Integrations {
        varchar(64) Name PK
        varchar(16) Status
        timestamptz LastWriteAt
        bigint LastWriteDurationMs
    }

    IntegrationHealthSamples {
        uuid Id PK
        varchar(64) IntegrationName FK
        timestamptz SampledAt
        varchar(16) Status
        bigint DurationMs
    }

    Tags {
        uuid CaptureId FK
        varchar(64) Value
    }

    Captures ||--o{ Tags : "has"
    Captures ||--o{ SkillRuns : "routed via"
    Skills ||--o{ SkillRuns : "executed"
    Integrations ||--o{ IntegrationHealthSamples : "sampled"
```

## FK Strategy

| Relationship | Type | Reason |
|---|---|---|
| Capture.Source → Channel.Name | **Soft** (no DB FK) | Channels can be deregistered without orphan failures |
| Capture.MatchedSkill → Skill.Name | **Soft** (no DB FK) | Consistent with Beta MVP pattern |
| SkillRun.SkillName → Skill.Name | **Hard** (RESTRICT) | SkillRun is audit trail; Skill must exist |
| SkillRun.CaptureId → Capture.Id | **Hard** (CASCADE) | Run is meaningless without its Capture |
| IntegrationHealthSample.IntegrationName → Integration.Name | **Hard** (CASCADE) | Sample is meaningless without its Integration |
| Tag.CaptureId → Capture.Id | **Hard** (CASCADE) | Tag is owned by Capture |
