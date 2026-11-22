using egeszsegkalauz.services.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace egeszsegkalauz.api.Extensions;

public class ServiceExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService;

    public ServiceExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        this.problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int? status = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            _ => null,
        };
        if (status is null)
            return false;

        httpContext.Response.StatusCode = status.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Detail = exception.Message },
        });
    }
}
