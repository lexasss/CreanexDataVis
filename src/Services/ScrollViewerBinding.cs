using System.Windows;
using System.Windows.Controls;

namespace CreanexDataVis.Services;

public static class ScrollViewerBinding
{
    public static readonly DependencyProperty HorizontalOffsetProperty =
        DependencyProperty.RegisterAttached(
            "HorizontalOffset",
            typeof(double),
            typeof(ScrollViewerBinding),
            new FrameworkPropertyMetadata(0.0,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnHorizontalOffsetChanged));

    public static void SetHorizontalOffset(DependencyObject element, double value)
        => element.SetValue(HorizontalOffsetProperty, value);

    public static double GetHorizontalOffset(DependencyObject element)
        => (double)element.GetValue(HorizontalOffsetProperty);

    private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer sv)
        {
            sv.ScrollToHorizontalOffset((double)e.NewValue);
            sv.ScrollChanged -= ScrollChanged;
            sv.ScrollChanged += ScrollChanged;
        }
    }

    private static void ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        SetHorizontalOffset(sv, sv.HorizontalOffset);
    }


    public static readonly DependencyProperty BindableViewportWidthProperty =
        DependencyProperty.RegisterAttached(
            "BindableViewportWidth",
            typeof(double),
            typeof(ScrollViewerBinding),
            new FrameworkPropertyMetadata(0.0,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static double GetBindableViewportWidth(DependencyObject obj)
        => (double)obj.GetValue(BindableViewportWidthProperty);

    public static void SetBindableViewportWidth(DependencyObject obj, double value)
        => obj.SetValue(BindableViewportWidthProperty, value);


    public static readonly DependencyProperty EnableViewportWidthBindingProperty =
        DependencyProperty.RegisterAttached(
            "EnableViewportWidthBinding",
            typeof(bool),
            typeof(ScrollViewerBinding),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnEnableChanged));

    public static bool GetEnableViewportWidthBinding(DependencyObject obj)
        => (bool)obj.GetValue(EnableViewportWidthBindingProperty);
    public static void SetEnableViewportWidthBinding(DependencyObject obj, bool value)
        => obj.SetValue(EnableViewportWidthBindingProperty, value);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer sv)
        {
            if ((bool)e.NewValue)
                sv.ScrollChanged += Sv_ScrollChanged;
            else
                sv.ScrollChanged -= Sv_ScrollChanged;
        }
    }

    private static void Sv_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        SetBindableViewportWidth(sv, sv.ViewportWidth);
    }
}