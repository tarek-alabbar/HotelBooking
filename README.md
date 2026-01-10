# Hotel Booking API

A REST API for searching hotels, checking room availability, and creating bookings. Designed to enforce strict business rules, remain correct under concurrency, and be fully testable.

---

## 1. Overview

This system models a simplified but realistic hotel booking problem:

- Hotels have three room types: **Single, Double, Deluxe**
- Each hotel has **exactly 6 rooms**
- Guests book a **single room for a date range**
- **No room switching is allowed**
- A room **cannot be double-booked for any given night**
- A room **cannot exceed its capacity**
- Every booking has a **globally unique reference**

The API exposes functionality to:

- Search for hotels by name
- Find rooms available for a date range and guest count
- Create a booking
- Retrieve booking details by reference

---

## 2. Key Requirements & Guarantees

This system explicitly guarantees:

| Rule                        | How it is enforced                                         |
| --------------------------- | ---------------------------------------------------------- |
| No double booking per night | Database-level uniqueness on `(RoomId, NightDate)`         |
| No room switching           | Availability only returns rooms free for the _entire_ stay |
| Capacity respected          | Rooms are filtered by capacity before allocation           |
| Unique booking reference    | Database uniqueness constraint                             |
| Deterministic availability  | Single SQL query using `NOT EXISTS`                        |
| Testable system             | Seed/reset endpoints + in-memory DB                        |

These rules are not just “checked in code” they are structurally enforced by the model and database.

---

## 3. High-Level Architecture

### 3.1 Project structure

```
src/
  HotelBooking.Api          → ASP.NET Core Web API

tests/
  HotelBooking.Api.Tests    → Integration tests (xUnit + WebApplicationFactory)
```

There are only two projects by design:

- The API is the production system
- The test project treats the API as a black box and calls it over HTTP

---

### 3.2 Technology stack

| Technology                    | Why it was chosen                                     |
| ----------------------------- | ----------------------------------------------------- |
| ASP.NET Core                  | Modern, fast, production-grade API platform           |
| Entity Framework Core         | Strong relational modeling + migrations               |
| SQLite                        | Lightweight, deterministic, perfect for demos & tests |
| Swagger / OpenAPI             | Self-documenting API for reviewers                    |
| xUnit + WebApplicationFactory | True end-to-end API tests                             |

---

## 4. Domain Model

### 4.1 Core entities

| Entity         | Purpose                                   |
| -------------- | ----------------------------------------- |
| `Hotel`        | A hotel that owns rooms                   |
| `Room`         | A physical room with type & capacity      |
| `Booking`      | A guest booking for a room and date range |
| `BookingNight` | One row per occupied room per night       |

---

### 4.2 Why `BookingNight` exists (the most important design decision)

Most naïve booking systems model bookings like this:

```
Booking
  StartDate
  EndDate
  RoomId
```

Then they try to detect overlaps with date logic in code.

This is fragile under concurrency.

Two requests can race:

1. Both see the room as available
2. Both insert bookings
3. The room is now double-booked

This system does something fundamentally different.

It models each occupied night explicitly:

```
BookingNight
  RoomId
  NightDate
```

With a **unique index** on:

```
(RoomId, NightDate)
```

This means:

- The database itself enforces “a room cannot be double booked for any night”
- No race condition can violate this rule
- EF Core transactions + constraints guarantee correctness

This is how real booking engines are built.

---

## 5. How Availability Works

Availability is computed with a **single SQL query**:

A room is available if:

- It belongs to the hotel
- It has enough capacity
- **There does not exist any booking that overlaps the requested range**

Translated to SQL logic:

```
NOT EXISTS booking
WHERE booking.RoomId = room.Id
AND booking.StartDate <= requested.To
AND booking.EndDate >= requested.From
```

This guarantees:

- No partial overlaps
- No room switching
- No false positives

Only rooms free for the **entire stay** are returned.

---

## 6. How Booking Works

When creating a booking:

1. Rooms are filtered by:

   - Hotel
   - Capacity
   - Optional room type

2. They are sorted by **smallest capacity first** (best-fit allocation)

3. For each candidate room:
   - A booking is created
   - One `BookingNight` row is inserted per night
   - The database enforces uniqueness
4. If a conflict occurs, the next room is tried

5. If all rooms conflict → **409 Conflict**

All of this happens inside a **transaction**.

---

## 7. API Design

### 7.1 Endpoints

| Endpoint                            | Purpose                       |
| ----------------------------------- | ----------------------------- |
| `GET /api/hotels?name=`             | Search hotels by name         |
| `GET /api/hotels/{id}/availability` | Find available rooms          |
| `POST /api/bookings`                | Create a booking              |
| `GET /api/bookings/{ref}`           | Get booking details           |
| `POST /api/admin/reset`             | Reset database (test only)    |
| `POST /api/admin/seed`              | Seed test data                |
| `GET /api/admin/bookings`           | List all bookings (test only) |

---

### 7.2 DTO philosophy

DTOs are named by **purpose**, not by entity.

Examples:

- `HotelSummaryDto`
- `BookingDetailsDto`
- `RoomAvailabilityDto`

There is no generic `HotelDto`.

This prevents:

- Accidental over-exposure
- God-objects
- Breaking API changes

Each endpoint returns **only what it needs**.

---

### 7.3 Error handling

All errors use `ProblemDetails` (RFC 7807).

This gives:

- Machine-readable errors
- Human-friendly messages
- Standardized API behaviour

---

## 8. Admin & Test Endpoints

Seed/reset endpoints exist to:

- Make testing deterministic
- Allow reviewers to try scenarios quickly
- Enable automated integration tests

They are restricted to **Development/Test environments**.

---

## 9. Testing Strategy

Tests are **true integration tests**:

- The API runs in-memory
- SQLite runs in-memory
- Requests are sent over HTTP
- The real EF Core model is used

Tests verify:

- Availability logic
- Booking rules
- Conflict handling
- End-to-end flows

This ensures the system works as a whole — not just in isolation.

---

## 10. Running the system

```bash
dotnet run --project src/HotelBooking.Api
```

Swagger:

```
https://localhost:<port>/swagger
```

---

## 11. Running tests

```bash
dotnet test
```

---

## 12. Design Decisions & Trade-offs

- **BookingNight table** → concurrency-safe booking
- **DTO naming** → stable API contracts
- **AsNoTracking on reads** → performance + correctness
- **Validation at API boundary** → fail fast
- **SQLite** → deterministic and simple for demos
- **No authentication** → per assignment scope

---

## 13. Future Improvements

In a real system you would add:

- Authentication & authorization
- Pagination
- Cancellation / modification flows
- Payment processing
- Metrics & tracing
- Azure SQL + App Service deployment
