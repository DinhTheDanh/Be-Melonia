# Related Songs API Requirement (FE -> BE)

## 1) Objective

Add a dedicated endpoint to return "related songs" for the song detail page, instead of relying on keyword search from FE.

Business goals:

- Better relevance than generic search.
- Stable results (avoid empty list too often).
- Single API call from FE for fast rendering.

---

## 2) Endpoint Proposal

### `GET /Recommendation/related-songs/{songId}`

Query params:

- `limit` (optional, default: `6`, max: `20`)
- `excludeExplicit` (optional, default: `false`)
- `userId` (optional, for personalization if available)

Headers:

- Public endpoint can work without auth.
- If auth exists, backend may use user profile/history to personalize.

---

## 3) Response Contract

### Success `200 OK`

```json
{
  "Message": "OK",
  "Data": [
    {
      "Id": "guid-song-id",
      "Title": "Song title",
      "ArtistNames": "Artist A, Artist B",
      "ArtistIds": ["guid-artist-1", "guid-artist-2"],
      "Thumbnail": "https://...",
      "FileUrl": "https://...",
      "Duration": 215,
      "GenreName": "Pop",
      "LikeCount": 1200,
      "ListenCount": 53000,
      "ReleaseDate": "2026-03-10T00:00:00Z",
      "Score": 0.873,
      "Reasons": ["same_artist", "similar_genre", "co_listen"]
    }
  ]
}
```

Notes:

- `Score` and `Reasons` are optional for FE display/debug.
- `Data` should never include the current `songId`.

### Not Found `404`

```json
{
  "Message": "Song not found"
}
```

### Validation `400`

```json
{
  "Message": "Invalid songId"
}
```

---

## 4) Recommendation Logic (Priority)

Use weighted ranking (example):

1. Same artist: +0.40
2. Same primary genre: +0.25
3. Co-listen / co-like / same playlist behavior: +0.20
4. Similar popularity bucket (listen/like): +0.10
5. Freshness boost (newer songs): +0.05

Hard rules:

- Exclude current song.
- Exclude deleted/private/non-public songs.
- Exclude songs without playable `FileUrl`.

Fallback strategy:

1. Same artist
2. Same genre
3. Trending popular songs
4. Recent songs

Guarantee:

- Try to return at least `min(3, limit)` whenever possible.

---

## 5) Performance & Ops

Requirements:

- P95 response time <= 250ms for `limit <= 10`.
- Support cache (Redis/in-memory) by key: `related:{songId}:{limit}:{userSegment}`.
- Cache TTL suggestion: 10-30 minutes.
- Add DB indexes for query paths (songId, artistId, genreId, publish status).

---

## 6) Security / Access

- Endpoint should not leak private/unreleased songs.
- If `excludeExplicit=true`, filter by content rating.
- Sanitize and validate `songId` and `limit`.

---

## 7) FE Integration Expectations

Frontend will call:

`GET /Recommendation/related-songs/{songId}?limit=6`

Frontend expects:

- `Data` array sorted by relevance descending.
- Stable shape compatible with existing song card/player.

If `Data` is empty, FE will still show empty state text, but backend should minimize this with fallback logic.

---

## 8) Acceptance Criteria

- API returns related songs for at least 90% of active catalog songs.
- No current song appears in `Data`.
- No duplicate song IDs in `Data`.
- Works without login (non-personalized).
- Works with login (optionally personalized by user behavior).
- Meets P95 latency target.

---

## 9) Optional Future Extension

Add endpoint for bulk prefetch:

### `POST /Recommendation/related-songs/batch`

Body:

```json
{
  "SongIds": ["id1", "id2", "id3"],
  "Limit": 6
}
```

Use case:

- Improve detail-page navigation speed when users jump between songs quickly.
