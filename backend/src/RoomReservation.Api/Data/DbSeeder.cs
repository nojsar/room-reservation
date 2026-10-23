using RoomReservation.Api.Domain;

namespace RoomReservation.Api.Data;

/// <summary>
/// Fills the in-memory database with demo data when the app starts.
/// Reservations are generated around "today", so the data always has past, current and future meetings.
/// A fixed random seed makes the data the same on every start.
/// </summary>
public static class DbSeeder
{
    private const int RandomSeed = 42;
    private const int DaysBack = 14;
    private const int DaysAhead = 14;

    // Meeting slots on a working day: start time and length. The slots never overlap,
    // so a person can organize one meeting per slot and rules R2 and R3 hold for the seed data.
    private static readonly (TimeSpan Start, TimeSpan Length)[] Slots =
    {
        (new TimeSpan(9, 0, 0), TimeSpan.FromMinutes(60)),
        (new TimeSpan(10, 30, 0), TimeSpan.FromMinutes(90)),
        (new TimeSpan(13, 0, 0), TimeSpan.FromMinutes(60)),
        (new TimeSpan(14, 30, 0), TimeSpan.FromMinutes(90)),
    };

    private static readonly string[] Titles =
    {
        "Sprint planning", "Daily stand-up", "Client demo", "Design review", "1:1",
        "Retrospective", "Budget review", "Interview", "Architecture sync", "Onboarding",
        "Product roadmap", "Sales call", "Team lunch planning", "Security training",
    };

    // How each room is used: chance a slot is booked, chance a booking is cancelled,
    // and chance a held meeting ends early. "Minija" is the big room that gets booked a lot
    // but is often cancelled or left early, so the usage report has something to show.
    private sealed record RoomProfile(Room Room, double BookChance, double CancelChance, double EarlyEndChance);

    public static void Seed(AppDbContext db, TimeProvider timeProvider)
    {
        if (db.Rooms.Any())
        {
            return;
        }

        var profiles = new List<RoomProfile>
        {
            new(Room(1, "Neris", 4, 1, "By the reception", Equipment.TvScreen | Equipment.Whiteboard), 0.5, 0.05, 0.10),
            new(Room(2, "Nemunas", 12, 1, "East wing", Equipment.Projector | Equipment.VideoConferencing | Equipment.Whiteboard), 0.6, 0.10, 0.20),
            new(Room(3, "Dubysa", 6, 2, "West wing", Equipment.TvScreen | Equipment.VideoConferencing), 0.45, 0.05, 0.10),
            new(Room(4, "Šventoji", 8, 2, "East wing", Equipment.Projector | Equipment.Whiteboard), 0.4, 0.05, 0.10),
            new(Room(5, "Minija", 20, 3, "Main hall", Equipment.Projector | Equipment.TvScreen | Equipment.VideoConferencing | Equipment.Whiteboard), 0.7, 0.35, 0.45),
            new(Room(6, "Venta", 2, 3, "Phone booth by the kitchen", Equipment.None), 0.3, 0.05, 0.05),
            new(Room(7, "Merkys", 10, 4, "North wing", Equipment.Projector | Equipment.VideoConferencing, isActive: false), 0.4, 0.05, 0.10),
            new(Room(8, "Jūra", 16, 4, "South wing", Equipment.TvScreen | Equipment.VideoConferencing | Equipment.Whiteboard), 0.5, 0.10, 0.15),
        };

        var users = new List<User>
        {
            User(1, "Jonas", "Petraitis"),
            User(2, "Ieva", "Kazlauskienė"),
            User(3, "Lukas", "Jankauskas"),
            User(4, "Gabija", "Stankevičiūtė"),
            User(5, "Mantas", "Vasiliauskas"),
            User(6, "Agnė", "Žukauskaitė"),
            User(7, "Darius", "Butkus"),
            User(8, "Laura", "Paulauskienė"),
        };

        db.Rooms.AddRange(profiles.Select(p => p.Room));
        db.Users.AddRange(users);
        db.Reservations.AddRange(GenerateReservations(profiles, users, timeProvider));
        db.SaveChanges();
    }

    private static List<Reservation> GenerateReservations(List<RoomProfile> profiles, List<User> users, TimeProvider timeProvider)
    {
        var random = new Random(RandomSeed);
        var timeZone = timeProvider.LocalTimeZone;
        var now = timeProvider.GetUtcNow();
        var today = timeProvider.GetLocalNow().Date;
        var reservations = new List<Reservation>();

        for (var dayOffset = -DaysBack; dayOffset <= DaysAhead; dayOffset++)
        {
            var date = today.AddDays(dayOffset);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            foreach (var (slotStart, slotLength) in Slots)
            {
                // Each organizer gets at most one room per slot.
                var freeOrganizers = new Queue<User>(users.OrderBy(_ => random.Next()));

                foreach (var profile in profiles)
                {
                    // The inactive room was used until it closed for renovation a week ago.
                    if (!profile.Room.IsActive && dayOffset > -7)
                    {
                        continue;
                    }

                    if (random.NextDouble() > profile.BookChance || freeOrganizers.Count == 0)
                    {
                        continue;
                    }

                    var localStart = date + slotStart;
                    var start = new DateTimeOffset(localStart, timeZone.GetUtcOffset(localStart));
                    var reservation = new Reservation
                    {
                        RoomId = profile.Room.Id,
                        OrganizerId = freeOrganizers.Dequeue().Id,
                        Title = Titles[random.Next(Titles.Length)],
                        Start = start,
                        End = start + slotLength,
                        Attendees = random.Next(1, profile.Room.Capacity + 1),
                    };

                    if (random.NextDouble() < profile.CancelChance)
                    {
                        // Cancelled two hours before the start, or a moment ago for future meetings.
                        var cancelledAt = start.AddHours(-2);
                        reservation.CancelledAt = cancelledAt < now ? cancelledAt : now.AddMinutes(-30);
                    }
                    else if (random.NextDouble() < profile.EarlyEndChance)
                    {
                        // Ended after somewhere between a quarter and three quarters of the planned time.
                        var endedAt = start + slotLength * (0.25 + random.NextDouble() * 0.5);
                        if (endedAt <= now)
                        {
                            reservation.EndedEarlyAt = RoundToMinutes(endedAt, 5);
                        }
                    }

                    reservations.Add(reservation);
                }
            }
        }

        return reservations;
    }

    private static Room Room(int id, string name, int capacity, int floor, string location, Equipment equipment, bool isActive = true) =>
        new() { Id = id, Name = name, Capacity = capacity, Floor = floor, Location = location, Equipment = equipment, IsActive = isActive };

    private static User User(int id, string firstName, string lastName) =>
        new() { Id = id, FirstName = firstName, LastName = lastName };

    private static DateTimeOffset RoundToMinutes(DateTimeOffset value, int minutes)
    {
        var ticks = TimeSpan.FromMinutes(minutes).Ticks;
        return new DateTimeOffset(value.Ticks / ticks * ticks, value.Offset);
    }
}
