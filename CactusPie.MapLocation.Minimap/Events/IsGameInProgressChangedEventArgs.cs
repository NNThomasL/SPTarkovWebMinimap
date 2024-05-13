using System;

namespace CactusPie.MapLocation.Minimap.Events;

public class IsGameInProgressChangedEventArgs : EventArgs
{
    public IsGameInProgressChangedEventArgs(bool isGameInProgress)
    {
        IsGameInProgress = isGameInProgress;
    }

    public bool IsGameInProgress { get; }
}