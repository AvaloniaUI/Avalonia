using System;
using System.Threading.Tasks;

namespace Avalonia.X11.DragDrop;

internal interface IEventWaiter : IDisposable
{
    Task<XEvent?> WaitForEventAsync(Func<XEvent, bool> predicate, TimeSpan timeout);
}
