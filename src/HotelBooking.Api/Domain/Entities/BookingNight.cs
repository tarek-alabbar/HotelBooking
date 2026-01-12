namespace HotelBooking.Api.Domain.Entities;

public sealed class BookingNight
{
    public long Id { get; private set; }
    public int BookingId { get; private set; }
    public Booking Booking { get; private set; } = default!;
    public int RoomId { get; private set; }
    public Room Room { get; private set; } = default!;
    public DateOnly NightDate { get; private set; }

    /// <summary>
    /// EF Core constructor.
    /// </summary>
    private BookingNight() { }

    /// <summary>
    /// Creates a new occupied-night record for a booking.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    /// <param name="roomId">The room identifier.</param>
    /// <param name="nightDate">The occupied night date.</param>
    public BookingNight(int bookingId, int roomId, DateOnly nightDate)
    {
        if (bookingId <= 0) throw new ArgumentOutOfRangeException(nameof(bookingId));
        if (roomId <= 0) throw new ArgumentOutOfRangeException(nameof(roomId));
        BookingId = bookingId;
        RoomId = roomId;
        NightDate = nightDate;
    }
}
