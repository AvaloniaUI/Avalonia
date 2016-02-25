// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Markup.Xaml.Data;
using Perspex.Media;
using Perspex.Styling;
using Perspex.UnitTests;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Xaml
{
    public class StyleTests
    {
        [Fact]
        public void Color_Can_Be_Added_To_Style_Resources()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = @"
<UserControl xmlns='https://github.com/perspex'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <Color x:Key='color'>#ff506070</Color>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
                var loader = new PerspexXamlLoader();
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
<UserControl xmlns='https://github.com/perspex'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
                var loader = new PerspexXamlLoader();
                var userControl = (UserControl)loader.Load(xaml);
                var brush = (SolidColorBrush)((Style)userControl.Styles[0]).Resources["brush"];

                Assert.Equal(0xff506070, brush.Color.ToUint32());
            }
        }

        [Fact]
        public void StyleResource_Can_Be_Assigned_To_Property()
        {
            var xaml = @"
<UserControl xmlns='https://github.com/perspex'
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

            var loader = new PerspexXamlLoader();
            var userControl = (UserControl)loader.Load(xaml);
            var border = userControl.FindControl<Border>("border");

            DelayedBinding.ApplyBindings(border);

            var brush = (SolidColorBrush)border.Background;
            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void StyleResource_Can_Be_Assigned_To_Setter()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/perspex'
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

                var loader = new PerspexXamlLoader();
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
<Window xmlns='https://github.com/perspex'
        xmlns:mut='https://github.com/perspex/mutable'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style>
            <Style.Resources>
                <Color x:Key='color'>#ff506070</Color>
                <mut:SolidColorBrush x:Key='brush' Color='{StyleResource color}'/>
            </Style.Resources>
        </Style>
    </Window.Styles>
    <Button Name='button' Background='{StyleResource brush}'/>
</Window>";

                var loader = new PerspexXamlLoader();
                var window = (Window)loader.Load(xaml);
                var brush = (Perspex.Media.Mutable.SolidColorBrush)window.FindStyleResource("brush");
                var button = window.FindControl<Button>("button");

                DelayedBinding.ApplyBindings(button);

                var buttonBrush = (Perspex.Media.Mutable.SolidColorBrush)button.Background;

                Assert.Equal(0xff506070, brush.Color.ToUint32());
                Assert.Equal(0xff506070, buttonBrush.Color.ToUint32());
            }
        }

        [Fact]
        public void StyleResource_Can_Be_Found_In_TopLevel_Styles()
        {
            var xaml = @"
<Styles xmlns='https://github.com/perspex'
        xmlns:mut='https://github.com/perspex/mutable'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style>
        <Style.Resources>
            <Color x:Key='color'>#ff506070</Color>
            <mut:SolidColorBrush x:Key='brush' Color='{StyleResource color}'/>
        </Style.Resources>
    </Style>
</Styles>";

            var loader = new PerspexXamlLoader();
            var styles = (Styles)loader.Load(xaml);
            var brush = (Perspex.Media.Mutable.SolidColorBrush)styles.FindResource("brush");

            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }
    }
}
