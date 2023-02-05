// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

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
