// -----------------------------------------------------------------------
// <copyright file="AdornerTemplate.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;

    public class AdornerTemplate
    {
        private Func<Control> build;

        public AdornerTemplate(Func<Control> build)
        {
            Contract.Requires<NullReferenceException>(build != null);

            this.build = build;
        }

        public Control Build()
        {
            return this.build();
        }
    }
}
