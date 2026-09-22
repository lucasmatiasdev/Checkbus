using Checkbus.ApiService.Domain.Exceptions.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.ApiService.ExceptionHandling
{
    public sealed class AuthExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            switch (exception)
            {
                case ValidationException validationException:
                    await WriteValidationProblemAsync(httpContext, validationException, cancellationToken);
                    return true;

                case UserNotFoundException:
                case InvalidCredentialsException:
                case UserInactiveException:
                    await WriteAuthenticationFailedAsync(httpContext, cancellationToken);
                    return true;

                default:
                    return false;
            }
        }

        private static Task WriteValidationProblemAsync(
            HttpContext httpContext,
            ValidationException exception,
            CancellationToken cancellationToken)
        {
            var errors = exception.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed"
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        }

        private static Task WriteAuthenticationFailedAsync(HttpContext httpContext, CancellationToken cancellationToken)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication failed",
                Detail = "Invalid email or password."
            };

            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
        }
    }
}
