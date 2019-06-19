using System.Collections.Generic;

namespace Avalonia.OpenGL
{
    public class AngleOptions
    {
        public enum PlatformApi
        {
			DirectX9,
			DirectX11
        }

        public List<PlatformApi> AllowedPlatformApis = new List<PlatformApi>
        {
            PlatformApi.DirectX9
        };
    }
}
