using HotelBooking.Api.Domain.Enums;

namespace HotelBooking.Api.Domain.Entities;

public sealed class Room
{
    public int Id { get; private set; }

    public int HotelId { get; private set; }
    public Hotel Hotel { get; private set; } = default!;

    public int RoomNumber { get; private set; } // 1..6 per hotel

    public RoomType RoomType { get; private set; }

    public int Capacity { get; private set; }

    // Navigation
    public ICollection<Booking> Bookings { get; private set; } = new List<Booking>();

    /// <summary>
    /// EF Core constructor.
    /// </summary>
    private Room() { }

    /// <summary>
    /// Creates a new room for a specific hotel, validating business constraints.
    /// </summary>
    /// <param name="hotelId">The owning hotel identifier.</param>
    /// <param name="roomNumber">The room number (1..6 per hotel).</param>
    /// <param name="roomType">The room type (Single/Double/Deluxe).</param>
    /// <param name="capacity">Maximum number of guests allowed in the room.</param>
    public Room(int hotelId, int roomNumber, RoomType roomType, int capacity)
    {
        if (hotelId <= 0) throw new ArgumentOutOfRangeException(nameof(hotelId));
        if (roomNumber is < 1 or > 6) throw new ArgumentOutOfRangeException(nameof(roomNumber), "RoomNumber must be between 1 and 6.");
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

        HotelId = hotelId;
        RoomNumber = roomNumber;
        RoomType = roomType;
        Capacity = capacity;
    }
}
