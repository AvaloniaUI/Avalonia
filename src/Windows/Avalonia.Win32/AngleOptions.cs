using System.Collections.Generic;
using Avalonia.OpenGL;

namespace Avalonia.Win32
{
    public class AngleOptions
    {
        public enum PlatformApi
        {
			DirectX9,
			DirectX11
        }

        public IList<GlVersion> GlProfiles { get; set; } = new List<GlVersion>
        {
            new GlVersion(GlProfileType.OpenGLES, 3, 0),
            new GlVersion(GlProfileType.OpenGLES, 2, 0)
        };

        public IList<PlatformApi>? AllowedPlatformApis { get; set; } = null;
    }
}
