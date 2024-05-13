using System.ComponentModel;

namespace CactusPie.MapLocation.Minimap.Data.Enums;

public enum BoundButtonType
{
    [Description("Min X")]
    X1,

    [Description("Max X")]
    X2,

    [Description("Min Z")]
    Z1,

    [Description("Max Z")]
    Z2,

    [Description("Min Y")]
    Y1,

    [Description("Max Y")]
    Y2,
}