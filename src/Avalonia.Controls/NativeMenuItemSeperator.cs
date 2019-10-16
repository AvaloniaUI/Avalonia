using System;

namespace Avalonia.Controls
{
    public class NativeMenuItemSeperator : NativeMenuItemBase
    {
        [Obsolete("This is a temporary hack to make our MenuItem recognize this as a separator, don't use", true)]
        public string Header => "-";
    }
}
