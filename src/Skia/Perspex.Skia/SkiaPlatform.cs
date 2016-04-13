using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Platform;

namespace Perspex
{
	public static class SkiaApplicationExtensions
	{
		public static AppT UseSkia<AppT>(this AppT app) where AppT : Application
		{
			Perspex.Skia.SkiaPlatform.Initialize();
			return app;
		}
	}
}

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
