namespace HotelBooking.Api.Tests.Contracts;

public sealed record SearchResultDto<T>(
    IReadOnlyList<T> Items,
    string Message);
