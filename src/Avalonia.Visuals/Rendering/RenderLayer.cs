using System;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class RenderLayer : IComparable<RenderLayer>
    {
        public RenderLayer(
            IRenderTargetBitmapImpl bitmap,
            Size size,
            IVisual layerRoot)
        {
            Bitmap = bitmap;
            Size = size;
            LayerRoot = layerRoot;
            Order = GetDistanceFromRenderRoot(layerRoot);
        }

        public IRenderTargetBitmapImpl Bitmap { get; }
        public Size Size { get; }
        public IVisual LayerRoot { get; }
        public int Order { get; }

        private static int GetDistanceFromRenderRoot(IVisual visual)
        {
            var root = visual as IRenderRoot;
            var result = 0;

            while (root == null)
            {
                ++result;
                visual = visual.VisualParent;

                if (visual == null)
                {
                    throw new AvaloniaInternalException("Visual is not rooted.");
                }

                root = visual as IRenderRoot;
            }

            return result;
        }

        public int CompareTo(RenderLayer other)
        {
            return Order - other.Order;
        }
    }
}
