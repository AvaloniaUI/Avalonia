using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Platform;

namespace Perspex.Skia
{
    public static class SkiaPlatform
    {
        private static bool s_forceSoftwareRendering;

        public static void Initialize() 
            => PerspexLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(new PlatformRenderInterface());

        public static bool ForceSoftwareRendering
        {
            get { return s_forceSoftwareRendering; }
            set
            {
                s_forceSoftwareRendering = value;
                MethodTable.Instance.SetOption(MethodTable.Option.ForceSoftware, new IntPtr(value ? 1 : 0));
            }
        }
    }
}
