namespace Checkbus.ApiService.Application.Users
{
    public enum UserEmailGenerationError
    {
        None = 0,
        EmptyLocalPart,
        EmptyDomain,
        DisambiguationExhausted
    }

    public readonly record struct UserEmailGenerationResult(string? Email, UserEmailGenerationError Error)
    {
        public bool Succeeded => Error == UserEmailGenerationError.None && Email is not null;

        public static UserEmailGenerationResult Success(string email) =>
            new(email, UserEmailGenerationError.None);

        public static UserEmailGenerationResult Failure(UserEmailGenerationError error) =>
            new(null, error);
    }
}
