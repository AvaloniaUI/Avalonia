using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Platform.Interop;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.OpenGL
{
    public delegate IntPtr GlGetProcAddressDelegate(string procName);

    public unsafe partial class GlInterface : GlBasicInfoInterface<GlInterface.GlContextInfo>
    {
        public string Version { get; }
        public string Vendor { get; }
        public string Renderer { get; }
        public GlContextInfo ContextInfo { get; }

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
                var basicInfoInterface = new GlBasicInfoInterface(getProcAddress);
                var exts = basicInfoInterface.GetExtensions();
                return new GlContextInfo(version, new HashSet<string>(exts));
            }
        }

        private GlInterface(GlContextInfo info, Func<string, IntPtr> getProcAddress) : base(getProcAddress, info)
        {
            ContextInfo = info;
            Version = GetString(StringName.GL_VERSION);
            Renderer = GetString(StringName.GL_RENDERER);
            Vendor = GetString(StringName.GL_VENDOR);
        }

        public GlInterface(GlVersion version, Func<string, IntPtr> getProcAddress) : this(
            GlContextInfo.Create(version, getProcAddress), getProcAddress)
        {
        }

        public GlInterface(GlVersion version, Func<Utf8Buffer, IntPtr> n) : this(version, ConvertNative(n))
        {

        }

        public static GlInterface FromNativeUtf8GetProcAddress(GlVersion version, Func<Utf8Buffer, IntPtr> getProcAddress) =>
            new GlInterface(version, getProcAddress);


        public T GetProcAddress<T>(string proc) => Marshal.GetDelegateForFunctionPointer<T>(GetProcAddress(proc));

        public void ShaderSourceString(uint shader, string source)
        {
            using (var b = new Utf8Buffer(source))
            {
                var ptr = b.DangerousGetHandle();
                var len = new IntPtr(b.ByteLen);
                ShaderSource(shader, 1, new IntPtr(&ptr), new IntPtr(&len));
            }                        
        }

        public unsafe string CompileShaderAndGetError(uint shader, string source)
        {
            ShaderSourceString(shader, source);
            CompileShader(shader);

            int compiled;
            GetShaderiv(shader, ShaderParameterName.GL_COMPILE_STATUS, &compiled);            
            if (compiled != 0)
                return null;

            int logLength;
            GetShaderiv(shader, ShaderParameterName.GL_INFO_LOG_LENGTH, &logLength);            
            if (logLength == 0)
                logLength = 4096;
            var logData = new byte[logLength];
            int len;
            fixed (void* ptr = logData)
                GetShaderInfoLog(shader, logLength, out len, ptr);
            return Encoding.UTF8.GetString(logData, 0, len);
        }

        public unsafe string LinkProgramAndGetError(uint program)
        {
            LinkProgram(program);
            uint compiled;
            GetProgramiv(program, ProgramPropertyARB.GL_LINK_STATUS, &compiled);
            if (compiled != 0)
                return null;
            int logLength;
            GetProgramiv(program, ProgramPropertyARB.GL_INFO_LOG_LENGTH, &logLength);
            var logData = new byte[logLength];
            int len;
            fixed (void* ptr = logData)
                GetProgramInfoLog(program, logLength, out len, ptr);
            return Encoding.UTF8.GetString(logData, 0, len);
        }

        public void BindAttribLocationString(uint program, uint index, string name)
        {
            using (var b = new Utf8Buffer(name))
                 BindAttribLocation(program, index, b.DangerousGetHandle());            
        }

        public uint GenBuffer()
        {
            var rv = new uint[1];            
            GenBuffers(1, rv);
            return rv[0];
        }

        public int GetAttribLocationString(uint program, string name)
        {
            using (var b = new Utf8Buffer(name))
                return GetAttribLocation(program, b.DangerousGetHandle());                
        }

        public int GetUniformLocationString(uint program, string name)
        {
            using (var b = new Utf8Buffer(name))
                return GetUniformLocation(program, b.DangerousGetHandle());                
        }

    }
}
