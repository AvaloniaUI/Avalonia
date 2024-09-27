using System;
using Android.Runtime;
using Android.Views;

using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace Avalonia.Android
{
    public class AndroidViewControlHandle : PlatformHandle, INativeControlHostDestroyableControlHandle
    {
        internal static string AndroidViewDescriptor = "android.view.View"; 

        public AndroidViewControlHandle(View view) : base(view.Handle, AndroidViewDescriptor)
        {
            View = view;
        }

        public View View { get; private set; }

        public void Destroy()
        {
            View?.Dispose();
        }
    }
}
