using Checkbus.Infrastructure.Security;

namespace Checkbus.UnitTests.Infrastructure.Security
{
    public class IdentityPasswordHasherTests
    {
        private readonly IdentityPasswordHasher _sut = new();

        [Fact]
        public void Hash_ThenVerify_RoundTripsTrue()
        {
            var hash = _sut.Hash("correct-password");

            var result = _sut.Verify(hash, "correct-password");

            Assert.True(result);
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            var hash = _sut.Hash("correct-password");

            var result = _sut.Verify(hash, "wrong-password");

            Assert.False(result);
        }
    }
}
