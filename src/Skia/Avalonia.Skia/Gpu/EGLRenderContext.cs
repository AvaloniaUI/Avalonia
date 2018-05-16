// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform.Gpu;
using SkiaSharp;

namespace Avalonia.Skia.Gpu
{
    public class EGLRenderContextBase : IGpuRenderContextBase
    {
        protected IEGLPlatform Platform { get; }

        public EGLRenderContextBase(IEGLPlatform platform, GRContext context)
        {
            Platform = platform ?? throw new ArgumentNullException(nameof(platform));
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public GRContext Context { get; }

        public virtual bool PrepareForRendering()
        {
            return true; // I think we don't need to set context current, need to test it.
        }
    }

    public class EGLRenderContext : EGLRenderContextBase, IGpuRenderContext
    {
        private readonly IEGLSurface _surface;
        
        public EGLRenderContext(IEGLSurface surface, IEGLPlatform platform, GRContext context)
            : base(platform, context)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        }
        
        public void Dispose()
        {
            PrepareForRendering();

            Platform.DestroySurface(_surface);
        }
       
        public FramebufferParameters GetFramebufferParameters()
        {
            return _surface.GetFramebufferParameters();
        }

        public override bool PrepareForRendering()
        {
            return Platform.MakeCurrent(_surface);
        }

        public bool Present()
        {
            return Platform.SwapBuffers(_surface);
        }

        public Size GetFramebufferSize()
        {
            var (width, height) = _surface.GetSize();

            return new Size(width, height);
        }
    }
}