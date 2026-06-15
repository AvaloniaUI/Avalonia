using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{

    interface IServerTreeVisitor
    {
        void PreSubgraph(ServerCompositionVisual visual, out bool visitChildren);
        void PostSubgraph(ServerCompositionVisual visual);
    }

    public record struct TreeWalkerFrame(ServerCompositionVisual Visual, int CurrentIndex);
    
    static class ServerTreeWalker<TVisitor> where TVisitor : struct, IServerTreeVisitor
    {
        public static void Walk(ref TVisitor visitor, ServerCompositionVisual root)
        {
            var stackPool = root.Compositor.Pools.TreeWalkerFrameStackPool;
            var frames = stackPool.Rent();
            try
            {
                visitor.PreSubgraph(root, out var visitChildren);

                var container = root;
                if(!visitChildren 
                   || container.Children == null 
                   || container.Children.List.Count == 0)
                {
                    visitor.PostSubgraph(root);
                    return;
                }

                int currentIndex = 0;
                
                while (true)
                {
                    if (currentIndex >= container.Children!.List.Count)
                    {
                        // Exiting "recursion"
                        
                        visitor.PostSubgraph(container);
                        
                        if(!frames.TryPop(out var frame))
                            break;
                        (container, currentIndex) = frame;
                        continue;
                    }
                    var child = container.Children.List[currentIndex];
                    visitor.PreSubgraph(child, out visitChildren);
                    if (visitChildren && child.Children!.List.Count > 0)
                    {
                        // Go deeper
                        frames.Push(new TreeWalkerFrame(container, currentIndex + 1));
                        container = child;
                        currentIndex = 0;
                        continue; // Enter "recursion"
                    }
                    
                    // Haven't entered recursion, still call PostSubgraph and go to the next sibling
                    visitor.PostSubgraph(child);
                    currentIndex++;
                }
            }
            finally
            {
                stackPool.Return(frames);
            }
        }

    }
    
    struct TreeWalkContext : IDisposable
    {
        private readonly CompositorPools _pools;
        public CompositorPools Pools => _pools;
        public Matrix Transform;
        public LtrbRect Clip;

        private Stack<Matrix> _transformStack;
        private Stack<LtrbRect> _clipStack;
        
        public TreeWalkContext(CompositorPools pools, Matrix transform, LtrbRect clip)
        {
            _pools = pools;
            Transform = transform;
            Clip = clip;
            _transformStack = pools.MatrixStackPool.Rent();
            _clipStack = pools.LtrbRectStackPool.Rent();
        }
    
        public void PushTransform(in Matrix m)
        {
            _transformStack.Push(Transform);
            Transform = m * Transform;
        }
        
        public void PushSetTransform(in Matrix m)
        {
            _transformStack.Push(Transform);
            Transform = m;
        }
    
        public void PushClip(LtrbRect rect)
        {
            _clipStack.Push(Clip);
            Clip = Clip.IntersectOrEmpty(rect);
        }

        public void ResetClip(LtrbRect rect)
        {
            _clipStack.Push(Clip);
            Clip = rect;
        }
    
        public void PopTransform()
        {
            Transform = _transformStack.Pop();
        }
    
        public void PopClip()
        {
            Clip = _clipStack.Pop();
        }

        public void Dispose()
        {
            _pools.MatrixStackPool.Return(ref _transformStack);
            _pools.LtrbRectStackPool.Return(ref _clipStack);
        }
    }
}