// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Perspex.Base.UnitTests.Collections
{
    internal class PropertyChangedTracker
    {
        public PropertyChangedTracker(INotifyPropertyChanged obj)
        {
            this.Names = new List<string>();
            obj.PropertyChanged += this.PropertyChanged;
        }

        public List<string> Names { get; private set; }

        public void Reset()
        {
            this.Names.Clear();
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.Names.Add(e.PropertyName);
        }
    }
}
