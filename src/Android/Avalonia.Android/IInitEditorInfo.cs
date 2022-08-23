using System;
using System.Collections.Generic;
using System.Text;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.SkiaPlatform;

namespace Avalonia.Android
{
    interface IInitEditorInfo
    {
        void InitEditorInfo(Func<View, EditorInfo, InputConnectionImpl> init);
    }
}
