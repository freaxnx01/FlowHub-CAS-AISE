# `capture-not-retryable` problem

Type URI: `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-retryable.md`

Returned when `POST /api/v1/captures/{id}/retry` is called against a capture whose lifecycle stage is not retryable. Status code: `409 Conflict`.

Only captures in `Orphan` or `Unhandled` are retryable. Captures in `Raw`, `Classified`, `Routed`, or `Completed` cannot be retried — `Raw`/`Classified`/`Routed` are in-flight; `Completed` is the success terminal state.

## Response shape

```json
{
  "type": "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-retryable.md",
  "title": "Capture stage is not retryable.",
  "status": 409,
  "detail": "Captures may only be retried from Orphan or Unhandled. Current stage: Completed.",
  "instance": "/api/v1/captures/<guid>/retry",
  "traceId": "00-..."
}
```

## How to fix

If the capture is in flight (`Raw`/`Classified`/`Routed`), wait — the pipeline will resolve it to a terminal state on its own. If it's `Completed`, no retry is needed. If you need to recover from a permanent failure, that capture is already `Orphan` or `Unhandled` and is retryable from there.
