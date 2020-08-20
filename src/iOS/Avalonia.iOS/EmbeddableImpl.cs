using System;
using System.Reactive.Disposables;
using Avalonia.Platform;

namespace Avalonia.iOS
{
    class EmbeddableImpl : TopLevelImpl
    {
        public void SetTitle(string title)
        {
            
        }

        public void SetMinMaxSize(Size minSize, Size maxSize)
        {
        }

        public IDisposable ShowDialog()
        {
            return Disposable.Empty;
        }

        public void SetSystemDecorations(SystemDecorations enabled)
        {
        }
    }
}
