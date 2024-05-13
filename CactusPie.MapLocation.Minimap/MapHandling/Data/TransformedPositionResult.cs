namespace CactusPie.MapLocation.Minimap.MapHandling.Data;

public sealed class TransformedPositionResult
{
    public TransformedPositionResult(double transformedXPosition, double transformedZPosition)
    {
        TransformedXPosition = transformedXPosition;
        TransformedZPosition = transformedZPosition;
    }

    public double TransformedXPosition { get; }

    public double TransformedZPosition { get; }
}