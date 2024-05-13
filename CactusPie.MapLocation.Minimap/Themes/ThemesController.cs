using System;
using System.Windows;

namespace CactusPie.MapLocation.Minimap.Themes;

public static class ThemesController
{
    public enum ThemeTypes
    {
        Light,

        ColourfulLight,

        Dark,

        ColourfulDark,
    }

    public static ThemeTypes CurrentTheme { get; set; }

    private static ResourceDictionary ThemeDictionary
    {
        get => Application.Current.Resources.MergedDictionaries[0];
        set => Application.Current.Resources.MergedDictionaries[0] = value;
    }

    private static void ChangeTheme(Uri uri)
    {
        ThemeDictionary = new ResourceDictionary { Source = uri };
    }

    public static void SetTheme(ThemeTypes theme)
    {
        string? themeName = null;
        CurrentTheme = theme;
        themeName = theme switch
        {
            ThemeTypes.Dark => "DarkTheme",
            ThemeTypes.Light => "LightTheme",
            ThemeTypes.ColourfulDark => "ColourfulDarkTheme",
            ThemeTypes.ColourfulLight => "ColourfulLightTheme",
            _ => themeName,
        };

        try
        {
            if (!string.IsNullOrEmpty(themeName))
            {
                ChangeTheme(new Uri($"Themes/{themeName}.xaml", UriKind.Relative));
            }
        }
        catch
        {
        }
    }
}