// -----------------------------------------------------------------------
// <copyright file="TabStrip.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TabStrip : ItemsControl
    {
        private static readonly ItemsPanelTemplate PanelTemplate = new ItemsPanelTemplate(
            () => new StackPanel());

        private static readonly DataTemplate TabTemplate = new DataTemplate(
            o => new TabItem { Content = o });

        static TabStrip()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(TabStrip), PanelTemplate);
            ItemTemplateProperty.OverrideDefaultValue(typeof(TabStrip), TabTemplate);
        }
    }
}
