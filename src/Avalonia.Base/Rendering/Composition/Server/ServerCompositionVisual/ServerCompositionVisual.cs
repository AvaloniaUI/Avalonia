using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side <see cref="CompositionVisual"/> counterpart.
    /// Is responsible for computing the transformation matrix, for applying various visual
    /// properties before calling visual-specific drawing code and for notifying the
    /// <see cref="ServerCompositionTarget"/> for new dirty rects
    /// </summary>
    partial class ServerCompositionVisual : ServerObject
    {
        public ServerCompositionVisualCollection? Children { get; private set; } = null!;
        public ServerCompositionVisualCache? Cache { get; private set; }

        partial void OnRootChanging()
        {
            if (Root != null)
            {
                Root.RemoveVisual(this);
                OnDetachedFromRoot(Root);
            }
        }

        protected virtual void OnDetachedFromRoot(ServerCompositionTarget target)
        {
        }

        partial void OnRootChanged()
        {
            if (Root != null)
            {
                Root.AddVisual(this);
                OnAttachedToRoot(Root);
                AdornerHelper_AttachedToRoot();
            }
            Cache?.FreeResources();
        }

        protected virtual void OnAttachedToRoot(ServerCompositionTarget target)
        {
        }

        partial void OnCacheModeChanging()
        {
            CacheMode?.Unsubscribe(this);
            Cache?.FreeResources();
            Cache = null;
        }
        
        partial void OnCacheModeChanged()
        {
            Cache = CacheMode is ServerCompositionBitmapCache bitmapCache ? new ServerCompositionVisualCache(this, bitmapCache) : null;
            CacheMode?.Subscribe(this);
            OnCacheModeStateChanged();
        }

        public void OnCacheModeStateChanged()
        {
            Cache?.InvalidateProperties();
            InvalidateContent();
        }


        protected virtual void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
        {
        }

        partial void Initialize()
        {
            Children = new ServerCompositionVisualCollection(Compositor);
        }
    }
}
