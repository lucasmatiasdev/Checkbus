using System.Text.RegularExpressions;
using Checkbus.Infrastructure.Security;

namespace Checkbus.UnitTests.Infrastructure.Security
{
    public class CryptoPasswordGeneratorTests
    {
        private readonly CryptoPasswordGenerator _sut = new();

        [Fact]
        public void Generate_ReturnsSixteenCharacters()
        {
            var password = _sut.Generate();

            Assert.Equal(16, password.Length);
        }

        [Fact]
        public void Generate_ReturnsAlphanumericOnly()
        {
            var password = _sut.Generate();

            Assert.Matches(new Regex("^[A-Za-z0-9]+$"), password);
        }

        [Fact]
        public void Generate_TwoCalls_ReturnDifferentValues()
        {
            var first = _sut.Generate();
            var second = _sut.Generate();

            Assert.NotEqual(first, second);
        }
    }
}
