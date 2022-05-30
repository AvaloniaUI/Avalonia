using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Android
{
    internal class AvaloniaViewModel : AndroidX.Lifecycle.ViewModel
    {
        public object Content { get; set; }
    }
}
