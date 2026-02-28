# AssignmentOrderTracker

This repository contains **two independent deliverables** implemented in separate folders so they can be worked on in parallel.

## Folder structure
- `Question1-OrderTracker/`
  - `backend/` -> .NET 8 Web API (Controller -> Service -> Repository, Swagger, logging, unit/integration tests)
  - `frontend/` -> React + TypeScript UI (search, details, notes, status update)
- `Question2-EdTech-Design/`
  - `EdTech-System-Design.md` -> 1-2 page architecture/design submission
- `Question2-EdTech-WorkingModel/`
  - `backend/` -> runnable Node working model for dashboard composition + checkout APIs
  - `frontend/` -> browser UI with CMS component registry rendering

---

## Parallel task execution plan
You can execute these tracks independently (different terminals/people):

### Track A (Question 1 Backend)
```bash
cd Question1-OrderTracker/backend
dotnet restore
dotnet build
dotnet run --project src/AssignmentOrderTracker.Api
dotnet test
```

### Track B (Question 1 Frontend)
```bash
cd Question1-OrderTracker/frontend
npm install
npm run dev
```

### Track C (Question 2 Design Document)
- Open `Question2-EdTech-Design/EdTech-System-Design.md` and export to PDF/Doc if needed.

### Track D (Question 2 Working Model Code)
```bash
cd Question2-EdTech-WorkingModel
node backend/server.js
```
Open `http://localhost:8080`.

---

## Run locally on Windows laptop (VS Code)

### Prerequisites
- .NET SDK 8
- Node.js 20+
- VS Code + C# extension + ESLint/TypeScript extension

### Start backend
```bash
cd Question1-OrderTracker/backend
dotnet run --project src/AssignmentOrderTracker.Api
```
Swagger URL: `http://localhost:5000/swagger` (or shown in console).

### Start frontend
In a second terminal:
```bash
cd Question1-OrderTracker/frontend
npm install
npm run dev
```
Then browse to `http://localhost:4173`.

If API runs on different port, set env var before starting frontend:
```powershell
$env:VITE_API_URL="http://localhost:5000"
npm run dev
```

---

## Assumptions
- Seed data includes `ORD-1001` and `ORD-1002`.
- Search requires non-empty query.
- Status transitions follow: `PLACED->PAID->SHIPPED->DELIVERED` and any non-delivered status can move to `CANCELLED`.
- Note max length is 500.
