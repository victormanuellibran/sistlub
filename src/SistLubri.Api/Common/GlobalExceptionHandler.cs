using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistLubri.Application.Common.Exceptions;
using SistLubri.Domain.Common;

namespace SistLubri.Api.Common;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto de negocio", exception.Message),
            DomainException => (StatusCodes.Status400BadRequest, "Datos invalidos", exception.Message),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Conflicto de concurrencia",
                "Los datos fueron modificados por otro usuario. Volve a consultarlos antes de reintentar."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno",
                "Ocurrio un error inesperado al procesar la solicitud.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation(
                "Request rejected with status {StatusCode}: {ExceptionType} - {Message}",
                status,
                exception.GetType().Name,
                exception.Message);
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            },
            cancellationToken);

        return true;
    }
}
