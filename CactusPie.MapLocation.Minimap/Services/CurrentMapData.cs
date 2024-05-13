using System;
using System.Collections.Generic;
using CactusPie.MapLocation.Minimap.Data;
using CactusPie.MapLocation.Minimap.Events;
using CactusPie.MapLocation.Minimap.MapHandling.Data;
using CactusPie.MapLocation.Minimap.MapHandling.Interfaces;
using CactusPie.MapLocation.Minimap.Services.Interfaces;
using TechHappy.MapLocation.Common.Requests.Data;

namespace CactusPie.MapLocation.Minimap.Services;

public class CurrentMapData : ICurrentMapData
{
    private Func<string>? _getMapCreationData;

    private bool _isGameInProgress;

    private MapPositionData? _lastReceivedPosition;

    private IReadOnlyList<QuestData>? _quests;

    private BoundData? _selectedBound;

    private IMap? _selectedMap;

    public IMap? SelectedMap
    {
        get => _selectedMap;
        set
        {
            _selectedMap = value;
            MapSelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public MapPositionData? LastReceivedPosition
    {
        get => _lastReceivedPosition;
        set
        {
            _lastReceivedPosition = value;
            LastReceivedPositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public BoundData? SelectedBound
    {
        get => _selectedBound;
        set
        {
            if (_selectedBound != value)
            {
                BeforeSelectedBoundChanged?.Invoke(this, new BeforeSelectedBoundChangedEventArgs(_selectedBound, value));
                _selectedBound = value;
                SelectedBoundChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string? CurrentMapCreationData => _getMapCreationData?.Invoke();

    public int PositionRefreshRate { get; set; } = 100;

    public bool AutomaticallySwitchLevels { get; set; } = true;

    public IReadOnlyList<QuestData>? Quests
    {
        get => _quests;
        set
        {
            _quests = value;
            OnQuestDataChanged();
        }
    }

    public bool IsGameInProgress
    {
        get => _isGameInProgress;
        set
        {
            if (value != _isGameInProgress)
            {
                _isGameInProgress = value;
                OnIsGameInProgressChanged();
            }
        }
    }

    public event EventHandler<EventArgs>? MapSelectionChanged;

    public event EventHandler<EventArgs>? LastReceivedPositionChanged;

    public event EventHandler<BeforeSelectedBoundChangedEventArgs>? BeforeSelectedBoundChanged;

    public event EventHandler<EventArgs>? SelectedBoundChanged;

    public event EventHandler<EventArgs>? BoundDataUpdated;

    public event EventHandler<QuestDataReceivedEventArgs>? QuestsChanged;

    public event EventHandler<IsGameInProgressChangedEventArgs>? GameStateChanged;

    public void OnBoundDataUpdated(object sender)
    {
        BoundDataUpdated?.Invoke(sender, EventArgs.Empty);
    }

    public void SetCurrentMapCreationDataSource(Func<string>? getMapCreationData)
    {
        _getMapCreationData = getMapCreationData;
    }

    private void OnQuestDataChanged()
    {
        EventHandler<QuestDataReceivedEventArgs>? handler = QuestsChanged;
        handler?.Invoke(this, new QuestDataReceivedEventArgs(_quests));
    }

    private void OnIsGameInProgressChanged()
    {
        EventHandler<IsGameInProgressChangedEventArgs>? handler = GameStateChanged;
        handler?.Invoke(this, new IsGameInProgressChangedEventArgs(_isGameInProgress));
    }
}