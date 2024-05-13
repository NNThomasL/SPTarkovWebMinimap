using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CactusPie.MapLocation.Minimap;

public class ZoomBorder : Border
{
    private UIElement? _child;

    private Point _origin;

    private Point _start;

    public override UIElement? Child
    {
        get => base.Child;
        set
        {
            if (value != null && value != Child)
            {
                Initialize(value);
            }

            base.Child = value;
        }
    }

    public TranslateTransform GetTranslateTransform()
    {
        if (_child == null)
        {
            throw new NullReferenceException("Child cannot be null");
        }

        return (TranslateTransform)((TransformGroup)_child.RenderTransform)
            .Children.First(transform => transform is TranslateTransform);
    }

    public ScaleTransform GetScaleTransform()
    {
        if (_child == null)
        {
            throw new NullReferenceException("Child cannot be null");
        }

        return (ScaleTransform)((TransformGroup)_child.RenderTransform)
            .Children.First(transform => transform is ScaleTransform);
    }

    public void Initialize(UIElement? element)
    {
        _child = element;
        if (_child == null)
        {
            return;
        }

        var transformGroup = new TransformGroup();
        var scaleTransform = new ScaleTransform();
        transformGroup.Children.Add(scaleTransform);
        var translateTransform = new TranslateTransform();
        transformGroup.Children.Add(translateTransform);
        _child.RenderTransform = transformGroup;
        _child.RenderTransformOrigin = new Point(0.0, 0.0);
        MouseWheel += Child_MouseWheel;
        MouseLeftButtonDown += Child_MouseLeftButtonDown;
        MouseLeftButtonUp += Child_MouseLeftButtonUp;
        MouseMove += Child_MouseMove;
        PreviewMouseRightButtonDown += Child_PreviewMouseRightButtonDown;
    }

    public void Reset()
    {
        if (_child == null)
        {
            return;
        }

        // reset zoom
        ScaleTransform scaleTransform = GetScaleTransform();
        scaleTransform.ScaleX = 1.0;
        scaleTransform.ScaleY = 1.0;

        // reset pan
        TranslateTransform translateTransform = GetTranslateTransform();
        translateTransform.X = 0.0;
        translateTransform.Y = 0.0;
    }

    private void Child_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_child == null)
        {
            return;
        }

        ScaleTransform scaleTransform = GetScaleTransform();
        TranslateTransform translateTransform = GetTranslateTransform();

        double zoom = e.Delta > 0 ? .2 : -.2;

        if (!(e.Delta > 0) && (scaleTransform.ScaleX < .4 || scaleTransform.ScaleY < .4))
        {
            return;
        }

        Point relative = e.GetPosition(_child);

        double absoluteX = relative.X * scaleTransform.ScaleX + translateTransform.X;
        double absoluteY = relative.Y * scaleTransform.ScaleY + translateTransform.Y;

        scaleTransform.ScaleX += zoom;
        scaleTransform.ScaleY += zoom;

        translateTransform.X = absoluteX - relative.X * scaleTransform.ScaleX;
        translateTransform.Y = absoluteY - relative.Y * scaleTransform.ScaleY;
    }

    private void Child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_child == null)
        {
            return;
        }

        TranslateTransform translateTransform = GetTranslateTransform();
        _start = e.GetPosition(this);
        _origin = new Point(translateTransform.X, translateTransform.Y);
        Cursor = Cursors.Hand;
        _child.CaptureMouse();
    }

    private void Child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_child == null)
        {
            return;
        }

        _child.ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
    }

    private void Child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Reset();
    }

    private void Child_MouseMove(object sender, MouseEventArgs e)
    {
        if (_child is not { IsMouseCaptured: true })
        {
            return;
        }

        TranslateTransform translateTransform = GetTranslateTransform();
        Vector vector = _start - e.GetPosition(this);
        translateTransform.X = _origin.X - vector.X;
        translateTransform.Y = _origin.Y - vector.Y;
    }
}