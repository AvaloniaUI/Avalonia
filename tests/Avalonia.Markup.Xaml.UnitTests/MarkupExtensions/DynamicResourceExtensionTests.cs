// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Data;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.MarkupExtensions
{
    public class DynamicResourceExtensionTests : XamlTestBase
    {
        [Fact]
        public void DynamicResource_Can_Be_Assigned_To_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void DynamicResource_Can_Be_Assigned_To_Attached_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <x:Int32 x:Key='col'>5</x:Int32>
    </UserControl.Resources>

    <Border Name='border' Grid.Column='{DynamicResource col}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Equal(5, Grid.GetColumn(border));
        }

        [Fact]
        public void DynamicResource_From_Style_Can_Be_Assigned_To_Property()
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

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void DynamicResource_From_MergedDictionary_Can_Be_Assigned_To_Property()
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

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void DynamicResource_From_MergedDictionary_In_Style_Can_Be_Assigned_To_Property()
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

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void DynamicResource_From_Application_Can_Be_Assigned_To_Property_In_Window()
        {
            using (StyledWindow())
            {
                Application.Current.Resources.Add("brush", new SolidColorBrush(0xff506070));

                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var border = window.FindControl<Border>("border");

                var brush = (SolidColorBrush)border.Background;
                Assert.Equal(0xff506070, brush.Color.ToUint32());
            }
        }

        [Fact]
        public void DynamicResource_From_Application_Can_Be_Assigned_To_Property_In_UserControl()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                Application.Current.Resources.Add("brush", new SolidColorBrush(0xff506070));

                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
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
        public void DynamicResource_Can_Be_Assigned_To_Setter()
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
            <Setter Property='Background' Value='{DynamicResource brush}'/>
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
        public void DynamicResource_From_Style_Can_Be_Assigned_To_Setter()
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
            <Setter Property='Background' Value='{DynamicResource brush}'/>
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
        public void DynamicResource_Can_Be_Assigned_To_Setter_In_Styles_File()
        {
            var styleXaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Styles.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </Styles.Resources>

    <Style Selector='Border'>
        <Setter Property='Background' Value='{DynamicResource brush}'/>
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
        public void DynamicResource_Can_Be_Assigned_To_Property_In_ControlTemplate_In_Styles_File()
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
                <Border Name='border' Background='{DynamicResource brush}'/>
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

                Assert.Equal(0xff506070, brush.Color.ToUint32());
            }
        }

        [Fact]
        public void DynamicResource_Can_Be_Assigned_To_Resource_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <Color x:Key='color'>#ff506070</Color>
        <SolidColorBrush x:Key='brush' Color='{DynamicResource color}'/>
    </UserControl.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }


        [Fact]
        public void DynamicResource_Can_Be_Assigned_To_ItemTemplate_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <DataTemplate x:Key='PurpleData'>
          <TextBlock Text='{Binding Name}' Background='Purple'/>
        </DataTemplate>
    </UserControl.Resources>

    <ListBox Name='listBox' ItemTemplate='{DynamicResource PurpleData}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var listBox = userControl.FindControl<ListBox>("listBox");

            DelayedBinding.ApplyBindings(listBox);

            Assert.NotNull(listBox.ItemTemplate);
        }

        [Fact]
        public void DynamicResource_Tracks_Added_Resource()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Null(border.Background);

            userControl.Resources.Add("brush", new SolidColorBrush(0xff506070));

            var brush = (SolidColorBrush)border.Background;
            Assert.NotNull(brush);
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void DynamicResource_Tracks_Added_Style_Resource()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Null(border.Background);

            userControl.Styles.Resources.Add("brush", new SolidColorBrush(0xff506070));

            var brush = (SolidColorBrush)border.Background;
            Assert.NotNull(brush);
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void DynamicResource_Tracks_Added_Nested_Style_Resource()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
        </Style>
    </UserControl.Styles>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Null(border.Background);

            ((Style)userControl.Styles[0]).Resources.Add("brush", new SolidColorBrush(0xff506070));

            var brush = (SolidColorBrush)border.Background;
            Assert.NotNull(brush);
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void DynamicResource_Tracks_Added_MergedResource()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Null(border.Background);

            ((IResourceDictionary)userControl.Resources.MergedDictionaries[0]).Add("brush", new SolidColorBrush(0xff506070));

            var brush = (SolidColorBrush)border.Background;
            Assert.NotNull(brush);
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void DynamicResource_Tracks_Added_MergedResource_Dictionary()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Null(border.Background);

            var dictionary = new ResourceDictionary
            {
                { "brush", new SolidColorBrush(0xff506070) },
            };

            userControl.Resources.MergedDictionaries.Add(dictionary);

            var brush = (SolidColorBrush)border.Background;
            Assert.NotNull(brush);
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void DynamicResource_Tracks_Added_Style_MergedResource_Dictionary()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
        </Style>
    </UserControl.Styles>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Null(border.Background);

            var dictionary = new ResourceDictionary
            {
                { "brush", new SolidColorBrush(0xff506070) },
            };

            ((Style)userControl.Styles[0]).Resources.MergedDictionaries.Add(dictionary);

            var brush = (SolidColorBrush)border.Background;
            Assert.NotNull(brush);
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void DynamicResource_Can_Be_Found_Across_Xaml_Style_Files()
        {
            var style1Xaml = @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Style.Resources>
    <Color x:Key='Red'>Red</Color>
  </Style.Resources>
</Style>";
            var style2Xaml = @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Style.Resources>
    <SolidColorBrush x:Key='RedBrush' Color='{DynamicResource Red}'/>
  </Style.Resources>
</Style>";
            using (StyledWindow(
                ("test:style1.xaml", style1Xaml), 
                ("test:style2.xaml", style2Xaml)))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='test:style1.xaml'/>
        <StyleInclude Source='test:style2.xaml'/>
    </Window.Styles>
    <Border Name='border' Background='{DynamicResource RedBrush}'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var border = window.FindControl<Border>("border");
                var borderBrush = (ISolidColorBrush)border.Background;

                Assert.NotNull(borderBrush);
                Assert.Equal(0xffff0000, borderBrush.Color.ToUint32());
            }
        }

        [Fact]
        public void Control_Property_Is_Updated_When_Parent_Is_Changed()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());

            userControl.Content = null;

            Assert.Null(border.Background);

            userControl.Content = border;

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
                        new FuncControlTemplate<Window>((x, scope) =>
                            new ContentPresenter
                            {
                                Name = "PART_ContentPresenter",
                                [!ContentPresenter.ContentProperty] = x[!Window.ContentProperty],
                            }.RegisterInNameScope(scope)))
                }
            };
        }
    }
}
