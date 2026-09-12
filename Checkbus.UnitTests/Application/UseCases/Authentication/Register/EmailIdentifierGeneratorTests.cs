using Checkbus.Application.UseCases.Authentication.Register;

namespace Checkbus.UnitTests.Application.UseCases.Authentication.Register
{
    public class EmailIdentifierGeneratorTests
    {
        [Fact]
        public void Slug_NameWithAccents_StripsDiacriticsAndLowercases()
        {
            var result = EmailIdentifierGenerator.Slug("Juan Pérez", ".", 60);

            Assert.Equal("juan.perez", result);
        }

        [Fact]
        public void Slug_MultiWordName_JoinsTokensWithSeparator()
        {
            var result = EmailIdentifierGenerator.Slug("María José García", ".", 60);

            Assert.Equal("maria.jose.garcia", result);
        }

        [Fact]
        public void Slug_PunctuatedOrganizationName_TreatsPunctuationAsTokenBreakAndJoinsWithoutSeparator()
        {
            var result = EmailIdentifierGenerator.Slug("Transportes del Norte S.A.", "", 63);

            Assert.Equal("transportesdelnortesa", result);
        }

        [Fact]
        public void Slug_LocalPartLongerThan60Chars_TruncatesTo60()
        {
            var longName = new string('a', 70);

            var result = EmailIdentifierGenerator.Slug(longName, ".", 60);

            Assert.Equal(60, result.Length);
            Assert.Equal(new string('a', 60), result);
        }

        [Fact]
        public void Slug_DomainLabelLongerThan63Chars_TruncatesTo63()
        {
            var longOrgName = new string('b', 70);

            var result = EmailIdentifierGenerator.Slug(longOrgName, "", 63);

            Assert.Equal(63, result.Length);
            Assert.Equal(new string('b', 63), result);
        }

        [Fact]
        public void Slug_PunctuationOnlyInput_ReturnsEmptyString()
        {
            var result = EmailIdentifierGenerator.Slug("!!!", ".", 60);

            Assert.Equal(string.Empty, result);
        }
    }
}
