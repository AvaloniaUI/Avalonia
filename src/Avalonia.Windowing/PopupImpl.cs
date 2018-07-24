using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Windowing.Bindings;

namespace Avalonia.Windowing
{
    public class PopupImpl : WindowImpl, IPopupImpl 
    {
        public PopupImpl(IWindowWrapper wrapper) : base(wrapper)
        {
        }
    }
}
