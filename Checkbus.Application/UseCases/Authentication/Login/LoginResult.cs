namespace Checkbus.Application.UseCases.Authentication.Login
{
    public sealed record LoginResult
    {
        public bool Success { get; init; }
        public LoginFailure? Failure { get; init; }
        public int UserId { get; init; }
        public string? Email { get; init; }
        public int OrganizationId { get; init; }
        public string? OrganizationName { get; init; }
        public bool MustChangePassword { get; init; }

        public static LoginResult Succeeded(int userId, string email, int organizationId, string organizationName, bool mustChangePassword) =>
            new()
            {
                Success = true,
                UserId = userId,
                Email = email,
                OrganizationId = organizationId,
                OrganizationName = organizationName,
                MustChangePassword = mustChangePassword
            };

        public static LoginResult Failed(LoginFailure reason) =>
            new()
            {
                Success = false,
                Failure = reason
            };
    }
}
