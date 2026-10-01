using System.Globalization;
using Checkbus.ApiService.Application.Auth.Commands;
using Checkbus.ApiService.Application.Users;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.Users;

public class UserEmailGeneratorTests
{
    private const string Slug = "checkbus-demo";
    private static readonly IReadOnlySet<string> NoTakenEmails = new HashSet<string>();

    [Theory]
    [InlineData("José", "Díaz", "jose.diaz")]
    [InlineData("Ñuñez", "Müller", "nunez.muller")]
    [InlineData("Van", "Der Berg", "van.derberg")]
    [InlineData("O'Brien", "Díaz-Pérez", "obrien.diazperez")]
    public void Generate_NormalizesNameAndSurname_AndSatisfiesLoginEmailRules(
        string name, string surname, string expectedLocalPart)
    {
        var result = UserEmailGenerator.Generate(name, surname, Slug, NoTakenEmails);

        Assert.True(result.Succeeded);
        Assert.Equal($"{expectedLocalPart}@{Slug}.com", result.Email);
        AssertSatisfiesLoginEmailRules(result.Email!);
    }

    [Fact]
    public void Generate_LowercasesUsingInvariantCulture_NotAmbientCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            var result = UserEmailGenerator.Generate("Isil", "Yildiz", Slug, NoTakenEmails);

            Assert.True(result.Succeeded);
            Assert.Equal($"isil.yildiz@{Slug}.com", result.Email);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("checkbus-demo", "checkbus-demo")]
    [InlineData("Checkbus---Demo", "checkbus-demo")]
    [InlineData("-Checkbus-", "checkbus")]
    [InlineData("Ñandú", "nandu")]
    public void SanitizeSlug_PreservesHyphensAndCollapsesRuns(string input, string expected)
    {
        Assert.Equal(expected, UserEmailGenerator.SanitizeSlug(input));
    }

    [Theory]
    [InlineData("李", "Diaz")]
    [InlineData("Jose", "李")]
    public void Generate_NonLatinScriptNameOrSurname_ReturnsEmptyLocalPart(string name, string surname)
    {
        var result = UserEmailGenerator.Generate(name, surname, Slug, NoTakenEmails);

        Assert.False(result.Succeeded);
        Assert.Equal(UserEmailGenerationError.EmptyLocalPart, result.Error);
        Assert.Null(result.Email);
    }

    [Fact]
    public void Generate_SameNormalizedName_DifferentOrganizationSlugs_ProducesDifferentDomains()
    {
        // Regression proof for the spec's `user-identity` -> Organization Slug Uniqueness
        // scenario "Unique slugs prevent cross-tenant email collisions" (Phase 5, task 5.3
        // gap closure): two organizations with distinct slugs registering identically
        // normalizing names must never collide, because the domain is derived from the
        // (uniquely-indexed) slug.
        var resultA = UserEmailGenerator.Generate("Jose", "Diaz", "org-a", NoTakenEmails);
        var resultB = UserEmailGenerator.Generate("Jose", "Diaz", "org-b", NoTakenEmails);

        Assert.True(resultA.Succeeded);
        Assert.True(resultB.Succeeded);
        Assert.Equal("jose.diaz@org-a.com", resultA.Email);
        Assert.Equal("jose.diaz@org-b.com", resultB.Email);
        Assert.NotEqual(resultA.Email, resultB.Email);
    }

    [Fact]
    public void Generate_SlugNormalizesToEmpty_ReturnsEmptyDomain()
    {
        var result = UserEmailGenerator.Generate("Jose", "Diaz", "---", NoTakenEmails);

        Assert.False(result.Succeeded);
        Assert.Equal(UserEmailGenerationError.EmptyDomain, result.Error);
        Assert.Null(result.Email);
    }

    [Fact]
    public void Generate_MaxLengthNameAndSurname_TruncatesBaseAndStaysValidForLogin()
    {
        var longName = new string('a', 100);
        var longSurname = new string('b', 100);

        var result = UserEmailGenerator.Generate(longName, longSurname, Slug, NoTakenEmails);

        Assert.True(result.Succeeded);
        var localPart = result.Email!.Split('@')[0];
        Assert.Equal(UserEmailGenerator.MaxBaseLocalPartLength, localPart.Length);
        AssertSatisfiesLoginEmailRules(result.Email);
    }

    [Theory]
    [InlineData(new string[] { }, "jose.diaz")]
    [InlineData(new[] { "jose.diaz@checkbus-demo.com" }, "jose.diaz1")]
    [InlineData(new[] { "jose.diaz@checkbus-demo.com", "jose.diaz1@checkbus-demo.com" }, "jose.diaz2")]
    [InlineData(new[] { "jose.diaz@checkbus-demo.com", "jose.diaz2@checkbus-demo.com" }, "jose.diaz1")]
    public void Generate_CollisionSequence_PicksLowestFreeSuffix(string[] takenEmails, string expectedLocalPart)
    {
        var taken = new HashSet<string>(takenEmails, StringComparer.OrdinalIgnoreCase);

        var result = UserEmailGenerator.Generate("Jose", "Diaz", Slug, taken);

        Assert.True(result.Succeeded);
        Assert.Equal($"{expectedLocalPart}@{Slug}.com", result.Email);
    }

    [Fact]
    public void Generate_OverFetchedSimilarAddress_DoesNotConsumeExactMatch()
    {
        // The caller's prefix projection query may over-fetch "jose.diazxyz@..." alongside
        // "jose.diaz@...". Generate must check EXACT membership, not prefix membership.
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "jose.diazxyz@checkbus-demo.com"
        };

        var result = UserEmailGenerator.Generate("Jose", "Diaz", Slug, taken);

        Assert.True(result.Succeeded);
        Assert.Equal($"jose.diaz@{Slug}.com", result.Email);
    }

    [Fact]
    public void Generate_AllAttemptsExhausted_ReturnsDisambiguationExhausted()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { $"jose.diaz@{Slug}.com" };
        for (var suffix = 1; suffix <= 99; suffix++)
        {
            taken.Add($"jose.diaz{suffix}@{Slug}.com");
        }

        var result = UserEmailGenerator.Generate("Jose", "Diaz", Slug, taken);

        Assert.False(result.Succeeded);
        Assert.Equal(UserEmailGenerationError.DisambiguationExhausted, result.Error);
        Assert.Null(result.Email);
    }

    private static void AssertSatisfiesLoginEmailRules(string email)
    {
        var loginCommand = new LoginCommand { Email = email, Password = "password123" };
        var validationResult = new LoginCommandValidator().TestValidate(loginCommand);

        validationResult.ShouldNotHaveValidationErrorFor(c => c.Email);
    }
}
