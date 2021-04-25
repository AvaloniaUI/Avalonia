using System;
using System.Collections.Generic;
using System.Text;
using Android.Views.InputMethods;

namespace Avalonia.Android
{
    interface IInitEditorInfo
    {
        void InitEditorInfo(Action<EditorInfo> init);
    }
}
