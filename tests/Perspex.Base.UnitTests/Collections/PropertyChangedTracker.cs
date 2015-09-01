// -----------------------------------------------------------------------
// <copyright file="PropertyChangedTracker.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Base.UnitTests.Collections
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

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
