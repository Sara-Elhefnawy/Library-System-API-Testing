# LibrarySystem

A library management REST API built with **ASP.NET Core (.NET 10)**, **EF Core**, and **SQL Server**. Members borrow and return books under a set of business rules (loan limits, fines, membership expiry), and the project is covered by four layers of automated tests: unit, integration, architecture, and load.

---

## Why this project exists

The API itself is deliberately small. The point of the project is to show how a backend is **verified**:

| Question | Answered by |
|---|---|
| Does the business logic behave correctly in isolation? | Unit tests (Moq, EF InMemory, SQLite) |
| Does the whole stack work against a real database? | Integration tests (WebApplicationFactory + Testcontainers SQL Server) |
| Does the code structure stay clean over time? | Architecture tests (NetArchTest) |
| Does it hold up under traffic? | Load tests (NBomber) |

---

## Architecture

Three layers, with dependencies pointing inward only:

```
LibrarySystem.API        Minimal API endpoints, DI setup, Swagger
        |
        v
LibrarySystem.Services   Business rules (BorrowService), custom exceptions
        |
        v
LibrarySystem.Data       EF Core DbContext, entities, repositories, seeding
```

- `Data` knows nothing about `Services` or `API`.
- `Services` knows nothing about `API`.
- Endpoints talk to services (or repositories for simple reads), never to the `DbContext` directly.

---

## Tech stack

- **Runtime:** .NET 10, ASP.NET Core Minimal APIs
- **Data:** EF Core, SQL Server (SQLite and InMemory providers in unit tests only)
- **API docs:** built-in OpenAPI document + Swagger UI
- **Testing:** xUnit, Moq, Shouldly, FluentAssertions, NetArchTest.Rules, Testcontainers (MsSql), `WebApplicationFactory`, NBomber

---

## API endpoints

| Method | Route | Description | Success | Errors |
|---|---|---|---|---|
| GET | `/api/books` | List books. `?available=true` returns only books with copies left | 200 | |
| GET | `/api/books/{id}` | Get one book | 200 | 404 |
| POST | `/api/books` | Create a book (ISBN must be 13 characters and unique) | 201 | 400, 409 |
| POST | `/api/members` | Register a member (email must be unique) | 201 | 400 |
| GET | `/api/members/{id}` | Member details | 200 | 404 |
| POST | `/api/borrow` | Borrow a book. Body: `{ "memberId": 1, "bookId": 1 }` | 201 | 422 |
| PUT | `/api/borrow/{id}/return` | Return a book, calculating any fine | 200 | 404, 422 |
| GET | `/api/borrow?memberId=1` | A member's loans, newest first, with book title | 200 | 400 |

### Business rules (enforced in `BorrowService`)

- A member can have at most **3 active loans**.
- A book with **0 available copies** cannot be borrowed.
- A member with an **expired membership** cannot borrow.
- A member with an **outstanding fine** cannot borrow.
- Loans are due **14 days** after borrowing.
- Late returns are fined **£0.50 per day overdue**, added to the member's outstanding fine.
- Returning a loan that was already returned is rejected.
- Borrowing decreases a book's `AvailableCopies` by one; returning increases it by one.

A violated rule returns **422 Unprocessable Entity** with a readable message.

---

## Getting started

### Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB or a container) for running the API
- Docker, only for the integration tests

### Run the API

1. Set the connection string `DefaultConnection` in `LibrarySystem.API/appsettings.Development.json` (or user secrets).
2. Run:

```bash
dotnet run --project LibrarySystem.API
```

In the **Development** environment, startup automatically applies migrations and seeds **50 books and 200 members** (skipped if they already exist). This block is gated on `IsDevelopment()`, so a production deployment never auto-migrates or inserts fake data.

Open `http://localhost:5154/swagger` to explore the endpoints.

---

## Testing

