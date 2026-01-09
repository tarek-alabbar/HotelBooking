using HotelBooking.Api.Domain.Enums;

namespace HotelBooking.Api.Application.Dtos;

public sealed record BookingDetailsDto(
    string BookingReference,
    int HotelId,
    string HotelName,
    int RoomId,
    int RoomNumber,
    RoomType RoomType,
    int Capacity,
    string From,
    string To,
    int Guests,
    string CreatedUtc
);