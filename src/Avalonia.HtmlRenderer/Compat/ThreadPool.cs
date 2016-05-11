using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.Core.Handlers;

namespace System.Threading
{
    class ThreadPool
    {
        public static void QueueUserWorkItem(Action<object> cb, object state)
        {
            Task.Factory.StartNew(() => cb(state));
        }

        public static void QueueUserWorkItem(Action<object> cb)
        {
            Task.Factory.StartNew(() => cb(null));
        }
    }
}
