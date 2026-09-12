namespace Checkbus.Application.UseCases.Authentication.Register
{
    public sealed record RegisterResult
    {
        public bool Success { get; init; }
        public RegisterFailure? Failure { get; init; }
        public int UserId { get; init; }
        public string? GeneratedEmail { get; init; }
        public string? TemporaryPassword { get; init; }

        public static RegisterResult Succeeded(int userId, string generatedEmail, string temporaryPassword) =>
            new()
            {
                Success = true,
                UserId = userId,
                GeneratedEmail = generatedEmail,
                TemporaryPassword = temporaryPassword
            };

        public static RegisterResult Failed(RegisterFailure reason) =>
            new()
            {
                Success = false,
                Failure = reason
            };
    }
}
