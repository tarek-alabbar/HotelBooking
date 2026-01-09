using HotelBooking.Api.Domain.Enums;

namespace HotelBooking.Api.Application.Dtos;

public sealed record BookingCreatedDto(
    string BookingReference,
    int HotelId,
    int RoomId,
    int RoomNumber,
    RoomType RoomType,
    int Capacity,
    string From,
    string To,
    int Guests
);
