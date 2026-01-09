using HotelBooking.Api.Domain.Enums;

namespace HotelBooking.Api.Domain.Entities;

public sealed class Hotel
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    // Navigation
    public ICollection<Room> Rooms { get; private set; } = new List<Room>();

    // EF Core needs a parameterless constructor
    private Hotel() { }

    public Hotel(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Hotel name is required.", nameof(name));

        Name = name.Trim();
    }
}
