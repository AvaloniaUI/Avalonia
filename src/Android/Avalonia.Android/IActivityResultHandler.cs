using System;
using Android.App;
using Android.Content;

namespace Avalonia.Android
{
    public interface IActivityResultHandler
    {
        public Action<int, Result, Intent> ActivityResult { get; set; }
    }
}
