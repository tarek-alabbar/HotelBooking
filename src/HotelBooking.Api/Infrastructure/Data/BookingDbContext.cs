using HotelBooking.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HotelBooking.Api.Infrastructure.Data;

public sealed class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite doesn't natively support DateOnly as a dedicated type.
        // We store it as TEXT "yyyy-MM-dd" (stable, readable, index-friendly).
        var dateOnlyConverter = new ValueConverter<DateOnly, string>(
            d => d.ToString("yyyy-MM-dd"),
            s => DateOnly.Parse(s));

        // -----------------------
        // Hotel
        // -----------------------
        modelBuilder.Entity<Hotel>(entity =>
        {
            entity.ToTable("Hotels");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(x => x.Name);

            entity.HasMany(x => x.Rooms)
                .WithOne(x => x.Hotel)
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -----------------------
        // Room
        // -----------------------
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("Rooms");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RoomNumber)
                .IsRequired();

            entity.Property(x => x.RoomType)
                .IsRequired();

            entity.Property(x => x.Capacity)
                .IsRequired();

            // A hotel has exactly 6 rooms by business rule (we'll enforce via seeding/tests),
            // but at least enforce uniqueness of room number per hotel at DB level.
            entity.HasIndex(x => new { x.HotelId, x.RoomNumber })
                .IsUnique();

            entity.HasMany(x => x.Bookings)
                .WithOne(x => x.Room)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // -----------------------
        // Booking
        // -----------------------
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.BookingReference)
                .IsRequired()
                .HasMaxLength(32);

            // Enforce unique booking reference at DB level
            entity.HasIndex(x => x.BookingReference)
                .IsUnique();

            entity.Property(x => x.StartDate)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("TEXT")
                .IsRequired();

            entity.Property(x => x.EndDate)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("TEXT")
                .IsRequired();

            entity.Property(x => x.GuestCount)
                .IsRequired();

            entity.Property(x => x.CreatedUtc)
                .IsRequired();

            // Helpful query index for availability + conflict checks
            entity.HasIndex(x => new { x.RoomId, x.StartDate, x.EndDate });

            entity.HasOne(x => x.Hotel)
                .WithMany() // we don't need Hotel.Bookings navigation
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
