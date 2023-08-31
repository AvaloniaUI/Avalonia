using System;
using System.Threading.Tasks;

using Avalonia.Metadata;

namespace Avalonia.Rendering
{
    [PrivateApi]
    internal interface IRenderLoopTask
    {
        void Render();
    }
}
