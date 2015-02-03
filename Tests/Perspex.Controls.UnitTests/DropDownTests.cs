// -----------------------------------------------------------------------
// <copyright file="DropDownTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using Moq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Platform;
    using Perspex.Styling;
    using Perspex.VisualTree;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Splat;
    using Xunit;

    public class DropDownTests
    {
        [Fact]
        public void Logical_Children_Should_Be_Children_Of_Container()
        {
            var target = new DropDown();

            target.Template = this.GetTemplate();
            target.ApplyTemplate();

            var childIds = ((ILogical)target).LogicalChildren.Cast<Control>().Select(x => x.Id);

            Assert.Equal(new[] { "contentControl", "toggle", "popup" }, childIds);
        }

        private ControlTemplate GetTemplate()
        {
            return ControlTemplate.Create<DropDown>(parent =>
            {
                return new Panel
                {
                    Id = "container",
                    Children = new Controls
                    {
                        new ContentControl
                        {
                            Id = "contentControl",
                            [~ContentPresenter.ContentProperty] = parent[~DropDown.ContentProperty],
                        },
                        new ToggleButton
                        {
                            Id = "toggle",
                        },
                        new Popup
                        {
                            Id = "popup",
                        }
                    }
                };
            });
        }

        private IDisposable RegisterServices()
        {
            var result = Locator.CurrentMutable.WithResolver();
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var renderInterface = fixture.Create<IPlatformRenderInterface>();
            Locator.CurrentMutable.RegisterConstant(renderInterface, typeof(IPlatformRenderInterface));
            return result;
        }
    }
}
