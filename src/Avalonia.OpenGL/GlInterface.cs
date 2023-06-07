using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Platform.Interop;
using Avalonia.SourceGenerator;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.OpenGL
{
    public unsafe partial class GlInterface : GlBasicInfoInterface
    {
        private readonly Func<string, IntPtr> _getProcAddress;
        public string? Version { get; }
        public string? Vendor { get; }
        public string? Renderer { get; }
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

        private GlInterface(GlContextInfo info, Func<string, IntPtr> getProcAddress) : base(getProcAddress)
        {
            _getProcAddress = getProcAddress;
            ContextInfo = info;
            Version = GetString(GL_VERSION);
            Renderer = GetString(GL_RENDERER);
            Vendor = GetString(GL_VENDOR);
            Initialize(getProcAddress, ContextInfo);
        }

        public GlInterface(GlVersion version, Func<string, IntPtr> getProcAddress) : this(
            GlContextInfo.Create(version, getProcAddress), getProcAddress)
        {
        }

        public IntPtr GetProcAddress(string proc) => _getProcAddress(proc);

        [GetProcAddress("glGetError")]
        public partial int GetError();

        [GetProcAddress("glClearStencil")]
        public partial void ClearStencil(int s);

        [GetProcAddress("glClearColor")]
        public partial void ClearColor(float r, float g, float b, float a);

        [GetProcAddress("glClear")]
        public partial void Clear(int bits);

        [GetProcAddress("glViewport")]
        public partial void Viewport(int x, int y, int width, int height);

        [GetProcAddress("glFlush")]
        public partial void Flush();

        [GetProcAddress("glFinish")]
        public partial void Finish();

        [GetProcAddress("glGenFramebuffers")]
        public partial void GenFramebuffers(int count, int* res);

        public int GenFramebuffer()
        {
            int rv = 0;
            GenFramebuffers(1, &rv);
            return rv;
        }

        [GetProcAddress("glDeleteFramebuffers")]
        public partial void DeleteFramebuffers(int count, int* framebuffers);

        public void DeleteFramebuffer(int fb)
        {
            DeleteFramebuffers(1, &fb);
        }

        [GetProcAddress("glBindFramebuffer")]
        public partial void BindFramebuffer(int target, int fb);

        [GetProcAddress("glCheckFramebufferStatus")]
        public partial int CheckFramebufferStatus(int target);

        [GlMinVersionEntryPoint("glBlitFramebuffer", 3, 0), GetProcAddress(true)]
        public partial void BlitFramebuffer(int srcX0,
            int srcY0,
            int srcX1,
            int srcY1,
            int dstX0,
            int dstY0,
            int dstX1,
            int dstY1,
            int mask,
            int filter);


        [GetProcAddress("glGenRenderbuffers")]
        public partial void GenRenderbuffers(int count, int* res);

        public int GenRenderbuffer()
        {
            int rv = 0;
            GenRenderbuffers(1, &rv);
            return rv;
        }

        [GetProcAddress("glDeleteRenderbuffers")]
        public partial void DeleteRenderbuffers(int count, int* renderbuffers);

        public void DeleteRenderbuffer(int renderbuffer)
        {
            DeleteRenderbuffers(1, &renderbuffer);
        }

        [GetProcAddress("glBindRenderbuffer")]
        public partial void BindRenderbuffer(int target, int fb);

        [GetProcAddress("glRenderbufferStorage")]
        public partial void RenderbufferStorage(int target, int internalFormat, int width, int height);

        [GetProcAddress("glFramebufferRenderbuffer")]
        public partial void FramebufferRenderbuffer(int target, int attachment,
            int renderbufferTarget, int renderbuffer);

        [GetProcAddress("glGenTextures")]
        public partial void GenTextures(int count, int* res);

        public int GenTexture()
        {
            int rv = 0;
            GenTextures(1, &rv);
            return rv;
        }

        [GetProcAddress("glBindTexture")]
        public partial void BindTexture(int target, int fb);

        [GetProcAddress("glActiveTexture")]
        public partial void ActiveTexture(int texture);

        [GetProcAddress("glDeleteTextures")]
        public partial void DeleteTextures(int count, int* textures);

        public void DeleteTexture(int texture) => DeleteTextures(1, &texture);

        [GetProcAddress("glTexImage2D")]
        public partial void TexImage2D(int target, int level, int internalFormat, int width, int height, int border,
            int format, int type, IntPtr data);

        [GetProcAddress("glCopyTexSubImage2D")]
        public partial void CopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y,
            int width, int height);

        [GetProcAddress("glTexParameteri")]
        public partial void TexParameteri(int target, int name, int value);


        [GetProcAddress("glFramebufferTexture2D")]
        public partial void FramebufferTexture2D(int target, int attachment,
            int texTarget, int texture, int level);

        [GetProcAddress("glCreateShader")]
        public partial int CreateShader(int shaderType);

        [GetProcAddress("glShaderSource")]
        public partial void ShaderSource(int shader, int count, IntPtr strings, IntPtr lengths);

        public void ShaderSourceString(int shader, string source)
        {
            using (var b = new Utf8Buffer(source))
            {
                var ptr = b.DangerousGetHandle();
                var len = new IntPtr(b.ByteLen);
                ShaderSource(shader, 1, new IntPtr(&ptr), new IntPtr(&len));
            }
        }

        [GetProcAddress("glCompileShader")]
        public partial void CompileShader(int shader);

        [GetProcAddress("glGetShaderiv")]
        public partial void GetShaderiv(int shader, int name, int* parameters);

        [GetProcAddress("glGetShaderInfoLog")]
        public partial void GetShaderInfoLog(int shader, int maxLength, out int length, void* infoLog);

        public unsafe string? CompileShaderAndGetError(int shader, string source)
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
            return Encoding.UTF8.GetString(logData, 0, len);
        }


        [GetProcAddress("glCreateProgram")]
        public partial int CreateProgram();

        [GetProcAddress("glAttachShader")]
        public partial void AttachShader(int program, int shader);

        [GetProcAddress("glLinkProgram")]
        public partial void LinkProgram(int program);

        [GetProcAddress("glGetProgramiv")]
        public partial void GetProgramiv(int program, int name, int* parameters);

        [GetProcAddress("glGetProgramInfoLog")]
        public partial void GetProgramInfoLog(int program, int maxLength, out int len, void* infoLog);

        public unsafe string? LinkProgramAndGetError(int program)
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
            return Encoding.UTF8.GetString(logData, 0, len);
        }

        [GetProcAddress("glBindAttribLocation")]
        public partial void BindAttribLocation(int program, int index, IntPtr name);

        public void BindAttribLocationString(int program, int index, string name)
        {
            using (var b = new Utf8Buffer(name))
                BindAttribLocation(program, index, b.DangerousGetHandle());
        }

        [GetProcAddress("glGenBuffers")]
        public partial void GenBuffers(int len, int* rv);

        public int GenBuffer()
        {
            int rv;
            GenBuffers(1, &rv);
            return rv;
        }

        [GetProcAddress("glBindBuffer")]
        public partial void BindBuffer(int target, int buffer);

        [GetProcAddress("glBufferData")]
        public partial void BufferData(int target, IntPtr size, IntPtr data, int usage);

        [GetProcAddress("glGetAttribLocation")]
        public partial int GetAttribLocation(int program, IntPtr name);

        public int GetAttribLocationString(int program, string name)
        {
            using (var b = new Utf8Buffer(name))
                return GetAttribLocation(program, b.DangerousGetHandle());
        }

        [GetProcAddress("glVertexAttribPointer")]
        public partial void VertexAttribPointer(int index, int size, int type,
            int normalized, int stride, IntPtr pointer);

        [GetProcAddress("glEnableVertexAttribArray")]
        public partial void EnableVertexAttribArray(int index);

        [GetProcAddress("glUseProgram")]
        public partial void UseProgram(int program);

        [GetProcAddress("glDrawArrays")]
        public partial void DrawArrays(int mode, int first, IntPtr count);

        [GetProcAddress("glDrawElements")]
        public partial void DrawElements(int mode, int count, int type, IntPtr indices);

        [GetProcAddress("glGetUniformLocation")]
        public partial int GetUniformLocation(int program, IntPtr name);

        public int GetUniformLocationString(int program, string name)
        {
            using (var b = new Utf8Buffer(name))
                return GetUniformLocation(program, b.DangerousGetHandle());
        }

        [GetProcAddress("glUniform1f")]
        public partial void Uniform1f(int location, float falue);


        [GetProcAddress("glUniformMatrix4fv")]
        public partial void UniformMatrix4fv(int location, int count, bool transpose, void* value);

        [GetProcAddress("glEnable")]
        public partial void Enable(int what);

        [GetProcAddress("glDeleteBuffers")]
        public partial void DeleteBuffers(int count, int* buffers);

        public void DeleteBuffer(int buffer) => DeleteBuffers(1, &buffer);

        [GetProcAddress("glDeleteProgram")]
        public partial void DeleteProgram(int program);

        [GetProcAddress("glDeleteShader")]
        public partial void DeleteShader(int shader);

        [GetProcAddress("glGetRenderbufferParameteriv")]
        public partial void GetRenderbufferParameteriv(int target, int name, out int value);
        // ReSharper restore UnassignedGetOnlyAutoProperty

        [GetProcAddress(true)]
        [GlMinVersionEntryPoint("glDeleteVertexArrays", 3, 0)]
        [GlExtensionEntryPoint("glDeleteVertexArraysOES", "GL_OES_vertex_array_object")]
        public partial void DeleteVertexArrays(int count, int* arrays);

        public void DeleteVertexArray(int array) => DeleteVertexArrays(1, &array);

        [GetProcAddress(true)]
        [GlMinVersionEntryPoint("glBindVertexArray", 3, 0)]
        [GlExtensionEntryPoint("glBindVertexArrayOES", "GL_OES_vertex_array_object")]
        public partial void BindVertexArray(int array);


        [GetProcAddress(true)]
        [GlMinVersionEntryPoint("glGenVertexArrays", 3, 0)]
        [GlExtensionEntryPoint("glGenVertexArraysOES", "GL_OES_vertex_array_object")]
        public partial void GenVertexArrays(int n, int* rv);

        public int GenVertexArray()
        {
            int rv = 0;
            GenVertexArrays(1, &rv);
            return rv;
        }

        public static GlInterface FromNativeUtf8GetProcAddress(GlVersion version, Func<IntPtr, IntPtr> getProcAddress)
        {
            return new GlInterface(version, s =>
            {
                var ptr = Marshal.StringToHGlobalAnsi(s);
                var rv = getProcAddress(ptr);
                Marshal.FreeHGlobal(ptr);
                return rv;
            });
        }
    }
}
