// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Xml;
using Avalonia.Controls;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class StyleTests : XamlTestBase
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

                Assert.Single(window.Styles);

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

        [Fact]
        public void Setter_Can_Set_Attached_Property()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='DockPanel.Dock' Value='Right'/>
        </Style>
    </Window.Styles>
    <TextBlock/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var textBlock = (TextBlock)window.Content;

                window.ApplyTemplate();

                Assert.Equal(Dock.Right, DockPanel.GetDock(textBlock));
            }
        }

        [Fact(Skip = "The animation system currently needs to be able to set any property on any object")]
        public void Disallows_Setting_Non_Registered_Property()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='Button.IsDefault' Value='True'/>
        </Style>
    </Window.Styles>
    <TextBlock/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var ex = Assert.Throws<XmlException>(() => loader.Load(xaml));

                Assert.Equal(
                    "Property 'Button.IsDefault' is not registered on 'Avalonia.Controls.TextBlock'.",
                    ex.InnerException.Message);
            }
        }

        [Fact]
        public void Style_Can_Use_Not_Selector()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border:not(.foo)'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo' Classes='foo bar'/>
        <Border Name='notFoo' Classes='bar'/>
    </StackPanel>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var foo = window.FindControl<Border>("foo");
                var notFoo = window.FindControl<Border>("notFoo");

                Assert.Null(foo.Background);
                Assert.Equal(Colors.Red, ((ISolidColorBrush)notFoo.Background).Color);
            }
        }
    }
}
