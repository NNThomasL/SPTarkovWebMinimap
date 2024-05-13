using System;
using System.Linq;
using System.Windows.Controls;
using CactusPie.MapLocation.Minimap.Data;
using CactusPie.MapLocation.Minimap.Themes;

namespace CactusPie.MapLocation.Minimap.Controls;

public partial class ThemeSelector : UserControl
{
    private readonly MapConfiguration _mapConfiguration;

    public ThemeSelector(MapConfiguration mapConfiguration)
    {
        _mapConfiguration = mapConfiguration;
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        if (_mapConfiguration.Theme != null)
        {
            ThemeComboBox.SelectedItem = ThemeComboBox.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(x => x.Content as string == _mapConfiguration.Theme);
        }
    }

    private void ThemeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string? selectedThemeName = (e.AddedItems[0] as ComboBoxItem)?.Content as string;

        if (selectedThemeName == null)
        {
            throw new InvalidOperationException("Invalid theme name");
        }

        ThemesController.ThemeTypes themeType = selectedThemeName switch
        {
            "Light Theme" => ThemesController.ThemeTypes.Light,
            "Dark Theme" => ThemesController.ThemeTypes.Dark,
            "Colorful Light Theme" => ThemesController.ThemeTypes.ColourfulLight,
            "Colorful Dark Theme" => ThemesController.ThemeTypes.ColourfulDark,
            _ => throw new ArgumentOutOfRangeException($"Invalid theme name: {selectedThemeName}"),
        };

        ThemesController.SetTheme(themeType);
    }
}