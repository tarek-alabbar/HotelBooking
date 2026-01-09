using HotelBooking.Api.Domain.Enums;

namespace HotelBooking.Api.Application.Dtos;

public sealed record CreateBookingRequest(
    int HotelId,
    DateOnly From,
    DateOnly To,
    int Guests,
    RoomType? RoomType // optional filter; omit for best-fit allocation
);
