using System;
using Android.Views;

namespace Avalonia.Android.Platform.Specific
{
    public interface IAndroidView
    {
        [Obsolete("Use TopLevel.TryGetPlatformHandle instead, which can be casted to AndroidViewControlHandle.")]
        View View { get; }
    }
}
