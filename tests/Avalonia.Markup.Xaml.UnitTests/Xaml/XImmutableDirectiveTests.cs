using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class XImmutableDirectiveTests : XamlTestBase
{
    [Fact]
    public void Literal_SolidColorBrush_Is_Created_As_Immutable_Without_Deferral()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <SolidColorBrush x:Key="Brush" x:Immutable="True" Color="#80FF0000" Opacity="0.5" />
                <SolidColorBrush x:Key="ColorOnly" x:Immutable="True" Color="Blue" />
            </ResourceDictionary>
            """);

        // Enumerator returns raw values, without resolving deferred content.
        var brush = Assert.IsType<ImmutableSolidColorBrush>(resources.Single(r => "Brush".Equals(r.Key)).Value);
        Assert.Equal(Color.Parse("#80FF0000"), brush.Color);
        Assert.Equal(0.5, brush.Opacity);
        Assert.Null(brush.Transform);
        Assert.Null(brush.RelativeTransform);

        var colorOnly = Assert.IsType<ImmutableSolidColorBrush>(resources.Single(r => "ColorOnly".Equals(r.Key)).Value);
        Assert.Equal(Colors.Blue, colorOnly.Color);
        Assert.Equal(1, colorOnly.Opacity);
    }

    [Fact]
    public void Text_Content_SolidColorBrush_Is_Created_As_Immutable_Without_Deferral()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <SolidColorBrush x:Key="Brush" x:Immutable="True" Opacity="0.5">Red</SolidColorBrush>
            </ResourceDictionary>
            """);

        var brush = Assert.IsType<ImmutableSolidColorBrush>(resources.Single(r => "Brush".Equals(r.Key)).Value);
        Assert.Equal(Colors.Red, brush.Color);
        Assert.Equal(0.5, brush.Opacity);
    }

    [Fact]
    public void Literal_SolidColorBrush_Respects_Explicit_XShared_True()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <SolidColorBrush x:Key="Brush" x:Immutable="True" x:Shared="True" Color="{StaticResource Accent}" />
                <Color x:Key="Accent">Red</Color>
            </ResourceDictionary>
            """);

        Assert.Same(resources["Brush"], resources["Brush"]);
    }

    [Fact]
    public void Literal_SolidColorBrush_Respects_XShared()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <SolidColorBrush x:Key="Brush" x:Immutable="True" x:Shared="False" Color="Red" />
            </ResourceDictionary>
            """);

        var brush1 = Assert.IsType<ImmutableSolidColorBrush>(resources["Brush"]);
        var brush2 = Assert.IsType<ImmutableSolidColorBrush>(resources["Brush"]);
        Assert.NotSame(brush1, brush2);
        Assert.Equal(Colors.Red, brush1.Color);
    }

    [Fact]
    public void SolidColorBrush_With_StaticResource_Is_Deferred()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Color x:Key="Accent">Green</Color>
                <SolidColorBrush x:Key="Brush" x:Immutable="True" Color="{StaticResource Accent}" Opacity="0.25" />
            </ResourceDictionary>
            """);

        // StaticResource is resolved lazily.
        Assert.IsAssignableFrom<IDeferredContent>(resources.Single(r => "Brush".Equals(r.Key)).Value);

        var brush = Assert.IsType<ImmutableSolidColorBrush>(resources["Brush"]);
        Assert.Equal(Colors.Green, brush.Color);
        Assert.Equal(0.25, brush.Opacity);
    }

    [Fact]
    public void SolidColorBrush_With_Transform_Is_Created_As_Immutable_Without_Deferral()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <SolidColorBrush x:Key="Brush" x:Immutable="True" Color="Red">
                    <SolidColorBrush.Transform>
                        <RotateTransform Angle="45" />
                    </SolidColorBrush.Transform>
                </SolidColorBrush>
            </ResourceDictionary>
            """);

        var brush = Assert.IsType<ImmutableSolidColorBrush>(resources.Single(r => "Brush".Equals(r.Key)).Value);
        Assert.Equal(Colors.Red, brush.Color);
        Assert.Equal(Matrix.CreateRotation(Matrix.ToRadians(45)), Assert.IsType<ImmutableTransform>(brush.Transform).Value);
    }

    [Fact]
    public void SolidColorBrush_With_TransformOrigin_Uses_ToImmutable()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <SolidColorBrush x:Key="Brush" x:Immutable="True" Color="Red" TransformOrigin="50%,50%" />
            </ResourceDictionary>
            """);

        Assert.IsAssignableFrom<IDeferredContent>(resources.Single(r => "Brush".Equals(r.Key)).Value);
        var brush = Assert.IsType<ImmutableSolidColorBrush>(resources["Brush"]);
        Assert.Equal(Colors.Red, brush.Color);
    }

    [Fact]
    public void LinearGradientBrush_Uses_ToImmutable()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <LinearGradientBrush x:Key="Brush" x:Immutable="True" StartPoint="0%,0%" EndPoint="100%,0%">
                    <GradientStop Offset="0" Color="Red" />
                    <GradientStop Offset="1" Color="Blue" />
                </LinearGradientBrush>
            </ResourceDictionary>
            """);

        // ToImmutable is called on a mutable brush, which is deferred as any other resource.
        Assert.IsAssignableFrom<IDeferredContent>(resources.Single(r => "Brush".Equals(r.Key)).Value);

        var brush = Assert.IsType<ImmutableLinearGradientBrush>(resources["Brush"]);
        Assert.Equal(new RelativePoint(0, 0, RelativeUnit.Relative), brush.StartPoint);
        Assert.Equal(new RelativePoint(1, 0, RelativeUnit.Relative), brush.EndPoint);
        Assert.Equal(1, brush.Opacity);
        Assert.Equal(GradientSpreadMethod.Pad, brush.SpreadMethod);
        Assert.Equal(RelativePoint.TopLeft, brush.TransformOrigin);
        Assert.Null(brush.Transform);
        Assert.Equal(2, brush.GradientStops.Count);
        Assert.Equal(0, brush.GradientStops[0].Offset);
        Assert.Equal(Colors.Red, brush.GradientStops[0].Color);
        Assert.Equal(1, brush.GradientStops[1].Offset);
        Assert.Equal(Colors.Blue, brush.GradientStops[1].Color);
    }

    [Fact]
    public void LinearGradientBrush_Defaults_Match_Mutable_Brush()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <LinearGradientBrush x:Key="Brush" x:Immutable="True">
                    <GradientStop />
                </LinearGradientBrush>
            </ResourceDictionary>
            """);

        var brush = Assert.IsType<ImmutableLinearGradientBrush>(resources["Brush"]);
        var mutable = new LinearGradientBrush { GradientStops = { new GradientStop() } };
        Assert.Equal(mutable.StartPoint, brush.StartPoint);
        Assert.Equal(mutable.EndPoint, brush.EndPoint);
        Assert.Equal(mutable.Opacity, brush.Opacity);
        Assert.Equal(mutable.SpreadMethod, brush.SpreadMethod);
        Assert.Equal(mutable.GradientStops[0].Offset, brush.GradientStops[0].Offset);
        Assert.Equal(mutable.GradientStops[0].Color, brush.GradientStops[0].Color);
    }

    [Fact]
    public void LinearGradientBrush_With_StaticResource_Stops_And_Transform_Uses_ToImmutable()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Color x:Key="First">Red</Color>
                <Color x:Key="Second">Blue</Color>
                <LinearGradientBrush x:Key="Brush" x:Immutable="True" StartPoint="0,0" EndPoint="0,3"
                                     TransformOrigin="50%,50%" SpreadMethod="Reflect" Opacity="0.5">
                    <LinearGradientBrush.Transform>
                        <ScaleTransform ScaleY="-1" />
                    </LinearGradientBrush.Transform>
                    <LinearGradientBrush.GradientStops>
                        <GradientStop Offset="0.33" Color="{StaticResource First}" />
                        <GradientStop Offset="1.0" Color="{StaticResource Second}" />
                    </LinearGradientBrush.GradientStops>
                </LinearGradientBrush>
            </ResourceDictionary>
            """);

        Assert.IsAssignableFrom<IDeferredContent>(resources.Single(r => "Brush".Equals(r.Key)).Value);

        var brush = Assert.IsType<ImmutableLinearGradientBrush>(resources["Brush"]);
        Assert.Equal(new RelativePoint(0, 0, RelativeUnit.Absolute), brush.StartPoint);
        Assert.Equal(new RelativePoint(0, 3, RelativeUnit.Absolute), brush.EndPoint);
        Assert.Equal(new RelativePoint(0.5, 0.5, RelativeUnit.Relative), brush.TransformOrigin);
        Assert.Equal(GradientSpreadMethod.Reflect, brush.SpreadMethod);
        Assert.Equal(0.5, brush.Opacity);
        Assert.Equal(Matrix.CreateScale(1, -1), Assert.IsType<ImmutableTransform>(brush.Transform).Value);
        Assert.Equal(0.33, brush.GradientStops[0].Offset);
        Assert.Equal(Colors.Red, brush.GradientStops[0].Color);
        Assert.Equal(Colors.Blue, brush.GradientStops[1].Color);
    }

    [Fact]
    public void Pen_Uses_ToImmutable()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Pen x:Key="Pen" x:Immutable="True" Brush="Red" Thickness="2">
                    <Pen.DashStyle>
                        <DashStyle Dashes="2,2" />
                    </Pen.DashStyle>
                </Pen>
            </ResourceDictionary>
            """);

        var pen = Assert.IsType<ImmutablePen>(resources["Pen"]);
        Assert.Equal(2, pen.Thickness);
        Assert.IsType<ImmutableDashStyle>(pen.DashStyle);
    }

    [Fact]
    public void Inline_Property_Value_Is_Immutable()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var border = (Border)AvaloniaRuntimeXamlLoader.Load("""
                <Border xmlns="https://github.com/avaloniaui"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    <Border.Background>
                        <SolidColorBrush x:Immutable="True" Color="Red" />
                    </Border.Background>
                    <Border.BorderBrush>
                        <SolidColorBrush x:Immutable="True" Color="Red">
                            <SolidColorBrush.Transform>
                                <RotateTransform Angle="45" />
                            </SolidColorBrush.Transform>
                        </SolidColorBrush>
                    </Border.BorderBrush>
                </Border>
                """);

            Assert.IsType<ImmutableSolidColorBrush>(border.Background);
            Assert.IsType<ImmutableSolidColorBrush>(border.BorderBrush);
        }
    }

    [Fact]
    public void False_Keeps_Mutable_Object()
    {
        var resources = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <SolidColorBrush x:Key="Brush" x:Immutable="False" Color="Red" />
            </ResourceDictionary>
            """);

        Assert.IsType<SolidColorBrush>(resources["Brush"]);
    }

    [Fact]
    public void DynamicResource_Is_Not_Allowed()
    {
        AssertThrows(() => AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <SolidColorBrush x:Key="Brush" x:Immutable="True" Color="{DynamicResource Accent}" />
            </ResourceDictionary>
            """), "dynamic resources");
    }

    [Fact]
    public void Unsupported_Type_Is_Not_Allowed()
    {
        AssertThrows(() => AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <Button x:Key="Button" x:Immutable="True" />
            </ResourceDictionary>
            """), "not supported");

        AssertThrows(() => AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                x:Immutable="True" />
            """), "not supported");
    }

    [Fact]
    public void Non_Literal_Value_Is_Not_Allowed()
    {
        AssertThrows(() => AvaloniaRuntimeXamlLoader.Load("""
            <ResourceDictionary xmlns="https://github.com/avaloniaui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <SolidColorBrush x:Key="Brush" x:Immutable="{x:Null}" Color="Red" />
            </ResourceDictionary>
            """), "literal boolean");
    }

    private static void AssertThrows(System.Action action, string message)
    {
        var ex = Assert.ThrowsAny<System.Xml.XmlException>(action);
        Assert.Contains(message, ex.Message);
    }
}
