namespace Checkbus.Application.UseCases.Authentication
{
    public sealed record LoginResult
    {
        public bool Success { get; init; }
        public LoginFailure? Failure { get; init; }
        public int UserId { get; init; }
        public string? Email { get; init; }
        public int OrganizationId { get; init; }

        public static LoginResult Succeeded(int userId, string email, int organizationId) =>
            new()
            {
                Success = true,
                UserId = userId,
                Email = email,
                OrganizationId = organizationId
            };

        public static LoginResult Failed(LoginFailure reason) =>
            new()
            {
                Success = false,
                Failure = reason
            };
    }
}
