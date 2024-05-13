using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CactusPie.MapLocation.Minimap.Data;
using CactusPie.MapLocation.Minimap.Data.Enums;
using CactusPie.MapLocation.Minimap.Events;
using CactusPie.MapLocation.Minimap.Helpers;
using CactusPie.MapLocation.Minimap.MapHandling;
using CactusPie.MapLocation.Minimap.MapHandling.Data;
using CactusPie.MapLocation.Minimap.MapHandling.Interfaces;
using CactusPie.MapLocation.Minimap.Services.Interfaces;
using TechHappy.MapLocation.Common.Requests.Data;

namespace CactusPie.MapLocation.Minimap.Controls;

public partial class MapControl : UserControl
{
    private readonly ICurrentMapData _currentMapData;

    private readonly IMapDataRetriever _mapDataRetriever;

    private readonly RotateTransform _playerDotRotation = new();

    private readonly ScaleTransform _playerDotScale = new();

    private bool _botMarkersVisible = false;

    private bool _drawingEnabled;

    private AirdropData? _lastReceivedAirdropData = null;

    private List<IMap>? _maps;

    private bool _questMarkersVisible = true;

    public MapControl(ICurrentMapData currentMapData, IMapDataRetriever mapDataRetriever)
    {
        _currentMapData = currentMapData;
        _mapDataRetriever = mapDataRetriever;
        InitializeComponent();
    }

    private Dictionary<int, Image> BotMarkers { get; set; } = new(35);

    public bool BotMarkersVisible
    {
        get => _botMarkersVisible;
        set
        {
            _botMarkersVisible = value;
            SetBotMarkerVisibility(value);
        }
    }

    public bool DrawingEnabled
    {
        get => _drawingEnabled;
        set
        {
            _drawingEnabled = value;
            DrawingInkCanvas.IsHitTestVisible = value;
        }
    }

    public double PlayerMapXPosition { get; set; } = 1.0f;

    public double PlayerMapZPosition { get; set; } = 1.0f;

    private Dictionary<string, QuestMarker> QuestMarkers { get; set; } = new(15);

    public bool QuestMarkersVisible
    {
        get => _questMarkersVisible;
        set
        {
            _questMarkersVisible = value;
            SetQuestMarkerVisibility(value);
        }
    }

    public event EventHandler<MouseButtonEventArgs>? MouseClickedOnCanvas;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        _currentMapData.BoundDataUpdated += CurrentBoundDataOnBoundDataUpdated;
        _currentMapData.SelectedBoundChanged += CurrentMapDataOnSelectedBoundChanged;
        _currentMapData.QuestsChanged += CurrentMapDataOnQuestsChanged;
        _currentMapData.LastReceivedPositionChanged += CurrentMapDataOnLastReceivedPositionChanged;
        _currentMapData.GameStateChanged += CurrentMapDataOnGameStateChanged;
        Unloaded += OnUnloaded;

        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(_playerDotRotation);
        transformGroup.Children.Add(_playerDotScale);

        PlayerDot.RenderTransform = transformGroup;

