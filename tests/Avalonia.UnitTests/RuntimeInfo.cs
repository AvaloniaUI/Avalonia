using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Avalonia.Shared.PlatformSupport
{
    internal partial class StandardRuntimePlatform : IRuntimePlatform
    {
        public RuntimePlatformInfo GetRuntimeInfo()
        {
            return new RuntimePlatformInfo();
        }
    }
}
