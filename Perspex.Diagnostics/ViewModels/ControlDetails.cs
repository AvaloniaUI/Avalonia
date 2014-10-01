// -----------------------------------------------------------------------
// <copyright file="ControlDetails.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Controls;
    using ReactiveUI;

    internal class ControlDetails : ReactiveObject
    {
        public ControlDetails(IVisual visual)
        {
            PerspexObject po = visual as PerspexObject;

            if (po != null)
            {
                this.Properties = po.GetSetValues().Select(x => Tuple.Create(x.Item1.Name, x.Item2));
            }
        }

        public IEnumerable<Tuple<string, object>> Properties
        {
            get;
            private set;
        }
    }
}
