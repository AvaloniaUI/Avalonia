// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Reactive.Linq;
using Perspex.Controls;
using Perspex.Data;
using Perspex.Markup.Xaml.Data;
using Perspex.Media;
using Perspex.Styling;
using Perspex.UnitTests;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests
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
            var brush = (SolidColorBrush)border.Background;

            Assert.Equal(0xff506070, brush.Color.ToUint32());
        }

        [Fact]
        public void Binding_Should_Be_Assigned_To_Setter_Value_Instead_Of_Bound()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
            {
                var xaml = "<Style xmlns='https://github.com/perspex'><Setter Value='{Binding}'/></Style>";
                var loader = new PerspexXamlLoader();
                var style = (Style)loader.Load(xaml);
                var setter = (Setter)(style.Setters.First());

                Assert.IsType<Binding>(setter.Value);
            }                
        }

        [Fact]
        public void Setter_With_TwoWay_Binding_Should_Update_Source()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var data = new Data
                {
                    Foo = "foo",
                };

                var control = new TextBox
                {
                    DataContext = data,
                };

                var setter = new Setter
                {
                    Property = TextBox.TextProperty,
                    Value = new Binding
                    {
                        Path = "Foo",
                        Mode = BindingMode.TwoWay
                    }
                };

                setter.Apply(null, control, null);
                Assert.Equal("foo", control.Text);

                control.Text = "bar";
                Assert.Equal("bar", data.Foo);
            }
        }

        [Fact]
        public void Setter_With_TwoWay_Binding_And_Activator_Should_Update_Source()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var data = new Data
                {
                    Foo = "foo",
                };

                var control = new TextBox
                {
                    DataContext = data,
                };

                var setter = new Setter
                {
                    Property = TextBox.TextProperty,
                    Value = new Binding
                    {
                        Path = "Foo",
                        Mode = BindingMode.TwoWay
                    }
                };

                var activator = Observable.Never<bool>().StartWith(true);

                setter.Apply(null, control, activator);
                Assert.Equal("foo", control.Text);

                control.Text = "bar";
                Assert.Equal("bar", data.Foo);
            }
        }

        private class Data
        {
            public string Foo { get; set; }
        }
    }
}
