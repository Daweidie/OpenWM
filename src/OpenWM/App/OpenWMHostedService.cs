using Microsoft.Extensions.Hosting;

namespace OpenWM.App;

public sealed class OpenWMHostedService : BackgroundService
{
    private readonly OpenWMApp _app;

    public OpenWMHostedService(OpenWMApp app)
    {
        _app = app;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _app.RunAsync(stoppingToken);
    }
}
