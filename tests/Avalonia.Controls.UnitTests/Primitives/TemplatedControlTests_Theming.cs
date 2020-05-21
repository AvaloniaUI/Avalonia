using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class TemplatedControlTests_Theming
    {
        [Fact]
        public void IThemed_Theme_Returns_Default_Theme_If_Theme_Property_Unset()
        {
            var theme = CreateTheme();
            var target = new ThemedControl(theme);

            Assert.Same(theme, ((IThemed)target).Theme);
        }

        [Fact]
        public void IThemed_Theme_Returns_Theme_Property_If_Set()
        {
            var theme1 = CreateTheme();
            var theme2 = CreateTheme();
            var target = new ThemedControl(theme1) { Theme = theme2 };

            Assert.Same(theme2, ((IThemed)target).Theme);
        }

        [Fact]
        public void Theme_Is_Applied_When_Attached_To_Logical_Tree()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);
            var target = new ThemedControl();

            Assert.Null(target.Template);

            var root = new TestRoot(target);

            Assert.NotNull(target.Template);
        }

        [Fact]
        public void Theme_Is_Detached_When_Theme_Property_Changed()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);
            var target = new ThemedControl();
            var root = new TestRoot(target);

            target.Theme = CreateTheme();

            Assert.Null(target.Template);
        }

        private static ControlTheme CreateTheme()
        {
            var template = new FuncControlTemplate<ThemedControl>((o, n) =>
                new Border { Name = "PART_Border" });

            return new ControlTheme
            {
                Setters =
                {
                    new Setter(ThemedControl.TemplateProperty, template),
                }
            };
        }

        private class ThemedControl : TemplatedControl
        {
            private ControlTheme _defaultTheme;

            public ThemedControl(ControlTheme? defaultTheme = null)
            {
                _defaultTheme = defaultTheme ?? CreateTheme();
            }

            protected override IStyle GetDefaultControlTheme() => _defaultTheme;
        }
    }
}
