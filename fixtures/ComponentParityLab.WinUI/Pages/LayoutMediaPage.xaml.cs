using Microsoft.UI.Xaml.Controls;

namespace ComponentParityLab.WinUI;

public sealed partial class LayoutMediaPage : Page
{
    public LayoutMediaPage()
    {
        InitializeComponent();
        NativeControlSamples.PopulateLayoutAndMedia(
            DiagnosticSymbolIcon,
            DiagnosticXamlControlsResources,
            DiagnosticThemeDictionaries,
            DiagnosticColor,
            DiagnosticSolidColorBrush,
            DiagnosticCornerRadius,
            DiagnosticExpander,
            DiagnosticAnnotatedScrollbar,
            DiagnosticSemanticZoom,
            DiagnosticSplitView,
            DiagnosticTwoPaneView,
            DiagnosticAnimatedIcon,
            DiagnosticShapes,
            DiagnosticMediaPlayerElement,
            DiagnosticWebView2,
            DiagnosticInkControls,
            DiagnosticTitleBarCustomization,
            DiagnosticSystemBackdrop);
    }
}
