namespace Avalonia.OpenGL
{
    public enum GlProfileType
    {
        OpenGL,
        OpenGLES
    }
    
    public record struct GlVersion
    {
        public GlProfileType Type { get; }
        public int Major { get; }
        public int Minor { get; }

        public GlVersion(GlProfileType type, int major, int minor)
        {
            Type = type;
            Major = major;
            Minor = minor;
        }
    }
}
