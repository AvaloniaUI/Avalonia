// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions
{
    public class StaticResourceExtensionTests
    {
        [Fact]
        public void StaticResource_Can_Be_Assigned_To_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void StaticResource_Can_Be_Assigned_To_Attached_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <x:Int32 x:Key='col'>5</x:Int32>
    </UserControl.Resources>

    <Border Name='border' Grid.Column='{StaticResource col}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            Assert.Equal(5, Grid.GetColumn(border));
        }

        [Fact]
        public void StaticResource_From_Style_Can_Be_Assigned_To_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
            </Style.Resources>
        </Style>
    </UserControl.Styles>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void StaticResource_From_Application_Can_Be_Assigned_To_Property_In_Window()
        {
            using (StyledWindow())
            {
                Application.Current.Resources.Add("brush", new SolidColorBrush(0xff506070));

                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{StaticResource brush}'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var border = window.FindControl<Border>("border");

                var brush = (SolidColorBrush)border.Background;
                Assert.Equal(0xff506070, brush.Color.ToUint32());
            }
        }

        [Fact]
        public void StaticResource_From_MergedDictionary_Can_Be_Assigned_To_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void StaticResource_From_MergedDictionary_In_Style_Can_Be_Assigned_To_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <ResourceDictionary>
                    <ResourceDictionary.MergedDictionaries>
                        <ResourceDictionary>
                            <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
                        </ResourceDictionary>
                    </ResourceDictionary.MergedDictionaries>
                </ResourceDictionary>
            </Style.Resources>
        </Style>
    </UserControl.Styles>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void StaticResource_From_Application_Can_Be_Assigned_To_Property_In_UserControl()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Application.Current.Resources.Add("brush", new SolidColorBrush(0xff506070));

                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

                var loader = new AvaloniaXamlLoader();
                var userControl = (UserControl)loader.Load(xaml);
                var border = userControl.FindControl<Border>("border");

                // We don't actually know where the global styles are until we attach the control
                // to a window, as Window has StylingParent set to Application.
                var window = new Window { Content = userControl };
                window.Show();

                var brush = (SolidColorBrush)border.Background;
                Assert.Equal(0xff506070, brush.Color.ToUint32());
            }
        }

        [Fact]
        public void StaticResource_Can_Be_Assigned_To_Setter()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </Window.Resources>
    <Window.Styles>
        <Style Selector='Button'>
            <Setter Property='Background' Value='{StaticResource brush}'/>
        </Style>
    </Window.Styles>
    <Button Name='button'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");
                var brush = (SolidColorBrush)button.Background;

                Assert.Equal(0xff506070, brush.Color.ToUint32());
            }
        }

        [Fact]
        public void StaticResource_From_Style_Can_Be_Assigned_To_Setter()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style>
            <Style.Resources>
                <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
            </Style.Resources>
        </Style>
        <Style Selector='Button'>
            <Setter Property='Background' Value='{StaticResource brush}'/>
        </Style>
    </Window.Styles>
    <Button Name='button'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");
                var brush = (SolidColorBrush)button.Background;

                Assert.Equal(0xff506070, brush.Color.ToUint32());
            }
        }

        [Fact]
        public void StaticResource_Can_Be_Assigned_To_Setter_In_Styles_File()
        {
            var styleXaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Styles.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </Styles.Resources>

    <Style Selector='Border'>
        <Setter Property='Background' Value='{StaticResource brush}'/>
    </Style>
</Styles>";

            using (StyledWindow(assets: ("test:style.xaml", styleXaml)))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='test:style.xaml'/>
    </Window.Styles>
    <Border Name='border'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var border = window.FindControl<Border>("border");
                var brush = (SolidColorBrush)border.Background;

                Assert.Equal(0xff506070, brush.Color.ToUint32());
            }
        }

        [Fact]
        public void StaticResource_Can_Be_Assigned_To_Resource_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <Color x:Key='color'>#ff506070</Color>
        <SolidColorBrush x:Key='brush' Color='{StaticResource color}'/>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void StaticResource_Can_Be_Assigned_To_Resource_Property_In_Styles_File()
        {
            var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Styles.Resources>
        <Color x:Key='color'>#ff506070</Color>
        <SolidColorBrush x:Key='brush' Color='{StaticResource color}'/>
    </Styles.Resources>
</Styles>";

            var loader = new AvaloniaXamlLoader();
            var styles = (Styles)loader.Load(xaml);
            var brush = (SolidColorBrush)styles.Resources["brush"];

            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact(Skip = "Not yet supported by Portable.Xaml")]
        public void StaticResource_Can_Be_Assigned_To_Property_In_ControlTemplate_In_Styles_File()
        {
            var styleXaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Styles.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </Styles.Resources>

    <Style Selector='Button'>
        <Setter Property='Template'>
            <ControlTemplate>
                <Border Name='border' Background='{StaticResource brush}'/>
            </ControlTemplate>
        </Setter>
    </Style>
</Styles>";

            using (StyledWindow(assets: ("test:style.xaml", styleXaml)))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='test:style.xaml'/>
    </Window.Styles>
    <Button Name='button'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var button = window.FindControl<Button>("button");

                window.Show();

                var border = (Border)button.GetVisualChildren().Single();
                var brush = (SolidColorBrush)border.Background;

                // To make this work we somehow need to be able to get hold of the parent ambient
                // context from Portable.Xaml. See TODO in StaticResourceExtension.
                Assert.Equal(0xff506070, brush.Color.ToUint32());
            }
        }

        [Fact]
        public void StaticResource_Can_Be_Assigned_To_ItemTemplate_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <DataTemplate x:Key='PurpleData'>
          <TextBlock Text='{Binding Name}' Background='Purple'/>
        </DataTemplate>
    </UserControl.Resources>

    <ListBox Name='listBox' ItemTemplate='{StaticResource PurpleData}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var listBox = userControl.FindControl<ListBox>("listBox");

            Assert.NotNull(listBox.ItemTemplate);
        }

        [Fact]
        public void StaticResource_Can_Be_Assigned_To_Converter()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Window.Resources>
        <local:TestValueConverter x:Key='converter' Append='bar'/>
    </Window.Resources>

    <TextBlock Name='textBlock' Text='{Binding Converter={StaticResource converter}}'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.DataContext = "foo";
                window.ApplyTemplate();

                Assert.Equal("foobar", textBlock.Text);
            }
        }

        [Fact]
        public void Control_Property_Is_Not_Updated_When_Parent_Is_Changed()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());

            userControl.Content = null;

            brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        private IDisposable StyledWindow(params (string, string)[] assets)
        {
            var services = TestServices.StyledWindow.With(
                assetLoader: new MockAssetLoader(assets),
                theme: () => new Styles
                {
                    WindowStyle(),
                });

            return UnitTestApplication.Start(services);
        }

        private Style WindowStyle()
        {
            return new Style(x => x.OfType<Window>())
            {
                Setters =
                {
                    new Setter(
                        Window.TemplateProperty,
                        new FuncControlTemplate<Window>(x =>
                            new ContentPresenter
                            {
                                Name = "PART_ContentPresenter",
                                [!ContentPresenter.ContentProperty] = x[!Window.ContentProperty],
                            }))
                }
            };
        }
    }
}
