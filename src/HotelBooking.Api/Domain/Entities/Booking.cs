namespace HotelBooking.Api.Domain.Entities;

public sealed class Booking
{
    public int Id { get; private set; }
    public string BookingReference { get; private set; } = string.Empty;
    public int HotelId { get; private set; }
    public Hotel Hotel { get; private set; } = default!;
    public int RoomId { get; private set; }
    public Room Room { get; private set; } = default!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public int GuestCount { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public ICollection<BookingNight> Nights { get; private set; } = new List<BookingNight>();

    /// <summary>
    /// EF Core constructor.
    /// </summary>
    private Booking() { }

    /// <summary>
    /// Creates a new booking for a single room across an inclusive date range.
    /// </summary>
    /// <param name="bookingReference">Unique booking reference for lookup.</param>
    /// <param name="hotelId">Hotel identifier.</param>
    /// <param name="roomId">Room identifier.</param>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="guestCount">Number of guests for the booking.</param>
    /// <param name="createdUtc">Creation time in UTC.</param>
    public Booking(
        string bookingReference,
        int hotelId,
        int roomId,
        DateOnly startDate,
        DateOnly endDate,
        int guestCount,
        DateTime createdUtc)
    {
        if (string.IsNullOrWhiteSpace(bookingReference))
            throw new ArgumentException("Booking reference is required.", nameof(bookingReference));

        if (hotelId <= 0) throw new ArgumentOutOfRangeException(nameof(hotelId));
        if (roomId <= 0) throw new ArgumentOutOfRangeException(nameof(roomId));
        if (guestCount <= 0) throw new ArgumentOutOfRangeException(nameof(guestCount));
        if (endDate < startDate) throw new ArgumentException("EndDate must be on or after StartDate.");

        BookingReference = bookingReference.Trim();
        HotelId = hotelId;
        RoomId = roomId;
        StartDate = startDate;
        EndDate = endDate;
        GuestCount = guestCount;
        CreatedUtc = createdUtc;
    }
}
