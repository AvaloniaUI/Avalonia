using System;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.SkiaPlatform;

namespace Avalonia.Android
{
    internal interface IInitEditorInfo
    {
        void InitEditorInfo(Func<TopLevelImpl, EditorInfo, IInputConnection> init);
    }
}
