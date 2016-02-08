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
    }
}
