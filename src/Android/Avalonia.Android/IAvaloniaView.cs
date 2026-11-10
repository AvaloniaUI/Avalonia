using Android.Content;
using Android.Views;
using Android.Widget;
using Avalonia.Android.Platform.SkiaPlatform;

namespace Avalonia.Android
{
    internal interface IAvaloniaView : IInitEditorInfo
    {
        Context? Context { get; }
        ViewGroup View { get; }
        TopLevelImpl TopLevelImpl { get; }
    }
}
