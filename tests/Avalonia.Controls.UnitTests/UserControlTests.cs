using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class UserControlTests
    {
        [Fact]
        public void Should_Be_Styled_As_UserControl()
        {
            var target = new UserControl();
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<UserControl>())
                    {
                        Setters =
                        {
                            new Setter(TemplatedControl.TemplateProperty, GetTemplate())
                        }
                    }
                },
                Child = target,
            };

            Assert.NotNull(target.Template);
        }

        private static FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<UserControl>((parent, scope) =>
            {
                return new Border
                {
                    Background = new Media.SolidColorBrush(0xffffffff),
                    Child = new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                    }.RegisterInNameScope(scope)
                };
            });
        }
    }
}
