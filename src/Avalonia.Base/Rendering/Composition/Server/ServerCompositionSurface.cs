using System;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server
{
    internal abstract partial class ServerCompositionSurface : ServerObject
    {
        protected ServerCompositionSurface(ServerCompositor compositor) : base(compositor)
        {
        }
        
        public abstract IRef<IBitmapImpl>? Bitmap { get; }
        public Action? Changed { get; set; }
    }
}
