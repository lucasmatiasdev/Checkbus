namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Application.Users.Commands.RegisterUserCommand</c>,
/// the request body for <c>POST /api/Users</c>. Checkbus.Web does not reference the API projects
/// (pure BFF), so this DTO is duplicated here rather than shared — matching the existing
/// <c>CurrentUserResponse</c> mirror-DTO convention. <see cref="WebDocumentType"/> and
/// <see cref="WebRole"/> are serialized as plain integers over the wire, since no
/// <c>JsonStringEnumConverter</c> is registered in this solution.
/// </summary>
public sealed record RegisterUserRequest
{
    public required string Name { get; init; }
    public required string Surname { get; init; }
    public WebDocumentType DocumentType { get; init; } = WebDocumentType.DNI;
    public required string DocumentNumber { get; init; }
    public required WebRole Role { get; init; }
}
