using HotelBooking.Api.Domain.Enums;

namespace HotelBooking.Api.Domain.Entities;

public sealed class Hotel
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ICollection<Room> Rooms { get; private set; } = new List<Room>();

    /// <summary>
    /// EF Core constructor.
    /// </summary>
    private Hotel() { }

    /// <summary>
    /// Creates a new hotel with a validated name.
    /// </summary>
    /// <param name="name">The hotel name.</param>
    public Hotel(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Hotel name is required.", nameof(name));

        Name = name.Trim();
    }
}
