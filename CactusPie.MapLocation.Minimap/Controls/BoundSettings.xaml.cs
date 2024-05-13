using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CactusPie.MapLocation.Minimap.Data;
using CactusPie.MapLocation.Minimap.Data.Enums;
using CactusPie.MapLocation.Minimap.Events;
using CactusPie.MapLocation.Minimap.Helpers;
using CactusPie.MapLocation.Minimap.Services.Data;
using CactusPie.MapLocation.Minimap.Services.Interfaces;

namespace CactusPie.MapLocation.Minimap.Controls;

public partial class BoundSettings : UserControl
{
    private readonly Func<AddNewBoundDialog> _addNewBoundDialogFactory;

    private readonly ICurrentMapData _currentMapData;

    private readonly IMapCreationDataManager _mapCreationDataManager;

    public BoundSettings(
        ICurrentMapData currentMapData,
        IMapCreationDataManager mapCreationDataManager,
        Func<AddNewBoundDialog> addNewBoundDialogFactory)
    {
        _currentMapData = currentMapData;
        _mapCreationDataManager = mapCreationDataManager;
        _addNewBoundDialogFactory = addNewBoundDialogFactory;
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        this.Unloaded += OnUnloaded;

        AddBoundButton(BoundButtonType.X1);
        AddBoundButton(BoundButtonType.X2);
        AddBoundButton(BoundButtonType.Z1);
        AddBoundButton(BoundButtonType.Z2);
        AddBoundButton(BoundButtonType.Y1);
        AddBoundButton(BoundButtonType.Y2);
        ReloadBounds();

        _currentMapData.GameStateChanged += CurrentMapDataOnGameStateChanged;
        _currentMapData.MapSelectionChanged += CurrentMapDataOnMapSelectionChanged;
    }

    private void AddBoundButton(BoundButtonType boundButtonType)
    {
        var boundButton = new BoundButton(boundButtonType, _currentMapData);
        boundButton.Width = 110;
        boundButton.Height = 30;
        boundButton.Margin = new Thickness(0, 0, 5, 0);
        BoundControlsStackPanel.Children.Add(boundButton);
    }

    private void AddNewBoundButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentMapData.SelectedMap == null)
        {
            MessageBoxHelper.ShowError("You must select a map first in order to add a new bound");
            return;
        }

        AddNewBoundDialog addBoundDialog = _addNewBoundDialogFactory();
        WeakEventManager<AddNewBoundDialog, BoundAddedEventArgs>.AddHandler(
            addBoundDialog,
            nameof(AddNewBoundDialog.BoundAdded),
            OnAddBoundDialogOnBoundAdded);

        addBoundDialog.ShowDialog();
    }

    private void ReloadBounds()
    {
        if (_currentMapData.SelectedMap == null)
        {
            return;
        }

        List<BoundComboBoxItem> newSource = _currentMapData.SelectedMap.CustomBounds
            .Select(x => new BoundComboBoxItem(x.BoundName ?? "Error", x.BoundName))
            .Prepend(new BoundComboBoxItem("Default", null))
            .ToList();

        BoundComboBox.ItemsSource = newSource;

        int selectedBoundIndex = newSource.FindIndex(x => x.BoundName == _currentMapData.SelectedBound?.BoundName);
        if (selectedBoundIndex == -1)
        {
            selectedBoundIndex = 0;
        }

        BoundComboBox.SelectedIndex = selectedBoundIndex;
    }

    private void BoundComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_currentMapData.SelectedMap == null)
        {
            return;
        }

        string? selectedBoundName = BoundComboBox.SelectedValue as string;
        if (_currentMapData.SelectedBound?.BoundName == selectedBoundName)
        {
            return;
        }

        _mapCreationDataManager.SaveMapData(_currentMapData.SelectedMap);

        if (selectedBoundName == null)
        {
            _currentMapData.SelectedBound = null;
        }
        else
        {
            _currentMapData.SelectedBound = _currentMapData.SelectedMap?.GetBound(selectedBoundName);
        }
    }

    private void SaveBoundButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentMapData.SelectedMap == null)
        {
            MessageBoxHelper.ShowError("You must select a map first");
            return;
        }

        if (_currentMapData.SelectedBound == null)
        {
            MessageBoxHelper.ShowError("You must select a bound first");
            return;
        }

        _mapCreationDataManager.SaveMapPositionData(
            _currentMapData.SelectedMap,
            _currentMapData.CurrentMapCreationData ?? string.Empty,
            _currentMapData.SelectedBound?.BoundName);

        _mapCreationDataManager.SaveMapData(_currentMapData.SelectedMap);
    }

    private void DeleteBound_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentMapData.SelectedMap == null)
        {
            MessageBoxHelper.ShowError("You need to select a map first");
            return;
        }

        if (_currentMapData.SelectedBound == null)
        {
            MessageBoxHelper.ShowError("You cannot delete the default bound");
            return;
        }

        MessageBoxResult result = MessageBox.Show(
            $"Are you sure you want to remove bound {_currentMapData.SelectedBound!.BoundName}?",
            "Are you sure?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _currentMapData.SelectedMap!.DeleteBoundData(_currentMapData.SelectedBound!.BoundName!);
        _mapCreationDataManager.RemoveBoundFile(_currentMapData.SelectedMap, _currentMapData.SelectedBound!.BoundName!);
        _mapCreationDataManager.SaveMapData(_currentMapData.SelectedMap);
        ReloadBounds();
    }

    private void CurrentMapDataOnGameStateChanged(object? sender, IsGameInProgressChangedEventArgs e)
    {
        if (e.IsGameInProgress)
        {
            Dispatcher.Invoke(() => IsEnabled = true);
        }
        else
        {
            Dispatcher.Invoke(() => IsEnabled = false);
        }
    }

    private void CurrentMapDataOnMapSelectionChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(ReloadBounds);
    }

    private void OnAddBoundDialogOnBoundAdded(object? sender, BoundAddedEventArgs args)
    {
        if (_currentMapData.SelectedMap == null)
        {
            // This should never really happen
            MessageBoxHelper.ShowError("No map was selected for the new bound");
            return;
        }

        if (_currentMapData.SelectedMap?.GetBound(args.BoundData.BoundName!) != null)
        {
            // This should never really happen
            MessageBoxHelper.ShowError("A bound with that name already exists");
            return;
        }

        AddNewBoundResult addNewBoundResult = _mapCreationDataManager.AddNewBound(
            _currentMapData.SelectedMap!.MapName,
            args.BoundData);

        if (!addNewBoundResult.Success)
        {
            MessageBoxHelper.ShowError($"Failed to add a bound: {addNewBoundResult.ErrorMessage}");
            return;
        }

        _currentMapData.SelectedMap.CustomBounds = addNewBoundResult.NewMapData!.CustomBounds!;
        ReloadBounds();
        BoundComboBox.SelectedValue = args.BoundData.BoundName;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _currentMapData.GameStateChanged -= CurrentMapDataOnGameStateChanged;
        _currentMapData.MapSelectionChanged -= CurrentMapDataOnMapSelectionChanged;
    }
}