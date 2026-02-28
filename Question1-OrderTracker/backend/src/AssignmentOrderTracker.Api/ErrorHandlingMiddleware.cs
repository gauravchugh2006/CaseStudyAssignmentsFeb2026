using AssignmentOrderTracker.Application;

namespace AssignmentOrderTracker.Api;

public class ErrorHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(new ErrorResponse(ex.Code, ex.Message, context.TraceIdentifier));
        }
        catch (Exception)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new ErrorResponse("INTERNAL_ERROR", "Unexpected server error", context.TraceIdentifier));
        }
    }
}

public record ErrorResponse(string Code, string Message, string CorrelationId);
