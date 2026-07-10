using ClubeBeneficios.Notifications.Worker.Configuration;
using ClubeBeneficios.Notifications.Worker.Services;
using Microsoft.Extensions.Options;

namespace ClubeBeneficios.Notifications.Worker.Workers;

public class NotificationBackgroundWorker : BackgroundService
{
    private readonly ILogger<NotificationBackgroundWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly NotificationWorkerOptions _options;

    private bool _expiredLocksReleased;

    public NotificationBackgroundWorker(
        ILogger<NotificationBackgroundWorker> logger,
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime applicationLifetime,
        IOptions<NotificationWorkerOptions> options)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _applicationLifetime = applicationLifetime;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStartup();

        await ReleaseExpiredLocksOnStartAsync(stoppingToken);

        if (IsManualMode())
        {
            await ExecuteManualAsync(stoppingToken);
            _applicationLifetime.StopApplication();
            return;
        }

        await ExecuteWatchAsync(stoppingToken);
    }

    private async Task ExecuteManualAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Modo Manual selecionado. Iniciando ciclo único de processamento.");

        var processedCount = await ProcessCycleAsync(stoppingToken);

        _logger.LogInformation(
            "Modo Manual finalizado. Notificações processadas: {ProcessedCount}.",
            processedCount);
    }

    private async Task ExecuteWatchAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Modo Watch selecionado. Processamento contínuo iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedCount = await ProcessCycleAsync(stoppingToken);

                if (processedCount == 0)
                {
                    _logger.LogInformation(
                        "Nenhuma notificação pendente encontrada. Próximo ciclo em {Seconds} segundos.",
                        _options.PollingIntervalSeconds);
                }
                else
                {
                    _logger.LogInformation(
                        "Ciclo concluí­do. Notificações processadas: {ProcessedCount}. Próximo ciclo em {Seconds} segundos.",
                        processedCount,
                        _options.PollingIntervalSeconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro não tratado durante ciclo de processamento. Próxima tentativa em {Seconds} segundos.",
                    _options.PollingIntervalSeconds);
            }

            await DelayUntilNextCycleAsync(stoppingToken);
        }

        _logger.LogInformation("Modo Watch finalizado por solicitação de parada.");
    }

    private async Task<int> ProcessCycleAsync(CancellationToken cancellationToken)
    {
        ValidateOptions();

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<INotificationProcessingService>();

        return await service.ProcessPendingAsync(
            _options.BatchSize,
            _options.LockMinutes,
            _options.MaxItemsPerCycle,
            cancellationToken);
    }

    private async Task ReleaseExpiredLocksOnStartAsync(CancellationToken cancellationToken)
    {
        if (_expiredLocksReleased || !_options.ReleaseExpiredLocksOnStart)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<INotificationProcessingService>();

        var releasedCount = await service.ReleaseExpiredLocksAsync(cancellationToken);
        _expiredLocksReleased = true;

        _logger.LogInformation(
            "Locks expirados liberados na inicialização: {ReleasedCount}.",
            releasedCount);
    }

    private async Task DelayUntilNextCycleAsync(CancellationToken cancellationToken)
    {
        var seconds = _options.PollingIntervalSeconds <= 0
            ? 30
            : _options.PollingIntervalSeconds;

        await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
    }

    private bool IsManualMode()
        => string.Equals(_options.Mode, "Manual", StringComparison.OrdinalIgnoreCase);

    private void ValidateOptions()
    {
        if (_options.BatchSize <= 0)
        {
            throw new InvalidOperationException("NotificationWorker:BatchSize deve ser maior que zero.");
        }

        if (_options.LockMinutes <= 0)
        {
            throw new InvalidOperationException("NotificationWorker:LockMinutes deve ser maior que zero.");
        }

        if (_options.MaxItemsPerCycle <= 0)
        {
            throw new InvalidOperationException("NotificationWorker:MaxItemsPerCycle deve ser maior que zero.");
        }
    }

    private void LogStartup()
    {
        _logger.LogInformation(
            "ClubeBeneficios.Notifications.Worker iniciado. Mode={Mode}, PollingIntervalSeconds={PollingIntervalSeconds}, BatchSize={BatchSize}, LockMinutes={LockMinutes}, MaxItemsPerCycle={MaxItemsPerCycle}",
            _options.Mode,
            _options.PollingIntervalSeconds,
            _options.BatchSize,
            _options.LockMinutes,
            _options.MaxItemsPerCycle);
    }
}
