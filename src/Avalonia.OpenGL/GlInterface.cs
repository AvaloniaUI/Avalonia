using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.OpenGL;

namespace Avalonia.OpenGL
{
    public delegate IntPtr GlGetProcAddressDelegate(string procName);
    
    public static unsafe class GlInterface
    {
        public class GlContextInfo
        {
            public GlVersion Version { get; }
            public HashSet<string> Extensions { get; }

            public GlContextInfo(GlVersion version, HashSet<string> extensions)
            {
                Version = version;
                Extensions = extensions;
            }

            public static GlContextInfo Create(GlVersion version, Func<string, IntPtr> getProcAddress)
            {
                var gl = GL.GetApi(getProcAddress);
                var exts = Enumerable.Range(0, gl.GetInteger(GLEnum.NumExtensions))
                    .Select(x => gl.GetStringS(StringName.Extensions, (uint)x));
                return new GlContextInfo(version, new HashSet<string>(exts));
            }
        }

        public static unsafe string CompileShaderAndGetError(this GL gl, uint shader, string source)
        {
            gl.ShaderSource(shader, source);
            gl.CompileShader(shader);
            int compiled;
            gl.GetShader(shader, GLEnum.CompileStatus, &compiled);
            if (compiled != 0)
                return null;
            return gl.GetShaderInfoLog(shader);
        }
    }
}
