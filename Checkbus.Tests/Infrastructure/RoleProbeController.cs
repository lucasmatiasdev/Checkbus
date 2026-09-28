using Checkbus.ApiService.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Infrastructure;

/// <summary>
/// Test-only controller used to prove role-based authorization end-to-end without
/// depending on any production endpoint. Deliberately follows the same shape every
/// future role-restricted production endpoint must follow: the global
/// <c>AuthorizeFilter</c> only guarantees "authenticated" — this action still
/// carries its own explicit <see cref="AuthorizeAttribute"/> role restriction.
/// </summary>
[ApiController]
[Route("test/role-probe")]
public sealed class RoleProbeController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = nameof(Role.Administrador))]
    public IActionResult Get() => Ok();
}
