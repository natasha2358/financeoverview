# Finance Overview

Milestone 1 scaffolding for the personal finance tracker. This repo contains:
- React + TypeScript + Vite frontend scaffold.
- ASP.NET Core (.NET 10) Web API scaffold using Controllers.
- SQLite + EF Core configured with an initial migration.

> No PDF parsing is implemented yet.

## Prerequisites
- Node.js 20+
- .NET SDK 10 (preview) + `dotnet-ef` tool

## Run locally

### Frontend
```bash
cd frontend
npm install
npm run dev
```

### Backend
```bash
cd backend/FinanceOverview.Api
dotnet restore
dotnet run
```

The API will be available at `http://localhost:5000/api/health` by default.

## Migrations
```bash
# install once
dotnet tool install --global dotnet-ef

cd backend/FinanceOverview.Api
# create a new migration

dotnet ef migrations add <MigrationName>
# apply migrations

dotnet ef database update
```

## Tests
No automated tests yet. Add backend unit tests once parsing and services land.
