namespace Checkbus.Web.Contracts;

/// <summary>
/// Web-side mirror of <c>Checkbus.ApiService.Contracts.CurrentUserResponse</c>. Checkbus.Web
/// does not reference the API projects (pure BFF), so this DTO is duplicated here rather than
/// shared — matching the existing <c>LoginApiResult</c> mirror-DTO convention already used in
/// <c>Checkbus.Web/Program.cs</c>.
/// </summary>
public sealed record CurrentUserResponse(Guid? UserId, Guid? OrganizationId, string? Role, string? Username);
