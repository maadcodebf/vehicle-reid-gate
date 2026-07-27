# Vehicle Re-ID Gate MVP (.NET 10 + ONNX + Qdrant, Podman-first)

A minimal functional API that supports:

1. **Enroll** truck images as Re-ID embeddings.
2. **Match** new barrier images against previous barrier embeddings.
3. Return **MATCH / UNCERTAIN / NO_MATCH** using cosine similarity thresholds.

## 1) What this solves

When OCR fails, this service identifies whether the truck at barrier **N** is the same truck seen at **N-1**, using **Vehicle Re-ID embeddings**.

## 2) Repository layout

```text
vehicle-reid-gate/
  compose.yaml
  README.md
  models/
    vehicle_reid.onnx                # you provide this model file
  postman/
    VehicleReId.postman_collection.json
  src/
    VehicleReId.Api/
      Controllers/
      Data/
      Models/
      Services/
      Program.cs
      appsettings.json
      VehicleReId.Api.csproj
```

## 3) Developer environment requirements

### Required
- **.NET SDK 10.0**
- **Podman** 4+
- **podman-compose** (or Podman Compose plugin)
- **Git**

### Recommended tools
- **VS Code** or **JetBrains Rider**
- **Postman** or **Insomnia** for API tests
- `curl` + `jq` for CLI tests

### Optional useful tools
- `onnxruntime-tools` (model sanity checks)
- Python notebook for threshold calibration

## 4) Production environment requirements

### Runtime base
- Linux host (RHEL/Ubuntu) with Podman
- reverse proxy (Nginx/Traefik) for TLS and routing
- persistent storage for:
  - Qdrant vectors
  - SQLite DB (`reid.db`) or PostgreSQL (recommended for scale)

### Operational requirements
- Centralized logging (ELK, Loki, etc.)
- Metrics/monitoring (Prometheus/Grafana)
- Backup plan:
  - Qdrant storage volume
  - metadata DB
- Security hardening:
  - API authN/authZ
  - firewall rules
  - least privilege for service user

## 5) AI/model assets needed

You must provide a **vehicle Re-ID ONNX model** at:

```text
models/vehicle_reid.onnx
```

And configure `appsettings.json` to match the model:
- `ReId:InputName`
- `ReId:OutputName`
- `ReId:VectorSize`
- `ReId:InputWidth`, `ReId:InputHeight`

> If names/dimensions do not match your model, inference will fail.

## 6) Run Qdrant with Podman

From repo root:

```bash
podman compose up -d
```

Check health:

```bash
curl http://localhost:6333/collections
```

## 7) Run API locally

```bash
dotnet restore src/VehicleReId.Api/VehicleReId.Api.csproj
dotnet run --project src/VehicleReId.Api/VehicleReId.Api.csproj
```

Swagger URL appears in console (typically `http://localhost:5000/swagger` or `http://localhost:5xxx/swagger`).

## 8) Endpoints

- `GET /api/reid/health`
- `POST /api/reid/enroll`
- `POST /api/reid/match`

### Enroll example

```json
{
  "barrierId": "B1",
  "timestampUtc": "2026-07-27T14:00:00Z",
  "externalTruckId": "optional-id",
  "images": [
    { "fileName": "front1.jpg", "base64Jpeg": "<BASE64>" },
    { "fileName": "front2.jpg", "base64Jpeg": "<BASE64>" }
  ]
}
```

### Match example

```json
{
  "currentBarrierId": "B2",
  "previousBarrierId": "B1",
  "timestampUtc": "2026-07-27T14:06:00Z",
  "timeWindowMinutes": 20,
  "topK": 5,
  "images": [
    { "fileName": "front_now.jpg", "base64Jpeg": "<BASE64>" }
  ]
}
```

## 9) Postman

Import:

```text
postman/VehicleReId.postman_collection.json
```

Set variables:
- `baseUrl` (e.g. `http://localhost:5000`)
- `img1_base64`
- `img2_base64`
- `img_query_base64`

## 10) Convert local image to base64

### Linux/macOS

```bash
base64 -w 0 ./images/truck1.jpg
```

### PowerShell

```powershell
$bytes = [System.IO.File]::ReadAllBytes("C:\images\truck1.jpg")
[Convert]::ToBase64String($bytes) | Set-Clipboard
```

## 11) Decision thresholds

Initial values in `appsettings.json`:
- `ThresholdHigh = 0.82`
- `ThresholdLow = 0.72`

Tune using your real camera/barrier data (B1→B2, B2→B3 may need different thresholds).

## 12) Troubleshooting

- **Inference fails**
  - verify model path and ONNX input/output names.
- **Invalid embedding size**
  - set `ReId:VectorSize` correctly.
- **No candidates**
  - check `previousBarrierId`, time window, and timestamps in UTC.
- **Qdrant connection error**
  - ensure `podman compose up -d` is running and port `6333` is reachable.

## 13) AI operational docs

See:
- `ai/agents.md`
- `ai/skills.md`
- `ai/tools.md`

These files document how to operate, calibrate, and troubleshoot this Re-ID solution across development and production.

## 14) Next production steps

1. Add authentication/authorization (JWT or mTLS).
2. Add retries/circuit breaker for Qdrant client.
3. Add OpenTelemetry traces + metrics.
4. Add idempotency key on enroll endpoint.
5. Replace SQLite with PostgreSQL for multi-instance deployment.
