using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class DesignModeTests : XamlTestBase
{
    public static object? SomeStaticProperty { get; set; }

    [Fact]
    public void Design_Mode_PreviewWith_Should_Be_Ignored_Without_Design_Mode()
    {
        using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
        {
            var obj = (Control)AvaloniaRuntimeXamlLoader.Load(@"
<Button xmlns='https://github.com/avaloniaui'>
    <Design.PreviewWith>
        <Template>
            <Border />
        </Template>
    </Design.PreviewWith>
</Button>", designMode: false);
            var preview = Design.CreatePreviewWithControl(obj);
            // Should return the original control, not the preview.
            Assert.IsType<Button>(preview);
        }
    }

    [Fact]
    public void Design_Mode_PreviewWith_Works_With_Control_Template()
    {
        using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
        {
            var obj = (Control)AvaloniaRuntimeXamlLoader.Load(@"
<Button xmlns='https://github.com/avaloniaui'>
    <Design.PreviewWith>
        <Template>
            <Border>
                <Button />
            </Border>
        </Template>
    </Design.PreviewWith>
</Button>", designMode: true);
            var preview = Design.CreatePreviewWithControl(obj);
            var previewBorder = Assert.IsType<Border>(preview);
            Assert.IsType<Button>(previewBorder.Child);
        }
    }

    [Fact]
    public void Design_Mode_PreviewWith_Works_With_Style()
    {
        using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
        {
            var obj = (Style)AvaloniaRuntimeXamlLoader.Load(@"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       Selector='Border.preview-border' >
    <Design.PreviewWith>
        <Border Classes='preview-border' />
    </Design.PreviewWith>
    <Setter Property='Background' Value='Red'/>
</Style>", designMode: true);
            var preview = Design.CreatePreviewWithControl(obj);
            var previewBorder = Assert.IsType<Border>(preview);
            previewBorder.ApplyStyling();
            Assert.Equal(Colors.Red, (previewBorder.Background as ISolidColorBrush)?.Color);
        }
    }

    [Fact]
    public void Design_Mode_PreviewWith_Works_With_ResourceDictionary()
    {
        using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
        {
            var obj = (ResourceDictionary)AvaloniaRuntimeXamlLoader.Load(@"
<ResourceDictionary xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Design.PreviewWith>
        <Border Background='{DynamicResource PreviewBackground}' />
    </Design.PreviewWith>
    <SolidColorBrush x:Key='PreviewBackground' Color='Red'/>
</ResourceDictionary>", designMode: true);
            var preview = Design.CreatePreviewWithControl(obj);
            var previewBorder = Assert.IsType<Border>(preview);
            Assert.Equal(Colors.Red, (previewBorder.Background as ISolidColorBrush)?.Color);
        }
    }

    [Fact]
    public void Design_Mode_PreviewWith_Works_With_IDataTemplate()
    {
        using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
        {
            var obj = (DataTemplate)AvaloniaRuntimeXamlLoader.Load(@"
<DataTemplate xmlns='https://github.com/avaloniaui'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              x:DataType='SolidColorBrush'>
    <Design.PreviewWith>
        <ContentControl>
            <ContentControl.Content>
                <SolidColorBrush Color='Red'/>
            </ContentControl.Content>
        </ContentControl>
    </Design.PreviewWith>
    <Border Background='{Binding}' />
</DataTemplate>", designMode: true);
            var preview = Design.CreatePreviewWithControl(obj);
            var previewContentControl = Assert.IsType<ContentControl>(preview);
            previewContentControl.ApplyTemplate();
            previewContentControl.Presenter!.UpdateChild();
            var border = previewContentControl.FindDescendantOfType<Border>();
            Assert.NotNull(border);
            Assert.Equal(Colors.Red, (border.Background as ISolidColorBrush)?.Color);
        }
    }

    [Fact]
    public void Design_Mode_Properties_Should_Be_Ignored_At_Runtime_And_Set_In_Design_Mode()
    {
        using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
        {
            foreach (var designMode in new[] { true, false })
            {
                var obj = (Window)AvaloniaRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/avaloniaui' 
        xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
        xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006'
        mc:Ignorable='d'
        d:DataContext='data-context'
        d:DesignWidth='123'
        d:DesignHeight='321'>
</Window>", designMode: designMode);
                var context = Design.GetDataContext(obj);
                var width = Design.GetWidth(obj);
                var height = Design.GetHeight(obj);
                if (designMode)
                {
                    Assert.Equal("data-context", context);
                    Assert.Equal(123, width);
                    Assert.Equal(321, height);
                }
                else
                {
                    Assert.False(obj.IsSet(Design.DataContextProperty));
                    Assert.False(obj.IsSet(Design.WidthProperty));
                    Assert.False(obj.IsSet(Design.HeightProperty));
                }
            }
        }
    }

    // https://github.com/AvaloniaUI/Avalonia/issues/2570
    [Fact]
    public void Design_Mode_Throws_On_Invalid_Static_Property_Reference()
    {
        SomeStaticProperty = "123";
        var ex = Assert.ThrowsAny<Exception>(() => AvaloniaRuntimeXamlLoader
            .Load(@"
<UserControl 
    xmlns='https://github.com/avaloniaui'
    xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
    xmlns:tests='using:Avalonia.Markup.Xaml.UnitTests.Xaml'
    d:DataContext='{x:Static tests:DesignModeTests.SomeStaticPropery}'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'/>", typeof(XamlIlTests).Assembly,
                designMode: true));
        Assert.Contains("Unable to resolve ", ex.Message);
        Assert.Contains(" as static field, property, constant or enum value", ex.Message);
    }

    [Fact]
    public void Design_Mode_DataContext_Should_Be_Set()
    {
        SomeStaticProperty = "123";

        var loaded = (UserControl)AvaloniaRuntimeXamlLoader
            .Load(@"
<UserControl 
    xmlns='https://github.com/avaloniaui'
    xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
    xmlns:tests='using:Avalonia.Markup.Xaml.UnitTests.Xaml'
    d:DataContext='{x:Static tests:DesignModeTests.SomeStaticProperty}'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'/>", typeof(XamlIlTests).Assembly,
                designMode: true);
        Assert.Equal(Design.GetDataContext(loaded), SomeStaticProperty);
    }
}
