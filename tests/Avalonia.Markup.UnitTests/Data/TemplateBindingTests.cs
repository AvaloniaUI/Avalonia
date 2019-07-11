using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class TemplateBindingTests
    {
        [Fact]
        public void OneWay_Binding_Should_Be_Set_Up()
        {
            var source = new Button
            {
                Template = new FuncControlTemplate<Button>((parent, _) =>
                    new ContentPresenter
                    {
                        [~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty)
                    }),
            };

            source.ApplyTemplate();

            var target = (ContentPresenter)source.GetVisualChildren().Single();

            Assert.Null(target.Content);
            source.Content = "foo";
            Assert.Equal("foo", target.Content);
            source.Content = "bar";
            Assert.Equal("bar", target.Content);
        }

        [Fact]
        public void TwoWay_Binding_Should_Be_Set_Up()
        {
            var source = new Button
            {
                Template = new FuncControlTemplate<Button>((parent, _) =>
                    new ContentPresenter
                    {
                        [~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty)
                        {
                            Mode = BindingMode.TwoWay,
                        }
                    }),
            };

            source.ApplyTemplate();

            var target = (ContentPresenter)source.GetVisualChildren().Single();

            Assert.Null(target.Content);
            source.Content = "foo";
            Assert.Equal("foo", target.Content);
            target.Content = "bar";
            Assert.Equal("bar", source.Content);
        }

        [Fact]
        public void Converter_Should_Be_Used()
        {
            var source = new Button
            {
                Template = new FuncControlTemplate<Button>((parent, _) =>
                    new ContentPresenter
                    {
                        [~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty)
                        {
                            Mode = BindingMode.TwoWay,
                            Converter = new PrefixConverter(),
                            ConverterParameter = "Hello ",
                        }
                    }),
            };

            source.ApplyTemplate();

            var target = (ContentPresenter)source.GetVisualChildren().Single();

            Assert.Null(target.Content);
            source.Content = "foo";
            Assert.Equal("Hello foo", target.Content);
            target.Content = "Hello bar";
            Assert.Equal("bar", source.Content);
        }

        private class PrefixConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value != null && parameter != null)
                {
                    return parameter.ToString() + value;
                }

                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value != null && parameter != null)
                {
                    var s = value.ToString();
                    var prefix = parameter.ToString();

                    if (s.StartsWith(prefix) == true)
                    {
                        return s.Substring(prefix.Length);
                    }

                    return s;
                }

                return null;
            }
        }
    }
}
