using Microsoft.EntityFrameworkCore;
using RoomReservation.Api.Domain;

namespace RoomReservation.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(room =>
        {
            room.Property(r => r.Name).HasMaxLength(100).IsRequired();
            room.Property(r => r.Location).HasMaxLength(200);
        });

        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            user.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Reservation>(reservation =>
        {
            reservation.Property(r => r.Title).HasMaxLength(200).IsRequired();

            reservation.HasOne(r => r.Room)
                .WithMany(r => r.Reservations)
                .HasForeignKey(r => r.RoomId);

            reservation.HasOne(r => r.Organizer)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.OrganizerId);
        });
    }
}
