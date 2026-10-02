using System.Text.Json;
using Checkbus.ApiService.Domain.Exceptions.DriverRequirements;
using Checkbus.ApiService.ExceptionHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Auth;

public class DriverRequirementExceptionHandlerTests
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
    public async Task TryHandleAsync_AccessDeniedException_Returns403WithProblemDetails()
    {
        var handler = new DriverRequirementExceptionHandler();
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new DriverRequirementAccessDeniedException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Forbidden", problem!.Title);
    }

    [Fact]
    public async Task TryHandleAsync_NotFoundException_Returns404WithProblemDetails()
    {
        var handler = new DriverRequirementExceptionHandler();
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new DriverRequirementNotFoundException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal("Not Found", problem!.Title);
    }

    [Fact]
    public async Task TryHandleAsync_UnrelatedException_ReturnsFalse()
    {
        var handler = new DriverRequirementExceptionHandler();
        var context = CreateContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

        Assert.False(handled);
    }
}
