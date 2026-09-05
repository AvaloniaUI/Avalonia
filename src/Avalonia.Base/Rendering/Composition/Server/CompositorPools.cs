using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal class CompositorPools
{
    public class StackPool<T> : Stack<Stack<T>>
    {
        public Stack<T> Rent()
        {
            if (Count > 0)
                return Pop()!;
            return new Stack<T>();
        }

        public void Return(ref Stack<T> stack)
        {
            Return(stack);
            stack = null!;
        }
        
        public void Return(Stack<T>? stack)
        {
            if (stack == null)
                return;
            
            stack.Clear();
            Push(stack);
        }
    }
    
    public class ListPool<T> : Stack<List<T>>
    {
        public List<T> Rent()
        {
            if (Count > 0)
                return Pop()!;
            return new List<T>();
        }

        public void Return(ref List<T> list)
        {
            Return(list);
            list = null!;
        }

        public void Return(List<T>? list)
        {
            if (list == null)
                return;

            list.Clear();
            Push(list);
        }
    }

    public StackPool<ServerCompositionVisual.TreeWalkerFrame> TreeWalkerFrameStackPool { get; } = new();
    public StackPool<ServerCompositionVisual> VisualStackPool { get; } = new();
    public StackPool<Matrix> MatrixStackPool { get; } = new();
    public StackPool<LtrbRect> LtrbRectStackPool { get; } = new();
    public StackPool<double> DoubleStackPool { get; } = new();
    public StackPool<int> IntStackPool { get; } = new();
    public StackPool<IDirtyRectCollector> DirtyRectCollectorStackPool { get; } = new();

    public ListPool<LtrbRect> LtrbRectListPool { get; } = new();
    public ListPool<ServerCompositionTarget.BackdropVolatilePass.HostScan> BackdropHostScanListPool { get; } = new();
    public ListPool<ServerCompositionTarget.BackdropVolatilePass.VolatileEntry> BackdropVolatileEntryListPool { get; } = new();
    public ListPool<ServerCompositionTarget.BackdropVolatilePass.RetainedEntry> BackdropRetainedEntryListPool { get; } = new();
}