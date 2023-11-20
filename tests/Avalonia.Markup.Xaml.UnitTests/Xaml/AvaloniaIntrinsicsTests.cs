using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class AvaloniaIntrinsicsTests : XamlTestBase
{
    [Fact]
    public void All_Intrinsics_Are_Parsed_And_Set()
    {
        var xaml = @"<local:TestIntrinsicsControl 
            xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
            TimeSpanProperty='00:10:10'
            ThicknessProperty='1 1 1 1'
            PointProperty='15, 15'
            VectorProperty='16.6, 16.6'
            SizeProperty='20, 20'
            MatrixProperty='1 0 0 1 0 0'
            CornerRadiusProperty='4'
            ColorProperty='#44ff11'
            RelativePointProperty='50%, 50%'
            GridLengthProperty='10*'
            IBrushProperty='#44ff11'
            TextTrimmingProperty='CharacterEllipsis'
            TextDecorationCollectionProperty='Strikethrough'
            WindowTransparencyLevelProperty='AcrylicBlur'
            UriProperty='https://avaloniaui.net/'
            ThemeVariantProperty='Dark'
            PointsProperty='1, 1, 2, 2' />";

        var target = AvaloniaRuntimeXamlLoader.Parse<TestIntrinsicsControl>(xaml);

        Assert.NotNull(target);
        Assert.Equal(new TimeSpan(0, 10, 10), target.TimeSpanProperty);
        Assert.Equal(new Thickness(1), target.ThicknessProperty);
        Assert.Equal(new Thickness(1), target.ThicknessProperty);
        Assert.Equal(new Point(15, 15), target.PointProperty);
        Assert.Equal(new Vector(16.6, 16.6), target.VectorProperty);
        Assert.Equal(new Size(20, 20), target.SizeProperty);
        Assert.Equal(new Matrix(1, 0, 0, 1, 0, 0), target.MatrixProperty);
        Assert.Equal(new CornerRadius(4), target.CornerRadiusProperty);
        Assert.Equal(Color.Parse("#44ff11"), target.ColorProperty);
        Assert.Equal(new RelativePoint(0.5, 0.5, RelativeUnit.Relative), target.RelativePointProperty);
        Assert.Equal(new GridLength(10, GridUnitType.Star), target.GridLengthProperty);
        Assert.Equal(new ImmutableSolidColorBrush(Color.Parse("#44ff11")), target.IBrushProperty);
        Assert.Equal(TextTrimming.CharacterEllipsis, target.TextTrimmingProperty);
        Assert.Equal(TextDecorations.Strikethrough, target.TextDecorationCollectionProperty);
        Assert.Equal(WindowTransparencyLevel.AcrylicBlur, target.WindowTransparencyLevelProperty);
        Assert.Equal(new Uri("https://avaloniaui.net/"), target.UriProperty);
        Assert.Equal(ThemeVariant.Dark, target.ThemeVariantProperty);
        Assert.Equal(new[] { new Point(1, 1), new Point(2, 2) }, target.PointsProperty);
    }

    [Fact]
    public void All_Intrinsics_Report_Errors_If_Failed()
    {
        var xaml = @"<local:TestIntrinsicsControl 
            xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
            TimeSpanProperty='00:00:10,1'
            ThicknessProperty='1 1 1'
            PointProperty='15% 15%'
            VectorProperty='16.6. 16.6'
            SizeProperty='20%, 20%'
            MatrixProperty='1 0 1 0 0'
            CornerRadiusProperty='4 1 4'
            ColorProperty='#44ff1'
            RelativePointProperty='50, 50%'
            GridLengthProperty='10%'
            PointsProperty='1, 1, 2' />";
        // TODO: double check why we don't throw error on other supported types. Should it be warnings?

        var diagnostics = new List<RuntimeXamlDiagnostic>();
        Assert.Throws<AggregateException>(() => AvaloniaRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml),
            new RuntimeXamlLoaderConfiguration
            {
                DiagnosticHandler = diagnostic =>
                {
                    diagnostics.Add(diagnostic);
                    return diagnostic.Severity;
                }
            }));

        Assert.Collection(
            diagnostics,
            d => AssertDiagnostic(d, "time span"),
            d => AssertDiagnostic(d, "thickness"),
            d => AssertDiagnostic(d, "point"),
            d => AssertDiagnostic(d, "vector"),
            d => AssertDiagnostic(d, "size"),
            d => AssertDiagnostic(d, "matrix"),
            d => AssertDiagnostic(d, "corner radius"),
            d => AssertDiagnostic(d, "color"),
            d => AssertDiagnostic(d, "relative point"),
            d => AssertDiagnostic(d, "grid length"),
            d => AssertDiagnostic(d, "points list"),
            // Compiler attempts to parse PointsList twice - as a list and as a point.
            d => AssertDiagnostic(d, "point"));

        void AssertDiagnostic(RuntimeXamlDiagnostic runtimeXamlDiagnostic, string contains)
        {
            Assert.Equal(RuntimeXamlDiagnosticSeverity.Error, runtimeXamlDiagnostic.Severity);
            Assert.Contains(contains, runtimeXamlDiagnostic.Title, StringComparison.OrdinalIgnoreCase);
        }
    }
}

public class TestIntrinsicsControl : Control
{
    public TimeSpan TimeSpanProperty { get; set; }

    // public FontFamily FontFamilyProperty { get; set; }
    public Thickness ThicknessProperty { get; set; }
    public Point PointProperty { get; set; }
    public Vector VectorProperty { get; set; }
    public Size SizeProperty { get; set; }
    public Matrix MatrixProperty { get; set; }
    public CornerRadius CornerRadiusProperty { get; set; }
    public Color ColorProperty { get; set; }
    public RelativePoint RelativePointProperty { get; set; }
    public GridLength GridLengthProperty { get; set; }
    public IBrush IBrushProperty { get; set; }
    public TextTrimming TextTrimmingProperty { get; set; }
    public TextDecorationCollection TextDecorationCollectionProperty { get; set; }
    public WindowTransparencyLevel WindowTransparencyLevelProperty { get; set; }
    public Uri UriProperty { get; set; }
    public ThemeVariant ThemeVariantProperty { get; set; }
    public Points PointsProperty { get; set; }
}
