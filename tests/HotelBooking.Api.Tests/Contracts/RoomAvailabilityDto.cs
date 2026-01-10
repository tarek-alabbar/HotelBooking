namespace HotelBooking.Api.Tests.Contracts;

public sealed record RoomAvailabilityDto(
    int RoomId,
    int RoomNumber,
    int RoomType,
    int Capacity);