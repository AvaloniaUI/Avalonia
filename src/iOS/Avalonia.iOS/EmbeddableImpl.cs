using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using Avalonia.Platform;
using UIKit;

namespace Avalonia.iOS
{
    class EmbeddableImpl : TopLevelImpl, IEmbeddableWindowImpl
    {
        public void SetTitle(string title)
        {
            
        }

        public IDisposable ShowDialog()
        {
            return Disposable.Empty;
        }

        public void SetSystemDecorations(bool enabled)
        {
        }

        public event Action LostFocus;
    }
}
