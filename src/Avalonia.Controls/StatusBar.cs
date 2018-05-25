// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;

namespace Avalonia.Controls
{
    /// <summary>
    /// An <see cref="ItemsControl"/> in which individual items can be selected.
    /// </summary>
    public class StatusBar : ItemsControl
    {
        private static readonly ITemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new DockPanel() { [DockPanel.DockProperty] = Dock.Bottom });

        static StatusBar()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(StatusBar), DefaultPanel);
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<StatusBarItem>(
                this,
                StatusBarItem.ContentProperty,
                StatusBarItem.ContentTemplateProperty);
        }
    }
}
