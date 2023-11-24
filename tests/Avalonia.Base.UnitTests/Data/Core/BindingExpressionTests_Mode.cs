using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    [InvariantCulture]
    public class BindingExpressionTests_Mode
    {
        [Fact]
        public void OneTime_Binding_Sets_Target_Only_Once()
        {
            var data = new ViewModel();
            var binding = new Binding(nameof(data.Foo), BindingMode.OneTime);
            var target = new Control { DataContext = data };

            target.Bind(Control.TagProperty, binding);
            Assert.Equal("foo", target.Tag);

            data.Foo = "bar";
            Assert.Equal("foo", target.Tag);
        }

        [Fact]
        public void OneTime_Binding_Waits_For_DataContext()
        {
            var data = new ViewModel();
            var binding = new Binding(nameof(data.Foo), BindingMode.OneTime);
            var target = new Control();

            target.Bind(Control.TagProperty, binding);
            Assert.Null(target.Tag);

            target.DataContext = data;
            Assert.Equal("foo", target.Tag);

            data.Foo = "bar";
            Assert.Equal("foo", target.Tag);
        }

        [Fact]
        public void OneTime_Binding_Waits_For_DataContext_With_Matching_Property_Name()
        {
            var data1 = new { Baz = "baz" };
            var data2 = new ViewModel();
            var binding = new Binding(nameof(data2.Foo), BindingMode.OneTime);
            var target = new Control { DataContext = data1 };

            target.Bind(Control.TagProperty, binding);
            Assert.Null(target.Tag);

            target.DataContext = data2;
            Assert.Equal("foo", target.Tag);

            data2.Foo = "bar";
            Assert.Equal("foo", target.Tag);
        }

        [Fact]
        public void OneTime_Binding_Waits_For_DataContext_With_Matching_Property_Type()
        {
            var data1 = new { Bar = new object() };
            var data2 = new ViewModel();
            var binding = new Binding(nameof(data2.Bar), BindingMode.OneTime);
            var target = new Control { DataContext = data1 };

            target.Bind(Control.OpacityProperty, binding);
            Assert.Equal(1, target.Opacity);

            target.DataContext = data2;
            Assert.Equal(0.5, target.Opacity);

            data2.Bar = 0.2;
            Assert.Equal(0.5, target.Opacity);
        }

        private class ViewModel : NotifyingBase
        {
            private string _foo;
            private double _bar;

            public ViewModel(string foo = "foo", double bar = 0.5)
            {
                _foo = foo;
                _bar = bar;
            }

            public string Foo
            {
                get => _foo;
                set
                {
                    if (_foo != value)
                    {
                        _foo = value;
                        RaisePropertyChanged();
                    }
                }
            }

            public double Bar 
            {
                get => _bar;
                set
                {
                    if (_bar != value)
                    {
                        _bar = value;
                        RaisePropertyChanged();
                    }
                }
            }
        }
    }
}
