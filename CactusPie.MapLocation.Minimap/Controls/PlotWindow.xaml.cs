using System;
using System.Drawing;
using System.Windows;
using CactusPie.MapLocation.Minimap.Data;
using CactusPie.MapLocation.Minimap.Helpers;
using ScottPlot;
using ScottPlot.Plottable;

namespace CactusPie.MapLocation.Minimap.Controls;

public partial class PlotWindow : Window
{
    public PlotWindow()
    {
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        WpfPlot.Plot.XLabel("In-Game coordinates");
        WpfPlot.Plot.YLabel("Map coordinates");
    }

    public void RenderPlot(PlotData xPlotData, PlotData zPlotData, string boundName)
    {
        ArgumentNullException.ThrowIfNull(xPlotData.Coefficients);
        ArgumentNullException.ThrowIfNull(zPlotData.Coefficients);

        BoundNameTextBlock.Text = $"Plot for bound: {boundName}";

        //WpfPlot.Plot.AddScatterPoints(xPlotData.GameCoordinates, xPlotData.MapCoordinates, Color.Blue);
        WpfPlot.Plot.AddScatterPoints(xPlotData.GameCoordinates, xPlotData.MapCoordinates, Color.Blue, label: "X Coordinates");
        WpfPlot.Plot.AddScatterPoints(zPlotData.GameCoordinates, zPlotData.MapCoordinates, Color.Orange, label: "Z Coordinates");

        FunctionPlot xPolynomialPlot = new(d => PolynomialHelper.CalculatePolynomialValue(d, xPlotData.Coefficients))
        {
            Color = Color.Purple,
            LineWidth = 1,
            LineStyle = LineStyle.Solid,
            FillColor = Color.FromArgb(50, Color.Purple),
            Label = "X coefficient fit",
        };
        WpfPlot.Plot.Add(xPolynomialPlot);

        FunctionPlot zPolynomialPlot = new(d => PolynomialHelper.CalculatePolynomialValue(d, zPlotData.Coefficients))
        {
            Color = Color.Red,
            LineWidth = 1,
            LineStyle = LineStyle.Solid,
            FillColor = Color.FromArgb(50, Color.Red),
            Label = "Z coefficient fit",
        };
        WpfPlot.Plot.Add(zPolynomialPlot);

        WpfPlot.Plot.Legend(true, Alignment.UpperLeft);
        WpfPlot.Refresh();
    }
}