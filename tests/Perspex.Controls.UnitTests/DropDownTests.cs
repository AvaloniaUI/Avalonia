// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Platform;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class DropDownTests
    {
        [Fact(Skip = "Need to decide if this is right")]
        public void Logical_Children_Should_Be_Children_Of_Container()
        {
            var target = new DropDown();

            target.Template = GetTemplate();
            target.ApplyTemplate();

            var childIds = ((ILogical)target).LogicalChildren.Cast<Control>().Select(x => x.Name);

            Assert.Equal(new[] { "contentControl", "toggle", "popup" }, childIds);
        }

        private FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<DropDown>(parent =>
            {
                return new Panel
                {
                    Name = "container",
                    Children = new Controls
                    {
                        new ContentControl
                        {
                            Name = "contentControl",
                            [~ContentPresenter.ContentProperty] = parent[~DropDown.SelectionBoxItemProperty],
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
            var result = PerspexLocator.EnterScope();
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var renderInterface = fixture.Create<IPlatformRenderInterface>();
            PerspexLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(renderInterface);

            return result;
        }
    }
}
