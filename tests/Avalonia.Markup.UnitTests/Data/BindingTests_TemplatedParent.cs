using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_TemplatedParent
    {
        [Fact]
        public void OneWay_Binding_Should_Be_Set_Up()
        {
            var source = new Button
            {
                Template = new FuncControlTemplate<Button>((parent, _) =>
                    new ContentPresenter
                    {
                        [~ContentPresenter.ContentProperty] = new Binding
                        {
                            Mode = BindingMode.OneWay,
                            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                            Path = "Content",
                        }
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
                        [~ContentPresenter.ContentProperty] = new Binding
                        {
                            Mode = BindingMode.TwoWay,
                            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                            Path = "Content",
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
    }
}
