using System;

namespace Avalonia.Controls
{
    // TODO12: change this to be a struct, remove ResourcesChangedToken
    public class ResourcesChangedEventArgs : EventArgs
    {
        public static new readonly ResourcesChangedEventArgs Empty = new ResourcesChangedEventArgs();
    }
}
