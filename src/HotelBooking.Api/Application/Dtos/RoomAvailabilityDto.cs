using HotelBooking.Api.Domain.Enums;

namespace HotelBooking.Api.Application.Dtos;

public sealed record RoomAvailabilityDto(
    int RoomId,
    int RoomNumber,
    RoomType RoomType,
    int Capacity);
