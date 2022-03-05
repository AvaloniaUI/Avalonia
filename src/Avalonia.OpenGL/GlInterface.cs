using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Platform.Interop;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.OpenGL
{
    public delegate IntPtr GlGetProcAddressDelegate(string procName);
    
    public unsafe class GlInterface : GlBasicInfoInterface<GlInterface.GlContextInfo>
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
            Version = GetString(GlConsts.GL_VERSION);
            Renderer = GetString(GlConsts.GL_RENDERER);
            Vendor = GetString(GlConsts.GL_VENDOR);   
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

        // ReSharper disable UnassignedGetOnlyAutoProperty
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GlGetError();
        [GlEntryPoint("glGetError")]
        public GlGetError GetError { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlClearStencil(int s);
        [GlEntryPoint("glClearStencil")]
        public GlClearStencil ClearStencil { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlClearColor(float r, float g, float b, float a);
        [GlEntryPoint("glClearColor")]
        public GlClearColor ClearColor { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlClear(int bits);
        [GlEntryPoint("glClear")]
        public GlClear Clear { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlViewport(int x, int y, int width, int height);
        [GlEntryPoint("glViewport")]
        public GlViewport Viewport { get; }
        
        [GlEntryPoint("glFlush")]
        public UnmanagedAction Flush { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void UnmanagedAction();
        
        [GlEntryPoint("glFinish")]
        public UnmanagedAction Finish { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate IntPtr GlGetString(int v);
        [GlEntryPoint("glGetString")]
        public GlGetString GetStringNative { get; }

        public string GetString(int v)
        {
            var ptr = GetStringNative(v);
            if (ptr != IntPtr.Zero)
                return Marshal.PtrToStringAnsi(ptr);
            return null;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlGetIntegerv(int name, out int rv);
        [GlEntryPoint("glGetIntegerv")]
        public GlGetIntegerv GetIntegerv { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlGenFramebuffers(int count, int[] res);
        [GlEntryPoint("glGenFramebuffers")]
        public GlGenFramebuffers GenFramebuffers { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlDeleteFramebuffers(int count, int[] framebuffers);
        [GlEntryPoint("glDeleteFramebuffers")]
        public GlDeleteFramebuffers DeleteFramebuffers { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlBindFramebuffer(int target, int fb);
        [GlEntryPoint("glBindFramebuffer")]
        public GlBindFramebuffer BindFramebuffer { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GlCheckFramebufferStatus(int target);
        [GlEntryPoint("glCheckFramebufferStatus")]
        public GlCheckFramebufferStatus CheckFramebufferStatus { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlBlitFramebuffer(int srcX0,
            int srcY0,
            int srcX1,
            int srcY1,
            int dstX0,
            int dstY0,
            int dstX1,
            int dstY1,
            int mask,
            int filter);
        [GlMinVersionEntryPoint("glBlitFramebuffer", 3, 0), GlOptionalEntryPoint]
        public GlBlitFramebuffer BlitFramebuffer { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlGenRenderbuffers(int count, int[] res);
        [GlEntryPoint("glGenRenderbuffers")]
        public GlGenRenderbuffers GenRenderbuffers { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlDeleteRenderbuffers(int count, int[] renderbuffers);
        [GlEntryPoint("glDeleteRenderbuffers")]
        public GlDeleteTextures DeleteRenderbuffers { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlBindRenderbuffer(int target, int fb);
        [GlEntryPoint("glBindRenderbuffer")]
        public GlBindRenderbuffer BindRenderbuffer { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlRenderbufferStorage(int target, int internalFormat, int width, int height);
        [GlEntryPoint("glRenderbufferStorage")]
        public GlRenderbufferStorage RenderbufferStorage { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlFramebufferRenderbuffer(int target, int attachment,
            int renderbufferTarget, int renderbuffer);
        [GlEntryPoint("glFramebufferRenderbuffer")]
        public GlFramebufferRenderbuffer FramebufferRenderbuffer { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlGenTextures(int count, int[] res);
        [GlEntryPoint("glGenTextures")]
        public GlGenTextures GenTextures { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlBindTexture(int target, int fb);
        [GlEntryPoint("glBindTexture")]
        public GlBindTexture BindTexture { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlActiveTexture(int texture);
        [GlEntryPoint("glActiveTexture")]
        public GlActiveTexture ActiveTexture { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlDeleteTextures(int count, int[] textures);
        [GlEntryPoint("glDeleteTextures")]
        public GlDeleteTextures DeleteTextures { get; }


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlTexImage2D(int target, int level, int internalFormat, int width, int height, int border,
            int format, int type, IntPtr data);
        [GlEntryPoint("glTexImage2D")]
        public GlTexImage2D TexImage2D { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlCopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y,
            int width, int height);
        
        [GlEntryPoint("glCopyTexSubImage2D")]
        public GlCopyTexSubImage2D CopyTexSubImage2D { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlTexParameteri(int target, int name, int value);
        [GlEntryPoint("glTexParameteri")]
        public GlTexParameteri TexParameteri { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlFramebufferTexture2D(int target, int attachment,
            int texTarget, int texture, int level);
        [GlEntryPoint("glFramebufferTexture2D")]
        public GlFramebufferTexture2D FramebufferTexture2D { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GlCreateShader(int shaderType);
        [GlEntryPoint("glCreateShader")]
        public GlCreateShader CreateShader { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlShaderSource(int shader, int count, IntPtr strings, IntPtr lengths);
        [GlEntryPoint("glShaderSource")]
        public GlShaderSource ShaderSource { get; }

        public void ShaderSourceString(int shader, string source)
        {
            using (var b = new Utf8Buffer(source))
            {
                var ptr = b.DangerousGetHandle();
                var len = new IntPtr(b.ByteLen);
                ShaderSource(shader, 1, new IntPtr(&ptr), new IntPtr(&len));
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlCompileShader(int shader);
        [GlEntryPoint("glCompileShader")]
        public GlCompileShader CompileShader { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlGetShaderiv(int shader, int name, int* parameters);
        [GlEntryPoint("glGetShaderiv")]
        public GlGetShaderiv GetShaderiv { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlGetShaderInfoLog(int shader, int maxLength, out int length, void*infoLog);
        [GlEntryPoint("glGetShaderInfoLog")]
        public GlGetShaderInfoLog GetShaderInfoLog { get; }

        public unsafe string CompileShaderAndGetError(int shader, string source)
        {
            ShaderSourceString(shader, source);
            CompileShader(shader);
            int compiled;
            GetShaderiv(shader, GL_COMPILE_STATUS, &compiled);
            if (compiled != 0)
                return null;
            int logLength;
            GetShaderiv(shader, GL_INFO_LOG_LENGTH, &logLength);
            if (logLength == 0)
                logLength = 4096;
            var logData = new byte[logLength];
            int len;
            fixed (void* ptr = logData)
                GetShaderInfoLog(shader, logLength, out len, ptr);
            return Encoding.UTF8.GetString(logData,0, len);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GlCreateProgram();
        [GlEntryPoint("glCreateProgram")]
        public GlCreateProgram CreateProgram { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlAttachShader(int program, int shader);
        [GlEntryPoint("glAttachShader")]
        public GlAttachShader AttachShader { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlLinkProgram(int program);
        [GlEntryPoint("glLinkProgram")]
        public GlLinkProgram LinkProgram { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlGetProgramiv(int program, int name, int* parameters);
        [GlEntryPoint("glGetProgramiv")]
        public GlGetProgramiv GetProgramiv { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlGetProgramInfoLog(int program, int maxLength, out int len, void* infoLog);
        [GlEntryPoint("glGetProgramInfoLog")]
        public GlGetProgramInfoLog GetProgramInfoLog { get; }

        public unsafe string LinkProgramAndGetError(int program)
        {
            LinkProgram(program);
            int compiled;
            GetProgramiv(program, GL_LINK_STATUS, &compiled);
            if (compiled != 0)
                return null;
            int logLength;
            GetProgramiv(program, GL_INFO_LOG_LENGTH, &logLength);
            var logData = new byte[logLength];
            int len;
            fixed (void* ptr = logData)
                GetProgramInfoLog(program, logLength, out len, ptr);
            return Encoding.UTF8.GetString(logData,0, len);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlBindAttribLocation(int program, int index, IntPtr name);
        [GlEntryPoint("glBindAttribLocation")]
        public GlBindAttribLocation BindAttribLocation { get; }

        public void BindAttribLocationString(int program, int index, string name)
        {
            using (var b = new Utf8Buffer(name))
                BindAttribLocation(program, index, b.DangerousGetHandle());
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlGenBuffers(int len, int[] rv);
        [GlEntryPoint("glGenBuffers")]
        public GlGenBuffers GenBuffers { get; }

        public int GenBuffer()
        {
            var rv = new int[1];
            GenBuffers(1, rv);
            return rv[0];
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlBindBuffer(int target, int buffer);
        [GlEntryPoint("glBindBuffer")]
        public GlBindBuffer BindBuffer { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlBufferData(int target, IntPtr size, IntPtr data, int usage);
        [GlEntryPoint("glBufferData")]
        public GlBufferData BufferData { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GlGetAttribLocation(int program, IntPtr name);
        [GlEntryPoint("glGetAttribLocation")]
        public GlGetAttribLocation GetAttribLocation { get; }

        public int GetAttribLocationString(int program, string name)
        {
            using (var b = new Utf8Buffer(name))
                return GetAttribLocation(program, b.DangerousGetHandle());
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlVertexAttribPointer(int index, int size, int type,
            int normalized, int stride, IntPtr pointer);
        [GlEntryPoint("glVertexAttribPointer")]
        public GlVertexAttribPointer VertexAttribPointer { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlEnableVertexAttribArray(int index);
        [GlEntryPoint("glEnableVertexAttribArray")]
        public GlEnableVertexAttribArray EnableVertexAttribArray { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlUseProgram(int program);
        [GlEntryPoint("glUseProgram")]
        public GlUseProgram UseProgram { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlDrawArrays(int mode, int first, IntPtr count);
        [GlEntryPoint("glDrawArrays")]
        public GlDrawArrays DrawArrays { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlDrawElements(int mode, int count, int type, IntPtr indices);
        [GlEntryPoint("glDrawElements")]
        public GlDrawElements DrawElements { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GlGetUniformLocation(int program, IntPtr name);
        [GlEntryPoint("glGetUniformLocation")]
        public GlGetUniformLocation GetUniformLocation { get; }

        public int GetUniformLocationString(int program, string name)
        {
            using (var b = new Utf8Buffer(name))
                return GetUniformLocation(program, b.DangerousGetHandle());
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlUniform1f(int location, float falue);
        [GlEntryPoint("glUniform1f")]
        public GlUniform1f Uniform1f { get; }


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlUniformMatrix4fv(int location, int count, bool transpose, void* value);
        [GlEntryPoint("glUniformMatrix4fv")]
        public GlUniformMatrix4fv UniformMatrix4fv { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlEnable(int what);
        [GlEntryPoint("glEnable")]
        public GlEnable Enable { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlDeleteBuffers(int count, int[] buffers);
        [GlEntryPoint("glDeleteBuffers")]
        public GlDeleteBuffers DeleteBuffers { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlDeleteProgram(int program);
        [GlEntryPoint("glDeleteProgram")]
        public GlDeleteProgram DeleteProgram { get; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GlDeleteShader(int shader);
        [GlEntryPoint("glDeleteShader")]
        public GlDeleteShader DeleteShader { get; }
        
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void GLGetRenderbufferParameteriv(int target, int name, int[] value);
        [GlEntryPoint("glGetRenderbufferParameteriv")]
        public GLGetRenderbufferParameteriv GetRenderbufferParameteriv { get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty
    }
}
