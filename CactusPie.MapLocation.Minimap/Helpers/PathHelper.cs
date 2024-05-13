using System;
using System.IO;

namespace CactusPie.MapLocation.Minimap.Helpers;

public static class PathHelper
{
    private static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

    public static string GetAbsolutePath(string subPath)
    {
        return Path.Combine(BaseDirectory, subPath);
    }

    public static string GetAbsolutePath(string subPath1, string subPath2)
    {
        return Path.Combine(BaseDirectory, subPath1, subPath2);
    }
}