// -----------------------------------------------------------------------
// <copyright file="ControlDetailsViewModel.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Controls;
    using ReactiveUI;

    internal class ControlDetailsViewModel : ReactiveObject
    {
        public ControlDetailsViewModel(Control control)
        {
            if (control != null)
            {
                this.Properties = control.GetRegisteredProperties()
                    .Select(x => new PropertyDetails(control, x))
                    .OrderBy(x => x.Name)
                    .OrderBy(x => x.IsAttached);
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
