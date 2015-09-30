using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Perspex.Platform;
using Perspex.Threading;

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

        public override Size ClientSize
        {
            get { return Platform.NativeWindowImpl.ClientSize; }
            set
            {
                Resized?.Invoke(ClientSize);
                Dispatcher.UIThread.InvokeAsync(() => Resized?.Invoke(ClientSize));
            }

        }

        public void Hide() => Platform.Scene.RemoveWindow(this);

        public void SetSize(Size clientSize)
        {
            ClientSize = clientSize;
            Resized?.Invoke(clientSize);

        }
    }
}
