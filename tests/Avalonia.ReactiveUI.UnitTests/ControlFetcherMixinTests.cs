using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Xunit;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class ControlFetcherMixinTests
    {
        public class SampleView : UserControl
        {
            public TextBlock FooText { get; set; }

            public TextBlock BarText { get; set; }

            public SampleView()
            {
                InitializeComponent();
                this.WireUpControls();
            }

            private void InitializeComponent()
            {
                var scope = new NameScope();
                NameScope.SetNameScope(this, scope);
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Name = "FooText",
                            Text = "SecretFoo"
                        }.RegisterInNameScope(scope),
                        new TextBlock
                        {
                            Name = "BarText",
                            Text = "SecretBar"
                        }.RegisterInNameScope(scope)
                    }
                };
            }
        }

        [Fact]
        public void Should_Wire_Up_Controls_With_Properties()
        {
            var view = new SampleView();

            Assert.NotNull(view.FindControl<TextBlock>("FooText"));
            Assert.NotNull(view.FindControl<TextBlock>("BarText"));

            Assert.NotNull(view.FooText);
            Assert.NotNull(view.BarText);

            Assert.Equal("SecretFoo", view.FooText.Text);
            Assert.Equal("SecretBar", view.BarText.Text);
        }
    }
}
