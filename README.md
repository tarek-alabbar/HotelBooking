# Hotel Booking API

A REST API for searching hotels, checking room availability, and creating bookings. Designed to enforce strict business rules, remain correct under concurrency, and be fully testable.

---

# 🚀 Running the system

This API is **already deployed and running on Azure** and can be used immediately without cloning or building anything.

### 🌍 Live Azure environment

| Purpose      | URL                                                                                                                                  |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------ |
| Swagger UI   | [https://hotelbookingbytarek.azurewebsites.net/swagger/index.html](https://hotelbookingbytarek.azurewebsites.net/swagger/index.html) |
| Health check | [https://hotelbookingbytarek.azurewebsites.net/health](https://hotelbookingbytarek.azurewebsites.net/health)                         |

Swagger provides a fully interactive UI to:

- Search hotels
- Check room availability
- Create bookings
- Look up bookings by reference
- (Admin) Reset and seed test data

This live environment is intended for reviewers to explore the system and verify behaviour without needing to run anything locally.

---

## 🧪 Reset & seed (for reviewers)

The API exposes special **admin-only endpoints** to make testing deterministic and fast:

| Action         | Endpoint                |
| -------------- | ----------------------- |
| Reset database | `POST /api/admin/reset` |
| Seed test data | `POST /api/admin/seed`  |

These populate:

- Hotels
- Rooms
- Example bookings
- Per-night occupancy records

> These endpoints are disabled by default in production and only enabled for testing and review purposes.

---

## 🖥 Running locally

```bash
dotnet run --project src/HotelBooking.Api
```

Swagger will be available at:

```
http://localhost:<port>/swagger/index.html
```

(The exact port is printed in the console on startup.)

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

These rules are not just “checked in code” — they are structurally enforced by the model and database.

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

Availability is computed with a **single SQL query**.

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

Only rooms free for the **entire stay** are returned.

---

## 6. How Booking Works

1. Candidate rooms are filtered by hotel, capacity, and optional room type
2. They are ordered by **smallest capacity first** (best-fit allocation)
3. For each room:

   - A booking is created
   - A `BookingNight` row is inserted for each night
   - The database enforces uniqueness

4. If a conflict occurs, the next room is tried
5. If all rooms conflict → **409 Conflict**

Everything runs inside a **transaction**.

---

## 7. API Endpoints

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

## 8. Testing Strategy

All tests are **true integration tests**:

- Real API
- Real EF Core
- Real SQLite
- HTTP calls

This validates behaviour exactly as it runs in production.

---

## 9. Running tests

```bash
dotnet test
```

---

## 10. Design Decisions & Trade-offs

- `BookingNight` → concurrency-safe bookings
- DTOs by purpose → stable API
- Best-fit allocation → efficient room usage
- SQLite → deterministic & simple
- No auth → assignment scope

---

## 11. Future Improvements

- Authentication & roles
- Cancellations & modifications
- Pagination
- Payments
- Metrics & tracing
- Azure SQL
