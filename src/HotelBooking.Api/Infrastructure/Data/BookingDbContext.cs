using HotelBooking.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HotelBooking.Api.Infrastructure.Data;

public sealed class BookingDbContext : DbContext
{
    /// <summary>
    /// Creates a new DbContext instance used for hotel/room/booking persistence.
    /// </summary>
    /// <param name="options">DbContext options configured by dependency injection.</param>
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingNight> BookingNights => Set<BookingNight>();

    /// <summary>
    /// Configures the EF Core model: tables, relationships, indexes, and conversions.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure entities.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite doesn't have a dedicated DateOnly type; store as "yyyy-MM-dd" TEXT.
        var dateOnlyConverter = new ValueConverter<DateOnly, string>(
            d => d.ToString("yyyy-MM-dd"),
            s => DateOnly.Parse(s));

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

            entity.HasIndex(x => new { x.HotelId, x.RoomNumber })
                .IsUnique();

            entity.HasMany(x => x.Bookings)
                .WithOne(x => x.Room)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.BookingReference)
                .IsRequired()
                .HasMaxLength(32);

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

            entity.HasIndex(x => new { x.RoomId, x.StartDate, x.EndDate });

            entity.HasOne(x => x.Hotel)
                .WithMany()
                .HasForeignKey(x => x.HotelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BookingNight>(entity =>
        {
            entity.ToTable("BookingNights");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.NightDate)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("TEXT")
                .IsRequired();

            // Core invariant: a room cannot be double-booked for the same night.
            entity.HasIndex(x => new { x.RoomId, x.NightDate })
                .IsUnique();

            entity.HasOne(x => x.Booking)
                .WithMany(x => x.Nights)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Room)
                .WithMany()
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    }
}
