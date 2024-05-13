using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CactusPie.MapLocation.Minimap.Data.Enums;
using CactusPie.MapLocation.Minimap.Helpers;
using CactusPie.MapLocation.Minimap.Services.Interfaces;

namespace CactusPie.MapLocation.Minimap.Controls;

public partial class BoundButton : UserControl
{
    private readonly ICurrentMapData _currentMapData;

    private readonly string? _label;

    private readonly BoundButtonType _type;

    private bool _editMode;

    private double _value;

    public BoundButton(BoundButtonType type, ICurrentMapData currentMapData)
    {
        _type = type;
        _label = $"Set {type.ToString()}";
        _currentMapData = currentMapData;
        InitializeComponent();
    }

    public double Value
    {
        get => _value;
        set
        {
            _value = value;
            BoundButtonDoubleUpDown.Value = value;
            UpdateButtonLabel();
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        UpdateButtonLabel();

        WeakEventManager<ICurrentMapData, EventArgs>.AddHandler(
            _currentMapData,
            nameof(ICurrentMapData.SelectedBoundChanged),
            OnSelectedMapDataOnSelectedBoundChanged);
    }

    private void OnSelectedMapDataOnSelectedBoundChanged(object? sender, EventArgs args)
    {
        Value = _type switch
        {
            BoundButtonType.X1 => _currentMapData.SelectedBound?.X1 ?? 0,
            BoundButtonType.X2 => _currentMapData.SelectedBound?.X2 ?? 0,
            BoundButtonType.Z1 => _currentMapData.SelectedBound?.Z1 ?? 0,
            BoundButtonType.Z2 => _currentMapData.SelectedBound?.Z2 ?? 0,
            BoundButtonType.Y1 => _currentMapData.SelectedBound?.Y1 ?? 0,
            BoundButtonType.Y2 => _currentMapData.SelectedBound?.Y2 ?? 0,
            _ => throw new InvalidEnumArgumentException(nameof(_type), (int)_type, typeof(BoundButtonType)),
        };
    }

    private void MainButton_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentMapData.SelectedBound == null)
        {
            MessageBoxHelper.ShowError("You must selected a non-default bound first");
            return;
        }

        ToggleEditMode();
        e.Handled = true;
    }

    private void ToggleEditMode()
    {
        if (_editMode)
        {
            DisableEditMode();
        }
        else
        {
            EnableEditMode();
        }
    }

    private void EnableEditMode()
    {
        _editMode = true;
        MainButton.Visibility = Visibility.Collapsed;
        BoundButtonDoubleUpDown.Visibility = Visibility.Visible;
        BoundButtonDoubleUpDown.Focus();
        UpdateButtonLabel();
    }

    private void DisableEditMode()
    {
        SetBoundValueFromDoubleUpDownValue();
        _editMode = false;
        MainButton.Visibility = Visibility.Visible;
        BoundButtonDoubleUpDown.Visibility = Visibility.Collapsed;
        UpdateButtonLabel();
        OnBoundDataUpdated();
    }

    private void SetBoundValueFromDoubleUpDownValue()
    {
        Value = BoundButtonDoubleUpDown.Value ?? 0d;
        if (_currentMapData.SelectedBound != null)
        {
            switch (_type)
            {
                case BoundButtonType.X1:
                    _currentMapData.SelectedBound!.X1 = _value;
                    break;
                case BoundButtonType.X2:
                    _currentMapData.SelectedBound!.X2 = _value;
                    break;
                case BoundButtonType.Z1:
                    _currentMapData.SelectedBound!.Z1 = _value;
                    break;
                case BoundButtonType.Z2:
                    _currentMapData.SelectedBound!.Z2 = _value;
                    break;
                case BoundButtonType.Y1:
                    _currentMapData.SelectedBound!.Y1 = _value;
                    break;
                case BoundButtonType.Y2:
                    _currentMapData.SelectedBound!.Y2 = _value;
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(_type), (int)_type, typeof(BoundButtonType));
            }
        }
    }

    private void BoundButtonDoubleUpDown_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape)
        {
            DisableEditMode();
        }
    }

    private void BoundButtonDoubleUpDown_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void UpdateButtonLabel()
    {
        MainButton.Content = $"{_label} ({_value:F2})";
    }

    private void MainButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentMapData.SelectedBound == null)
        {
            MessageBoxHelper.ShowError("You must selected a non-default bound first");
            return;
        }

        switch (_type)
        {
            case BoundButtonType.X1:
                Value = _currentMapData.LastReceivedPosition?.XPosition ?? 0;
                _currentMapData.SelectedBound.X1 = _value;
                break;
            case BoundButtonType.X2:
                Value = _currentMapData.LastReceivedPosition?.XPosition ?? 0;
                _currentMapData.SelectedBound.X2 = _value;
                break;
            case BoundButtonType.Z1:
                Value = _currentMapData.LastReceivedPosition?.ZPosition ?? 0;
                _currentMapData.SelectedBound.Z1 = _value;
                break;
            case BoundButtonType.Z2:
                Value = _currentMapData.LastReceivedPosition?.ZPosition ?? 0;
                _currentMapData.SelectedBound.Z2 = _value;
                break;
            case BoundButtonType.Y1:
                Value = _currentMapData.LastReceivedPosition?.YPosition ?? 0;
                _currentMapData.SelectedBound.Y1 = _value;
                break;
            case BoundButtonType.Y2:
                Value = _currentMapData.LastReceivedPosition?.YPosition ?? 0;
                _currentMapData.SelectedBound.Y2 = _value;
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(_type), (int)_type, typeof(BoundButtonType));
        }

        OnBoundDataUpdated();
    }

    private void BoundButtonDoubleUpDown_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        SetBoundValueFromDoubleUpDownValue();
        OnBoundDataUpdated();
    }

    private void OnBoundDataUpdated()
    {
        if (_currentMapData.SelectedMap != null)
        {
            _currentMapData.OnBoundDataUpdated(this);
        }
    }
}