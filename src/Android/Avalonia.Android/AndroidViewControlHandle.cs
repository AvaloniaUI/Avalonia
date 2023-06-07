#nullable enable

using System;

using Android.Views;

using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace Avalonia.Android
{
    public class AndroidViewControlHandle : INativeControlHostDestroyableControlHandle
    {
        internal const string AndroidDescriptor = "JavaObjectHandle";

        public AndroidViewControlHandle(View view)
        {
            View = view;
        }

        public View View { get; }

        public string HandleDescriptor => AndroidDescriptor;

        IntPtr IPlatformHandle.Handle => View.Handle;

        public void Destroy()
        {
            View?.Dispose();
        }
    }
}
