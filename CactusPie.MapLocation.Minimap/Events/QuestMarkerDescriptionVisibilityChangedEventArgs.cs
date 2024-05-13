using System;

namespace CactusPie.MapLocation.Minimap.Events;

public sealed class QuestMarkerDescriptionVisibilityChangedEventArgs : EventArgs
{
    public QuestMarkerDescriptionVisibilityChangedEventArgs(bool isDescriptionVisible)
    {
        IsDescriptionVisible = isDescriptionVisible;
    }

    public bool IsDescriptionVisible { get; }
}