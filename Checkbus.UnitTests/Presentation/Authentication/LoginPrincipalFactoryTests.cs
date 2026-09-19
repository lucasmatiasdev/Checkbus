using System.Security.Claims;
using Checkbus.Application.UseCases.Authentication.Login;
using Checkbus.Presentation.Authentication;

namespace Checkbus.UnitTests.Presentation.Authentication
{
    public class LoginPrincipalFactoryTests
    {
        [Fact]
        public void Create_SuccessfulLoginResult_MapsAllClaimsCorrectly()
        {
            var result = LoginResult.Succeeded(userId: 42, email: "user@example.com", organizationId: 7, organizationName: "Acme Transit", mustChangePassword: true);

            var principal = LoginPrincipalFactory.Create(result);

            Assert.True(principal.Identity?.IsAuthenticated);
            Assert.Equal("42", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal("user@example.com", principal.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Equal("user@example.com", principal.FindFirst(ClaimTypes.Name)?.Value);
            Assert.Equal("7", principal.FindFirst(CheckbusClaims.OrganizationId)?.Value);
            Assert.Equal("Acme Transit", principal.FindFirst(CheckbusClaims.OrganizationName)?.Value);
            Assert.Equal("True", principal.FindFirst(CheckbusClaims.MustChangePassword)?.Value);
        }

        [Fact]
        public void Create_FailedLoginResult_Throws()
        {
            var result = LoginResult.Failed(LoginFailure.InvalidCredentials);

            Assert.Throws<InvalidOperationException>(() => LoginPrincipalFactory.Create(result));
        }
    }
}
