using System;
using System.Collections.Generic;
using CactusPie.MapLocation.Minimap.Helpers;

namespace CactusPie.MapLocation.Minimap.MapHandling.Data;

public sealed class BoundData
{
    public string? BoundName { get; init; }

    public double X1 { get; set; }

    public double X2 { get; set; }

    public IReadOnlyList<double>? XCoefficients { get; set; }

    public double Y1 { get; set; }

    public double Y2 { get; set; }

    public double Z1 { get; set; }

    public double Z2 { get; set; }

    public IReadOnlyList<double>? ZCoefficients { get; set; }

    public bool IsValidForGamePosition(double gameXPosition, double gameZPosition, double gameYPosition)
    {
        // Rules split into separate if for easier debugging
        if (gameXPosition <= X2)
        {
            if (!(gameXPosition >= X1))
            {
                return false;
            }
        }
        else if (!(gameXPosition <= X1))
        {
            return false;
        }

        if (gameZPosition <= Z2)
        {
            if (!(gameZPosition >= Z1))
            {
                return false;
            }
        }
        else if (!(gameZPosition <= Z1))
        {
            return false;
        }

        if (gameYPosition <= Y2)
        {
            if (!(gameYPosition >= Y1))
            {
                return false;
            }
        }
        else if (!(gameYPosition <= Y1))
        {
            return false;
        }

        return true;
    }

    public double TransformXPosition(double mapXPosition)
    {
        return PolynomialHelper.CalculatePolynomialValue(mapXPosition, XCoefficients ?? Array.Empty<double>());
    }

    public double TransformZPosition(double mapZPosition)
    {
        return PolynomialHelper.CalculatePolynomialValue(mapZPosition, ZCoefficients ?? Array.Empty<double>());
    }
}