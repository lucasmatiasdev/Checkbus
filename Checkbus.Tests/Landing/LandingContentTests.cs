using Checkbus.Web.Components.Landing;

namespace Checkbus.Tests.Landing;

public class LandingContentTests
{
    [Fact]
    public void Plans_HasExactlyThreePlans()
    {
        Assert.Equal(3, LandingContent.Plans.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Plans_EachPlanHasNonEmptyNamePriceAndFeatures(int index)
    {
        var plan = LandingContent.Plans[index];

        Assert.False(string.IsNullOrWhiteSpace(plan.Name));
        Assert.False(string.IsNullOrWhiteSpace(plan.Price));
        Assert.NotEmpty(plan.Features);
        Assert.All(plan.Features, feature => Assert.False(string.IsNullOrWhiteSpace(feature)));
    }

    [Fact]
    public void Solutions_IsNotEmpty()
    {
        Assert.NotEmpty(LandingContent.Solutions);
    }

    [Fact]
    public void Solutions_NoItemHasEmptyTitleOrDescription()
    {
        Assert.All(LandingContent.Solutions, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Title));
            Assert.False(string.IsNullOrWhiteSpace(item.Description));
        });
    }

    [Fact]
    public void Testimonials_IsNotEmpty()
    {
        Assert.NotEmpty(LandingContent.Testimonials);
    }

    [Fact]
    public void Testimonials_NoItemHasEmptyQuoteOrAuthor()
    {
        Assert.All(LandingContent.Testimonials, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Quote));
            Assert.False(string.IsNullOrWhiteSpace(item.Author));
        });
    }

    [Fact]
    public void ContactEmail_IsNotEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(LandingContent.ContactEmail));
    }
}
