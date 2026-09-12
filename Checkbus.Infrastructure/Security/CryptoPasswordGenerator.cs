using System.Security.Cryptography;
using Checkbus.Application.Abstractions;

namespace Checkbus.Infrastructure.Security
{
    public class CryptoPasswordGenerator : IPasswordGenerator
    {
        private const int PasswordLength = 16;
        private const string Alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public string Generate() =>
            new string(RandomNumberGenerator.GetItems<char>(Alphabet, PasswordLength));
    }
}
