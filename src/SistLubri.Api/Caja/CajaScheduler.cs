using SistLubri.Application.Caja;

namespace SistLubri.Api.Caja;

public sealed class CajaScheduler(IServiceScopeFactory scopeFactory, ILogger<CajaScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<CajaService>();
                var now = DateTimeOffset.UtcNow;
                await service.EjecutarAperturaAutomaticaAsync(now, stoppingToken);
                await service.EjecutarCierreAutomaticoAsync(now, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Fallo la ejecucion automatica de caja.");
            }
        }
    }
}
