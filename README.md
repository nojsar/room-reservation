# Room Reservation

A web application for booking meeting rooms: browse rooms, book them, manage reservations and see how rooms are actually used.

Technical task for the Present Connection IDT internship. Work in progress.

## Structure

```
backend/
  src/RoomReservation.Api/     ASP.NET Core Web API (.NET 8), EF Core in-memory database
    Domain/                    Room, User, Reservation, Equipment, ReservationStatus
    Data/                      AppDbContext and DbSeeder (demo data generated around today)
    Options/                   BookingOptions, bound from appsettings.json
  tests/RoomReservation.Tests/ xUnit tests
frontend/                      React + TypeScript + Material UI (coming next)
```

## Running the back end

```
cd backend
dotnet run --project src/RoomReservation.Api
```

Swagger UI: http://localhost:5080/swagger

## Tests

```
cd backend
dotnet test
```
