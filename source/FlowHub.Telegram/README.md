# FlowHub.Telegram — placeholder (not yet implemented)

**Status:** Planned. This folder is an intentional placeholder — it contains **no
code** and is **not** part of `FlowHub.slnx` or any build.

## Planned role

An inbound channel adapter: receive messages via the Telegram Bot API (webhook)
and turn them into Captures through the same `ICaptureService` entry point the Web
Quick-Capture and the REST API already use. Adding it is an adapter swap on the
existing driving side — no domain or pipeline change.

## Why it is not implemented yet

The implemented capture channels for the CAS submission are the Web Quick-Capture
field and the REST API (`/api/v1/captures`). Telegram is post-submission product
work (see `SCOPE-FREEZE.md`). It is documented here so the project tree is
self-describing rather than appearing as a phantom module.
