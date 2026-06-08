# FlowHub.Integrations — placeholder (not yet implemented)

**Status:** Planned / superseded by `FlowHub.Skills`. This folder is an
intentional placeholder — it contains **no code** and is **not** part of
`FlowHub.slnx` or any build.

## Background

Early drafts (ADR 0002) sketched a separate `Integrations` layer for outbound
downstream-service clients. In implementation, that responsibility landed in
**`FlowHub.Skills`**, where each downstream target (Wallabag, Vikunja) is an
`ISkillIntegration` adapter. `FlowHub.Skills` is the real, implemented home for
this concern.

## Why the folder remains

Kept as a reserved namespace for any future generic (non-skill) integration that
does not fit the skill-routing model. It is documented here so the project tree
is self-describing; for the current system, see `source/FlowHub.Skills/`.
