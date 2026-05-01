using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenWM.App;
using OpenWM.Configuration;
using OpenWM.Core;
using OpenWM.DesktopManager;
using OpenWM.Hotkeys;
using OpenWM.Layout;
using OpenWM.Platform;
//using OpenWM.Workspaces;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
	Args = args,
	ContentRootPath = AppContext.BaseDirectory
});

builder.Configuration
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddEnvironmentVariables(prefix: "OPENWM_");

builder.Logging.AddSimpleConsole(options =>
{
	options.TimestampFormat = "HH:mm:ss ";
	options.SingleLine = true;
});

builder.Services.AddSingleton<ILayoutStrategy, DwindleLayoutStrategy>();
builder.Services.AddSingleton<ILayoutStrategy, MasterLayoutStrategy>();
builder.Services.AddSingleton<ILayoutStrategy, DynamicLayoutStrategy>();
builder.Services.AddSingleton<ILayoutStrategy, FloatingLayoutStrategy>();
builder.Services.AddSingleton<LayoutEngine>();

builder.Services
	.AddOptions<OpenWMOptions>()
	.Bind(builder.Configuration.GetSection("OpenWM"))
	.ValidateOnStart();

builder.Services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<OpenWMOptions>, OpenWMOptionsValidator>();

builder.Services.AddSingleton<IWorkspaceManager>(sp =>
{
	var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<OpenWMOptions>>().CurrentValue;
	return new WorkspaceManager(opt.WorkspaceCount, opt.DefaultLayout);
});

if (OperatingSystem.IsWindows())
{
	builder.Services.AddSingleton<IWindowSystem, WindowsWindowSystem>();
	builder.Services.AddSingleton<IHotkeyService, WindowsHotkeyService>();
}
else
{
	builder.Services.AddSingleton<IWindowSystem, NullWindowSystem>();
	builder.Services.AddSingleton<IHotkeyService, NullHotkeyService>();
}

builder.Services.AddSingleton<OpenWMApp>();
builder.Services.AddSingleton<VirtualDesktopManager>();
builder.Services.AddHostedService<OpenWMHostedService>();

using var host = builder.Build();
await host.RunAsync();
