using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Threading;

namespace Perspex.TinyWM
{
    class InvalidationHelper
    {
        public event Action Invalidated;
        private bool _queued;

        public void Invalidate()
        {
            if(_queued)
                return;
            _queued = true;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _queued = false;
                Invalidated();
            });
        }
    }
}
