using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWM.Configuration;
using OpenWM.Core;
using OpenWM.Platform;

namespace OpenWM.DesktopManager;

public sealed class VirtualDesktopManager
{
    private readonly IWorkspaceManager _workspaces;
    private readonly IWindowSystem _windowSystem;
    private readonly IOptionsMonitor<OpenWMOptions> _options;
    private readonly ILogger<VirtualDesktopManager> _logger;
    private bool _startupCommandsLaunched;

    public VirtualDesktopManager(
        IWorkspaceManager workspaces,
        IWindowSystem windowSystem,
        IOptionsMonitor<OpenWMOptions> options,
        ILogger<VirtualDesktopManager> logger)
    {
        _workspaces = workspaces;
        _windowSystem = windowSystem;
        _options = options;
        _logger = logger;
    }

    public void Initialize()
    {
        TryLaunchStartupCommands();
        ApplyWorkspaceIsolation();
    }

    public void ApplyWorkspaceIsolation()
    {
        var opts = _options.CurrentValue.VirtualDesktop;
        if (!opts.Enabled || !_windowSystem.SupportsWindowVisibilityControl)
        {
            return;
        }

        var activeId = _workspaces.Active.Id;
        foreach (var ws in _workspaces.All)
        {
            var visible = ws.Id == activeId;
            foreach (var win in ws.Windows)
            {
                _ = _windowSystem.TrySetWindowVisibility(win.Handle, visible);
            }
        }
    }

    public void SwitchWorkspace(int workspaceId)
    {
        _workspaces.SetActive(workspaceId);
        ApplyWorkspaceIsolation();

        var focused = _workspaces.FocusedWindow();
        if (focused is not null)
        {
            _ = _windowSystem.TryFocus(focused.Handle);
        }
    }

    public void SwitchWorkspaceRelative(int delta)
    {
        if (_workspaces.All.Count == 0)
        {
            return;
        }

        var currentIndex = _workspaces.Active.Id - 1;
        var nextIndex = (currentIndex + delta + _workspaces.All.Count) % _workspaces.All.Count;
        SwitchWorkspace(_workspaces.All[nextIndex].Id);
    }

    public bool MoveFocusedToWorkspace(int workspaceId, bool follow)
    {
        var focused = _workspaces.FocusedWindow();
        if (focused is null)
        {
            return false;
        }

        if (!_workspaces.MoveToWorkspace(focused.Handle, workspaceId))
        {
            return false;
        }

        if (follow)
        {
            SwitchWorkspace(workspaceId);
            _ = _windowSystem.TryFocus(focused.Handle);
        }
        else
        {
            ApplyWorkspaceIsolation();
        }

        return true;
    }

    private void TryLaunchStartupCommands()
    {
        if (_startupCommandsLaunched)
        {
            return;
        }

        var opts = _options.CurrentValue.VirtualDesktop;
        if (!opts.Enabled || !opts.LaunchStartupCommands)
        {
            return;
        }

        foreach (var command in opts.StartupCommands)
        {
            if (_windowSystem.TryLaunchProcess(command))
            {
                _logger.LogInformation("Virtual desktop startup command launched: {Command}", command);
            }
            else
            {
                _logger.LogWarning("Virtual desktop startup command failed: {Command}", command);
            }
        }

        _startupCommandsLaunched = true;
    }
}
