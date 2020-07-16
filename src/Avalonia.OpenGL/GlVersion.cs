namespace Avalonia.OpenGL
{
    public enum GlProfileType
    {
        OpenGL,
        OpenGLES
    }
    
    public struct GlVersion
    {
        public GlProfileType Type { get; }
        public uint Major { get; }
        public uint Minor { get; }

        public GlVersion(GlProfileType type, uint major, uint minor)
        {
            Type = type;
            Major = major;
            Minor = minor;
        }
    }
}
