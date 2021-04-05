using Android.Views;
using Avalonia.Input;

namespace Avalonia.Android.Platform.Specific
{
    public interface IAndroidView
    {
        View View { get; }


    }

    public interface IAndroidSoftInput
    {
        void ShowSoftInput(ISoftInputElement softInputElement);

        void HideSoftInput();
    }
}
