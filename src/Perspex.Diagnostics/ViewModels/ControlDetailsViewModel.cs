// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Perspex.Controls;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class ControlDetailsViewModel : ReactiveObject
    {
        public ControlDetailsViewModel(Control control)
        {
            if (control != null)
            {
                Properties = PerspexPropertyRegistry.Instance.GetRegistered(control)
                    .Select(x => new PropertyDetails(control, x))
                    .OrderBy(x => x.IsAttached)
                    .ThenBy(x => x.Name);
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
