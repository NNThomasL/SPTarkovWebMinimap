using System;
using System.IO;
using System.Windows;
using CactusPie.MapLocation.Minimap.Events;
using CactusPie.MapLocation.Minimap.Helpers;
using Microsoft.Win32;

namespace CactusPie.MapLocation.Minimap.Controls;

public partial class AddNewMapDialog : Window
{
    public AddNewMapDialog()
    {
        InitializeComponent();
    }

    public event EventHandler<MapAddedEventArgs>? MapAdded;

    private void AddMapImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                     "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                     "Portable Network Graphic (*.png)|*.png",
            Multiselect = false,
        };

        if (openFileDialog.ShowDialog() == true)
        {
            MapImagePathTextBox.Text = openFileDialog.FileName;
        }
    }

    private void AddMapButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MapNameTextBox.Text))
        {
            this.ShowError("You must provide a map name");
            return;
        }

        if (string.IsNullOrWhiteSpace(MapIdentifierTextBox.Text))
        {
            this.ShowError("You must provide a map identifier");
            return;
        }

        if (string.IsNullOrEmpty(MapImagePathTextBox.Text) || !File.Exists(MapImagePathTextBox.Text))
        {
            this.ShowError("You must provide a valid map file path");
            return;
        }

        if (MapRotationDoubleUpDown.Value == null)
        {
            this.ShowError("You must provide a valid map rotation");
            return;
        }

        if (MarkerScaleDoubleUpDown.Value == null)
        {
            this.ShowError("You must provide a valid marker scale");
            return;
        }

        var eventArgs = new MapAddedEventArgs(
                MapNameTextBox.Text,
                MapIdentifierTextBox.Text,
                MapImagePathTextBox.Text,
                MapRotationDoubleUpDown.Value.Value,
                MarkerScaleDoubleUpDown.Value.Value
            );
        EventHandler<MapAddedEventArgs>? handler = MapAdded;
        handler?.Invoke(this, eventArgs);
        Close();
    }
}