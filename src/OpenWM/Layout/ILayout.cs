using OpenWM.Native;

namespace OpenWM.Layout;

/// <summary>
/// Describes how a collection of windows should be arranged within a bounding rectangle.
/// </summary>
public interface ILayout
{
    /// <summary>Human-readable name of this layout.</summary>
    string Name { get; }

    /// <summary>
    /// Calculate the target rectangle for each window handle given the available work area
    /// and configured gaps.
    /// </summary>
    /// <param name="windows">Ordered list of window handles to tile (floating excluded).</param>
    /// <param name="workArea">Available screen area (excluding taskbar).</param>
    /// <param name="gaps">Gap in pixels between windows and screen edges.</param>
    /// <returns>Mapping of window handle to its target <see cref="RECT"/>.</returns>
    Dictionary<IntPtr, RECT> Arrange(IReadOnlyList<IntPtr> windows, RECT workArea, int gaps);
}
