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
        [Fact(Skip = "Need to decide if this is right")]
        public void Logical_Children_Should_Be_Children_Of_Container()
        {
            var target = new DropDown();

            target.Template = this.GetTemplate();
            target.ApplyTemplate();

            var childIds = ((ILogical)target).LogicalChildren.Cast<Control>().Select(x => x.Name);

            Assert.Equal(new[] { "contentControl", "toggle", "popup" }, childIds);
        }

        private ControlTemplate GetTemplate()
        {
            return new ControlTemplate<DropDown>(parent =>
            {
                return new Panel
                {
                    Name = "container",
                    Children = new Controls
                    {
                        new ContentControl
                        {
                            Name = "contentControl",
                            [~ContentPresenter.ContentProperty] = parent[~DropDown.ContentProperty],
                        },
                        new ToggleButton
                        {
                            Name = "toggle",
                        },
                        new Popup
                        {
                            Name = "popup",
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
