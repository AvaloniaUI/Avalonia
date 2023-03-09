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

        [GetProcAddress("glGetString")]
        public partial IntPtr GetStringNative(int v);

        [GetProcAddress("glGetStringi")]
        public partial IntPtr GetStringiNative(int v, int v1);

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
            var sp = GetString(GlConsts.GL_EXTENSIONS);
            if (sp != null)
                return sp.Split(' ').ToList();
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
