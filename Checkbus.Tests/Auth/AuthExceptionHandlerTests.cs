using System.Text.Json;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using Checkbus.ApiService.ExceptionHandling;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Auth;

public class AuthExceptionHandlerTests
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

    [Fact]
    public async Task TryHandleAsync_ValidationException_Returns400WithFieldErrors()
    {
        var handler = new AuthExceptionHandler();
        var context = CreateContext();
        var exception = new ValidationException([
            new ValidationFailure("Email", "Email is required.")
        ]);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        var problem = JsonSerializer.Deserialize<ValidationProblemDetails>(body, JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Validation failed", problem!.Title);
        Assert.Contains("Email", problem.Errors.Keys);
    }

    public static TheoryData<Exception> AuthExceptions => new()
    {
        new UserNotFoundException(),
        new InvalidCredentialsException(),
        new UserInactiveException()
    };

    [Theory]
    [MemberData(nameof(AuthExceptions))]
    public async Task TryHandleAsync_AuthExceptions_Return401WithGenericBody(Exception exception)
    {
        var handler = new AuthExceptionHandler();
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Authentication failed", problem!.Title);
        Assert.Equal("Invalid email or password.", problem.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_ThreeAuthExceptions_ProduceByteIdenticalBodies()
    {
        var handler = new AuthExceptionHandler();

        var context1 = CreateContext();
        await handler.TryHandleAsync(context1, new UserNotFoundException(), CancellationToken.None);
        var body1 = await ReadBodyAsync(context1);

        var context2 = CreateContext();
        await handler.TryHandleAsync(context2, new InvalidCredentialsException(), CancellationToken.None);
        var body2 = await ReadBodyAsync(context2);

        var context3 = CreateContext();
        await handler.TryHandleAsync(context3, new UserInactiveException(), CancellationToken.None);
        var body3 = await ReadBodyAsync(context3);

        Assert.Equal(context1.Response.StatusCode, context2.Response.StatusCode);
        Assert.Equal(context2.Response.StatusCode, context3.Response.StatusCode);
        Assert.Equal(body1, body2);
        Assert.Equal(body2, body3);
    }

    [Fact]
    public async Task TryHandleAsync_UnrelatedException_ReturnsFalse()
    {
        var handler = new AuthExceptionHandler();
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.False(handled);
    }
}
