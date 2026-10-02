using Checkbus.Web.Contracts;
using Checkbus.Web.Models;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="UserListFilter.Apply"/> covers every combination of role and search-text
/// filtering used by the Usuarios list page. This is the only logic-bearing piece of that page
/// pushed into plain testable code (this repo has no bUnit to render the Razor component itself).
/// </summary>
public class UserListFilterTests
{
    private static readonly UserListItemResponse John = new()
    {
        Id = Guid.NewGuid(),
        Name = "John",
        Surname = "Doe",
        Email = "jdoe@checkbus.local",
        Role = WebRole.Chofer
    };

    private static readonly UserListItemResponse Jane = new()
    {
        Id = Guid.NewGuid(),
        Name = "Jane",
        Surname = "Smith",
        Email = "jsmith@checkbus.local",
        Role = WebRole.Administrador
    };

    private static readonly UserListItemResponse Mark = new()
    {
        Id = Guid.NewGuid(),
        Name = "Mark",
        Surname = "Johnson",
        Email = "mjohnson@checkbus.local",
        Role = WebRole.Planificador
    };

    private static readonly IReadOnlyList<UserListItemResponse> AllUsers = [John, Jane, Mark];

    [Fact]
    public void Apply_WhenRoleOnly_ReturnsOnlyMatchingRole()
    {
        var result = UserListFilter.Apply(AllUsers, WebRole.Chofer, null);

        Assert.Equal([John], result);
    }

    [Fact]
    public void Apply_WhenSearchMatchesName_ReturnsMatchingUser()
    {
        var result = UserListFilter.Apply(AllUsers, null, "Jane");

        Assert.Equal([Jane], result);
    }

    [Fact]
    public void Apply_WhenSearchMatchesSurname_ReturnsMatchingUser()
    {
        var result = UserListFilter.Apply(AllUsers, null, "Johnson");

        Assert.Equal([Mark], result);
    }

    [Theory]
    [InlineData("jane")]
    [InlineData("JANE")]
    [InlineData("  Jane  ")]
    public void Apply_SearchIsCaseInsensitiveAndTrimmed(string searchText)
    {
        var result = UserListFilter.Apply(AllUsers, null, searchText);

        Assert.Equal([Jane], result);
    }

    [Fact]
    public void Apply_WhenRoleAndSearchBothSet_CombinesWithAnd()
    {
        var result = UserListFilter.Apply(AllUsers, WebRole.Chofer, "John");

        Assert.Equal([John], result);
    }

    [Fact]
    public void Apply_WhenRoleAndSearchBothSetButNoOverlap_ReturnsEmpty()
    {
        var result = UserListFilter.Apply(AllUsers, WebRole.Administrador, "John");

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Apply_WhenRoleIsNullAndSearchIsEmptyOrWhitespace_ReturnsEverythingUnchanged(string? searchText)
    {
        var result = UserListFilter.Apply(AllUsers, null, searchText);

        Assert.Equal(AllUsers, result);
    }

    [Fact]
    public void Apply_WhenNoMatches_ReturnsEmptyList()
    {
        var result = UserListFilter.Apply(AllUsers, null, "Nonexistent");

        Assert.Empty(result);
    }
}
