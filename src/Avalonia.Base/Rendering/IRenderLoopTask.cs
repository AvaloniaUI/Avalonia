using System;
using System.Threading.Tasks;

namespace Avalonia.Rendering
{
    internal interface IRenderLoopTask
    {
        bool Render();
    }
}
