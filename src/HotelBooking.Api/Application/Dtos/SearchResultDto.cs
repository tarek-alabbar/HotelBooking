namespace HotelBooking.Api.Application.Dtos;

public sealed record SearchResultDto<T>(
    IReadOnlyList<T> Items,
    string Message);
