using System.Windows;
using System.Windows.Controls;

namespace CreanexDataVis.Helpers;

/// <summary>
/// Additional properties for ScrollViewer to allow MVVM
/// </summary>
public static class ScrollViewerProperties
{
    // ------------------------------
    // HorizontalOffset
    // ------------------------------
    public static readonly DependencyProperty HorizontalOffsetProperty =
        DependencyProperty.RegisterAttached(
            "HorizontalOffset",
            typeof(double),
            typeof(ScrollViewerProperties),
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
        }
    }

    // ------------------------------
    // BindableViewportWidth
    // ------------------------------
    public static readonly DependencyProperty BindableViewportWidthProperty =
        DependencyProperty.RegisterAttached(
            "BindableViewportWidth",
            typeof(double),
            typeof(ScrollViewerProperties),
            new FrameworkPropertyMetadata(0.0,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static double GetBindableViewportWidth(DependencyObject obj)
        => (double)obj.GetValue(BindableViewportWidthProperty);

    public static void SetBindableViewportWidth(DependencyObject obj, double value)
        => obj.SetValue(BindableViewportWidthProperty, value);


    // ------------------------------
    // EnableExtraBindings
    // ------------------------------
    public static readonly DependencyProperty EnableExtraBindingsProperty =
        DependencyProperty.RegisterAttached(
            "EnableExtraBindings",
            typeof(bool),
            typeof(ScrollViewerProperties),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnEnableExtraBindingsChanged));

    public static bool GetEnableExtraBindings(DependencyObject obj)
        => (bool)obj.GetValue(EnableExtraBindingsProperty);
    public static void SetEnableExtraBindings(DependencyObject obj, bool value)
        => obj.SetValue(EnableExtraBindingsProperty, value);

    private static void OnEnableExtraBindingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer sv)
        {
            if ((bool)e.NewValue)
                sv.ScrollChanged += ScrollChanged;
            else
                sv.ScrollChanged -= ScrollChanged;
        }
    }

    private static void ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        SetHorizontalOffset(sv, sv.HorizontalOffset);
        SetBindableViewportWidth(sv, sv.ViewportWidth);
    }
}