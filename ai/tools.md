# Tools and Commands

## Runtime tools
- .NET 10 SDK
- Podman + podman-compose
- Qdrant container image

## Build/run
```bash
dotnet restore src/VehicleReId.Api/VehicleReId.Api.csproj
dotnet build src/VehicleReId.Api/VehicleReId.Api.csproj
dotnet run --project src/VehicleReId.Api/VehicleReId.Api.csproj
```

## Start vector DB (Podman)
```bash
podman compose up -d
podman ps
```

## Basic checks
```bash
curl http://localhost:6333/collections
curl http://localhost:5000/api/reid/health
```

## Useful API test command (health)
```bash
curl -s http://localhost:5000/api/reid/health | jq
```

## Backup hints
- Backup Qdrant persistent volume.
- Backup `reid.db` (or PostgreSQL dump if migrated).
