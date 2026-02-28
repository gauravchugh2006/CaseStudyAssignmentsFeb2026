# Question 1 - Backend (.NET 8 Web API)

## Run locally (Windows + VS Code)
1. Install **.NET 8 SDK**.
2. Open terminal in `Question1-OrderTracker/backend`.
3. Restore/build/run:
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project src/AssignmentOrderTracker.Api
   ```
4. Open Swagger: `https://localhost:5001/swagger` (or URL printed by API).

## Run tests
```bash
dotnet test
```

## Notes
- Architecture: Controller -> Service -> Repository.
- Uses EF Core InMemory with seeded sample orders (`ORD-1001`, `ORD-1002`).
- Standard error shape returned by middleware for 400/404/500.
