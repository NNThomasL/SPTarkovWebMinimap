using System;
using CactusPie.MapLocation.Minimap.MapHandling.Data;

namespace CactusPie.MapLocation.Minimap.Events;

public sealed class BeforeSelectedBoundChangedEventArgs : EventArgs
{
    public BeforeSelectedBoundChangedEventArgs(BoundData? previousBound, BoundData? newBound)
    {
        PreviousBound = previousBound;
        NewBound = newBound;
    }

    public BoundData? NewBound { get; }

    public BoundData? PreviousBound { get; }
}