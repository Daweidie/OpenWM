using OpenWM.Core;
using OpenWM.Layout;

namespace OpenWM.App;

public sealed class LayoutEngine
{
    private readonly Dictionary<LayoutKind, ILayoutStrategy> _strategies;

    public LayoutEngine(IEnumerable<ILayoutStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Kind, s => s);
    }

    public IReadOnlyList<PositionedWindow> Build(Workspace workspace, Rect area, int gaps, double masterRatio)
    {
        if (!_strategies.TryGetValue(workspace.Layout, out var strategy))
        {
            strategy = _strategies[LayoutKind.Dwindle];
        }

        var tiled = workspace.Windows.Where(w => !w.IsFloating && !w.IsFullscreen && w.IsManaged).ToList();
        var fullscreen = workspace.Windows.FirstOrDefault(w => w.IsFullscreen);

        if (fullscreen is not null)
        {
            return [new PositionedWindow(fullscreen, area)];
        }

        return strategy.Arrange(tiled, area, gaps, masterRatio);
    }
}
