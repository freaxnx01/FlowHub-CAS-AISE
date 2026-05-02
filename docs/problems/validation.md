# `validation` problem

Type URI: `https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/validation.md`

Returned when one or more request fields fail validation. Status code: `400 Bad Request`.

## Response shape

```json
{
  "type": "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/validation.md",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "instance": "/api/v1/captures",
  "errors": {
    "Content": ["Content must not be empty."]
  },
  "traceId": "00-..."
}
```

## How to fix

The `errors` object lists violating field names with messages. Resubmit the request after correcting the listed fields.

## Where it surfaces

- `POST /api/v1/captures` — invalid `content` or `source`
- `GET /api/v1/captures` — malformed `cursor` or unknown `stage` value
