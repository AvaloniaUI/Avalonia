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
    
    public StackPool<ServerCompositionVisual.TreeWalkerFrame> TreeWalkerFrameStackPool { get; } = new();
    public StackPool<Matrix> MatrixStackPool { get; } = new();
    public StackPool<LtrbRect> LtrbRectStackPool { get; } = new();
    public StackPool<double> DoubleStackPool { get; } = new();
    public StackPool<int> IntStackPool { get; } = new();
    public StackPool<IDirtyRectCollector> DirtyRectCollectorStackPool { get; } = new();
    
}