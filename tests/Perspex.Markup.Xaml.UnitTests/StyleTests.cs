// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Reactive.Linq;
using Moq;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Data;
using Perspex.Markup.Xaml.Data;
using Perspex.Platform;
using Perspex.Styling;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests
{
    public class StyleTests
    {
        [Fact]
        public void Binding_Should_Be_Assigned_To_Setter_Value_Instead_Of_Bound()
        {
            using (PerspexLocator.EnterScope())
            {
                PerspexLocator.CurrentMutable
                    .Bind<IPclPlatformWrapper>()
                    .ToConstant(Mock.Of<IPclPlatformWrapper>());

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
            using (PerspexLocator.EnterScope())
            {
                PerspexLocator.CurrentMutable
                    .Bind<IPlatformThreadingInterface>()
                    .ToConstant(Mock.Of<IPlatformThreadingInterface>(x => 
                        x.CurrentThreadIsLoopThread == true));

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
            using (PerspexLocator.EnterScope())
            {
                PerspexLocator.CurrentMutable
                    .Bind<IPlatformThreadingInterface>()
                    .ToConstant(Mock.Of<IPlatformThreadingInterface>(x =>
                        x.CurrentThreadIsLoopThread == true));

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
