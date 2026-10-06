using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Common.Exceptions;

namespace TaskBoard.API.ErrorHandling;

// Application katmanının fırlattığı hataları standart ProblemDetails (RFC 9457) yanıtlarına çevirir.
// Böylece controller'larda try/catch yazmamıza gerek kalmaz.
public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            BadRequestException => (StatusCodes.Status400BadRequest, "Geçersiz istek"),
            NotFoundException =>(StatusCodes.Status404NotFound, "Bulunamadı"),
            ConflictException => (StatusCodes.Status409Conflict, "Çakışma"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Yetkisiz"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Erişim engellendi"),
            _ => (StatusCodes.Status500InternalServerError, "Sunucu hatası")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Beklenmeyen hata");

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Beklenmeyen hatalarda iç detayı istemciye sızdırmıyoruz.
                Detail = exception is AppException ? exception.Message : null
            }
        });
    }
}
