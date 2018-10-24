// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ControlDetailsViewModel : ViewModelBase, IDisposable
    {
        public ControlDetailsViewModel(IVisual control)
        {
            if (control is AvaloniaObject avaloniaObject)
            {
                Properties = AvaloniaPropertyRegistry.Instance.GetRegistered(avaloniaObject)
                    .Select(x => new PropertyDetails(avaloniaObject, x))
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

        public void Dispose()
        {
           foreach(var d in Properties)
            {
                d.Dispose();
            }
        }
    }
}
