# Room Reservation

A web application for booking meeting rooms in an office. People can browse rooms, book them, manage their reservations, and the office can see which rooms are often booked but rarely used.

Technical task for the Present Connection IDT internship.

## Features

| Requirement | Where |
| --- | --- |
| Rooms list with capacity, floor and location, equipment and status; filters by minimum capacity, equipment and floor | Rooms page, `GET /api/rooms` |
| Users list with the number of active reservations | Users page, `GET /api/users` |
| Reservations list with room, organizer, title, dates, attendees and status; filters by room, organizer, status and date range | Reservations page, `GET /api/reservations` |
| Create, edit, cancel and end a meeting early | Reservations page, `POST`, `PUT`, `POST /cancel`, `POST /end` |
| Booking rules R1 to R7 checked in the back end, with an error code and a readable message shown in the UI | `Booking/Rules`, the booking dialog |
| 7 and 90 day limits configurable | `appsettings.json`, section `Booking` |
| Which rooms are often booked but rarely used | Usage page, `GET /api/reports/room-usage` |

Optional parts done: unit tests, dependency injection, custom mapping, TypeScript, database seeder.

## Running it

Requirements: .NET 8 SDK and Node.js 20 or newer.

Back end (http://localhost:5080, Swagger at http://localhost:5080/swagger):

```
cd backend
dotnet run --project src/RoomReservation.Api
```

Front end (http://localhost:5173), in a second terminal:

```
cd frontend
npm install
npm run dev
```

The front end calls `/api` on its own origin and Vite forwards those calls to the back end, so no CORS setup is needed.

The database is in memory and is filled with demo data on every start: 8 rooms, 8 users and about 300 reservations from two weeks ago to two weeks ahead. "Minija" is set up as a room that is booked a lot but often cancelled or left early, so the usage report has something to show. "Merkys" is inactive (under renovation).

Tests:

```
cd backend
dotnet test
```

## Architecture

### Back end

ASP.NET Core Web API on .NET 8 with an EF Core in-memory database, in one project with folders by responsibility:

```
Controllers/   HTTP only: read the request, call a service, turn the result into a response
Services/      Use cases: rooms, users, reservations, usage report
Booking/       Booking rules R1 to R7 and the validator that runs them
Domain/        Entities: Room, User, Reservation, Equipment, ReservationStatus
Data/          AppDbContext and the demo data seeder
Dtos/          What the API sends and receives
Mapping/       Entity to DTO mapping
Options/       Settings bound from appsettings.json
Common/        Result and Error types
```

For an application of this size one project is enough. If it grew, the next step would be separate projects for the domain, the application logic and the data access.

Key decisions:

- **Booking rules as separate classes (Strategy pattern).** Every rule implements `IBookingRule` and lives in its own class (`MaxDurationRule`, `NoRoomOverlapRule`, ...). `BookingValidator` loads the room, the organizer and the related reservations once, builds a `BookingContext` and runs every registered rule. The rules never touch the database, so each one is a small pure function with its own unit tests. A new rule is a new class and one line in `Program.cs`.
- **All broken rules at once.** The validator returns every broken rule, not only the first, so the user can fix everything in one go.
- **Result pattern instead of exceptions** for expected situations. Services return `Result<T>`: a value or a list of errors with stable codes (`ROOM_ALREADY_BOOKED`, `CAPACITY_EXCEEDED`, ...). The base controller maps them to HTTP: 422 for broken rules, 404 for missing items, and 400 for requests that cannot be read. Every error response has the same shape: `{ "errors": [ { "code": "...", "message": "..." } ] }`.
- **Status is derived, not stored.** `Reservation.GetStatus(now)` returns Cancelled when the booking was cancelled, Completed when its end (or early end) has passed, otherwise Booked. A meeting becomes Completed by itself when time passes, so nothing has to update the database on a schedule.
- **The back end decides what is allowed.** Each reservation comes with `canEdit`, `canCancel` and `canEndEarly`, so the front end shows only the buttons that will work and does no business calculations.
- **Overlaps.** Time ranges are start inclusive and end exclusive, so 9:00 to 10:00 and 10:00 to 11:00 do not clash. Cancelled bookings do not block a room, and a meeting that ended early only blocks the room until it ended.
- **Options pattern** for the limits (`BookingOptions`, `UsageReportOptions`), so they live in `appsettings.json` instead of the code.
- **`TimeProvider` instead of `DateTime.Now`.** The clock is injected, so tests freeze time with `FakeTimeProvider` and give the same result on every run.
- **Hand-written mapping** (`MappingExtensions`) instead of a mapping library: easy to read and checked by the compiler.
- **Equipment as a flags enum.** Each item is one bit, so a room can have several and the equipment filter is a single bit check. The trade-off: a new kind of equipment needs a code change. With a real database and equipment managed by users, a separate table would be better.

How usage is measured: booked time includes bookings that were later cancelled; used time counts cancelled bookings as zero and meetings that ended early only until they ended. Only the past is measured. A room is flagged when it has at least 5 bookings in the period and less than 60% of the booked time was used (both configurable).

### Front end

React 18 with TypeScript, built with Vite, using Material UI and MUI date pickers.

```
src/api/         API client, types that mirror the DTOs, React Query hooks
src/pages/       Rooms, Reservations, Users, Usage
src/components/  Reusable parts: booking dialog, chips, confirmation dialog, error display
```

- **React Query** keeps server data in a cache, handles loading and error states, and reloads the lists that depend on reservations after every change.
- **Filters in the URL** on the reservations page, so a filtered view can be linked: the users page links to a person's bookings, and "Book" on the rooms page opens the booking dialog for that room.
- **Errors from the back end are shown as they are.** The booking dialog lists every message the API returns. The form does not repeat the business rules, so they exist in one place only.
- Times are sent to the API in UTC and shown in the browser's local time.

## API

| Method | Path | Description |
| --- | --- | --- |
| GET | `/api/rooms?minCapacity=&floor=&equipment=` | Rooms; repeat `equipment` to require several items |
| GET | `/api/rooms/{id}` | One room |
| GET | `/api/rooms/floors` | Floors that have rooms |
| GET | `/api/users` | Users with their active reservation count |
| GET | `/api/reservations?roomId=&organizerId=&status=&from=&to=` | Reservations that overlap the date range |
| GET | `/api/reservations/{id}` | One reservation |
| POST | `/api/reservations` | Create (201, or 422 with the broken rules) |
| PUT | `/api/reservations/{id}` | Edit an upcoming reservation |
| POST | `/api/reservations/{id}/cancel` | Cancel a reservation that has not started |
| POST | `/api/reservations/{id}/end` | End a meeting that is in progress |
| GET | `/api/reports/room-usage?from=&to=` | Booked versus used time per room |
| GET | `/api/settings/booking` | The configured booking limits |

## Tests

47 xUnit tests in `backend/tests`:

- every booking rule, including the edge cases from the task: exactly 7 days and exactly 90 days are allowed, back-to-back bookings are allowed, a cancelled booking does not block the room, a meeting that ended early frees the rest of its time, the limits come from configuration;
- the reservation service with the real validator: several broken rules returned together, editing a reservation does not clash with itself, cancel and end-early rules, filters;
- room filters, active reservation counts, the usage report;
- the demo data respects the booking rules.

## Tools used

- .NET 8 SDK, ASP.NET Core Web API, EF Core in-memory provider, Swagger (Swashbuckle)
- xUnit and `Microsoft.Extensions.TimeProvider.Testing` for tests
- React 18, TypeScript, Vite, Material UI, MUI X date pickers, TanStack React Query, React Router, Day.js
- Git and GitHub
- **Claude Code (AI assistant).** I built this project together with Claude Code, the way I work on client projects. It proposed options and wrote code in small steps; I chose the approach, reviewed the code, ran it and tested it. Commits made this way have a "Co-Authored-By: Claude" line.

## What I would do next

- A real database (SQL Server or PostgreSQL) with migrations, and the status filter moved into the database query.
- Protection against two people booking the same slot at the same moment: a transaction or a database constraint.
- Authentication, so the organizer is the signed-in user.
- A calendar view of a room's day or week, and exporting the usage report to PDF.
- Server-side paging for the reservations list.
