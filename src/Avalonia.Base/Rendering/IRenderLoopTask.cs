using System;
using System.Threading.Tasks;

using Avalonia.Metadata;

namespace Avalonia.Rendering
{
    [PrivateApi]
    public interface IRenderLoopTask
    {
        void Render();
    }
}
