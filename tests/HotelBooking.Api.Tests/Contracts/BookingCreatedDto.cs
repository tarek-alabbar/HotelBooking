namespace HotelBooking.Api.Tests.Contracts;

public sealed record BookingCreatedDto(
    string BookingReference,
    int HotelId,
    int RoomId,
    int RoomNumber,
    int RoomType,
    int Capacity,
    string From,
    string To,
    int Guests);
