using System;
using System.Collections.Generic;
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (ISolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (ISolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (ISolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (ISolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var border = window.FindControl<Border>("border");

                var brush = (SolidColorBrush)border.Background;
                Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

                var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
                var border = userControl.FindControl<Border>("border");

                // We don't actually know where the global styles are until we attach the control
                // to a window, as Window has StylingParent set to Application.
                var window = new Window { Content = userControl };
                window.Show();

                var brush = (SolidColorBrush)border.Background;
                Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.FindControl<Button>("button");
                var brush = (ISolidColorBrush)button.Background;

                Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var button = window.FindControl<Button>("button");
                var brush = (ISolidColorBrush)button.Background;

                Assert.Equal(0xff506070, brush.Color.ToUInt32());
            }
        }

        [Fact]
        public void DynamicResource_Can_Be_Assigned_To_Setter_In_Styles_File()
        {
            var documents = new[]
            {
                new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Style.xaml"), @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Styles.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </Styles.Resources>
    <Style Selector='Border'>
        <Setter Property='Background' Value='{DynamicResource brush}'/>
    </Style>
</Styles>"),
                new RuntimeXamlLoaderDocument(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='avares://Tests/Style.xaml'/>
    </Window.Styles>
    <Border Name='border'/>
</Window>")
            };
            
            using (StyledWindow())
            {
                var compiled = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
                var window = Assert.IsType<Window>(compiled[1]);
                var border = window.FindControl<Border>("border");
                var brush = (ISolidColorBrush)border.Background;

                Assert.Equal(0xff506070, brush.Color.ToUInt32());
            }
        }

        [Fact]
        public void DynamicResource_Can_Be_Assigned_To_Property_In_ControlTemplate_In_Styles_File()
        {
            var documents = new[]
            {
                new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Style.xaml"), @"
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
</Styles>"),
                new RuntimeXamlLoaderDocument(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='avares://Tests/Style.xaml'/>
    </Window.Styles>
    <Button Name='button'/>
</Window>")
            };
            
            using (StyledWindow())
            {
                var compiled = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
                var window = Assert.IsType<Window>(compiled[1]);
                var button = window.FindControl<Button>("button");

                window.Show();

                var border = (Border)button.GetVisualChildren().Single();
                var brush = (ISolidColorBrush)border.Background;

                Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
        }

        [Fact]
        public void DynamicResource_Can_Be_Assigned_To_Resource_Property_In_Application()
        {
            var xaml = @"
<Application xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Application.Resources>
        <Color x:Key='color'>#ff506070</Color>
        <SolidColorBrush x:Key='brush' Color='{DynamicResource color}'/>
    </Application.Resources>
</Application>";

            var application = (Application)AvaloniaRuntimeXamlLoader.Load(xaml);
            var brush = (SolidColorBrush)application.Resources["brush"];

            Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Null(border.Background);

            userControl.Resources.Add("brush", new SolidColorBrush(0xff506070));

            var brush = (SolidColorBrush)border.Background;
            Assert.NotNull(brush);
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
        }

        [Fact]
        public void DynamicResource_Tracks_Added_Style_Resource()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Null(border.Background);

            userControl.Styles.Resources.Add("brush", new SolidColorBrush(0xff506070));

            var brush = (SolidColorBrush)border.Background;
            Assert.NotNull(brush);
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Null(border.Background);

            ((Style)userControl.Styles[0]).Resources.Add("brush", new SolidColorBrush(0xff506070));

            var brush = (SolidColorBrush)border.Background;
            Assert.NotNull(brush);
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            Assert.Null(border.Background);

            ((IResourceDictionary)userControl.Resources.MergedDictionaries[0]).Add("brush", new SolidColorBrush(0xff506070));

            var brush = (SolidColorBrush)border.Background;
            Assert.NotNull(brush);
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
        }

        [Fact]
        public void DynamicResource_Tracks_Added_MergedResource_Dictionary()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
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
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
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

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
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
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
        }

        [Fact]
        public void DynamicResource_Can_Be_Found_Across_Xaml_Style_Files()
        {
            var documents = new[]
            {
                new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Style1.xaml"), @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Style.Resources>
    <Color x:Key='Red'>Red</Color>
  </Style.Resources>
</Style>"),
                new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Style2.xaml"), @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Style.Resources>
    <SolidColorBrush x:Key='RedBrush' Color='{DynamicResource Red}'/>
  </Style.Resources>
</Style>"),
                new RuntimeXamlLoaderDocument(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='avares://Tests/Style1.xaml'/>
        <StyleInclude Source='avares://Tests/Style2.xaml'/>
    </Window.Styles>
    <Border Name='border' Background='{DynamicResource RedBrush}'/>
</Window>")
            };
            
            using (StyledWindow())
            {
                var compiled = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
                var window = Assert.IsType<Window>(compiled[2]);
                var border = window.FindControl<Border>("border");
                var borderBrush = (ISolidColorBrush)border.Background;

                Assert.NotNull(borderBrush);
                Assert.Equal(0xffff0000, borderBrush.Color.ToUInt32());
            }
        }

        [Fact]
        public void DynamicResource_Can_Be_Found_In_Nested_Style_File()
        {
            var documents = new[]
            {
                new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Style1.xaml"), @"
<Styles xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <StyleInclude Source='avares://Tests/Style2.xaml'/>
</Styles>"),
                new RuntimeXamlLoaderDocument(new Uri("avares://Tests/Style2.xaml"), @"
<Style xmlns='https://github.com/avaloniaui'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Style.Resources>
    <Color x:Key='Red'>Red</Color>
    <SolidColorBrush x:Key='RedBrush' Color='{DynamicResource Red}'/>
  </Style.Resources>
</Style>"),
                new RuntimeXamlLoaderDocument(@"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='avares://Tests/Style1.xaml'/>
    </Window.Styles>
    <Border Name='border' Background='{DynamicResource RedBrush}'/>
</Window>")
            };
            
            using (StyledWindow())
            {
                var compiled = AvaloniaRuntimeXamlLoader.LoadGroup(documents);
                var window = Assert.IsType<Window>(compiled[2]);
                var border = window.FindControl<Border>("border");
                var borderBrush = (ISolidColorBrush)border.Background;

                Assert.NotNull(borderBrush);
                Assert.Equal(0xffff0000, borderBrush.Color.ToUInt32());
            }
        }

        [Fact]
        public void Control_Property_Is_Updated_When_Parent_Is_Changed()
        {
            using (StyledWindow())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </Window.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var border = window.FindControl<Border>("border");

                DelayedBinding.ApplyBindings(border);

                var brush = (ISolidColorBrush)border.Background;
                Assert.Equal(0xff506070, brush.Color.ToUInt32());

                window.Content = null;

                Assert.Null(border.Background);

                window.Content = border;

                brush = (ISolidColorBrush)border.Background;
                Assert.Equal(0xff506070, brush.Color.ToUInt32());
            }
        }

        [Fact]
        public void Resource_With_DynamicResource_Is_Updated_When_Added_To_Parent()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush' Color='{DynamicResource color}'/>
    </UserControl.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0u, brush.Color.ToUInt32());

            brush.GetObservable(SolidColorBrush.ColorProperty).Subscribe(_ => { });

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window
                {
                    Resources =
                    {
                        { "color", Colors.Red }
                    },
                    Content = userControl,
                };

                window.Show();

                Assert.Equal(Colors.Red, brush.Color);
            }
        }

        [Fact]
        public void MergedDictionary_Resource_With_DynamicResource_Is_Updated_When_Added_To_Parent()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <SolidColorBrush x:Key='brush' Color='{DynamicResource color}'/>
                </ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0u, brush.Color.ToUInt32());

            brush.GetObservable(SolidColorBrush.ColorProperty).Subscribe(_ => { });

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window
                {
                    Resources =
                    {
                        { "color", Colors.Red }
                    },
                    Content = userControl,
                };

                window.Show();

                Assert.Equal(Colors.Red, brush.Color);
            }
        }

        [Fact]
        public void Style_Resource_With_DynamicResource_Is_Updated_When_Added_To_Parent()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <SolidColorBrush x:Key='brush' Color='{DynamicResource color}'/>
            </Style.Resources>
        </Style>
    </UserControl.Styles>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0u, brush.Color.ToUInt32());

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window
                {
                    Resources =
                    {
                        { "color", Colors.Red }
                    },
                    Content = userControl,
                };

                window.Show();

                Assert.Equal(Colors.Red, brush.Color);
            }
        }

        [Fact]
        public void Automatically_Converts_Color_To_SolidColorBrush()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <Color x:Key='color'>#ff506070</Color>
    </UserControl.Resources>

    <Border Name='border' Background='{DynamicResource color}'/>
