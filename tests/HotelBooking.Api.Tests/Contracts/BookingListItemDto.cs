namespace HotelBooking.Api.Tests.Contracts;

public sealed record BookingListItemDto(
    string BookingReference,
    int HotelId,
    string HotelName,
    int RoomNumber,
    int RoomType,
    int Guests,
    string From,
    string To,
    string CreatedUtc);