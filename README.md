# BusBooking

Backend for a Sri Lankan bus booking platform, built with ASP.NET Core using a **Modular Monolith + Clean Architecture** approach. All 22 backend phases from `Surena bus booking.docx` are complete: project structure and cross-cutting concerns; the full domain model; JWT + refresh-token authentication with role-based authorization; Bus, Seat Layout, Route/Stop, and Trip management with full status lifecycles; public trip search; Redis-backed atomic seat locking; segment-based seat availability; booking for registered customers, guests, and staff alike through one shared code path; payment (Cash real, an electronic gateway mocked pending a real Sri Lankan provider); ticket generation with QR verification; a staff passenger register; booking cancellation with configurable rules and cascading refunds; background email notifications (SMS/WhatsApp placeholders) via Hangfire; seven reporting endpoints; audit logging, correlation IDs, security headers, and rate limiting; a broad automated test suite (290 tests); and Docker/Compose support for local and production-shaped deployment. The React frontend (per the doc's own phase plan) is the next, separate body of work.

## Tech stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10) |
| Architecture | Clean Architecture + Modular Monolith |
| ORM | Entity Framework Core (SQL Server) |
| Auth | ASP.NET Core Identity + JWT + refresh tokens |
| Validation | FluentValidation |
| Mediator / CQRS | MediatR |
| Mapping | Mapster |
| API docs | Swagger / OpenAPI |
| Caching / seat locking | Redis *(Phase 11)* |
| Logging | Serilog |
| Background jobs | Hangfire *(Phase 18)* |
| QR codes | QRCoder *(Phase 15)* |
| Testing | xUnit + Moq + FluentAssertions |
| Containerization | Docker *(Phase 22)* |

## Solution structure

```
BusBooking.sln
Directory.Build.props        # shared TFM/nullable/langversion settings
src/
├── BusBooking.API            # Controllers, Middleware, Extensions, Program.cs
├── BusBooking.Application    # Use cases (CQRS), DTOs, validators, interfaces
├── BusBooking.Domain         # Entities, enums, value objects, domain rules
└── BusBooking.Infrastructure # EF Core, Identity, Redis, external services
tests/
├── BusBooking.UnitTests
└── BusBooking.IntegrationTests
```

Project reference rules (enforced by the `.csproj` files):

- **Domain** depends on nothing.
- **Application** depends only on **Domain**.
- **Infrastructure** depends on **Application** and **Domain**.
- **API** depends on **Application** and **Infrastructure**.

Most feature areas under `Application/` and `Infrastructure/` still contain only a `README.md` placeholder describing what lands there and in which phase — see the roadmap below.

## Domain model (Phase 02)

`BusBooking.Domain/Entities` currently has:

- **Bus** — registration number (unique), description, `BusType`, `BusStatus`, optional `SeatLayoutId`
- **SeatLayout** — name, description, rows/columns, owns a collection of `Seat`
- **Seat** — seat number (unique per layout), row/column, `SeatPositionType` (Seat/Driver/Door/Empty/Aisle), active flag
- **Route** — name, from/to, active flag, owns a collection of `RouteStop`
- **RouteStop** — stop name, stop order (unique per route), expected arrival/departure times, pickup/drop-off flags
- **Driver** — full name, phone, license number (unique), license expiry, active flag

All entities use `Guid` (v7, time-ordered) primary keys, encapsulate state behind constructors/methods (no public setters), and are mapped via `IEntityTypeConfiguration<T>` classes in `BusBooking.Infrastructure/Persistence/Configurations` — no EF Core attributes on the domain types. `ApplicationDbContext.SaveChanges(Async)` auto-populates `CreatedAt`/`UpdatedAt` for auditable entities. The initial migration (`InitialCreate`) is in `Persistence/Migrations`.

## Authentication (Phase 03)

`ApplicationDbContext` now also derives from `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` (tables renamed to drop the `AspNet` prefix — `Users`, `Roles`, `UserRoles`, etc.), plus a `RefreshTokens` table. Roles: `SuperAdmin`, `Admin`, `OperationsManager`, `BookingStaff`, `Driver`, `Conductor`, `Customer` (see `BusBooking.Domain.Constants.Roles`) — seeded automatically at startup by `IdentitySeeder`.

Endpoints (`AuthController`, `/api/auth`):

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `/register` | anonymous | Customer self-registration (FullName/Email/PhoneNumber/Password) → `Customer` role |
| POST | `/login` | anonymous | Business or customer login by username/email + password |
| POST | `/refresh-token` | anonymous | Rotates a refresh token — single-use; replaying a spent or expired one returns 401 |
| POST | `/logout` | JWT required | Revokes a specific refresh token (idempotent) |
| GET | `/me` | JWT required | Current user's profile + roles, from JWT claims |

Design notes:

- Access tokens are short-lived JWTs (HS256, 15 min default); refresh tokens are long-lived opaque random values (7 days default), **stored hashed (SHA-256)** in `RefreshTokens` — never in plain text, mirroring password hashing.
- Refresh is rotate-on-use: each call to `/refresh-token` revokes the presented token and issues a new pair atomically, so a stolen-but-unused old token can't be replayed after the legitimate client refreshes.
- Named authorization policies (`RequireSuperAdmin`, `RequireAdminOrAbove`, `RequireOperationsStaff`, `RequireBookingStaff`, `RequireCustomer`) are registered in `AuthorizationPolicies` for later phases to apply via `[Authorize(Policy = ...)]`.
- `Jwt:Secret` and `Seed:SuperAdminEmail`/`Seed:SuperAdminPassword` are **never** committed — set them via `dotnet user-secrets` locally or environment variables in any deployed environment. The SuperAdmin bootstrap account is only created if both `Seed:*` values are configured.

## Bus Management (Phase 04)

`IApplicationDbContext` (`Application/Common/Interfaces`) is the Application layer's only EF Core dependency going forward — it exposes `DbSet<T>` per entity so feature handlers write focused, composable queries directly instead of a repository per entity, per the source doc's own explicit guidance against that pattern.

Endpoints (`BusesController`, `/api/buses`):

| Method | Route | Policy | Purpose |
|---|---|---|---|
| GET | `/` | `RequireBookingStaff` | Paginated, filterable (search term, `busType`, `status`), sortable list |
| GET | `/{id}` | `RequireBookingStaff` | Bus details, including assigned seat layout name |
| POST | `/` | `RequireOperationsStaff` | Create bus (registration number enforced unique) |
| PUT | `/{id}` | `RequireOperationsStaff` | Update details |
| PATCH | `/{id}/activate` | `RequireOperationsStaff` | Soft activate |
| PATCH | `/{id}/deactivate` | `RequireOperationsStaff` | Soft deactivate — no hard delete exists, per the source doc |
| PATCH | `/{id}/seat-layout` | `RequireOperationsStaff` | Assign a `SeatLayout` (validated to exist) |

`RequireOperationsStaff` = SuperAdmin/Admin/OperationsManager; `RequireBookingStaff` additionally includes BookingStaff (view-only, matching the source doc). Enums (`BusType`, `BusStatus`, and all future ones) serialize as strings in JSON, configured once in `Program.cs`.

Two real bugs surfaced and fixed while building and testing this phase (not test-only issues — both would have broken a real deployment):

- `PaginatedList<T>` took `pageSize` in its constructor but never exposed it as a property, which silently breaks `System.Text.Json`'s parameterized-constructor deserialization for any client of a paginated endpoint. Fixed by adding the `PageSize` property.
- `BusBooking.API.csproj` had `InvariantGlobalization` set `true` since Phase 01 — `Microsoft.Data.SqlClient` (EF Core's SQL Server provider) requires ICU and does not run under Invariant Globalization Mode. Every real SQL Server connection would have failed at startup. Fixed by turning it off.

## Seat Layout Management (Phase 05)

The `SeatLayout`/`Seat` domain model was already complete from Phase 02 (entities, EF config, unique-seat-number-per-layout constraint), so this phase is pure Application/API — same `IApplicationDbContext` pattern and `RequireOperationsStaff`/`RequireBookingStaff` policy split as Bus Management.

Endpoints (`SeatLayoutsController`, `/api/seat-layouts`):

| Method | Route | Purpose |
|---|---|---|
| GET | `/` | Paginated, searchable list — lightweight `SeatLayoutSummaryDto` (seat *count*, not the full collection) |
| GET | `/{id}` | Complete layout with every seat, **ordered by row then column** so a client can render the grid directly |
| POST | `/` | Create layout (name, description, rows, columns) |
| PUT | `/{id}` | Update layout details — rejected if shrinking rows/columns would leave an existing seat out of bounds |
| POST | `/{id}/seats` | Add a seat — rejected if outside the layout's declared bounds, or if the seat number or the (row, column) position is already taken |
| PUT | `/{id}/seats/{seatId}/position` | Move a seat / change its `PositionType` (Seat/Driver/Door/Empty/Aisle) — same bounds/collision checks |
| PUT | `/{id}/seats/{seatId}/number` | Rename a seat — rejected on duplicate within the layout |
| PATCH | `/{id}/seats/{seatId}/activate` \| `/deactivate` | Toggle a seat's active flag |
| DELETE | `/{id}/seats/{seatId}` | Remove a seat (no "unused" enforcement yet — nothing can reference a seat until Trip/Booking exist in later phases) |

Assigning a layout to a bus is unchanged from Phase 04 (`PATCH /api/buses/{id}/seat-layout`).

## Routes & Stops (Phase 06)

Endpoints (`RoutesController`, `/api/routes`):

| Method | Route | Purpose |
|---|---|---|
| GET | `/` | Paginated, searchable (name/from/to), filterable by `isActive` list |
| GET | `/active` | Lightweight list of active routes only — for pickers (e.g. Trip creation later) |
| GET | `/{id}` | Complete route with every stop, ordered by `StopOrder` |
| POST | `/` | Create route (name, from, to — from ≠ to) — **starts inactive/draft**, see below |
| PUT | `/{id}` | Update route details |
| PATCH | `/{id}/activate` \| `/deactivate` | Toggle route active state |
| POST | `/{id}/stops` | Add a stop — `StopOrder` auto-assigned as next available; rejects a duplicate stop name in the route |
| PUT | `/{id}/stops/{stopId}` | Update a stop's name, expected arrival/departure, pickup/drop-off flags |
| DELETE | `/{id}/stops/{stopId}` | Remove a stop |
| PUT | `/{id}/stops/reorder` | Reorder stops — body is the full ordered list of stop ids |

Two invariants worth calling out:

- **"A route must contain at least two stops"** is enforced at the boundary that matters — activation — not at creation (a route can't have stops the moment it's created) or on every edit (that would make building up a route from scratch impossible). `RemoveStop` also blocks going below two stops, but only while the route `IsActive` — a draft route can be freely rebuilt down to zero stops.
- **Reordering stops is a two-phase update.** The unique `(RouteId, StopOrder)` index is checked per-statement, not deferred to commit, so writing final orders directly can collide mid-transaction (e.g. swapping stops 1 and 2 briefly makes two stops share order 2). `ReorderStopsCommandHandler` moves every stop to a disjoint temporary range first, saves, then writes final orders and saves again.

**A real bug surfaced and fixed while testing this phase:** `Route.IsActive` defaulted to `true` at construction (copied from `Bus`'s pattern in Phase 02) — which meant a brand-new route with zero stops was already "active," completely bypassing the "≥2 stops to activate" rule above before it could ever run. Integration tests for both `GetActiveRoutes` and stop-removal-on-an-inactive-route caught this immediately (a route I expected to be inactive kept showing up as active). Fixed by having `Route` start as an inactive draft; `Bus`'s default of `Active` on creation is intentionally unchanged, since a bus (unlike a route with no stops) is immediately meaningful on its own.

## Trip Management (Phase 07)

The doc flags this phase ⭐ as the heart of the application: `Trip` = Route + Date + Departure/Arrival times + Bus + optional Driver + Fare, with a status lifecycle `Draft → Scheduled → Boarding → Departed → Completed`, plus `Cancelled` from any non-terminal state.

Endpoints (`TripsController`, `/api/trips`):

| Method | Route | Purpose |
|---|---|---|
| GET | `/` | Paginated, filterable by route/bus/status/date range — one query covers "upcoming trips" (`fromDate=today`), "trips by date" and "trips by route" |
| GET | `/{id}` | Trip details |
| POST | `/` | Create trip (starts `Draft`) — validates route is active, bus is active *and* has a seat layout assigned, driver (if any) is active, and the bus has no overlapping trip |
| PUT | `/{id}` | Update date/times/fare — blocked once `Departed`/`Completed`/`Cancelled` |
| PATCH | `/{id}/bus` | Assign/change bus — same active/seat-layout/overlap checks as creation |
| PATCH \| DELETE | `/{id}/driver` | Assign/change driver, or remove it (driver is always optional) |
| PATCH | `/{id}/schedule` \| `/cancel` \| `/boarding` \| `/departed` \| `/completed` | Status transitions — each one only legal from the correct prior state |

Two design points worth calling out:

- **Overnight trips.** `DepartureTime`/`ExpectedArrivalTime` are time-of-day (`TimeSpan`), matching the doc's own example (departs 8 PM, arrives 5 AM). `Trip.ComputeArrivalDateTime` treats an arrival time-of-day that isn't *after* departure as landing the next calendar day. Bus double-booking is checked against these computed absolute-time windows (`[start, end)` interval overlap), not just matching dates — an overnight trip ending at 5 AM correctly blocks a second trip starting at 3 AM the same morning.
- **State-transition guards live in the domain entity**, not duplicated in every handler (`Trip.Schedule()`, `MarkBoarding()`, etc. each throw `InvalidOperationException` if called from the wrong state). `GlobalExceptionMiddleware` now maps `InvalidOperationException` → 400, so these guards surface as clean client errors without any handler needing its own pre-check.

`TripDto` is the business/admin-facing shape (shows bus registration number, driver name) — the doc explicitly requires a *separate*, deliberately restricted DTO for customer-facing trip search; that lands in Phase 09.

**A doc inconsistency worth flagging:** the source doc's summary table-of-contents lists "PHASE 08 → Driver Management," but its detailed phase-by-phase prompts skip straight from Trip Management (07) to Customer Management (08) — Driver never gets its own detailed prompt at all (its fields were already fully specified back in Phase 02, folded in with Bus/SeatLayout/Route). This README's roadmap follows the detailed prompts, which is what's actually been implemented phase-by-phase. Net effect: `Trip` references `Driver` by id and validates it's active, but there is no `/api/drivers` CRUD endpoint — tests seed drivers directly via the DbContext. Worth building a small DriversController on the Bus Management template if/when useful; ask if you want it.

## Customer Management (Phase 08)

`Customer` (Domain) extends the Phase 03 Identity account with the profile fields Identity doesn't have — `NIC`, `DateOfBirth` — sharing its primary key 1:1 with `ApplicationUser` (`Customer.Id == ApplicationUser.Id`, standard profile-extension pattern; `FullName`/`Email`/`PhoneNumber` stay on `ApplicationUser` as the single source of truth). The row is created lazily on first profile update, not at registration — `POST /api/auth/register` never collects NIC/DateOfBirth, so a fresh customer has no `Customer` row until they save a profile edit; `GET .../profile` just returns nulls for those fields until then, with no write (a GET must stay side-effect-free).

Endpoints (`CustomersController`, `/api/customers/me`) — deliberately self-service only, every action scoped to the caller's own JWT `sub` claim, never a route/body-supplied id:

| Method | Route | Purpose |
|---|---|---|
| GET | `/profile` | Combined view: Identity fields + NIC/DateOfBirth. Never includes password hashes or other security fields. |
| PUT | `/profile` | Update FullName + NIC (validated as a real Sri Lankan NIC format) + DateOfBirth (must be in the past) |
| PUT | `/phone-number` | Change phone number |
| PUT | `/email` | Change email — also updates `UserName` (which has followed `Email` since registration), so login continues to work; rejected if the new email is already registered to someone else |
| PUT | `/password` | Change password — `UserManager.ChangePasswordAsync` verifies the current password internally before accepting the new one |

`[Authorize(Policy = RequireCustomer)]` on the whole controller means business roles get a 403 here — this endpoint group isn't for staff at all, unlike every other controller so far which splits `RequireOperationsStaff`/`RequireBookingStaff`.

`IIdentityService` gained `UpdateFullNameAsync`, `ChangeEmailAsync`, `ChangePhoneNumberAsync`, `ChangePasswordAsync` — and `AuthenticatedUserDto` gained `PhoneNumber`/`CreatedAt`, which it needed for the profile view but never had before (Phase 03 only ever needed `Id`/`UserName`/`Email`/`FullName`/`Roles`).

"View booking history" (also listed in the doc for this phase) isn't implemented — `Booking` doesn't exist until Phase 12/13, so there's nothing to query yet.

## Trip Search (Phase 09)

The first genuinely public endpoint: `GET /api/trips/search?from=...&to=...&date=...`, `[AllowAnonymous]` on an otherwise `[Authorize]`-by-default controller — guests must be able to search before ever creating an account, per the doc.

`TripSearchResultDto` is a deliberately separate type from the admin-facing `TripDto` (built that way back in Phase 07 specifically for this) — it has no property for bus registration number, internal bus id, or driver info, so there's no field to accidentally leak. A test asserts this at the JSON wire level, not just "the DTO shape looks right," since a future refactor could otherwise reintroduce a leak without any type-level warning.

Only trips that are `Scheduled` (not `Draft`, not `Cancelled`, not already `Boarding`/`Departed`/`Completed`), on the exact requested date, on a route whose `From`/`To` match the search *and direction* (searching B→A does not return an A→B route), and have at least one available seat are returned. Results also include the route's pickup-enabled stops.

**"Available seat count" is a known interim simplification**, called out explicitly rather than hidden: it's computed from the bus's *physical* seat layout (active seats with `PositionType.Seat`), because `TripSeat` (per-trip seat state) and `Booking` don't exist until Phases 10 and 12. Since nothing can be booked yet, every physical seat genuinely is unbooked — this is a correct answer for the system's current state, not a stub, but it will need to change to query `TripSeat` status once that exists.

Two EF Core optimizations the doc explicitly asked for:
- `AsNoTracking()` throughout (read-only).
- No N+1: seat-count is computed via one projected query (a correlated `Count()` on the seat-layout navigation, not a separate round trip per trip), and pickup points for the whole result page are fetched in a single batched query keyed by the page's distinct route ids, not one query per trip.
- A new composite index `(RouteId, TripDate, Status)` on `Trips` matches this query's exact filter combination — `Route.From`/`To` already had a composite index from Phase 06.

## Trip Seats & Availability (Phase 10)

`TripSeat` tracks one seat's *lock* state (`Available`/`Held`/`Blocked`) for one specific trip — as of [Phase 13](#segment-based-seat-availability-phase-13) it has no "booked" state of its own. It's generated automatically, not exposed as its own CRUD:

- **On `CreateTrip`**, one `TripSeat` is created for every seat in the bus's layout that's both active and `PositionType.Seat` (Driver/Door/Empty/Aisle positions never get one). Trip's `Id` is generated client-side (`BaseEntity`), so it's already known before the first save — trip creation and seat generation are added to the same `DbContext` and committed with one `SaveChangesAsync`, making the pair transaction-safe by construction rather than by wrapping them in an explicit transaction.
- **On `AssignBus`** (changing a trip's bus), existing `TripSeat`s are deleted and regenerated for the new bus's layout. This is unconditionally safe *only* because nothing can hold or book a seat yet (Phases 11/12) — flagged in the handler as something that will need to change once holds/bookings exist, since blindly wiping them then would destroy real state.
- A unique index on `(TripId, SeatId)` makes "a seat must never be duplicated within a trip" a database guarantee, not just application logic.

Endpoints (added to `TripsController`):

| Method | Route | Auth | Purpose |
|---|---|---|---|
| GET | `/api/trips/{id}/seat-map` | Public | Seat number/row/column/position/status only — same reasoning as `/search`: no passenger or booking data, ever |
| GET | `/api/trips/{id}/seats` | `RequireBookingStaff` | Same shape — staff use `GetBookings?tripId=` ([Phase 12](#booking-phase-12)) to see which booking(s) occupy a seat |
| PATCH | `/api/trips/{id}/seats/{tripSeatId}/block` \| `/unblock` | `RequireOperationsStaff` | Manually block/unblock a seat (e.g. it's physically broken) — only legal from `Available`↔`Blocked`, guarded in the domain entity the same way `Trip`'s status machine is |

**A real gap caught and closed while building this:** `Seat` (Phase 05) had no protection against being removed while in use — `TripSeat`'s new FK to `Seat` is `Restrict`, so without a check, removing a seat already on a trip would have surfaced as a raw DB foreign-key-violation 500 instead of a clean 400. `RemoveSeatCommandHandler` now checks for existing `TripSeat` rows first and returns a proper validation error.

## Redis Seat Locking (Phase 11) ⭐

Prevents two customers from ever both grabbing the same seat. `ISeatLockService` (Application) / `RedisSeatLockService` (Infrastructure, `StackExchange.Redis`) — Redis key `{InstanceName}seatlock:{tripSeatId}`, value is a random per-lock token.

- **Acquire** is `SET key token NX EX 600` — one atomic Redis command. Two concurrent requests for the same seat can never both get a `true` back, regardless of how many API instances issue them; `ConnectionMultiplexer` is registered as a singleton (its intended lifetime — not created per-request), so this holds across the whole fleet, not just one process.
- **Release** can't be a plain GET-then-DEL (that has its own race between the two calls) — it's the standard atomic Lua script from Redis's own distributed-locks documentation: delete only if the stored value still equals the caller's token. Releasing with the wrong token fails without touching someone else's now-current lock; releasing an already-expired/never-existed lock is treated as a safe no-op, not an error — both cases matter for "handle expired locks safely."
- **Redis decides the race; the database stays the record of current state.** `TripSeat.Status`/`LockId`/`LockedUntil` mirror whatever Redis just decided (for the seat map and business views to read), but the database is never what arbitrates *who gets the lock* — only Redis's atomicity does that, per the doc's explicit "do not rely only on Redis for permanent booking state."

Endpoints (`TripsController`), both **public** — no account required, same reasoning as `/search` and `/seat-map`: a guest holds a seat while filling in the rest of the booking flow before ever creating an account:

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/trips/{id}/seats/{tripSeatId}/lock` | Acquire a 10-minute hold; `400` if already booked, blocked, or held by someone else |
| POST | `/api/trips/{id}/seats/{tripSeatId}/unlock` | Release a hold (body: `{ lockId }`) — booking completion and customer-cancels-mid-flow both go through this same endpoint, per the doc |

**Verified against a real Redis, not a fake.** No Docker or `redis-server` package was available in this sandbox (no root), so Redis was built from source in user space (`./configure && make` — only the core server was needed; the optional Bloom/Search/JSON/TimeSeries modules failed on missing OpenSSL headers and were ignored) and run locally on `localhost:6379`, matching `appsettings.Development.json`. This made it possible to test the actual thing the doc cares about: `LockSeat_TenConcurrentAttemptsOnSameSeat_ExactlyOneSucceeds` fires ten simultaneous unauthenticated requests at the same seat and asserts exactly one gets `200` and nine get `400` — run five times in a row with a consistent result, not a lucky pass. Lock expiry is tested by deleting the underlying Redis key directly (equivalent to its TTL having elapsed) rather than waiting out the real 10-minute duration.

## Booking (Phase 12)

Connects everything: Trip → TripSeat → Redis Lock → Booking, in one shared code path for all three actors the doc requires — **registered customer**, **guest**, and **business staff** — via a single public endpoint, `POST /api/bookings`. `Booking` owns a collection of `BookingPassenger` (full name, phone, gender, optional NIC/email, pickup/drop-off stop, seat, server-calculated fare) — `TotalAmount` is kept in sync automatically as passengers are added, never set directly.

**`CustomerId` is decided entirely server-side, never trusted from the request body**, the same pattern used everywhere else in this codebase for "must come from a trusted source" fields (e.g. `UpdateBusCommand`'s `Id` from the route, not the body): the controller inspects the JWT `sub` claim only when the caller is authenticated *and* holds the `Customer` role, and always overrides whatever the client sent — a guest or staff-authenticated request is always `null`, and a test asserts a spoofed `customerId` in the body is silently ignored.

**"Seat is available for the selected journey segment" was a known interim simplification at the time this phase was written** — this phase originally checked whole-trip availability only. It's resolved in [Phase 13](#segment-based-seat-availability-phase-13), which replaced the seat-level `Booked` status with a real segment-overlap check.

Every validation rule the doc lists is enforced, in this order:

1. **Trip is bookable** — `Trip.Status == Scheduled`.
2. **Pickup/drop-off stops belong to the route** — looked up against `trip.RouteId`, not just "some stop exists."
3. **Pickup occurs before drop-off** — compared by `RouteStop.StopOrder`.
4. **The Redis lock belongs to the caller** — the seat's `TripSeat.Status` must be `Held` *and* its `LockId` must exactly match the token the caller presents for that seat. This is enforced in the handler for every passenger — the same rule applies uniformly whether the caller is a customer, a guest, or staff keying in a phone booking, per the doc's explicit "do not duplicate booking logic" instruction.
5. **Fare is server-calculated** — `trip.Fare` is used directly; the client cannot supply or influence a fare value at all (there's no such field in the request contract to begin with).

**Two systems, two different atomicity guarantees, used together correctly:**
- **Database transaction:** creating the `Booking`, its `BookingPassenger`s, and releasing every booked `TripSeat`'s hold are all tracked on one `DbContext` and committed by a single `SaveChangesAsync` — EF Core wraps that in one SQL transaction automatically, satisfying "use database transactions" without an explicit `BeginTransactionAsync`.
- **Redis lock release, deliberately *after* that commit, and best-effort:** the doc requires releasing the lock once a booking completes, but by the time cleanup runs the booking has already succeeded in the database (the source of truth) — so a transient Redis failure during cleanup is caught and swallowed rather than surfacing as a failed booking to the client. The lock's own TTL cleans it up regardless.

**"Prevent duplicate booking"** doesn't need a separate idempotency-key mechanism: a duplicate submission for the exact same segment on the same seat fails at the segment-overlap check ([Phase 13](#segment-based-seat-availability-phase-13)) against the booking that already succeeded — it can be re-locked (the seat itself has no memory of the booking), but not re-booked for an overlapping segment.

Staff-only read endpoints (`GetBookings` paginated/filterable by trip/customer/status, `GetBookingById`) round out the module for basic operational visibility — gated behind `RequireBookingStaff`, unlike `Create`. A customer-facing "view my booking again" lookup is deliberately **not** built here: the doc's Phase 15 (Ticket & QR) is purpose-built for exactly that, with its own security model ("a secure ticket identifier rather than sensitive passenger information") — building an ad hoc guest-accessible `GetBookingById` now would either leak passenger PII to anyone who obtains a booking's `Guid`, or require inventing an auth mechanism Phase 15 already solves properly.

## Segment-Based Seat Availability (Phase 13)

Resolves the interim simplification flagged in Phase 12: a seat is now unavailable only for journey segments that actually **overlap** an existing booking on it, not for the whole trip.

**`TripSeat.Status` no longer has a `Booked` value, and `TripSeat` has no `BookingId`.** This isn't a small tweak — a global "booked" flag on the seat is fundamentally incompatible with the doc's requirement (Colombo→Kurunegala booked, Kurunegala→Jaffna on the same seat "must be allowed"), so it's removed entirely rather than left dormant. `TripSeat.Status` is now only `Available` / `Held` / `Blocked`: a Redis-backed lock state, not a booking record. `TripSeat.Book()` is gone too — a successful booking now calls `TripSeat.ReleaseHold()`, the same method used for an abandoned hold, so the seat returns to `Available` and can be locked again immediately for a different, non-overlapping segment. "Which booking(s) currently occupy a seat" is answered by `GetBookings?tripId=` (built in Phase 12), not by a field on the seat.

**The overlap algorithm — `Domain.Common.SegmentOverlap.Overlaps`** — treats each segment as the half-open range `[pickupOrder, dropOffOrder)` over `RouteStop.StopOrder`, the same interval-overlap shape already proven correct for bus/trip double-booking in Phase 07:

```csharp
firstPickupOrder < secondDropOffOrder && secondPickupOrder < firstDropOffOrder
```

Half-open is deliberate: two segments that only touch at a shared stop (one's drop-off equals the other's pickup — e.g. Colombo→Kurunegala and Kurunegala→Jaffna) do **not** overlap, matching the doc's own example exactly. A pure static method on a domain-level type, with no infrastructure dependency, so it's unit-tested directly against the doc's full required scenario list (exact same segment, partial overlap, one segment fully containing another, non-overlapping, adjacent, and the doc's own Dambulla→Jaffna-rejected-by-an-overlapping-booking example).

**`CreateBookingCommandHandler` enforces it with one batched query, not one query per passenger:** all existing, non-cancelled bookings' passengers for the seats involved in this request are fetched up front (joined against the trip's full route-stop list for `StopOrder`), grouped by seat, and checked against each new passenger's requested segment before that passenger is added to the booking. A rejected segment throws the same `ValidationException` → 400 path as every other booking-time validation rule. Segments from *other* passengers already validated earlier **in this same request** are folded into the same in-memory overlap set as they're processed, so two passengers in one multi-passenger booking can't double-book a seat against each other either — not just against prior bookings.

**Known scope boundary, deliberate:** the Redis seat *lock* (`LockSeat`/`Hold`) is still per `TripSeatId`, not per segment — it's a short-lived (10 minute) exclusivity fence for the checkout flow, established in Phase 11 before segments existed. The doc's Phase 13 requirement is specifically about what an existing *booking* blocks, not about the lock; making the lock itself segment-aware (letting two customers concurrently hold the same physical seat for different segments) would need per-segment lock keys and wasn't asked for. In practice this only matters if two customers try to check out non-overlapping segments of the same seat within the same ~10 minute window — the second simply waits for the first's hold to expire or complete.

A new migration (`RemoveTripSeatBookingId`) drops the now-unused `TripSeats.BookingId` column.

## Payment (Phase 14)

**`Payment` is deliberately not owned by `Booking`** — the doc's "don't tightly couple payment to booking" instruction, taken literally: no navigation collection on `Booking`, no cascade delete, just a `BookingId` foreign key. A booking can end up with zero, one, or several `Payment` rows (a `Failed` attempt followed by a retry, for instance) without `Booking` needing to know anything about payment mechanics. `Payment`'s own fields match the doc's spec exactly — `Id`, `BookingId`, `Amount`, `Currency`, `PaymentMethod`, `PaymentStatus`, `TransactionReference`, `PaidAt`, `CreatedAt` — and nothing else. **No card number or CVV field exists anywhere in this entity**, satisfying "do not store card numbers or CVV" by omission rather than by redaction logic that could fail.

**`IPaymentGateway`** is the abstraction the doc asks for, so a real Sri Lankan provider can be plugged in later without the Booking/Payment domain changing at all:

```csharp
public interface IPaymentGateway
{
    bool Supports(PaymentMethod method);
    Task<PaymentGatewayResult> ChargeAsync(PaymentGatewayRequest request, CancellationToken cancellationToken);
}
```

Two implementations are registered today, both in `Infrastructure/Payments/`:
- **`CashPaymentGateway`** — Cash isn't charged electronically; confirming it represents staff attesting they've physically collected the money, so it always succeeds immediately with a locally generated reference (`CASH-{paymentId}`).
- **`MockPaymentGateway`** — stands in for Card/Online/BankTransfer until a real provider is integrated; always succeeds with a fake reference (`MOCK-{guid}`). Swapping in a real gateway later means registering a different `IPaymentGateway` for those methods in `Infrastructure/DependencyInjection.cs` — nothing above the Infrastructure layer needs to change.

`ConfirmPaymentCommandHandler` picks whichever registered gateway's `Supports()` matches the payment's method (`IEnumerable<IPaymentGateway>` injected, `.First(g => g.Supports(...))` — no keyed DI ceremony needed for two implementations).

**Two-step flow, mirroring how a real checkout works:** `POST /api/payments` creates a `Pending` `Payment` (`Amount` always pulled server-side from `Booking.TotalAmount`, never client-supplied — the same "must come from a trusted source" rule as `Booking.TotalAmount`/`Trip.Fare`); `POST /api/payments/{id}/confirm` is the actual settlement attempt, calling the resolved gateway and then, only on success, calling **`Booking.Confirm()`** (`Pending → Confirmed`, guarded the same way every other status-machine transition in this codebase is) in the same database transaction as `Payment.MarkPaid()`. A booking can only ever reach `Confirmed` through this one path.

**"Ensure payment confirmation is idempotent"** is enforced at the domain level, not just the handler: `Payment.MarkPaid()` is a no-op if the payment is already `Paid`, and the handler returns the existing `PaymentDto` without touching `Booking` again — so a retried client call or a redelivered gateway webhook can never double-charge or call `Booking.Confirm()` on an already-`Confirmed` booking. `CreatePaymentCommandHandler` also rejects a second payment for a booking that already has one `Pending` or `Paid`, closing the race where two concurrent payments could both try to settle and confirm the same booking.

**Known scope boundary, deliberate:** `Create`/`Confirm` are public (`[AllowAnonymous]`), matching `Booking`'s own `Create` — a guest checkout never authenticates, and knowing a booking's `Guid` can't leak passenger data or move real money anywhere unexpected (Cash is a manual staff attestation; the mock gateway never touches a real account), so no extra ownership proof is required yet. A real electronic provider would carry its own session/webhook security entirely inside its `IPaymentGateway` implementation, without this controller needing to change. `Refunded`/`PartiallyRefunded` exist in `PaymentStatus` (and `Payment` has no `Refund()` method yet) for the same reason `BookingStatus` pre-declared `Cancelled`/`Refunded` in Phase 12 — so the column never needs a widening migration — but nothing reaches them until Phase 17 (Cancellation) adds the trigger for it.

## Ticket & QR (Phase 15)

**One `Ticket` per `BookingPassenger`, not per `Booking`.** Each passenger boards and alights at their own stops and needs their own scannable ticket, so `ConfirmPaymentCommandHandler` — right after `Payment.MarkPaid()` and `Booking.Confirm()`, in the same transaction — calls `ITicketGenerationService.GenerateForBookingAsync`, which creates one `Ticket` per passenger on the booking. Unlike `Payment`, a `Ticket` *is* tightly coupled to its `Booking` (`Booking`/`BookingPassenger` navigation properties, `Restrict` delete) — the doc's "don't tightly couple" instruction was specific to payment, and a ticket has no independent lifecycle of its own; it exists purely to represent a confirmed passenger's right to board.

**"QR should contain a secure ticket identifier rather than sensitive passenger information," enforced by construction, not by care at the call site:** `Ticket.TicketCode` is a 256-bit cryptographically random value (`RandomNumberGenerator.GetBytes(32)`, hex-encoded) generated in the entity's constructor — **deliberately not the same value as the row's own `Id`**, so the externally-shared, scannable credential is never the database primary key. The QR image encodes *only* `TicketCode`; nothing else ever goes into it. `Ticket.TicketNumber` is the separate, human-readable identifier (`TKT` + date + 6 random chars, generated the same way `Booking.BookingNumber` already was in Phase 12) shown to the passenger, not scanned.

**QR generation is its own abstraction, `IQrCodeGenerator`,** implemented in Infrastructure with QRCoder's `PngByteQRCode` renderer specifically — not the Bitmap-based `QRCode` class, which depends on System.Drawing/GDI+ and would behave differently (or fail) off Windows. `PngByteQRCode` produces PNG bytes directly, verified for real in the integration tests (decoding the returned base64 and checking the actual PNG magic bytes, not just asserting a non-empty string). QR images are generated on demand from `TicketCode` wherever a ticket is returned, never stored — they're fully deterministic from data already in the database, so persisting a duplicate copy would only be a staleness risk for no benefit.

**`ITicketGenerationService` lives in the Application layer itself, with no Infrastructure counterpart** — unlike `ISeatLockService`/`IPaymentGateway`/`IQrCodeGenerator`, it only needs `IApplicationDbContext` (already an Application-layer abstraction), so splitting it across an interface-here/implementation-there pair would add a layer for no reason. It's idempotent (skips any `BookingPassenger` that already has a `Ticket`), the same idempotency posture as everything else touched by `ConfirmPayment`.

Two endpoints, split by their actual security requirement rather than bundled onto one:
- **`GET /api/tickets/booking/{bookingId}`** — public, like `Booking`/`Payment`'s own `Create`. Whoever holds a booking's `Guid` already has full read/write access to it elsewhere in this API (creating its payment, confirming it), so fetching its tickets to display or print needs no further proof of ownership.
- **`GET /api/tickets/verify/{ticketCode}`** — `RequireBookingStaff`, the doc's explicit "only authorized staff can verify tickets." This is the one genuinely security-sensitive ticket operation, since its result is what decides whether someone boards. A code that matches no ticket returns `200 OK` with `IsValid: false` and a `Reason`, not a `404` — scanning a fake or garbled QR is an expected, non-exceptional outcome for a verification endpoint, not a server error. `IsValid` is `true` only while the ticket's booking is `Confirmed`; `Cancelled`/`Completed`/`NoShow`/`Refunded` all report `IsValid: false` with the specific `BookingStatus` and a reason, matching the doc's required response shape (Valid/invalid, passenger name, seat number, trip, pickup, drop-off, booking status) in one call.

## Passenger Register (Phase 16)

**`GET /api/trips/{id}/passenger-manifest`** — the doc's own explicit business requirement ("This is your business requirement"), returning exactly the fields it lists (seat number, passenger name, gender, phone number, pickup point, drop-off point, booking number, booking status) for every passenger on a trip, one row per `BookingPassenger` across every `Booking` on that trip regardless of status by default.

**Deliberately not paginated.** Every other list endpoint in this codebase (`GetBookings`, `GetPayments`, `GetTrips`) returns a `PaginatedList<T>`, but this one returns a plain `IReadOnlyList<PassengerManifestEntryDto>` on purpose: the doc asks for "all data required for an A4 printable passenger register... PDF generation later... Excel export later" — none of those consumers want "page 2 of the manifest," they need the whole trip's passenger list in one response to lay out or export.

**Search/sort/filter, matching the doc's list one-to-one:**
- **Search** (`searchTerm`) — matches passenger name, phone number, NIC, or booking number, the same `.Contains()`-based approach `GetBusesQueryHandler` already uses for its own search.
- **Sort by seat number** — the only sort the doc asks for, so `SortDescending` is a single bool rather than a generic `SortBy` field name; "seat number order" is implemented as `Seat.Row` then `Seat.Column`, the same physical-position ordering `GetTripSeatsQueryHandler` already sorts by, rather than a lexical sort on the `SeatNumber` string (which would misorder seats like "9" vs "10").
- **Filter by pickup point** (`pickupStopId`) and **filter by booking status** (`bookingStatus`).

The query is written as one projected LINQ query (`SelectMany` over each booking's passengers, then `.Select()` straight into the DTO) rather than `.Include()`-then-materialize-then-map — EF Core generates the necessary joins directly from the projection, so the database only returns the eight columns actually needed, not full `Booking`/`BookingPassenger`/`Seat`/`RouteStop` entity graphs.

**Staff-only** (`RequireBookingStaff`) — "ensure only authorized business roles can access passenger manifests," the same policy gate as every other endpoint exposing passenger PII in bulk (`GetBookings`, `GetPayments`). No frontend is built for this phase, per the doc's explicit instruction.

## Cancellation (Phase 17)

**`PATCH /api/bookings/{id}/cancel`** supports both actor paths the doc lists — customer and business-staff cancellation — through one endpoint and one shared authorization rule, gated behind a new `RequireBookingStaffOrCustomer` policy: staff can cancel *any* booking; an authenticated `Customer` can only cancel their *own* (`booking.CustomerId == callerId`, checked in the handler since it needs the loaded `Booking`, not just a role — a mismatch throws `ForbiddenAccessException` → 403). `CancelledBy` and "is this a staff cancellation" are always decided from the JWT, never the request body, the same "trusted source" rule as `CustomerId` on `Booking.Create`.

**Known scope boundary, deliberate: a guest booking (no linked `CustomerId`) cannot be self-cancelled yet.** There's no secure way for an unauthenticated caller to prove "this is my booking" today — the guest-checkout trust model used everywhere else (`Booking`/`Payment`/`Ticket` `Create`/`Confirm`/`Get`) works precisely *because* those actions are harmless to a third party holding the same `Guid`, but cancellation actively deprives the legitimate holder of their seat, so it doesn't get the same blanket trust. A guest who needs to cancel calls support, and staff cancel on their behalf through this same endpoint. A proper fix (a secure link emailed/texted at booking time) needs Phase 18's notification infrastructure, which doesn't exist yet.

**"Update payment/refund status," precisely:** `Booking.Cancel()` always transitions `Pending`/`Confirmed → Cancelled`. If the booking has a `Paid` `Payment`, that payment is also transitioned (`Payment.Refund()`, `Paid → Refunded`) and the booking is taken one step further, `Cancelled → Refunded` — giving `BookingStatus.Refunded` (pre-declared back in Phase 12, unreached until now) a precise meaning: *cancelled, and money was actually given back*, distinct from a plain `Cancelled` where nothing had been paid yet. `Payment.Refund()` is a full refund only — the doc doesn't ask for partial refunds in this phase, so `PaymentStatus.PartiallyRefunded` stays unreached, same as `Refunded` itself was until this phase.

**"Release applicable seats. Release Redis locks if applicable" needed no new code, and deliberately got none.** By the time a `Booking` exists at all, `CreateBookingCommandHandler` has already released its passengers' seat holds (`TripSeat.ReleaseHold()`) and their Redis locks (Phase 13) — there's nothing left tied to a `Pending`/`Confirmed` booking to release. A cancelled booking's segments stop counting toward the seat-overlap check automatically too, since that query already excludes `Cancelled` bookings (Phase 13's `b.Status != BookingStatus.Cancelled` filter). Writing code to "release" a `TripSeat`'s current `Held` state on cancellation would actually be a bug: that state has no reliable link back to *this* booking any more (a different customer could legitimately be holding the same physical seat for an unrelated, non-overlapping segment at that exact moment), so blindly touching it could steal someone else's active hold.

**"Create configurable cancellation rules"** → `CancellationPolicySettings` (bound from the `CancellationPolicy` config section, `MinimumHoursBeforeDeparture`, default 2), the same `IOptions<T>` pattern already used for `JwtSettings`/`RedisSettings` — but defined in the *Application* layer, not Infrastructure, since it's genuinely a business rule a handler needs to read, not an infrastructure integration detail; only its `services.Configure<T>(...)` binding lives in Infrastructure's `DependencyInjection.cs`, same as the others. The rule applies **only** to the customer path — staff can always cancel up until the trip itself completes, which is exactly why "customer cancellation" and "business staff cancellation" are two named actor paths in the doc rather than one.

**"Prevent cancellation of completed trips"** is a `Trip.Status == Completed` check in the handler (loaded via `booking.TripId`), independent of `Booking`'s own state-machine guard (`Cancel()` itself only allows `Pending`/`Confirmed`) — the two guards catch different mistakes: one stops re-cancelling an already-settled booking, the other stops cancelling a booking whose trip has already run.

**"Create audit records for cancellations"** is satisfied by `Booking`'s own new fields — `CancellationReason`, `CancelledBy`, `CancelledAt` — rather than a separate generic audit-log table. Once `Cancel()` sets them, nothing can overwrite them (cancelling twice is rejected outright), so they function as a permanent, immutable record of exactly what the doc asks for: what happened, who did it, when, and why. A general-purpose audit system is Phase 20's explicit job ("Audit & Security"); building a one-off `CancellationAudit` table now would duplicate work that phase is meant to formalize properly.

**Cancelling a Trip now cascades to its bookings** (`CancelTripCommandHandler`, extended in this phase): every `Pending`/`Confirmed` booking on a cancelled trip is cancelled and, if paid, refunded — using the exact same `BookingCancellationHelper.CancelAsync` the direct booking-cancellation path uses (the "don't duplicate booking logic" rule, applied here too), with `CancelledBy: null` marking it as system-triggered rather than a specific person's action. Before this phase, `CancelTripCommandHandler` didn't touch `Booking` at all — reasonably, since `Booking` didn't exist when Trip cancellation was built in Phase 07 — but leaving it that way now would mean a cancelled trip could still show seats "sold" and paid bookings with no refund ever recorded, so it's fixed here rather than left as a known gap.

## Notifications (Phase 18)

**`INotificationService.NotifyAsync`** is deliberately the only thing the rest of the app ever calls: it persists a `Pending` `NotificationLog` and enqueues a Hangfire job, with no channel I/O on the request path at all — the doc's explicit "do not make the booking API wait for email/SMS delivery." `NotificationLog`'s columns are exactly the doc's list (Recipient, Notification type, Status, SentAt, Error message, Retry count), plus `Channel`/`Subject`/`Body` so a retry has everything it needs from the row alone.

**Real delivery for Email, honest placeholders for SMS/WhatsApp — matching the doc's own wording exactly** ("Email, SMS placeholder, WhatsApp placeholder"): `EmailChannelSender` uses MailKit for real. Two modes, chosen entirely by config: if `Email:PickupDirectory` is set, every message is written as a genuine `.eml` file there instead of going out over the network — no SMTP server needed, the same "always works, no external dependency" posture `CashPaymentGateway` has — otherwise it connects to `Email:Host` for real SMTP. Both are exercised for real in the integration tests: they poll `NotificationLogs` for a terminal status (a real Hangfire background worker processes the job asynchronously — the same honest "eventually consistent" testing this phase's own architecture demands, not an immediate assertion), then parse the actual `.eml` files back with MimeKit and check their real `To`/`Subject`. `SmsChannelSender`/`WhatsAppChannelSender` log intent and report success — no real Sri Lankan gateway (Dialog, Mobitel, a Twilio-style provider) is integrated yet, exactly what "placeholder" means; swapping either for a real one is a new `INotificationChannelSender`, nothing else changes.

**`NotificationDispatchJob`** (what Hangfire actually invokes) throws on a failed send rather than swallowing it, specifically so Hangfire's own automatic retry re-invokes it later with backoff — `RetryCount` (incremented once per real invocation) is then a meaningful record of what actually happened, not a field that's always `1`. The registered failure-and-retry path isn't exercised by the integration tests, though — both real channels (pickup-directory email, the SMS/WhatsApp placeholders) always succeed by design, the same reason `MockPaymentGateway`'s failure path was only unit-tested back in Phase 14; retry semantics themselves are covered directly at the `NotificationLog` unit level instead.

**Four of the doc's six events are wired to real trigger points**; the other two are deliberate, documented gaps, not oversights:
- **`BookingConfirmed` and `PaymentSuccessful`** — both fire from `ConfirmPaymentCommandHandler`, the one place both things become true simultaneously.
- **`BookingCancelled`** — fires from `BookingCancellationHelper`, so it covers direct booking cancellation *and* Phase 17's trip-cancel cascade automatically, from one call site.
- **`UpcomingTripReminder`** — the one event that's time-based rather than handler-triggered, so it's a Hangfire *recurring* job (`UpcomingTripReminderJob`, hourly) instead. Deduplication is a 20-hour, per-recipient time window rather than a strict per-booking guarantee — `NotificationLog` has no `BookingId`/`TripId` column (the doc's field list doesn't include one), so "already reminded this exact booking" isn't a query that can be answered precisely; a same-day-per-recipient window is a much simpler proxy that still stops the same person being reminded every time the job runs.
- **`TripCancelled` and `TripTimeChanged`** are declared in `NotificationEventType` (so the column never needs a widening migration later, the same reasoning `BookingStatus`/`PaymentStatus` already used for values that waited several phases to become reachable) but nothing raises them yet. `TripCancelled` is functionally redundant with the `BookingCancelled` cascade already firing per passenger when a trip is cancelled; `TripTimeChanged` would need new schedule-change-detection logic in `UpdateTripCommandHandler` that's a genuinely separate feature, not just another notification call.

**Where Hangfire's job storage lives is a config-driven swap, the same idea EF Core's InMemory provider already applies to `ApplicationDbContext`:** `Hangfire.SqlServer` for real (sharing the app's own database — Hangfire manages its own schema there independently of EF Core migrations, so no EF migration exists for it), `Hangfire.MemoryStorage` only when `Hangfire:UseMemoryStorage` is set, which `CustomWebApplicationFactory` does for every test host. That flag has to be set as an **environment variable**, not through the usual `ConfigureAppConfiguration` test hook: `Program.cs` reads it synchronously while building services, before that hook's additions are visible, and an environment variable is folded into `builder.Configuration` from the very first line of `WebApplication.CreateBuilder(args)` instead. `Email:PickupDirectory` doesn't have this problem — `IOptions<EmailSettings>` is only resolved later, per-request, by which point the full test configuration has long since settled — so it's still set through the normal hook.

**The recurring job is registered in `Program.cs`, after `app.Build()`, deliberately not inside `AddInfrastructureServices`:** the static `RecurringJob.AddOrUpdate` helper needs Hangfire's job storage fully initialized and reachable, which `dotnet ef migrations add`/`dotnet ef migrations script` never provides (their design-time host builds just enough DI to construct `ApplicationDbContext` and stops) — registering it there broke every migration command. The fix is also the more correct one Hangfire's own error message recommends: resolve `IRecurringJobManager` from DI instead of using the static `RecurringJob` helper, which only runs once the app is genuinely finished starting.

**An unrelated environment fix, made in passing:** the integration test host now disables ASP.NET Core's config-file `reloadOnChange` watchers (`DOTNET_hostBuilder:reloadConfigOnChange=false`) and runs test classes sequentially rather than in parallel (`xunit.runner.json`). Neither is specific to notifications — both were needed because this phase pushed the number of `WebApplicationFactory` hosts far enough that their `FileSystemWatcher`s exhausted a low per-user `inotify` instance limit on constrained dev machines. Config reload was never used by any test anyway (config never changes mid-test-run), so removing the watcher costs nothing.

## Reports (Phase 19)

**`ReportsController` — all seven of the doc's reports, one controller, one shared authorization policy** (`RequireBookingStaff` at the class level, no per-action overrides — reports expose booking/passenger/revenue data in bulk, the same sensitivity level as `GetBookings`/`GetPayments`/the Phase 16 manifest). Every report returns the full filtered dataset rather than a page, for the same reason `GetPassengerManifest` did in Phase 16: the doc's own framing — "designed for future React dashboards and Excel/PDF export" — means the whole point is exporting everything that matches the filters, not paging through it.

**The doc's shared filter set (from date, to date, route, trip, booking status) means a different thing per report, because each report's underlying date field is different** — documented on each query rather than left implicit: Daily Booking Report and Customer Booking History filter on `Booking.CreatedAt` (when the booking was made); Trip Passenger Report, Seat Occupancy Report and Pickup-Point Passenger Report filter on `Trip.TripDate` (which trips are in scope); Revenue Report filters on `Payment.PaidAt` (when money actually arrived, not when the `Payment` row was created); Cancellation Report filters on `Booking.CancelledAt`. Seat Occupancy Report skips the `BookingStatus` filter entirely — occupancy is inherently about every non-cancelled booking on a trip, not a single status value.

**Two reports share one query builder, `PassengerReportQueryHelper`**, since Trip Passenger Report and Pickup-Point Passenger Report are the exact same join and filters (`Bookings → Passengers → Trip`) — they only sort the result differently (by trip/seat vs. by pickup point). One projected LINQ query each, not `.Include()`-then-map, so the database returns only the columns each DTO needs — the doc's "avoid loading unnecessary entities," applied consistently with how Phase 16's manifest query was already built.

**Revenue correctly excludes refunded payments with no extra logic**, because `PaymentStatus.Refunded` and `PaymentStatus.Paid` are different values — filtering `Payment.Status == Paid` for revenue automatically drops anything Phase 17's `Payment.Refund()` has since moved out of `Paid`, without the report needing to know anything about cancellation at all.

**Seat Occupancy Report** computes `TotalSeats` (every `TripSeat` generated for that trip) and `BookedSeats` (distinct `SeatId`s among that trip's non-`Cancelled`/non-`Refunded` bookings' passengers) as two correlated subqueries inside one projected query over `Trips`, then computes the percentage client-side after materializing — SQL Server and EF Core InMemory both translate the subquery form fine, but doing the division only after fetching the (small, per-trip) result avoids re-expressing the same two subqueries a third time just to compute a ratio.

## Audit & Security (Phase 20)

Several of the doc's asks were already satisfied by earlier phases and are called out here rather than rebuilt: **global exception handling** (Phase 04's `GlobalExceptionMiddleware`), **input validation** (FluentValidation's `ValidationBehavior`, Phase 01), **authorization checks** (policy-based `[Authorize]` everywhere since Phase 04), and **structured logging** (Serilog, Phase 01). What follows is what's actually new.

**`AuditLog`** has exactly the doc's fields (`Id`, `UserId`, `Action`, `EntityName`, `EntityId`, `OldValues`, `NewValues`, `IPAddress`, `Timestamp`) and no mutation methods at all — it's append-only, written once by its constructor and never touched again.

**Recording one happens automatically, not via an explicit call in each handler.** `AuditLoggingBehavior<TRequest,TResponse>` — a MediatR pipeline behavior, registered after `ValidationBehavior` so an invalid request never reaches it — records an `AuditLog` for any command implementing the `IAuditableRequest` marker interface (`AuditAction`, `AuditEntityName`, `AuditEntityId`). ~18 existing commands across every module the doc's "track important actions" list names opt in this way: `Login`; `CreateBus`/`UpdateBus`; `CreateRoute`/`UpdateRoute`/`ActivateRoute`/`DeactivateRoute`; `CreateTrip`/`UpdateTrip`/`CancelTrip`/`AssignDriver`/`RemoveDriver`; `CreateBooking`/`CancelBooking`; `CreatePayment`/`ConfirmPayment`; `BlockSeat`/`UnblockSeat`. `NewValues` is the handler's own response, serialized — no second query needed to capture "what it became." `OldValues` is deliberately left null throughout: capturing genuine "before" state generically would mean either loading every audited entity twice or teaching each of those ~18 handlers to report it itself, disproportionate to what this phase asks for.

**"User/role changes" is the one tracked-action category with nothing to hook into.** No command in this codebase changes a user's role at runtime — role assignment only ever happens at registration (fixed to `Customer`) or via the identity seeder — so there is genuinely no existing action to audit here yet. Flagged rather than silently skipped; building a new role-management feature just to have something to audit would be real scope creep for an auditing phase.

**Recording who acted needed one new abstraction, `ICurrentUserService`, that deliberately breaks this codebase's usual rule.** Everywhere else, "who's acting" is an explicit command parameter the controller decides from JWT claims itself (`Booking.Create`'s `CustomerId`, `CancelBooking`'s `CancelledBy`) — but audit logging is a cross-cutting concern applied uniformly to many unrelated commands by one pipeline behavior, so it needs ambient access to the caller's identity and IP instead of every audited command carrying its own copy of both. `CurrentUserService` (Infrastructure, backed by `IHttpContextAccessor`) is the one and only place this codebase reads current-user identity ambiently rather than through an explicit parameter.

**A real security bug was caught and fixed by actually running this, not just writing it:** the very first end-to-end test run threw `InvalidCastException: Unable to cast object of type 'System.String' to type 'System.DateTime'` from inside the audit serializer — `AuthResult.AccessTokenExpiresAtUtc` (a `DateTime`) matched the redaction rule's `"token"` name fragment (meant for `AccessToken`/`RefreshToken`) and got its getter replaced with a string, which the JSON writer then tried to serialize as a `DateTime` and failed on every single `Login`. Fixed by restricting redaction to string-typed properties only — a `DateTime` matching a sensitive-sounding name isn't itself a secret value anyway. Worth stating plainly: without running the real login flow through the real pipeline, this would have shipped as a 500 error on every login in production.

**"Do not log passwords/JWT tokens/card details," enforced at one choke point, not trusted to every DTO:** `AuditJsonSerializer.Serialize` is the *only* place any audit `NewValues`/`OldValues` payload is ever produced, using a `System.Text.Json` `TypeInfoResolver` modifier that redacts any string-typed property whose name contains `password`, `token`, `cvv`, `cardnumber`, `securitycode`, or `secret` — checked in `AuthResult.AccessToken`/`RefreshToken` specifically, but applied to *every* audited response's object graph, so a future DTO that accidentally grows a sensitive field is protected by construction, not by remembering to check. (Card details were never a risk to begin with — Phase 14 never gave `Payment` a card-number field at all.)

**`GET /api/audit-logs`** — paginated, unlike the Phase 19 reports, since an audit trail only grows rather than being a bounded export — is gated behind `RequireAdminOrAbove`, deliberately stricter than the `RequireBookingStaff` every other business-data endpoint uses: it exposes what *every* role, including staff themselves, has been doing across the whole system.

**Correlation ID** — `CorrelationIdMiddleware`, first in the pipeline — accepts an incoming `X-Correlation-Id` header (so a caller or gateway can thread its own id through) or generates one, echoes it on the response, and pushes it into Serilog's `LogContext` so *every* log line for a request carries it, not just its error response (which is all the pre-existing `TraceIdentifier`-based approach in `GlobalExceptionMiddleware` covered). That middleware now reads the same id instead of `TraceIdentifier` directly, so both mechanisms report the same value.

**Security headers** (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`) via a small middleware, scoped for what this actually is — a pure JSON API. No `Content-Security-Policy`: CSP governs what an HTML document is allowed to load and execute, which matters for the React SPA that will eventually consume this API, not for an API that never renders markup of its own (Swagger UI is dev-only, already gated behind `IsDevelopment()`). `UseHsts()` is added outside `Development`, matching the framework's own guidance not to use it there.

**Rate limiting** uses ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting` — no third-party package. A generous global default (300 req/min/IP) plus a much stricter policy on `/api/auth/login` specifically (5 req/min/IP, the doc's classic brute-force-prevention case), partitioned by client IP since the case that matters most — an anonymous login attempt — has no user identity yet to partition by. Both limits are configuration-driven, not hardcoded, specifically so tests can raise them: every test in the integration suite shares one loopback "IP", and a full run makes far more than 300 requests/minute (or 5 login calls/minute — one helper alone calls `/api/auth/login` once per test needing a staff token) in total. `CustomWebApplicationFactory` raises both to effectively unlimited; a separate `RateLimitedWebApplicationFactory` (its own type, not derived from it) sets a genuinely low login limit instead and proves a real `429 Too Many Requests` happens, restoring the shared environment variable in `Dispose` so no later test class inherits the low limit.

## Testing (Phase 21)

**This phase is mostly an audit, not a rebuild** — this codebase has followed a "verify every phase for real as it's built" discipline since Phase 01, so by the time Phase 21 was reached, the suite already stood at 287 tests (110 unit, 177 integration) covering essentially every item on the doc's checklist: Authentication, Bus management, Seat layout, Routes, Stops, Trips, Trip seats, Seat locking, Trip search, Segment availability, Booking (guest, registered customer, *and* manual business/staff booking), Payment, Cancellation, Ticket verification, and Authorization (woven through nearly every controller's own tests — 13 different test files assert a `401`/`403` somewhere — rather than collected into one separate file, so each auth rule sits next to the feature it protects). Phase 21's actual job was checking that inventory against the doc's checklist line by line and filling what was genuinely missing, not re-testing what already had real coverage.

**Two genuine gaps found in the doc's "especially test concurrency scenarios" list, both filled with real, non-mocked, non-flaky tests:**
- **"Booking after lock expiration"** — nothing previously proved that a *stale* lock token stops working once someone else has actually taken the seat. `Create_WithLockIdFromBeforeAnExpiredLockWasReacquired_ReturnsBadRequest` deletes the original lock's Redis key directly (simulating real TTL expiry, the same technique Phase 11's own expiry test already used), lets a second customer re-lock the now-free seat, and proves the *first* customer's original token — never explicitly revoked, just superseded — is rejected by `CreateBookingCommandHandler`'s database-mirror check once the second customer's token is what's actually on file. This also verifies something not spelled out anywhere before: `TripSeat`'s database row deliberately never auto-reverts to `Available` just because Redis's key expired — it stays `Held` under the original token until someone else's successful `LockSeat` overwrites it, which is exactly why the check has to compare *tokens*, not just status.
- **"Two customers attempting to book the same seat"**, end to end rather than at the lock layer alone (already covered by Phase 11's `LockSeat_TenConcurrentAttemptsOnSameSeat_ExactlyOneSucceeds`) — `Create_TwoCustomersRacingForTheSameSeat_ExactlyOneBookingSucceeds` runs two full lock-then-book sequences concurrently via `Task.WhenAll` (clients created up front and disposed only in `finally`, the same pattern already fixed once in Phase 11 after disposing inside the loop broke the race), asserting exactly one `201 Created` and the other a rejection. Stable across repeated runs, not just observed once.

The doc's other concurrency items — "booking cancellation" and "same seat on non-overlapping route segments" — were already covered by Phase 17's and Phase 13's own test suites respectively (not races in the same sense as the two above, more general correctness cases the doc's list re-emphasizes).

**"Use Moq or NSubstitute"** — Moq was already a dependency and is used exactly where it's the right tool: `ValidationBehaviorTests` mocks `IValidator<T>` for a pure Application-layer pipeline test with no need for a database at all. It's deliberately *not* used to fake `IApplicationDbContext`, Redis, or Hangfire anywhere else — this codebase's consistent choice since Phase 03 has been real dependencies wherever feasible (EF Core InMemory only as a drop-in swap for SQL Server, a real Redis built from source, real Hangfire with `MemoryStorage`), because a mocked DbContext or a mocked distributed lock can't actually prove a query, a race, or a TTL behaves correctly — only a real one can, which is exactly what made the two concurrency bugs found back in Phases 11 and 12 (and the audit-serializer bug in Phase 20) real, catchable bugs instead of passing mocks.

**"Use a test database/container where appropriate," addressed but deliberately not with a new container here:** no Docker or root access exists in this environment (the same constraint noted back in Phase 01, which is why Redis was built from source instead), and EF Core's InMemory provider has been the consistently documented SQL Server substitute since Phase 03. Introducing a real containerized SQL Server now, one phase early, would preempt Phase 22 (Docker & Deployment) — the doc's own designated home for containerization — rather than reuse it. Redis, the one dependency this project could and did stand up for real, has been real in every test that touches seat locking since Phase 11.

## Docker & Deployment (Phase 22)

The final backend phase — the doc's own architecture diagram (`React → ASP.NET Core API → SQL Server → Redis`) is exactly this project's dependency chain, containerized.

**`src/BusBooking.API/Dockerfile`** — multi-stage: an SDK stage restores (project files copied and restored *before* the rest of the source, so Docker's layer cache survives any change that doesn't touch a `.csproj`) and publishes; the runtime stage is the much smaller `aspnet` image, running as the base image's built-in unprivileged `app` user rather than root, listening on plain HTTP (`8080`, the .NET 8+ default, set explicitly rather than relied upon) — TLS is terminated by a reverse proxy in front of the container, a standard pattern, not by Kestrel itself. A `HEALTHCHECK` instruction (curl against `/health`, installed in a small `apt-get` layer since the slim runtime image doesn't ship it) means `docker run` alone reports container health, not just Compose.

**Two compose files, not one, because "development configuration" and "production configuration" need to actually differ, not just carry different labels:**
- **`docker-compose.yml`** — the base, development-shaped: SQL Server and Redis ports published to the host (for connecting a DB client or `redis-cli` directly while developing), `ASPNETCORE_ENVIRONMENT=Development`, and `Email:PickupDirectory` set so outgoing mail is written as real `.eml` files to an inspectable volume instead of requiring a real SMTP relay (the same pickup-directory mode `EmailChannelSender` has supported since Phase 18).
- **`docker-compose.prod.yml`** — a production *overlay*, applied with `-f docker-compose.yml -f docker-compose.prod.yml`: SQL Server/Redis ports no longer published to the host at all (only reachable from other containers on the same Docker network), `ASPNETCORE_ENVIRONMENT=Production` (picking up `appsettings.Production.json`'s compact single-line JSON console logs, meant for a log-aggregation pipeline reading stdout rather than a human reading a terminal), and no pickup-directory fallback for email — a real `Email__Host` becomes required, so a misconfigured production deployment fails loudly instead of silently writing undelivered mail to disk.

**Environment variable configuration, and the doc's explicit "do not hard-code" list, satisfied the same way this project has treated secrets since Phase 03's user-secrets:** nothing in any committed file — `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`, either compose file — contains a real database password, JWT secret, Redis password, or email credential; every one of them is `${VAR_NAME}` in compose, sourced from a local `.env` (gitignored, `.gitignore` already had the pattern staged since Phase 01) copied from the newly-added, fully-committed **`.env.example`** template. Docker Compose's own `${VAR:?message}` syntax is used for the genuinely required secrets (`DB_SA_PASSWORD`, `REDIS_PASSWORD`, `JWT_SECRET`) — `docker compose up` refuses to start with a clear message rather than silently booting with an empty password. Payment credentials get a placeholder variable and an explicit comment explaining why it's empty: no real Sri Lankan payment provider is integrated yet (Phase 14 built Cash and a Mock gateway; a real one is a future `IPaymentGateway` implementation, not a Phase 22 concern).

**Health checks — a real dependency check, not a ping-yourself endpoint:** `GET /health` (distinct from the pre-existing `GET /api/health`, a dependency-free liveness check from Phase 01) uses ASP.NET Core's built-in `Microsoft.Extensions.Diagnostics.HealthChecks`, wired to two real checks — `AddDbContextCheck<ApplicationDbContext>()` for the database (works against EF Core InMemory in tests too, reporting Healthy, since InMemory always "connects") and a two-line custom `RedisHealthCheck` that `PING`s the same `IConnectionMultiplexer` already used for seat locking — no third-party health-check package needed for either. Verified for real, not just wired up: `GetReadiness_ChecksTheRealDatabaseAndRedis_ReportsHealthy` hits `/health` through the full pipeline and asserts both checks report `Healthy` from an actual round trip, the same real-Redis discipline this project has followed since Phase 11. This is also what both the Dockerfile's own `HEALTHCHECK` and Compose's `depends_on: condition: service_healthy` for the `api` service ultimately depend on.

**The rest of this phase's checklist was mostly already built, and is called out rather than duplicated:** global exception handling and **production exception handling** specifically (Phase 04's `GlobalExceptionMiddleware` already omits stack traces outside `IsDevelopment()`); **logging** (Serilog since Phase 01, now with a genuinely different Production formatter); **CORS configuration for the future React app** (Phase 01, `Cors:AllowedOrigins`, wired through both compose files via `REACT_APP_ORIGIN`). What *is* new here: **HTTPS-ready configuration** for a reverse-proxy-fronted container — `ForwardedHeadersOptions` (`UseForwardedHeaders()`, first in the pipeline, before even the correlation-id middleware) so `X-Forwarded-For`/`X-Forwarded-Proto` from that proxy are trusted for the real client IP and scheme, which `UseHttpsRedirection`, request logging, and the Phase 20 rate limiter's IP-based partitioning all depend on to see the actual caller rather than the proxy's own address.

**What was, and wasn't, verified here — stated plainly:** everything checkable *without* a Docker daemon was — the health check endpoint against real Redis and a real (InMemory-swapped) DbContext, every `appsettings*.json` file parses as valid JSON, the full `dotnet build`/`dotnet test` suite (290 tests) still passes with `ForwardedHeadersOptions` and the health-check registrations in place. The Dockerfile and both compose files themselves could **not** be built or run in this sandbox — no Docker daemon or root access is available here, the same constraint noted since Phase 01 (and why Redis was built from source rather than containerized for local testing). They're written as carefully and conventionally as reasoning alone allows (matching Microsoft's own documented .NET container conventions), but — unlike everything else in this codebase — they carry no "verified end-to-end" claim; treat a first real `docker compose up` as the actual first test of them.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local install, or via Docker Compose — see [Docker & Deployment](#docker--deployment-phase-22))
- **Redis — required as of Phase 11.** The app now throws at startup if `Redis:ConnectionString` is unset. Any Redis 6+ works; no special configuration needed beyond `appsettings.Development.json`'s default of `localhost:6379`.

## Getting started

**Option A — Docker Compose** (runs the API, SQL Server, and Redis together; not verified end-to-end in this sandbox — see [Docker & Deployment](#docker--deployment-phase-22)):

```bash
cp .env.example .env   # then fill in real values — see the comments in that file
docker compose up --build
```

**Option B — run the API directly against your own SQL Server/Redis:**

```bash
# restore & build
dotnet restore BusBooking.sln
dotnet build BusBooking.sln

# set the JWT signing secret (required — the app throws on startup without it)
dotnet user-secrets set "Jwt:Secret" "<a long random string, 32+ bytes>" --project src/BusBooking.API

# apply the database schema (requires a running SQL Server matching appsettings.Development.json)
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/BusBooking.Infrastructure --startup-project src/BusBooking.API

# run the API (uses appsettings.Development.json)
dotnet run --project src/BusBooking.API

# run tests
dotnet test BusBooking.sln
```

Once running, Swagger UI is available at `/swagger` in the Development environment (use the "Authorize" button with `Bearer <access token>` to call protected endpoints). A dependency-free liveness check is exposed at `GET /api/health`; a real database + Redis readiness check is at `GET /health` (Phase 22).

All of the above (`restore`, `build`, `test`, migration generation, and an idempotent SQL script export) have been verified in the environment this scaffold was built in — including the full register/login/refresh/logout and bus-management flows via integration tests against an EF Core InMemory database (`tests/BusBooking.IntegrationTests/Common/CustomWebApplicationFactory`), Swagger/OpenAPI generation, and — since Phase 11 — real Redis-backed atomic seat locking (see that section for how Redis was obtained in this sandbox). The one thing still not verified here is running the app and applying migrations against a **live** SQL Server — no SQL Server or Docker was available in this environment, so `dotnet run` was attempted but fails at the identity role-seeding step trying to reach `localhost,1433`. That failure is expected in this sandbox, not a code issue.

## Configuration

Configuration follows the standard ASP.NET Core layering: `appsettings.json` → `appsettings.{Environment}.json` → environment variables → user secrets.

- `appsettings.Development.json` contains a **local-only** SQL Server connection string intended for a local/dev-container database. It is not a production credential.
- `appsettings.Production.json` (Phase 22) only overrides logging format — `ConnectionStrings:DefaultConnection`, `Cors:AllowedOrigins`, `Jwt:Secret`, and `Redis:ConnectionString` all stay at the base `appsettings.json`'s empty defaults in every committed file, supplied via environment variables at deploy time instead, e.g.:

  ```bash
  ConnectionStrings__DefaultConnection="Server=...;Database=...;User Id=...;Password=...;"
  Cors__AllowedOrigins__0="https://your-frontend-domain"
  ```

- Never commit real passwords, JWT signing keys, or payment credentials. `Jwt:Secret` follows the same pattern — empty in every committed `appsettings*.json`, set via `dotnet user-secrets` (Development) or `Jwt__Secret` (Production). `docker-compose.yml`/`docker-compose.prod.yml` (Phase 22) source all of these from a local, gitignored `.env` — see [Docker & Deployment](#docker--deployment-phase-22) and `.env.example`.

## Roadmap

Development proceeds in small, independently-tested phases (see `Surena bus booking.docx` for full prompts):

**Backend — all 22 phases complete:** 01 Solution Setup ✅ → 02 Domain & Database ✅ → 03 Authentication ✅ → 04 Bus Management ✅ → 05 Seat Layout ✅ → 06 Routes & Stops ✅ → 07 Trip Management ✅ → 08 Customer Management ✅ → 09 Trip Search ✅ → 10 Trip Seats ✅ → 11 Redis Seat Locking ✅ → 12 Booking ✅ → 13 Segment Availability ✅ → 14 Payment ✅ → 15 Ticket & QR ✅ → 16 Passenger Register ✅ → 17 Cancellation ✅ → 18 Notifications ✅ → 19 Reports ✅ → 20 Audit & Security ✅ → 21 Testing ✅ → 22 Docker & Deployment ✅

**Frontend** (separate `bus-booking-web/` React + TypeScript app, not yet started): Project Setup → Design System → API Integration → Authentication → Customer booking flow (search → seats → passengers → review → payment → ticket) → Customer Account → Admin Portal (auth, dashboard, bus/seat/route/driver/trip management, bookings, passenger register, reports, permissions) → Testing → Production Build.

The critical path is **Trip → TripSeat → Redis Lock → Booking → Payment → Ticket** — get that right first.
