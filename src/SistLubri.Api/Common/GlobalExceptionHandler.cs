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
        else if (exception is DbUpdateConcurrencyException concurrencyException)
        {
            var affectedEntries = concurrencyException.Entries.Select(entry =>
            {
                var key = entry.Properties
                    .Where(property => property.Metadata.IsPrimaryKey())
                    .Select(property => $"{property.Metadata.Name}={property.CurrentValue}");
                var rowVersion = entry.Properties.FirstOrDefault(property => property.Metadata.Name == "RowVersion");
                var expectedVersion = rowVersion?.OriginalValue is byte[] bytes
                    ? Convert.ToHexString(bytes)
                    : "n/a";
                return $"{entry.Metadata.ClrType.Name} [{string.Join(", ", key)}], state={entry.State}, expectedRowVersion={expectedVersion}";
            }).ToArray();
            logger.LogWarning(concurrencyException,
                "Concurrency conflict while processing {Path}; affected entries: {AffectedEntries}",
                httpContext.Request.Path,
                string.Join("; ", affectedEntries));
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
