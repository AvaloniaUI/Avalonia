using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Avalonia.Base.UnitTests.Collections
{
    internal class PropertyChangedTracker
    {
        public PropertyChangedTracker(INotifyPropertyChanged obj)
        {
            Names = new List<string>();
            obj.PropertyChanged += PropertyChanged;
        }

        public List<string> Names { get; }

        public void Reset()
        {
            Names.Clear();
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Names.Add(e.PropertyName);
        }
    }
}
