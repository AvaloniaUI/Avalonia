using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Perspex.Platform;

namespace Perspex.MobilePlatform
{
    class MobileWindow : MobileTopLevel, IWindowImpl
    {
        public Size MaxClientSize => ClientSize;
        public void SetTitle(string title)
        {
        }

        public void Show() => Platform.Scene.AddWindow(this);

        public IDisposable ShowDialog()
        {
            Show();
            return Disposable.Create(Hide);
        }

        public void Hide() => Platform.Scene.RemoveWindow(this);

        public void SetSize(Size clientSize)
        {
            ClientSize = clientSize;
            Resized?.Invoke(clientSize);

        }
    }
}
