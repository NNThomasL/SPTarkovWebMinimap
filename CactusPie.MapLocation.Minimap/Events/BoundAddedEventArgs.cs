using System;
using CactusPie.MapLocation.Minimap.MapHandling.Data;

namespace CactusPie.MapLocation.Minimap.Events;

public class BoundAddedEventArgs : EventArgs
{
    public BoundAddedEventArgs(BoundData boundData)
    {
        BoundData = boundData;
    }

    public BoundData BoundData { get; }
}