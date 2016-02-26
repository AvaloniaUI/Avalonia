// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Styling;
using Perspex.UnitTests;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class UserControlTests
    {
        [Fact]
        public void Should_Be_Styled_As_UserControl()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                var target = new UserControl();
                var root = new TestRoot
                {
                    Styles = new Styles
                    {
                        new Style(x => x.OfType<ContentControl>())
                        {
                            Setters = new[]
                            {
                                new Setter(TemplatedControl.TemplateProperty, GetTemplate())
                            }
                        }
                    },
                    Child = target,
                };

                Assert.NotNull(target.Template);
            }
        }

        private FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ContentControl>(parent =>
            {
                return new Border
                {
                    Background = new Media.SolidColorBrush(0xffffffff),
                    Child = new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                    }
                };
            });
        }
    }
}
