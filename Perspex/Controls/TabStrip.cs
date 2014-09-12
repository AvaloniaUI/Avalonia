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
        private static readonly DataTemplate TabTemplate = new DataTemplate(_ => true, CreateTab);

        protected override DataTemplate FindDataTemplate(object content)
        {
            return TabTemplate;
        }

        private static IVisual CreateTab(object o)
        {
            return new Tab { Content = o };
        }
    }
}
