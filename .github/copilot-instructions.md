# Vehicle Re-ID Gate

Minimal .NET 10 Web API that identifies whether a truck seen at barrier N is the same truck
seen at a previous barrier, using ONNX vehicle Re-ID embeddings + Qdrant vector search (used
when OCR of the license plate fails/is unreliable).

## Architecture

Request flow: `ReIdController` → `ReIdService` → `EmbeddingService` (ONNX/OpenCvSharp inference)
+ `QdrantService` (HTTP client to Qdrant REST API) + `AppDbContext` (SQLite, EF Core).

- **Enroll** (`POST /api/reid/enroll`): accepts 1-3 images + a `LicensePlate` (multipart form).
  Creates a `PassageEvent` (barrier is always hardcoded `"GLOBAL"` currently) in SQLite, computes
  one embedding per image, upserts each as a separate point in the Qdrant collection
  (`truck_reid`) with payload `{ passage_event_id, license_plate, timestamp_utc }`, and records an
  `EmbeddingRecord` per stored point.
- **Match** (`POST /api/reid/match`): accepts 1-3 images. For each image, searches Qdrant
  (no time-window filter — the whole collection is searched every time), merges hits by
  `passage_event_id` (keeping the best score per event), and returns the top-K candidates.
  Decision is derived purely from `bestScore` vs `appsettings.json`'s `ReId:ThresholdHigh`
  (→ `MATCH`) / `ReId:ThresholdLow` (→ `UNCERTAIN`) / below both (→ `NO_MATCH`).
- Timestamps (`TimestampUtc`) are always server-generated (`DateTime.UtcNow`), never
  client-provided — preserve this if touching enroll/match code.
- Qdrant is a separate container (`compose.yaml`), not embedded — `QdrantService` talks to it
  purely over HTTP (`PUT /collections/{name}`, `PUT /collections/{name}/points`,
  `POST /collections/{name}/points/search`), there is no Qdrant client SDK dependency.
- SQLite (`reid.db`, created via `EnsureCreated()` on startup, not migrations) stores relational
  metadata (`PassageEvent`, `EmbeddingRecord`); Qdrant stores the actual vectors. The two are
  linked by `passage_event_id` / `QdrantPointId`.

## Key configuration (`src/VehicleReId.Api/appsettings.json`)

- `ReId:OnnxModelPath`, `InputName`, `OutputName`, `InputWidth/Height`, `VectorSize` must match
  whatever ONNX model is placed at `models/vehicle_reid.onnx` (not checked into git — must be
  provided/exported, see README §5 for Fast-ReID export steps). Mismatches fail at inference time.
- `ReId:ThresholdHigh` / `ThresholdLow` control the MATCH/UNCERTAIN/NO_MATCH decision boundary —
  tune per-camera/barrier, not universal constants.
- `ReId:CollectionName` (`truck_reid`) is the Qdrant collection name used everywhere.
- `Qdrant:BaseUrl` defaults to `http://localhost:6333`.

## Build / run

```bash
podman compose up -d                                              # start Qdrant (localhost:6333)
dotnet restore src/VehicleReId.Api/VehicleReId.Api.csproj
dotnet run --project src/VehicleReId.Api/VehicleReId.Api.csproj    # API on localhost:5000 (Swagger/Scalar UI in console)
```

There is no test project in this repo — validation is done via the HTTP-based flows below, not
`dotnet test`.

## Manual test flow (`testing/`)

`testing/enroll.http` and `testing/match.http` are REST Client (VS Code `humao.rest-client`)
files that exercise the API end-to-end against `dataset/upload/` (enroll images) and
`dataset/test/` (match images, named `match-<PLATE>.jpeg` or `no-match.jpeg` to encode expected
outcome). Treat these `.http` files as the source of truth for plate/image pairs — don't hardcode
them elsewhere.

A dedicated custom agent, **reid-tester** (`.github/agents/reid-tester.agent.md`), runs this
suite against an already-running API + Qdrant, validates points actually landed in Qdrant, and
writes one dated report to `testing/results/YYYY-MM-DD_HHmmss-run.md` (see
`testing/results/README.md` for the required report structure). That agent must never modify
`.http` files, app source, or `appsettings.json`, and never resets Qdrant data — it only reports.
If invoked to run the test suite, prefer delegating to `reid-tester` rather than re-implementing
the flow.

When debugging Qdrant-specific issues surfaced by test runs (slow search, bad scores, connection
errors, collection/index problems), consult `.agents/skills/qdrant-advisor/SKILL.md` before
assuming it's an application bug.

## Conventions

- Comments explaining business rules in this codebase are written in Spanish (e.g. in
  `ReIdService.cs`); match this if adding similar rule-clarifying comments in that file.
- Form DTOs (`EnrollFormRequest`, `MatchFormRequest` in `Models/Dtos.cs`) use `[FromForm]` +
  multipart, not JSON bodies — image uploads always go through `IFormFile` collections capped at
  1-3 images.
