# History Events (Desktop)

## Storage location (Windows)
History event data is stored in the Grayjay desktop data directory:

```
%APPDATA%\Grayjay
```

The per-view event log is stored in the database table named `history_events`.

## Exporting
Use the Desktop UI **History** page → **History Events** section:
- **Export CSV** downloads `history_events.csv`.
- **Export JSONL** downloads `history_events.jsonl`.

You can also call the API directly:
```
GET /historyevents/Export?format=csv
GET /historyevents/Export?format=jsonl
```

Optional query parameters:
- `limit` (default: 10000)
- `before` / `after` (Unix milliseconds)
- `url` (filter by video URL)

## Schema
Each event represents a single playback session:

| Field | Type | Notes |
| --- | --- | --- |
| `id` | string (GUID) | Unique event id |
| `url` | string | Video URL |
| `source` | string | Platform/source (when available) |
| `videoId` | string | Platform video id (optional) |
| `title` | string | Video title (optional) |
| `channelName` | string | Channel/author name (optional) |
| `startedAtUtc` | string (UTC ISO-8601) | Playback start |
| `endedAtUtc` | string (UTC ISO-8601) | Playback end (nullable) |
| `watchMs` | number | Total watch time in ms (nullable) |
| `startPositionMs` | number | Position at start (optional) |
| `endPositionMs` | number | Position at end (optional) |
| `endedReason` | string | `stop`, `completed`, `app_exit`, `timeout`, `crash_recovered` |
