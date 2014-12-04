// -----------------------------------------------------------------------
// <copyright file="ItemsPanelTemplate.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;

    public class ItemsPanelTemplate
    {
        public ItemsPanelTemplate(Func<Panel> build)
        {
            Contract.Requires<ArgumentNullException>(build != null);

            this.Build = build;
        }

        public Func<Panel> Build { get; private set; }
    }
}
