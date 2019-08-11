using System;
using System.Collections.Generic;

namespace Avalonia.Controls.Platform
{
    public interface INativeMenuExporter
    {
        void SetMenu(IEnumerable<NativeMenuItem> menu);
    }
}
