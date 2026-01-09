using HotelBooking.Api.Domain.Enums;

namespace HotelBooking.Api.Application.Dtos;

public sealed record BookingListItemDto(
    string BookingReference,
    int HotelId,
    string HotelName,
    int RoomNumber,
    RoomType RoomType,
    int Guests,
    string From,
    string To,
    string CreatedUtc
);