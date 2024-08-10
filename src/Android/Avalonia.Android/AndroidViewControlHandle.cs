using System;

using Android.Views;

using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace Avalonia.Android
{
    public class JavaObjectPlatformHandle : PlatformHandle
    {
        internal const string JavaObjectDescriptor = "JavaObjectHandle";

        public JavaObjectPlatformHandle(Java.Lang.Object obj) : base(obj.Handle, JavaObjectDescriptor)
        {
            Object = obj;
        }

        public Java.Lang.Object Object { get; }
    }

    public class AndroidViewControlHandle : JavaObjectPlatformHandle, INativeControlHostDestroyableControlHandle
    {
        public AndroidViewControlHandle(View view) : base(view)
        {
        }

        public View View => (View)base.Object;

        public void Destroy()
        {
            View?.Dispose();
        }
    }
}
