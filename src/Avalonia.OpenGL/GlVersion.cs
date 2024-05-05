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
        public bool IsCompatibilityProfile { get; } // Only makes sense if Type is OpenGL and Version is >= 3.2

        public GlVersion(GlProfileType type, int major, int minor) : this(type, major, minor, false) { }
        public GlVersion(GlProfileType type, int major, int minor, bool isCompatibilityProfile)
        {
            Type = type;
            Major = major;
            Minor = minor;
            IsCompatibilityProfile = isCompatibilityProfile;
        }
    }
}