```bash
dotnet test LibrarySystem.UnitTests
dotnet test LibrarySystem.ArchitectureTests
dotnet test LibrarySystem.IntegrationTests   # needs Docker running
dotnet test LibrarySystem.LoadTests          # needs the API already running
```

### Unit tests

Fast tests with no real infrastructure.

- **`BorrowService` rules** with mocked repositories (Moq): loan limit, no copies available, expired membership, outstanding fine, already-returned loans, and verification that `AddAsync` / `Update` are called exactly once.
- **Fine calculation** as a `[Theory]` over on-time, 1-day, 3-day, 14-day, and early-return cases.
- **Repository queries** against the EF InMemory provider: available-book filtering, lookup by ISBN (hit and miss), active loans only.
- **Database constraints** against **SQLite in-memory** (a relational provider, so constraints are really enforced): duplicate email, duplicate ISBN, and the foreign key preventing deletion of a member who has loans.

### Integration tests

Run the real application through `WebApplicationFactory` against a **real SQL Server in a Docker container** (Testcontainers), not a fake database. That gives production-identical behaviour: real T-SQL, real constraints.

- One container per test class, with a freshly migrated and seeded schema.
- Tests that change data reset the rows they touched after every test, so tests never see each other's leftovers.
- Covered: books CRUD and filtering, the full borrow and return lifecycle, `AvailableCopies` staying consistent through borrow, return, borrow again, the loan limit, and duplicate-ISBN conflicts.

### Architecture tests

NetArchTest turns design rules into failing tests:

- `Services` must not depend on `API`; `Data` must not depend on `Services` or `API`.
- Controllers must not touch the `DbContext` or repository interfaces directly.
- Every service and repository class must implement its matching `I...` interface.
- Controllers must live in the `Controllers` namespace; no class name may contain "Manager".
- Custom exceptions must inherit `Exception`; service classes must not be static.

### Load tests

[NBomber](https://nbomber.com) drives real HTTP traffic at a running API (`http://localhost:5154`).

| Scenario | Load | Duration | Pass criteria |
|---|---|---|---|
| `get_all_books` | 100 req/s (5 s warm-up) | 30 s | p99 < 300 ms, 0 failures |
| `get_single_book` | 50 req/s | 30 s | p99 < 300 ms, 0 failures |
| `borrow_book` | 20 req/s | 60 s | 0 unexpected responses, at least one loan created |

The read scenarios run **concurrently** against the same instance. In `borrow_book`, a **422 counts as an expected outcome** (a business rule such as the loan limit or no copies left was hit), and only a response that is neither 201 nor 422 counts as a failure.

**Results**

Measured on a development machine with the API and the load generator running side by side, using a Debug build against a freshly seeded database (50 books, 200 members).

| Scenario | RPS | p50 | p95 | p99 | Failures |
|---|---|---|---|---|---|
| get_all_books | 100 | 2.89 ms | 4.8 ms | 6.62 ms | 0 / 3000 |
| get_single_book | 50 | 2.84 ms | 4.88 ms | 7.93 ms | 0 / 1500 |
| borrow_book | 20 | 4.76 ms | 14.27 ms | 19.9 ms | 0 / 1200 |

In the `borrow_book` run, 498 requests created a loan (201) and 702 were rejected by business rules (422), as expected: 1200 requests against 500 total copies and a 3-loan limit per member.

The HTML report is written to `LibrarySystem.LoadTests/bin/Debug/net10.0/reports/`.

---

## Project structure

```
LibrarySystem.API                  Endpoints, Program.cs
LibrarySystem.Services             BorrowService, custom exceptions
LibrarySystem.Data                 DbContext, models, repositories, seeding
LibrarySystem.UnitTests            Moq, InMemory, SQLite tests
LibrarySystem.IntegrationTests     WebApplicationFactory + Testcontainers
LibrarySystem.ArchitectureTests    NetArchTest rules
LibrarySystem.LoadTests            NBomber scenarios
```
