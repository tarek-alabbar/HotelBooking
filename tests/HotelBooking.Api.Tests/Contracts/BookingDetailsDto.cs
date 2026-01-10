namespace HotelBooking.Api.Tests.Contracts;

public sealed record BookingDetailsDto(
    string BookingReference,
    int HotelId,
    string HotelName,
    int RoomId,
    int RoomNumber,
    int RoomType,
    int Capacity,
    string From,
    string To,
    int Guests,
    string CreatedUtc);
