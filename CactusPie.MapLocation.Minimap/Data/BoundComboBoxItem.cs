using System.Diagnostics;

namespace CactusPie.MapLocation.Minimap.Data;

[DebuggerDisplay("{BoundDisplayName} {BoundName}")]
public sealed class BoundComboBoxItem
{
    public BoundComboBoxItem(string boundDisplayName, string? boundName)
    {
        BoundDisplayName = boundDisplayName;
        BoundName = boundName;
    }

    public string BoundDisplayName { get; set; }

    public string? BoundName { get; set; }
}