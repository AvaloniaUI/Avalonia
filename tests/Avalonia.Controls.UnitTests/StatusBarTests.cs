// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class StatusBarTests
    {
        [Fact]
        public void Logical_Children_Should_Be_StatusBarItems()
        {
            var items = new[]
            {
                new StatusBarItem { Content = "first" },
                new StatusBarItem { Content = "second" },
            };

            var target = new StatusBar
            {
                Template = new FuncControlTemplate<StatusBar>(CreateStatusBarTemplate),
                Items = items,
            };

            Assert.Equal(items, target.GetLogicalChildren());
            target.ApplyTemplate();
            Assert.Equal(items, target.GetLogicalChildren());
        }

        [Fact]
        public void StatusBar_Should_Be_Docked_To_Bottom()
        {
            var target = new StatusBar
            {
                Template = new FuncControlTemplate<StatusBar>(CreateStatusBarTemplate),
                Items = new[]
                {
                    new StatusBarItem { Content = "foo" },
                    new StatusBarItem { Content = "bar" },
                }
            };
            
            var checkType = target.ItemsPanel.Build();
            Assert.True(checkType is DockPanel);
            var dp = (DockPanel)checkType;
            Assert.Equal<Dock>(Dock.Bottom, dp.GetValue(DockPanel.DockProperty));
        }
        
        private Control CreateStatusBarTemplate(StatusBar parent)
        {
            return new StackPanel
            {
                Children =
                {
                    new StatusBar
                    {
                        Name = "PART_StatusBar",
                        Template = new FuncControlTemplate<StatusBar>(CreateStatusBarTemplate)
                    }
                }
            };
        }
    }
}
