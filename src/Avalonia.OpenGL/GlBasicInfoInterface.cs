using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.SourceGenerator;

namespace Avalonia.OpenGL
{
    public unsafe partial class GlBasicInfoInterface
    {
        public GlBasicInfoInterface(Func<string, IntPtr> getProcAddress)
        {
            Initialize(getProcAddress);
        }
    
        [GetProcAddress("glGetIntegerv")]
        public partial void GetIntegerv(int name, out int rv);
    
        [GetProcAddress("glGetFloatv")]
        public partial void GetFloatv(int name, out float rv);

        [GetProcAddress("glGetString")]
        public partial IntPtr GetStringNative(int v);

        [GetProcAddress("glGetStringi")]
        public partial IntPtr GetStringiNative(int v, int v1);

        [GetProcAddress("glGetError")]
        public partial int GetError();

        public string? GetString(int v)
        {
            var ptr = GetStringNative(v);
            if (ptr != IntPtr.Zero)
                return Marshal.PtrToStringAnsi(ptr);
            return null;
        }
        
        public string? GetString(int v, int index)
        {
            var ptr = GetStringiNative(v, index);
            if (ptr != IntPtr.Zero)
                return Marshal.PtrToStringAnsi(ptr);
            return null;
        }

        public List<string> GetExtensions()
        {
            // On some (generally older) versions of OpenGL, GL_EXTENSIONS is a space-separated list of available extensions.
            // For example:
            // https://learn.microsoft.com/en-us/windows/win32/opengl/glgetstring
            // https://docs.gl/gl2/glGetString
            // https://registry.khronos.org/OpenGL-Refpages/es3.0/html/glGetString.xhtml
            var sp = GetString(GlConsts.GL_EXTENSIONS);
            if (sp != null)
                return sp.Split(' ').ToList();
            // The OpenGL version is not such a version.
            // Consume the GL_INVALID_ENUM error from the GetString call above.
            GetError();

            // For other (generally newer) versions
            GetIntegerv(GlConsts.GL_NUM_EXTENSIONS, out int count);
            var rv = new List<string>(count);
            for (var c = 0; c < count; c++)
            {
                if (GetString(GlConsts.GL_EXTENSIONS, c) is { } extension)
                {
                    rv.Add(extension);
                }
            }

            return rv;
        }
    }
}