        ReloadMaps();
    }

    public void CenterMapOnPlayer()
    {
        TranslateTransform translateTransform = MapZoomBorder.GetTranslateTransform();
        ScaleTransform scaleTransform = MapZoomBorder.GetScaleTransform();

        translateTransform.X = -Canvas.GetLeft(PlayerDot) * scaleTransform.ScaleX + MapImage.Width / 2;
        translateTransform.Y = -Canvas.GetTop(PlayerDot) * scaleTransform.ScaleY + MapImage.Height / 2;
    }

    public void ClearDrawing()
    {
        DrawingInkCanvas.Strokes.Clear();
    }

    public void RemoveBot(int botId)
    {
        if (BotMarkers.TryGetValue(botId, out Image? botMarker))
        {
            BotMarkers.Remove(botId);
            PlayerOverlayCanvas.Children.Remove(botMarker);
        }
    }

    public void RemoveQuest(string questId)
    {
        if (QuestMarkers.TryGetValue(questId, out QuestMarker? questMarker))
        {
            QuestMarkers.Remove(questId);
            PlayerOverlayCanvas.Children.Remove(questMarker);
        }
    }

    public void UndoLastDrawing()
    {
        if (DrawingInkCanvas.Strokes.Count > 0)
        {
            DrawingInkCanvas.Strokes.RemoveAt(DrawingInkCanvas.Strokes.Count - 1);
        }
    }

    public void SetPlayerDotMapPosition(double xPosition, double zPosition, float angle = 0)
    {
        angle -= 180;

        PlayerMapXPosition = xPosition;
        PlayerMapZPosition = zPosition;
        Canvas.SetLeft(PlayerDot, xPosition - 10);
        Canvas.SetTop(PlayerDot, zPosition - 10);
        _playerDotRotation.Angle = angle;
    }

    public void SetBotGameLocations(List<BotLocation> botLocations, bool useCustomBounds)
    {
        // Remove bots that no longer exist
        List<int> botIdsToRemove =
            BotMarkers
                .Where(botMarker => !botLocations.Any(newBotLocation => newBotLocation.BotId == botMarker.Key))
                .Select(x => x.Key)
                .ToList();

        foreach (int botIdToRemove in botIdsToRemove)
        {
            RemoveBot(botIdToRemove);
        }

        IMap? selectedMap = _currentMapData.SelectedMap;

        if (selectedMap == null)
        {
            return;
        }

        BotMarkers.EnsureCapacity(botLocations.Count);

        Visibility visibility = BotMarkersVisible ? Visibility.Visible : Visibility.Hidden;

        // We scale it so it looks the same regardless of map size
        var scaleTransform = new ScaleTransform
        {
            ScaleX = selectedMap.MarkerScale,
            ScaleY = selectedMap.MarkerScale,
        };

        // Add or update bots
        foreach (BotLocation botLocation in botLocations)
        {
            double transformedXPosition = 0;
            double transformedZPosition = 0;

            bool foundMatchingBound = false;

            if (useCustomBounds)
            {
                foreach (BoundData customBound in _currentMapData.SelectedMap!.CustomBounds)
                {
                    if (customBound.IsValidForGamePosition(
                            botLocation.XPosition,
                            botLocation.ZPosition,
                            botLocation.YPosition))
                    {
                        transformedXPosition = customBound.TransformXPosition(botLocation.XPosition) - 10;
                        transformedZPosition = customBound.TransformZPosition(botLocation.ZPosition) - 10;
                        foundMatchingBound = true;
                        break;
                    }
                }
            }

            if (!foundMatchingBound)
            {
                transformedXPosition = selectedMap.TransformXPosition(botLocation.XPosition) - 10;
                transformedZPosition = selectedMap.TransformZPosition(botLocation.ZPosition) - 10;
            }

            if (BotMarkers.TryGetValue(botLocation.BotId, out Image? botMarker))
            {
                Canvas.SetLeft(botMarker, transformedXPosition);
                Canvas.SetTop(botMarker, transformedZPosition);
                botMarker.Visibility = visibility;
            }
            else
            {
                string imageName = botLocation.BotType switch
                {
                    BotType.Bear => "circle_orange.png",
                    BotType.Usec => "circle_blue.png",
                    BotType.Scav => "circle_yellow.png",
                    BotType.Boss => "circle_purple.png",
                    _ => "circle_orange_brown.png",
                };

                var image = new Image
                {
                    Width = 20,
                    Height = 20,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/{imageName}")),
                };

                image.RenderTransform = scaleTransform;
                image.Visibility = visibility;

                PlayerOverlayCanvas.Children.Add(image);

                Canvas.SetLeft(image, transformedXPosition);
                Canvas.SetTop(image, transformedZPosition);

                BotMarkers.TryAdd(botLocation.BotId, image);
            }
        }
    }

    public void ClearBotPositions()
    {
        foreach (Image botImage in BotMarkers.Values)
        {
            PlayerOverlayCanvas.Children.Remove(botImage);
        }

        BotMarkers.Clear();
    }

    public void ClearQuestPositions()
    {
        foreach (QuestMarker questMarker in QuestMarkers.Values)
        {
            PlayerOverlayCanvas.Children.Remove(questMarker);
        }

        QuestMarkers.Clear();
    }

    public void SetAirdropData(AirdropData? airdropData)
    {
        IMap? selectedMap = _currentMapData.SelectedMap;

        if (selectedMap == null)
        {
            return;
        }

        if (airdropData == null)
        {
            if (_lastReceivedAirdropData != null)
            {
                _lastReceivedAirdropData = null;
                AirdropMarkerImage.Visibility = Visibility.Hidden;
                return;
            }

            return;
        }

        if (_lastReceivedAirdropData == null || !_lastReceivedAirdropData.Equals(airdropData))
        {
            _lastReceivedAirdropData = airdropData;

            SetAirdropLocation(selectedMap);

            AirdropMarkerImage.Visibility = Visibility.Visible;
        }
    }

    public Point GetMousePositionOnCanvas()
    {
        return Mouse.GetPosition(PlayerOverlayCanvas);
    }

    public void ReloadMaps()
    {
        var maps = new List<IMap>(16);
        string mapsPath = PathHelper.GetAbsolutePath(@"Maps");
        foreach (string filePath in Directory.GetFiles(mapsPath).Where(x => x.EndsWith("json")))
        {
            string fileContent = File.ReadAllText(filePath);
            MapData? mapData = JsonSerializer.Deserialize<MapData>(fileContent);

            if (mapData == null)
            {
                throw new NullReferenceException("Could not deserialize the map data");
            }

            maps.Add(new CustomMap(mapData));
        }

        _maps = maps.OrderBy(x => x.MapName).ToList();

        if (_currentMapData.SelectedMap != null)
        {
            LoadMap(_currentMapData.SelectedMap.MapIdentifier);
        }
    }

    public void SetPlayerDotImage(PlayerDotType playerDotType)
    {
        string playerDotImageSource = playerDotType switch
        {
            PlayerDotType.Circle => "pack://application:,,,/Resources/circle.png",
            PlayerDotType.CircleWithArrow => "pack://application:,,,/Resources/circle_with_arrow.png",
            _ => throw new ArgumentOutOfRangeException(
                nameof(playerDotType),
                playerDotType,
                $"Invalid {nameof(PlayerDotType)} value"),
        };

        PlayerDot.Source = new BitmapImage(new Uri(playerDotImageSource));
    }

    private void LoadMap(string? mapIdentifier)
    {
        if (mapIdentifier == null)
        {
            MessageBoxHelper.ShowError("Map identifier not provided. This might be an error with the retrieved game data.");
            return;
        }

        IMap? map = _maps?.FirstOrDefault(
            x => x.MapIdentifier.Equals(mapIdentifier, StringComparison.InvariantCultureIgnoreCase));

        if (map == null)
        {
            ShowInvalidMapOverlay($"Map for {mapIdentifier} was not found");
            return;
        }

        string imagePath = PathHelper.GetAbsolutePath(@"Maps\Images", map.MapImageFile);
        MapImage.Source = new BitmapImage(new Uri(imagePath));
        _currentMapData.SelectedMap = map;

        if (_currentMapData.SelectedBound?.BoundName != null)
        {
            _currentMapData.SelectedBound = map.GetBound(_currentMapData.SelectedBound.BoundName);
        }
        else
        {
            _currentMapData.SelectedBound = null;
        }

        MapViewBox.RenderTransform = new RotateTransform
        {
            Angle = map.MapRotation,
        };

        HideInvalidMapOverlay();
    }

    private void PlayerOverlayCanvas_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        OnMouseClickedOnCanvas(e);
    }

    protected void OnMouseClickedOnCanvas(MouseButtonEventArgs eventArgs)
    {
        EventHandler<MouseButtonEventArgs>? handler = MouseClickedOnCanvas;
        handler?.Invoke(this, eventArgs);
    }

    public void SetPlayerMarkerScale(double scale)
    {
        if (PlayerDot == null)
        {
            return;
        }

        scale /= 100.0;

        _playerDotScale.ScaleX = scale;
        _playerDotScale.ScaleY = scale;
    }

    public void SetPlayerMarkerOpacity(double opacity)
    {
        if (PlayerDot == null)
        {
            return;
        }

        opacity /= 100.0;
        PlayerDot.Opacity = opacity;
    }

    public void ReloadAllMarkerPositions()
    {
        if (_currentMapData.Quests != null)
        {
            SetQuestGameLocations(_currentMapData.Quests);
        }

        MapPositionData? lastReceivedPosition = _currentMapData.LastReceivedPosition;
        if (lastReceivedPosition == null)
        {
            return;
        }

        if (_currentMapData.SelectedMap != null)
        {
            TransformedPositionResult mapPositions = _currentMapData.SelectedMap.TransformPositions(
                lastReceivedPosition.XPosition,
                lastReceivedPosition.ZPosition,
                lastReceivedPosition.YPosition,
                _currentMapData.AutomaticallySwitchLevels);

            SetPlayerDotMapPosition(mapPositions.TransformedXPosition, mapPositions.TransformedZPosition, lastReceivedPosition.XRotation);

            SetAirdropLocation(_currentMapData.SelectedMap);
        }

        if (lastReceivedPosition.BotLocations != null)
        {
            SetBotGameLocations(lastReceivedPosition.BotLocations, _currentMapData.AutomaticallySwitchLevels);
        }

    }

    private void OnQuestMarkerOnDescriptionVisibilityChanged(object? sender, QuestMarkerDescriptionVisibilityChangedEventArgs @event)
    {
        if (sender == null)
        {
            return;
        }

        var selectedQuestMarker = (QuestMarker)sender;

        if (!@event.IsDescriptionVisible)
        {
            Panel.SetZIndex(selectedQuestMarker, 10);
            return;
        }

        int maxZIndex = 10;

        foreach (QuestMarker questMarkerToModify in QuestMarkers.Values)
        {
            if (questMarkerToModify.QuestData.Id != selectedQuestMarker.QuestData.Id)
            {
                if (questMarkerToModify.IsDescriptionVisible)
                {
                    int currentZIndex = Panel.GetZIndex(questMarkerToModify);
                    if (currentZIndex > maxZIndex)
                    {
                        maxZIndex = currentZIndex;
                    }
                }
                else
                {
                    Panel.SetZIndex(questMarkerToModify, 10);
                }
            }
        }

        Panel.SetZIndex(selectedQuestMarker, maxZIndex + 1);
    }

    private void CurrentMapDataOnSelectedBoundChanged(object? sender, EventArgs e)
    {
        if (_currentMapData.SelectedBound == null || _currentMapData.SelectedMap == null)
        {
            BoundRectangle.Visibility = Visibility.Collapsed;
        }
        else
        {
            BoundRectangle.Visibility = Visibility.Visible;
            SetBoundRectangleSize();
        }
    }

    private void CurrentBoundDataOnBoundDataUpdated(object? sender, EventArgs e)
    {
        BoundData? selectedBound = _currentMapData.SelectedBound;

        if (_currentMapData.SelectedBound == null || _currentMapData.SelectedMap == null)
        {
            BoundRectangle.Visibility = Visibility.Collapsed;
            return;
        }

        BoundData? bound = _currentMapData.SelectedBound;
        if (bound != null)
        {
            bound.X1 = selectedBound!.X1;
            bound.X2 = selectedBound!.X2;
            bound.Z1 = selectedBound!.Z1;
            bound.Z2 = selectedBound!.Z2;
            bound.Y1 = selectedBound!.Y1;
            bound.Y2 = selectedBound!.Y2;
            bound.XCoefficients = selectedBound!.XCoefficients;
            bound.ZCoefficients = selectedBound!.ZCoefficients;
        }

        SetBoundRectangleSize();
    }

    private void SetQuestMarkerVisibility(bool areVisible)
    {
        Visibility visibility = areVisible ? Visibility.Visible : Visibility.Hidden;

        foreach (QuestMarker questMarker in QuestMarkers.Values)
        {
            questMarker.Visibility = visibility;
        }
    }

    private void SetBotMarkerVisibility(bool areVisible)
    {
        Visibility visibility = areVisible ? Visibility.Visible : Visibility.Hidden;

        foreach (Image botMarker in BotMarkers.Values)
        {
            botMarker.Visibility = visibility;
        }
    }

    private void SetBoundRectangleSize()
    {
        BoundData bound = _currentMapData.SelectedBound!;
        IReadOnlyList<double> xCoefficients = _currentMapData.SelectedMap!.XCoefficients;
        IReadOnlyList<double> zCoefficients = _currentMapData.SelectedMap.ZCoefficients;

        double mapX1 = PolynomialHelper.CalculatePolynomialValue(bound.X1, xCoefficients);
        double mapX2 = PolynomialHelper.CalculatePolynomialValue(bound.X2, xCoefficients);

        double mapZ1 = PolynomialHelper.CalculatePolynomialValue(bound.Z1, zCoefficients);
        double mapZ2 = PolynomialHelper.CalculatePolynomialValue(bound.Z2, zCoefficients);

        BoundRectangle.Width = Math.Abs(mapX1 - mapX2);
        BoundRectangle.Height = Math.Abs(mapZ1 - mapZ2);
        Canvas.SetLeft(BoundRectangle, Math.Min(mapX1, mapX2));
        Canvas.SetTop(BoundRectangle, Math.Min(mapZ1, mapZ2));
    }

    private void CurrentMapDataOnQuestsChanged(object? sender, QuestDataReceivedEventArgs e)
    {
        Dispatcher.Invoke(() => SetQuestGameLocations(e.Quests));
    }

    private void SetQuestGameLocations(IReadOnlyList<QuestData>? quests)
    {
        if (quests == null)
        {
            ClearQuestPositions();
            return;
        }

        IMap? selectedMap = _currentMapData.SelectedMap;
        if (selectedMap == null)
        {
            return;
        }

        // Remove quests that no longer exist
        List<string> questIdsToremove =
            QuestMarkers
                .Where(existingMarker => !quests.Any(newQuestLocation => newQuestLocation.Id == existingMarker.Key))
                .Select(x => x.Key)
                .ToList();

        foreach (string questIdToRemove in questIdsToremove)
        {
            RemoveQuest(questIdToRemove);
        }

        Visibility visibility = QuestMarkersVisible ? Visibility.Visible : Visibility.Hidden;

        // We scale it so it looks the same regardless of map size
        var transformGroup = new TransformGroup();
        var scaleTransform = new ScaleTransform
        {
            ScaleX = selectedMap.MarkerScale,
            ScaleY = selectedMap.MarkerScale,
        };

        transformGroup.Children.Add(new RotateTransform { Angle = -selectedMap.MapRotation });
        transformGroup.Children.Add(scaleTransform);

        foreach (QuestData quest in quests)
        {
            TransformedPositionResult mapPositions = selectedMap.TransformPositions(
                quest.Location.X,
                quest.Location.Z,
                quest.Location.Y,
                _currentMapData.AutomaticallySwitchLevels);

            if (QuestMarkers.TryGetValue(quest.Id, out QuestMarker? questMarker))
            {
                Canvas.SetLeft(questMarker, mapPositions.TransformedXPosition - 12);
                Canvas.SetTop(questMarker, mapPositions.TransformedZPosition - 12);
                questMarker.Visibility = visibility;
            }
            else
            {
                questMarker = new QuestMarker(quest);
                questMarker.Visibility = visibility;

                questMarker.Loaded += (_, _) =>
                {
                    // We want to rotate along the middle of the checkmark button. 2d is here because we want to halve
                    // the button height or width
                    double xTransform = questMarker.QuestButton.ActualWidth / (2d * questMarker.ActualWidth);
                    double yTransform = questMarker.QuestButton.ActualHeight/ (2d * questMarker.ActualHeight);
                    questMarker.RenderTransformOrigin = new Point(xTransform, yTransform);
                    questMarker.RenderTransform = transformGroup;
                };

                WeakEventManager<QuestMarker, QuestMarkerDescriptionVisibilityChangedEventArgs>
                    .AddHandler(questMarker, nameof(questMarker.DescriptionVisibilityChanged), OnQuestMarkerOnDescriptionVisibilityChanged);

                PlayerOverlayCanvas.Children.Add(questMarker);

                Canvas.SetLeft(questMarker, mapPositions.TransformedXPosition - 12);
                Canvas.SetTop(questMarker, mapPositions.TransformedZPosition - 12);

                QuestMarkers.TryAdd(quest.Id, questMarker);
            }
        }
    }

    private void SetAirdropLocation(IMap selectedMap)
    {
        if (_lastReceivedAirdropData == null)
        {
            AirdropMarkerImage.Visibility = Visibility.Hidden;
            return;
        }

        TransformedPositionResult transformedPositions = selectedMap.TransformPositions(
            _lastReceivedAirdropData!.XPosition,
            _lastReceivedAirdropData.ZPosition,
            _lastReceivedAirdropData.YPosition,
            _currentMapData.AutomaticallySwitchLevels);

        Canvas.SetLeft(AirdropMarkerImage, transformedPositions.TransformedXPosition - 10);
        Canvas.SetTop(AirdropMarkerImage, transformedPositions.TransformedZPosition - 10);
    }

    private void ShowInvalidMapOverlay(string errorText)
    {
        InvalidMapOverlay.Text = errorText;
        InvalidMapOverlay.Visibility = Visibility.Visible;
        MapViewBox.Visibility = Visibility.Collapsed;
        PlayerDot.Visibility = Visibility.Collapsed;
        DrawingInkCanvas.Visibility = Visibility.Collapsed;
    }

    private void HideInvalidMapOverlay()
    {
        InvalidMapOverlay.Visibility = Visibility.Collapsed;
        MapViewBox.Visibility = Visibility.Visible;
        PlayerDot.Visibility = Visibility.Visible;
        DrawingInkCanvas.Visibility = Visibility.Visible;
    }

    public async Task LoadQuests()
    {
        // Only case for retry, so let's make it simple
        for (int i = 0; i < 3; i++)
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(5));

                IReadOnlyList<QuestData>? quests = await _mapDataRetriever
                    .GetQuestData(cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                if (quests == null)
                {
                    await Task.Delay(1000);
                    continue;
                }

                _currentMapData.Quests = quests;
                break;
            }
            catch
            {
            }
        }
    }

    private void CurrentMapDataOnLastReceivedPositionChanged(object? sender, EventArgs e)
    {
        if (!string.Equals(
                _currentMapData.SelectedMap?.MapIdentifier,
                _currentMapData.LastReceivedPosition?.MapIdentifier,
                StringComparison.InvariantCultureIgnoreCase))
        {
            Dispatcher.Invoke(() => LoadMap(_currentMapData.LastReceivedPosition?.MapIdentifier));
        }
    }

    private void CurrentMapDataOnGameStateChanged(object? sender, IsGameInProgressChangedEventArgs e)
    {
        Dispatcher.Invoke(ClearBotPositions);
        Dispatcher.Invoke(ClearQuestPositions);
        Dispatcher.Invoke(() => AirdropMarkerImage.Visibility = Visibility.Collapsed);
        _lastReceivedAirdropData = null;

        if (e.IsGameInProgress)
        {
            Dispatcher.Invoke(HideInvalidMapOverlay);
            _ = LoadQuests();
            return;
        }

        Dispatcher.Invoke(() => ShowInvalidMapOverlay("Waiting for a raid to start"));
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _currentMapData.BoundDataUpdated -= CurrentBoundDataOnBoundDataUpdated;
        _currentMapData.SelectedBoundChanged -= CurrentMapDataOnSelectedBoundChanged;
        _currentMapData.QuestsChanged -= CurrentMapDataOnQuestsChanged;
        _currentMapData.LastReceivedPositionChanged -= CurrentMapDataOnLastReceivedPositionChanged;
        _currentMapData.GameStateChanged -= CurrentMapDataOnGameStateChanged;
    }
}