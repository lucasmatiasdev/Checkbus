using Checkbus.ApiService.Domain.Exceptions.Storage;

namespace Checkbus.Tests.Storage;

public class InvalidStorageKeyExceptionTests
{
    [Fact]
    public void Constructor_NoArguments_UsesDefaultMessage()
    {
        var exception = new InvalidStorageKeyException();

        Assert.Equal("Invalid storage key provided.", exception.Message);
    }

    [Fact]
    public void Constructor_CustomMessage_UsesProvidedMessage()
    {
        var exception = new InvalidStorageKeyException("Custom storage key failure.");

        Assert.Equal("Custom storage key failure.", exception.Message);
    }
}
