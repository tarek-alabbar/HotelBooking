namespace HotelBooking.Api.Application.Dtos;

public sealed record AvailabilityResultDto(
    int HotelId,
    string From,
    string To,
    int Guests,
    IReadOnlyList<RoomAvailabilityDto> AvailableRooms,
    string Message);