using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Platform
{
    public interface IPclPlatformWrapper
    {
        Assembly[] GetLoadedAssemblies();
    }
}