</UserControl>";

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            var brush = (ISolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUInt32());
        }

        [Fact]
        public void Resource_In_Non_Matching_Style_Is_Not_Resolved()
        {
            using var app = StyledWindow();

            var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'>
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <local:TrackingResourceProvider/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>

    <Window.Styles>
        <Style Selector='Border.nomatch'>
            <Setter Property='Tag' Value='{DynamicResource foo}'/>
        </Style>
        <Style Selector='Border'>
            <Setter Property='Tag' Value='{DynamicResource bar}'/>
        </Style>
    </Window.Styles>

    <Border Name='border'/>
</Window>";

            var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = window.FindControl<Border>("border");

            Assert.Equal("bar", border.Tag);

            var resourceProvider = (TrackingResourceProvider)window.Resources.MergedDictionaries[0];
            Assert.Contains("bar", resourceProvider.RequestedResources);
            Assert.DoesNotContain("foo", resourceProvider.RequestedResources);
        }

        [Fact]
        public void Resource_In_Non_Active_Style_Is_Not_Resolved()
        {
            using var app = StyledWindow();

            var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.MarkupExtensions'>
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <local:TrackingResourceProvider/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>

    <Window.Styles>
        <Style Selector='Border'>
            <Setter Property='Tag' Value='{DynamicResource foo}'/>
        </Style>
        <Style Selector='Border'>
            <Setter Property='Tag' Value='{DynamicResource bar}'/>
        </Style>
    </Window.Styles>

    <Border Name='border'/>
</Window>";

            var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
            var border = window.FindControl<Border>("border");

            Assert.Equal("bar", border.Tag);

            var resourceProvider = (TrackingResourceProvider)window.Resources.MergedDictionaries[0];
            Assert.Contains("bar", resourceProvider.RequestedResources);
            Assert.DoesNotContain("foo", resourceProvider.RequestedResources);
        }

        [Fact]
        public void Can_Detach_Control_With_DynamicResource_ControlTheme_That_Contains_DynamicResource()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);
            var xaml = $@"
<Window xmlns='https://github.com/avaloniaui'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    RequestedThemeVariant='Light'>
    <Window.Resources>
        <SolidColorBrush x:Key='Blue'>Blue</SolidColorBrush>
        <ControlTheme x:Key='MyTheme' TargetType='Button'>
            <Setter Property='Background' Value='{{DynamicResource Blue}}'/>
        </ControlTheme>
    </Window.Resources>

    <Button Theme='{{DynamicResource MyTheme}}'/>
</Window>";

            var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
            var target = Assert.IsType<Button>(window.Content);

            window.Show();

            Assert.Equal(Colors.Blue, ((ISolidColorBrush)target.Background).Color);

            window.Content = null;
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

    public class TrackingResourceProvider : IResourceProvider
    {
        public IResourceHost Owner { get; private set; }
        public bool HasResources => true;
        public List<object> RequestedResources { get; } = new List<object>();

        public event EventHandler OwnerChanged { add { } remove { } }

        public void AddOwner(IResourceHost owner) => Owner = owner;
        public void RemoveOwner(IResourceHost owner) => Owner = null;

        public bool TryGetResource(object key, ThemeVariant themeVariant, out object value)
        {
            RequestedResources.Add(key);
            value = key;
            return true;
        }
    }
}
