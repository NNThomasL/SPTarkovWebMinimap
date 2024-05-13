using System;
using System.Collections.Generic;
using System.Windows;
using CactusPie.MapLocation.Minimap.Events;
using CactusPie.MapLocation.Minimap.Helpers;
using CactusPie.MapLocation.Minimap.MapHandling.Data;

namespace CactusPie.MapLocation.Minimap.Controls;

public partial class AddNewBoundDialog : Window
{
    public AddNewBoundDialog()
    {
        InitializeComponent();
    }

    public event EventHandler<BoundAddedEventArgs>? BoundAdded;

    private void AddBoundButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(BoundNameTextBox.Text))
        {
            this.ShowError("You must provide a bound name");
            return;
        }

        var boundData = new BoundData
        {
            BoundName = BoundNameTextBox.Text,
            X1 = X1DoubleUpDown.Value ?? 0,
            X2 = X2DoubleUpDown.Value ?? 0,
            Z1 = Z1DoubleUpDown.Value ?? 0,
            Z2 = Z2DoubleUpDown.Value ?? 0,
            Y1 = Y1DoubleUpDown.Value ?? 0,
            Y2 = Y2DoubleUpDown.Value ?? 0,
            XCoefficients = new List<double>(),
            ZCoefficients = new List<double>(),
        };

        var eventArgs = new BoundAddedEventArgs(boundData);

        EventHandler<BoundAddedEventArgs>? handler = BoundAdded;
        handler?.Invoke(this, eventArgs);
        Close();
    }
}