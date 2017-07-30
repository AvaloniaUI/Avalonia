// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class StyleTests
    {
        [Fact]
        public void Color_Can_Be_Added_To_Style_Resources()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <Color x:Key='color'>#ff506070</Color>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
                var loader = new AvaloniaXamlLoader();
                var userControl = (UserControl)loader.Load(xaml);
                var color = (Color)((Style)userControl.Styles[0]).Resources["color"];

                Assert.Equal(0xff506070, color.ToUint32());
            }
        }

        [Fact]
        public void SolidColorBrush_Can_Be_Added_To_Style_Resources()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
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
</UserControl>";
                var loader = new AvaloniaXamlLoader();
                var userControl = (UserControl)loader.Load(xaml);
                var brush = (SolidColorBrush)((Style)userControl.Styles[0]).Resources["brush"];

                Assert.Equal(0xff506070, brush.Color.ToUint32());
            }
        }

        [Fact]
        public void StyleResource_Can_Be_Assigned_To_Property()
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

    <Border Name='border' Background='{StyleResource brush}'/>
</UserControl>";

            var loader = new AvaloniaXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void StyleResource_Can_Be_Assigned_To_Setter()
        {
            //skip default theme and styles, they are not needed
            using (UnitTestApplication.Start(TestServices.StyledWindow
                                        .With(theme: () => new Styles())))
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
            <Setter Property='Background' Value='{StyleResource brush}'/>
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
        public void StyleResource_Can_Be_Assigned_To_StyleResource_Property()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style>
            <Style.Resources>
                <Color x:Key='color'>#ff506070</Color>
                <SolidColorBrush x:Key='brush' Color='{StyleResource color}'/>
            </Style.Resources>
        </Style>
    </Window.Styles>
    <Button Name='button' Background='{StyleResource brush}'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var brush = (ISolidColorBrush)window.FindStyleResource("brush");
                var button = window.FindControl<Button>("button");

                DelayedBinding.ApplyBindings(button);

                var buttonBrush = (ISolidColorBrush)button.Background;

                Assert.Equal(0xff506070, brush.Color.ToUint32());
                Assert.Equal(0xff506070, buttonBrush.Color.ToUint32());
            }
        }

        [Fact]
        public void StyleResource_Can_Be_Found_In_TopLevel_Styles()
        {
            var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style>
        <Style.Resources>
            <Color x:Key='color'>#ff506070</Color>
            <SolidColorBrush x:Key='brush' Color='{StyleResource color}'/>
        </Style.Resources>
    </Style>
</Styles>";

            var loader = new AvaloniaXamlLoader();
            var styles = (Styles)loader.Load(xaml);
            var brush = (ISolidColorBrush)styles.FindResource("brush");

            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void StyleResource_Can_Be_Found_In_Sibling_Styles()
        {
            var xaml = @"
<Styles xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style>
        <Style.Resources>
            <Color x:Key='color'>#ff506070</Color>
        </Style.Resources>
    </Style>
    <Style>
        <Style.Resources>
            <SolidColorBrush x:Key='brush' Color='{StyleResource color}'/>
        </Style.Resources>
    </Style>
</Styles>";

            var loader = new AvaloniaXamlLoader();
            var styles = (Styles)loader.Load(xaml);
            var brush = (ISolidColorBrush)styles.FindResource("brush");

            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact(Skip = "TODO: Issue #492")]
        public void StyleResource_Can_Be_Found_Across_Xaml_Files()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='resm:Avalonia.Markup.Xaml.UnitTests.Xaml.Style1.xaml?assembly=Avalonia.Markup.Xaml.UnitTests'/>
        <StyleInclude Source='resm:Avalonia.Markup.Xaml.UnitTests.Xaml.Style2.xaml?assembly=Avalonia.Markup.Xaml.UnitTests'/>
    </Window.Styles>
    <Border Name='border' Background='{StyleResource RedBrush}'/>
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
        public void StyleInclude_Is_Built()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow
                                              .With(theme: () => new Styles())))
            {
                var xaml = @"
<ContentControl xmlns='https://github.com/avaloniaui'>
    <ContentControl.Styles>
        <StyleInclude Source='resm:Avalonia.Markup.Xaml.UnitTests.Xaml.Style1.xaml?assembly=Avalonia.Markup.Xaml.UnitTests'/>
    </ContentControl.Styles>
</ContentControl>";

                var window = AvaloniaXamlLoader.Parse<ContentControl>(xaml);

                Assert.Equal(1, window.Styles.Count);

                var styleInclude = window.Styles[0] as StyleInclude;

                Assert.NotNull(styleInclude);
                Assert.NotNull(styleInclude.Source);
                Assert.NotNull(styleInclude.Loaded);
            }
        }

        [Fact]
        public void Setter_Can_Contain_Template()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='ContentControl'>
            <Setter Property='Content'>
                <Template>
                    <TextBlock>Hello World!</TextBlock>
                </Template>
            </Setter>
        </Style>
    </Window.Styles>

    <ContentControl Name='target'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var target = window.Find<ContentControl>("target");

                Assert.IsType<TextBlock>(target.Content);
                Assert.Equal("Hello World!", ((TextBlock)target.Content).Text);
            }
        }

        [Fact]
        public void Setter_Value_Is_Bound_Directly_If_The_Target_Type_Derives_From_ITemplate()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector=':is(Control)'>
		  <Setter Property='FocusAdorner'>
			<FocusAdornerTemplate>
			  <Rectangle Stroke='Black'
						 StrokeThickness='1'
						 StrokeDashArray='1,2'/>
			</FocusAdornerTemplate>
		  </Setter>
		</Style>
	</Window.Styles>

    <TextBlock Name='target'/>
</Window>";

                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var target = window.Find<TextBlock>("target");

                Assert.NotNull(target.FocusAdorner);
            }
        }
    }
}