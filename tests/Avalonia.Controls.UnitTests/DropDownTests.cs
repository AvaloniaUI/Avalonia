// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Platform;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Xunit;

namespace Avalonia.Controls.UnitTests
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
