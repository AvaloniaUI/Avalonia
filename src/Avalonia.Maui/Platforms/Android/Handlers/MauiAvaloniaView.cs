using Android.Content;
using Avalonia.Maui.Controls;

namespace Avalonia.Maui.Platforms.Android.Handlers
{
    public class MauiAvaloniaView : Avalonia.Android.AvaloniaView
    {
        private AvaloniaView _mauiView;

        public MauiAvaloniaView(Context context, AvaloniaView mauiView) : base(context)
        {
            _mauiView = mauiView;
        }

        public void UpdateContent()
        {
            Content = _mauiView.Content;
        }
    }
}
