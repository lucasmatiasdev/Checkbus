using System.Text.Json;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using Checkbus.ApiService.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Auth;

public class UserConflictExceptionHandlerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    public static TheoryData<Exception> ConflictExceptions => new()
    {
        new DocumentNumberAlreadyRegisteredException(),
        new EmailAlreadyRegisteredException(),
        new EmailGenerationExhaustedException()
    };

    [Theory]
    [MemberData(nameof(ConflictExceptions))]
    public async Task TryHandleAsync_ConflictExceptions_Return409WithProblemDetails(Exception exception)
    {
        var handler = new UserConflictExceptionHandler();
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Conflict", problem!.Title);
    }

    [Fact]
    public async Task TryHandleAsync_UnrelatedException_ReturnsFalse()
    {
        var handler = new UserConflictExceptionHandler();
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.False(handled);
    }
}
