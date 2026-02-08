# AGENTS.md — How Codex should work in this repo

## Goal
Build a personal finance tracker as a learning project.
Optimize for clarity, correctness, and small, understandable steps.

The app focuses on **monthly PDF imports + dashboards**, not real-time syncing.

---

## How to work (important)
1. **Small, reviewable changes**
   - Prefer incremental milestones over large implementations.
   - If a task is large, propose a short plan first and wait for confirmation.

2. **Explain as you go**
   For every task, include:
   - What was done (high level)
   - **File-by-file summary**
   - How to run locally
   - How to test (commands)
   - Follow-ups / next milestone suggestions

3. **Ask before big decisions**
   - If multiple approaches are viable (PDF parsing libraries, dedupe strategy, UI patterns),
     present 2–3 options with tradeoffs and choose the simplest default unless told otherwise.

4. **Keep it minimal**
   - Do not add libraries “just in case.”
   - Prefer built-in framework capabilities unless there is a clear need.

---

## Tech stack (fixed)
- Frontend: React + TypeScript (Vite)
- Backend: ASP.NET Core (.NET 10) using **Controllers**
- ORM: EF Core
- Database: **SQLite** (local file database)
- User model: **single user only**, no authentication or authorization

---

## Product scope (finance tracker)

### Upload sources
1. **Bank statements**
   - Uploaded monthly as **PDF only**
   - PDFs are text-selectable (no OCR for MVP unless absolutely required)

2. **Investments**
   - Monthly uploads (Excel/CSV) or manual entry for MVP

---

## PDF statement import (must-have)
- Implement a safe, explicit pipeline:
  1. Upload PDF and store file + metadata
  2. Extract text (text-based extraction only for MVP)
  3. Parse extracted text into structured rows
  4. Show an **Import Review UI** before committing
  5. Persist transactions only after user confirmation

- Never commit real bank statements to the repo.
- Support **idempotent imports** (re-uploading the same statement must not duplicate data).

---

## Parsing & architecture rules
- Use a **pluggable parser design**:
  - Each bank/statement format has its own parser implementation
  - Parser selection based on detected format or user selection
- Start with a single known format, but design for extension.
- Store both:
  - raw extracted text (or reference to it)
  - parsed structured data
- Create **anonymized fixtures** (text or JSON) for tests instead of real PDFs.

---

## Core domain behavior

### Transactions
- Fields: date, raw description, normalized merchant, amount (decimal), currency, balance (if available)
- Amounts must use `decimal`, never float.

### Merchants
- Transactions are grouped by Merchant.
- Support merchant aliases / normalization (e.g., “LIDL #1234” → “Lidl”).

### Categories
- Merchants map to Categories via editable rules.
- Default category: `Uncategorized`.

### Investments
- Track contributions by:
  - platform
  - month
  - amount
- Focus on contributions (not market valuation) for MVP.

### Net state / net worth
- Track start-of-month and end-of-month balances.
- Manual entry is acceptable for MVP.

---

## Dashboards (UI goals)
- Monthly summary (income, expenses, net)
- Breakdown by category
- Breakdown by merchant
- Investments by platform (monthly view)
- Optimize for ease of use over advanced visualization.

---

## Backend coding standards (.NET)
- Use Controllers with attribute routing.
- Use DTOs for request/response (do not expose EF entities).
- Validate inputs; return consistent error responses.
- Keep business logic out of controllers (use services where it improves clarity).
- Use async APIs for DB and IO.
- Use migrations for schema changes.

---

## Frontend coding standards (React)
- Keep components small and focused.
- Use simple data fetching (fetch/axios).
- Handle loading and error states consistently.
- Prefer clarity over abstraction early on.

---

## Testing expectations
- Backend:
  - Unit tests for parsing and business logic.
  - Integration tests for critical endpoints when feasible.
- Frontend tests optional early; add once UI stabilizes.
- If tests are skipped, explain why and propose when to add them.

---

## Security & data safety
- Never commit secrets or real financial data.
- Use environment variables and document required config.
- Avoid logging raw financial data unless necessary for debugging.

---

## Documentation requirements
- Keep `README.md` up to date with:
  - setup steps
  - run commands
  - test commands
  - migration commands
- Document key decisions briefly in `/docs`.

### Sandbox limitations
- If dotnet SDK or npm install is unavailable in the Codex environment, do not block progress.
- Provide clear local run/test commands and what to verify manually instead.
- Prefer adding CI (GitHub Actions) to run tests reliably outside the sandbox.


## Codex output style
- Start each response with a short checklist of planned actions.
- End with:
  - ✅ Commands to run
  - ✅ What to verify manually
  - ➡️ Next suggested task
