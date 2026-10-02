namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Users.Queries.UserListItemDto</c>, one item of the <c>200</c>
/// response body array for <c>GET /api/Users</c>. Checkbus.Web does not reference the API projects
/// (pure BFF), so this DTO is duplicated here rather than shared — matching the existing
/// <c>RegisterUserResult</c> mirror-DTO convention. Unlike <c>RegisterUserResult.Role</c>, the real
/// DTO's <c>Role</c> is the domain <c>Role</c> enum itself (serialized as a plain number, no
/// <c>JsonStringEnumConverter</c> registered anywhere), so this mirrors it as <see cref="WebRole"/>
/// rather than as a string.
/// </summary>
public sealed record UserListItemResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Surname { get; init; }
    public required string Email { get; init; }
    public required WebRole Role { get; init; }
}
