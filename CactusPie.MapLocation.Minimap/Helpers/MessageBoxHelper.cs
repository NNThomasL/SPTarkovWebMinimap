using System;
using System.Linq;
using System.Windows;

namespace CactusPie.MapLocation.Minimap.Helpers;

public static class MessageBoxHelper
{
    public static void ShowError(this Window window, string message)
    {
        MessageBox.Show(window, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void ShowError(string message)
    {
        MainWindow? window = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();

        if (window == null)
        {
            throw new InvalidOperationException("Could not find a window to show the error message with");
        }

        MessageBox.Show(window, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}