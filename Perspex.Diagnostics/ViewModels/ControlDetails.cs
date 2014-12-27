// -----------------------------------------------------------------------
// <copyright file="ControlDetails.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Controls;
    using ReactiveUI;

    internal class ControlDetails : ReactiveObject
    {
        public ControlDetails(Control control)
        {
            if (control != null)
            {
                this.Properties = control.GetAllValues()
                    .Select(x => new PropertyDetails(x))
                    .OrderBy(x => x.Name);
            }
        }

        public IEnumerable<string> Classes
        {
            get;
            private set;
        }

        public IEnumerable<PropertyDetails> Properties
        {
            get;
            private set;
        }
    }
}
