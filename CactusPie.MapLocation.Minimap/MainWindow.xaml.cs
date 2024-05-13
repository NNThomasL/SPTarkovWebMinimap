using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CactusPie.MapLocation.Minimap.Controls;
using CactusPie.MapLocation.Minimap.Data;
using CactusPie.MapLocation.Minimap.Data.Enums;
using CactusPie.MapLocation.Minimap.Events;
using CactusPie.MapLocation.Minimap.Helpers;
using CactusPie.MapLocation.Minimap.MapHandling.Data;
using CactusPie.MapLocation.Minimap.Services.Interfaces;

namespace CactusPie.MapLocation.Minimap;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Func<AddNewMapDialog> _addNewMapDialogFactory;

    private readonly Func<BoundSettings> _boundSettingsControlFactory;

    private readonly ICurrentMapData _currentMapData;

    private readonly Func<MapControl> _mapControlFactory;

    private readonly IMapCreationDataManager _mapCreationDataManager;

    private readonly IMapDataRetriever _mapDataRetriever;

    private readonly Func<PlotWindow> _plotWindowFactory;

    private readonly Func<ThemeSelector> _themeSelectorFactory;

    private DateTime? _lastQuestChangeTime = null;

    private MapControl? _mapControl;

    public MainWindow(
        Func<IMapDataRetriever> mapDataReceiverFactory,
        IMapCreationDataManager mapCreationDataManager,
        ICurrentMapData currentMapData,
        Func<AddNewMapDialog> addNewMapDialogFactory,
        Func<PlotWindow> plotWindowFactory,
        Func<BoundSettings> boundSettingsControlFactory,
        Func<MapControl> mapControlFactory,
        Func<ThemeSelector> themeSelectorFactory
    )
    {
        _mapCreationDataManager = mapCreationDataManager;
        _currentMapData = currentMapData;
        _addNewMapDialogFactory = addNewMapDialogFactory;
        _plotWindowFactory = plotWindowFactory;
        _boundSettingsControlFactory = boundSettingsControlFactory;
        _mapControlFactory = mapControlFactory;
        _themeSelectorFactory = themeSelectorFactory;
        _mapDataRetriever = mapDataReceiverFactory();
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        _currentMapData.SetCurrentMapCreationDataSource(() => MapCreationDataTextBox.Text);

        _mapControl = _mapControlFactory();
        QuestMarkersVisibleCheckbox.IsChecked = _mapControl.QuestMarkersVisible;
        BotMarkersVisibleCheckbox.IsChecked = _mapControl.BotMarkersVisible;
        MapControlGrid.Children.Add(_mapControl);

        ReloadMapCreationPositionData();
        OtherSettingsStackPanek.Children.Insert(0, _themeSelectorFactory());

        BoundSettings boundSettings = _boundSettingsControlFactory();
        Grid.SetRow(boundSettings, 1);
        Grid.SetColumnSpan(boundSettings, 2);
        MapCreationGrid.Children.Add(boundSettings);

        _mapControl.MouseClickedOnCanvas += MapControl_OnMouseClickedOnCanvas;
        _currentMapData.LastReceivedPositionChanged += MapDataReceiverOnMapPositionDataReceived;
        _currentMapData.BeforeSelectedBoundChanged += CurrentMapDataOnBeforeSelectedBoundChanged;
        _currentMapData.SelectedBoundChanged += CurrentMapDataOnSelectedBoundChanged;
        _currentMapData.MapSelectionChanged += CurrentMapDataOnMapSelectionChanged;
        _currentMapData.GameStateChanged += CurrentMapDataOnGameStateChanged;
        _mapDataRetriever.StartReceivingData();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        _mapDataRetriever.StopReceivingData();
        _mapDataRetriever.Dispose();
    }

    private void MapDataReceiverOnMapPositionDataReceived(object? sender, EventArgs eventArgs)
    {
        Dispatcher.Invoke(
            () =>
            {
                if (_currentMapData.SelectedMap == null || _currentMapData.LastReceivedPosition == null)
                {
                    return;
                }

                // some methods take game positions and some map positions
                MapPositionData? lastReceivedPosition = _currentMapData.LastReceivedPosition;

                TransformedPositionResult transformedPositions = _currentMapData.SelectedMap.TransformPositions(
                    lastReceivedPosition.XPosition,
                    lastReceivedPosition.ZPosition,
                    lastReceivedPosition.YPosition,
                    _currentMapData.AutomaticallySwitchLevels);

                if (MapCreationModeCheckbox.IsChecked == true)
                {
                    CurrentPositionTextBox.Text =
                        $"{lastReceivedPosition.XPosition} {_mapControl?.PlayerMapXPosition} " +
                        $"{lastReceivedPosition.ZPosition} {_mapControl?.PlayerMapZPosition} " +
                        $"{lastReceivedPosition.YPosition}";
                }
                else
                {
                    _mapControl!.SetPlayerDotMapPosition(
                        transformedPositions.TransformedXPosition,
                        transformedPositions.TransformedZPosition,
                        lastReceivedPosition.XRotation);

                    if (FollowPlayerCheckbox.IsChecked == true)
                    {
                        _mapControl.CenterMapOnPlayer();
                    }

                    CurrentPositionTextBox.Text =
                        $"{lastReceivedPosition.XPosition} {transformedPositions.TransformedXPosition} " +
                        $"{lastReceivedPosition.ZPosition} {transformedPositions.TransformedZPosition} " +
                        $"{lastReceivedPosition.YPosition}";

                    if (lastReceivedPosition.BotLocations != null)
                    {
                        _mapControl.SetBotGameLocations(lastReceivedPosition.BotLocations, _currentMapData.AutomaticallySwitchLevels);
                    }
                }

                _mapControl!.SetAirdropData(lastReceivedPosition.AirdropData);

                if (_lastQuestChangeTime != lastReceivedPosition.LastQuestChangeTime)
                {
                    _lastQuestChangeTime = lastReceivedPosition.LastQuestChangeTime;
                    _ = _mapControl.LoadQuests();
                }
            });
    }

    private void ClearDrawingButton_OnClick(object sender, RoutedEventArgs e)
    {
        _mapControl!.ClearDrawing();
    }

    private void EnableDrawingCheckbox_OnChecked(object sender, RoutedEventArgs e)
    {
        _mapControl!.DrawingEnabled = true;
    }

    private void EnableDrawingCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        _mapControl!.DrawingEnabled = false;
    }

    private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Z when Keyboard.IsKeyDown(Key.LeftCtrl):
                _mapControl!.UndoLastDrawing();
                break;
            case Key.D:
                EnableDrawingCheckbox.IsChecked = !EnableDrawingCheckbox.IsChecked;
                break;
            case Key.C:
                MapCreationModeCheckbox.IsChecked = !MapCreationModeCheckbox.IsChecked;
                break;
            case Key.Q:
                QuestMarkersVisibleCheckbox.IsChecked = !QuestMarkersVisibleCheckbox.IsChecked;
                break;
            case Key.A:
                AutoSwitchLevelCheckbox.IsChecked = !AutoSwitchLevelCheckbox.IsChecked;
                break;
            case Key.B:
                BotMarkersVisibleCheckbox.IsChecked = !BotMarkersVisibleCheckbox.IsChecked;
                break;
            case Key.F:
                FollowPlayerCheckbox.IsChecked = !FollowPlayerCheckbox.IsChecked;
                break;
        }
    }

    private void FollowPlayerCheckbox_OnChecked(object sender, RoutedEventArgs e)
    {
        _mapControl!.CenterMapOnPlayer();
    }

    private void MapControl_OnMouseClickedOnCanvas(object? sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
        {
            return;
        }

        if (MapCreationModeCheckbox.IsChecked == true)
        {
            Point clickPosition = _mapControl!.GetMousePositionOnCanvas();
            _mapControl.SetPlayerDotMapPosition(clickPosition.X, clickPosition.Y);

            if (FollowPlayerCheckbox.IsChecked == true)
            {
                _mapControl.CenterMapOnPlayer();
            }
        }
    }

    private void MapCreationModeCheckbox_OnChecked(object sender, RoutedEventArgs e)
    {
        PositionDataGrid.Visibility = Visibility.Visible;
        SplitterColumnDefinition.Width = GridLength.Auto;
        MapCreationDataTextBoxColumnDefinition.Width = new GridLength(Width / 2);
        MapCreationSplitter.Visibility = Visibility.Visible;
        MapDataGrid.Visibility = Visibility.Visible;
        MapCreationControlsStackPanel.IsEnabled = true;
        _mapControl!.SetPlayerDotImage(PlayerDotType.Circle);

        if (_currentMapData.SelectedBound != null)
        {
            _mapControl.BoundRectangle.Visibility = Visibility.Visible;
        }
    }

    private void MapCreationModeCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        PositionDataGrid.Visibility = Visibility.Collapsed;
        SplitterColumnDefinition.Width = new GridLength(0);
        MapCreationDataTextBoxColumnDefinition.Width = new GridLength(0);
        MapCreationSplitter.Visibility = Visibility.Collapsed;
        MapDataGrid.Visibility = Visibility.Collapsed;
        _mapControl!.SetPlayerDotImage(PlayerDotType.CircleWithArrow);
        MapCreationControlsStackPanel.IsEnabled = false;
        _mapControl.BoundRectangle.Visibility = Visibility.Collapsed;
    }

    private void StartCreatingMapButton_OnClick(object sender, RoutedEventArgs e)
    {
        AddNewMapDialog addMapDialog = _addNewMapDialogFactory();
        addMapDialog.MapAdded += (_, args) =>
        {
            (bool Success, string? ErrorMessage) addNewMapResult =
                _mapCreationDataManager.AddNewMap(
                    args.MapName,
                    args.MapIdentifier,
                    args.MapRotation,
                    args.MarkerScale,
                    args.MapImagePath);

            if (!addNewMapResult.Success)
            {
                this.ShowError(addNewMapResult.ErrorMessage ?? "Failed to add a new map");
            }

            _mapControl!.ReloadMaps();
        };

        addMapDialog.ShowDialog();
    }

    private void ReloadMapCreationPositionData()
    {
        if (_currentMapData.SelectedMap != null)
        {
            MapCreationDataTextBox.Text = _mapCreationDataManager
                .GetMapPositionData(_currentMapData.SelectedMap, _currentMapData.SelectedBound?.BoundName);
        }
    }

    private void AddPositionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(CurrentPositionTextBox.Text))
        {
            this.ShowError("Position data is not yet available. Start the game and begin a new round first");
            return;
        }

        if (!MapCreationDataTextBox.Text.EndsWith('\n'))
        {
            MapCreationDataTextBox.Text += '\n';
        }

        MapCreationDataTextBox.Text += $"{CurrentPositionTextBox.Text}\n";
        SaveMapPositionDataToFile();
    }

    private void SaveMapPositionDataToFile()
    {
        if (_currentMapData.SelectedMap != null)
        {
            _mapCreationDataManager.SaveMapPositionData(
                _currentMapData.SelectedMap,
                MapCreationDataTextBox.Text,
                _currentMapData.SelectedBound?.BoundName);
        }
    }

    private void ClearPositionsButton_OnClick(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show(
            "Are you sure you want to clear all positions?",
            "Are you sure?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        MapCreationDataTextBox.Text = null;
        SaveMapPositionDataToFile();
    }

    private void UpdateMapTransformsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_currentMapData.IsGameInProgress)
        {
            this.ShowError("You must enter a raid before calculating the coefficients");
            return;
        }

        if (_currentMapData.SelectedMap == null)
        {
            this.ShowError("A map must first be loaded before calculating the coefficients");
            return;
        }

        _mapCreationDataManager.SaveMapPositionData(
            _currentMapData.SelectedMap,
            MapCreationDataTextBox.Text,
            _currentMapData.SelectedBound?.BoundName);

        foreach (BoundData? bound in _currentMapData.SelectedMap.CustomBounds.Prepend(null))
        {
            string boundDisplayName = bound?.BoundName ?? "Default";

            string? mapCreationData =
                _mapCreationDataManager.GetMapPositionData(_currentMapData.SelectedMap, bound?.BoundName);

            if (mapCreationData == null)
            {
                this.ShowError($"Could not retrieve map creation data for bound {boundDisplayName}");
                return;
            }

            MapCoefficientsGenerationResult result = _mapCreationDataManager.GenerateMapCoefficients(
                mapCreationData,
                PolynomialDegreeIntegerUpDown.Value ?? 1);

            if (!result.Success)
            {
                var errorMessage = $"An error occurred when generating coefficients for bound {boundDisplayName}";
                if (result.ErrorMessage != null)
                {
                    errorMessage += $": {result.ErrorMessage}";
                }

                this.ShowError(errorMessage);
                return;
            }

            if (result.MapCoefficients == null)
            {
                this.ShowError("Received null coefficients");
                return;
            }

            MapCoefficients mapCoefficients = result.MapCoefficients;
            ShowMapCoefficientsPlot(mapCoefficients, boundDisplayName);

            if (bound != null)
            {
                bound.XCoefficients = mapCoefficients.XCoefficients;
                bound.ZCoefficients = mapCoefficients.ZCoefficients;
            }
            else
            {
                _currentMapData.SelectedMap.XCoefficients = mapCoefficients.XCoefficients;
                _currentMapData.SelectedMap.ZCoefficients = mapCoefficients.ZCoefficients;
            }
        }

        var mapData = new MapData
        {
            MapName = _currentMapData.SelectedMap.MapName,
            MapIdentifier = _currentMapData.SelectedMap.MapIdentifier,
            MapRotation = _currentMapData.SelectedMap.MapRotation,
            MapImageFile = _currentMapData.SelectedMap.MapImageFile,
            MarkerScale = _currentMapData.SelectedMap.MarkerScale,
            XCoefficients = _currentMapData.SelectedMap.XCoefficients,
            ZCoefficients = _currentMapData.SelectedMap.ZCoefficients,
            CustomBounds = _currentMapData.SelectedMap.CustomBounds,
        };

        _mapCreationDataManager.UpdateMap(mapData);
        _mapControl!.ReloadMaps();
        _mapControl.ReloadAllMarkerPositions();
    }

    private void ShowMapCoefficientsPlot(MapCoefficients mapCoefficients, string boundName)
    {
        var xPlotData = new PlotData
        {
            GameCoordinates = mapCoefficients.GameXPositionsArray,
            MapCoordinates = mapCoefficients.MapXPositionsArray,
            Coefficients = mapCoefficients.XCoefficients,
        };

        var zPlotData = new PlotData
        {
            GameCoordinates = mapCoefficients.GameZPositionsArray,
            MapCoordinates = mapCoefficients.MapZPositionsArray,
            Coefficients = mapCoefficients.ZCoefficients,
        };

        PlotWindow plotWindow = _plotWindowFactory();
        plotWindow.RenderPlot(xPlotData, zPlotData, boundName);
        plotWindow.ShowDialog();
    }

    private void CurrentMapDataOnBeforeSelectedBoundChanged(object? sender, BeforeSelectedBoundChangedEventArgs e)
    {
        if (_currentMapData.SelectedMap == null || e.NewBound == e.PreviousBound)
        {
            return;
        }

        _mapCreationDataManager.SaveMapPositionData(
            _currentMapData.SelectedMap,
            MapCreationDataTextBox.Text,
            _currentMapData.SelectedBound?.BoundName);
    }

    private void CurrentMapDataOnSelectedBoundChanged(object? sender, EventArgs e)
    {
        ReloadMapCreationPositionData();
    }

    private void PlayerMarkerScaleSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _mapControl?.SetPlayerMarkerScale(PlayerMarkerScaleSlider.Value);
    }

    private void PlayerTransparencySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _mapControl?.SetPlayerMarkerOpacity(PlayerOpacitySlider.Value);
    }

    private void RefreshRateSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _currentMapData.PositionRefreshRate = (int)e.NewValue;
    }

    private void CurrentMapDataOnMapSelectionChanged(object? sender, EventArgs e)
    {
        ReloadMapCreationPositionData();
    }

    private void CurrentMapDataOnGameStateChanged(object? sender, IsGameInProgressChangedEventArgs e)
    {
        _lastQuestChangeTime = null;
    }

    private void QuestMarkersVisibleCheckbox_OnChecked(object sender, RoutedEventArgs e)
    {
        _mapControl!.QuestMarkersVisible = true;
    }

    private void QuestMarkersVisibleCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        _mapControl!.QuestMarkersVisible = false;
    }

    private void BotMarkersVisibleCheckbox_OnChecked(object sender, RoutedEventArgs e)
    {
        _mapControl!.BotMarkersVisible = true;
    }

    private void BotMarkersVisibleCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        _mapControl!.BotMarkersVisible = false;
    }

    private void AutoSwitchLevelCheckbox_OnChecked(object sender, RoutedEventArgs e)
    {
        _currentMapData.AutomaticallySwitchLevels = true;
        _mapControl?.ReloadAllMarkerPositions();
    }

    private void AutoSwitchLevelCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        _currentMapData.AutomaticallySwitchLevels = false;
        _mapControl?.ReloadAllMarkerPositions();
    }
}