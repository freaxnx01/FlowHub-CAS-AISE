# `capture-not-found` problem

Type URI: `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-found.md`

Returned when a capture id is referenced but no capture exists with that id. Status code: `404 Not Found`.

## Response shape

```json
{
  "type": "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-found.md",
  "title": "Capture not found.",
  "status": 404,
  "detail": "No capture exists with id <guid>.",
  "instance": "/api/v1/captures/<guid>",
  "traceId": "00-..."
}
```

## How to fix

Verify the capture id is correct. Captures in FlowHub are append-only — once created they persist, but ids are case-sensitive GUIDs and must be exact.

## Where it surfaces

- `GET /api/v1/captures/{id}`
- `POST /api/v1/captures/{id}/retry`
