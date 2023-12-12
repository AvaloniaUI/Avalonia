using Avalonia.Compatibility;
using Avalonia.OpenGL;
using Avalonia.Platform;

namespace Avalonia.Tizen;
internal class NuiGlPlatform : IPlatformGraphics
{

    public IPlatformGraphicsContext GetSharedContext() => Context;

    public bool UsesSharedContext => true;
    public IPlatformGraphicsContext CreateContext() => throw new NotSupportedException();
    public GlContext Context { get; }
    public static GlVersion GlVersion { get; } = new(GlProfileType.OpenGLES, 3, 0);

    public NuiGlPlatform()
    {
        const string library = "/usr/lib/driver/libGLESv2.so";
        var libGl = NativeLibraryEx.Load(library);
        if (libGl == IntPtr.Zero)
            throw new OpenGlException("Unable to load " + library);
        var iface = new GlInterface(GlVersion, proc =>
        {
            if (NativeLibraryEx.TryGetExport(libGl, proc, out var address))
                return address;
            return default;
        });
        Context = new(iface);
    }
}

class GlContext : IGlContext
{
    public GlContext(GlInterface glInterface)
    {
        GlInterface = glInterface;
    }

    public void Dispose()
    {
    }

    public IDisposable MakeCurrent()
    {
        return this;
    }

    public bool IsLost => false;

    public IDisposable EnsureCurrent()
    {
        return MakeCurrent();
    }

    public bool IsSharedWith(IGlContext context) => true;
    public bool CanCreateSharedContext => true;
    public IGlContext CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null)
    {
        return this;
    }

    public GlVersion Version => new GlVersion(GlProfileType.OpenGLES, 3, 0);
    public GlInterface GlInterface { get; }
    public int SampleCount
    {
        get
        {
            GlInterface.GetIntegerv(GlConsts.GL_SAMPLES, out var samples);
            return samples;
        }
    }
    public int StencilSize
    {
        get
        {
            GlInterface.GetIntegerv(GlConsts.GL_STENCIL_BITS, out var stencil);
            return stencil;
        }
    }

    public object? TryGetFeature(Type featureType) => null;

    public IntPtr GetProcAddress(string procName)
    {
        return GlInterface.GetProcAddress(procName);
    }
}
