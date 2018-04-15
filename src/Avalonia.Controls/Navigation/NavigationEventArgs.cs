using System;
using System.Collections.Generic;

namespace Avalonia.Controls {

    public sealed class NavigationEventArgs : EventArgs {

        public IEnumerable<object> Parameters
        {
            get;
            set;
        }

        public Type Type
        {
            get;
            set;
        }

        public NavigationMode Mode
        {
            get;
            set;
        }

    }

}