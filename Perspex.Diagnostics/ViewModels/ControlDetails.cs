// -----------------------------------------------------------------------
// <copyright file="ControlDetails.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using ReactiveUI;

    internal class ControlDetails : ReactiveObject
    {
        public ControlDetails(IVisual visual)
        {
            PerspexObject po = visual as PerspexObject;

            if (po != null)
            {
                this.Properties = po.GetAllValues()
                    .Select(x => new PropertyDetails(x))
                    .OrderBy(x => x.Name);
            }
        }

        public IEnumerable<PropertyDetails> Properties
        {
            get;
            private set;
        }
    }
}
