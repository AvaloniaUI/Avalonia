using System;
using System.Collections.Generic;
using System.Numerics;
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

        [GetProcAddress("glActiveTexture")]
        public partial void ActiveTexture(int texture);

        [GetProcAddress("glAttachShader")]
        public partial void AttachShader(int program, int shader);

        [GetProcAddress("glBindAttribLocation")]
        public partial void BindAttribLocation(int program, int index, byte* name);

        [GetProcAddress("glBindBuffer")]
        public partial void BindBuffer(int target, int buffer);

        [GetProcAddress("glBindFramebuffer")]
        public partial void BindFramebuffer(int target, int framebuffer);

        [GetProcAddress("glBindRenderbuffer")]
        public partial void BindRenderbuffer(int target, int renderbuffer);

        [GetProcAddress("glBindTexture")]
        public partial void BindTexture(int target, int texture);

        [GetProcAddress("glBlendColor")]
        public partial void BlendColor(float red, float green, float blue, float alpha);

        [GetProcAddress("glBlendEquation")]
        public partial void BlendEquation(int mode);

        [GetProcAddress("glBlendEquationSeparate")]
        public partial void BlendEquationSeparate(int modeRGB, int modeAlpha);

        [GetProcAddress("glBlendFunc")]
        public partial void BlendFunc(int sfactor, int dfactor);

        [GetProcAddress("glBlendFuncSeparate")]
        public partial void BlendFuncSeparate(int sfactorRGB, int dfactorRGB, int sfactorAlpha, int dfactorAlpha);

        [GetProcAddress("glBufferData")]
        public partial void BufferData(int target, IntPtr size, void* data, int usage);

        [GetProcAddress("glBufferSubData")]
        public partial void BufferSubData(int target, IntPtr offset, IntPtr size, void* data);

        [GetProcAddress("glCheckFramebufferStatus")]
        public partial int CheckFramebufferStatus(int target);

        [GetProcAddress("glClear")]
        public partial void Clear(uint mask);

        [GetProcAddress("glClearColor")]
        public partial void ClearColor(float red, float green, float blue, float alpha);

        [GetProcAddress("glClearDepthf")]
        public partial void ClearDepthf(float d);

        [GetProcAddress("glClearStencil")]
        public partial void ClearStencil(int s);

        [GetProcAddress("glColorMask")]
        public partial void ColorMask(bool red, bool green, bool blue, bool alpha);

        [GetProcAddress("glCompileShader")]
        public partial void CompileShader(int shader);

        [GetProcAddress("glCompressedTexImage2D")]
        public partial void CompressedTexImage2D(int target, int level, int internalformat, int width, int height, int border, int imageSize, void* data);

        [GetProcAddress("glCompressedTexSubImage2D")]
        public partial void CompressedTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, void* data);

        [GetProcAddress("glCopyTexImage2D")]
        public partial void CopyTexImage2D(int target, int level, int internalformat, int x, int y, int width, int height, int border);

        [GetProcAddress("glCopyTexSubImage2D")]
        public partial void CopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height);

        [GetProcAddress("glCreateProgram")]
        public partial int CreateProgram();

        [GetProcAddress("glCreateShader")]
        public partial int CreateShader(int type);

        [GetProcAddress("glCullFace")]
        public partial void CullFace(int mode);

        [GetProcAddress("glDeleteBuffers")]
        public partial void DeleteBuffers(int n, int* buffers);

        [GetProcAddress("glDeleteFramebuffers")]
        public partial void DeleteFramebuffers(int n, int* framebuffers);

        [GetProcAddress("glDeleteProgram")]
        public partial void DeleteProgram(int program);

        [GetProcAddress("glDeleteRenderbuffers")]
        public partial void DeleteRenderbuffers(int n, int* renderbuffers);

        [GetProcAddress("glDeleteShader")]
        public partial void DeleteShader(int shader);

        [GetProcAddress("glDeleteTextures")]
        public partial void DeleteTextures(int n, int* textures);

        [GetProcAddress("glDepthFunc")]
        public partial void DepthFunc(int func);

        [GetProcAddress("glDepthMask")]
        public partial void DepthMask(bool flag);

        [GetProcAddress("glDepthRangef")]
        public partial void DepthRangef(float n, float f);

        [GetProcAddress("glDetachShader")]
        public partial void DetachShader(int program, int shader);

        [GetProcAddress("glDisable")]
        public partial void Disable(int cap);

        [GetProcAddress("glDisableVertexAttribArray")]
        public partial void DisableVertexAttribArray(int index);

        [GetProcAddress("glDrawArrays")]
        public partial void DrawArrays(int mode, int first, int count);

        [GetProcAddress("glDrawElements")]
        public partial void DrawElements(int mode, int count, int type, void* indices);

        [GetProcAddress("glEnable")]
        public partial void Enable(int cap);

        [GetProcAddress("glEnableVertexAttribArray")]
        public partial void EnableVertexAttribArray(int index);

        [GetProcAddress("glFinish")]
        public partial void Finish();

        [GetProcAddress("glFlush")]
        public partial void Flush();

        [GetProcAddress("glFramebufferRenderbuffer")]
        public partial void FramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, int renderbuffer);

        [GetProcAddress("glFramebufferTexture2D")]
        public partial void FramebufferTexture2D(int target, int attachment, int textarget, int texture, int level);

        [GetProcAddress("glFrontFace")]
        public partial void FrontFace(int mode);

        [GetProcAddress("glGenBuffers")]
        public partial void GenBuffers(int n, int* buffers);

        [GetProcAddress("glGenerateMipmap")]
        public partial void GenerateMipmap(int target);

        [GetProcAddress("glGenFramebuffers")]
        public partial void GenFramebuffers(int n, int* framebuffers);

        [GetProcAddress("glGenRenderbuffers")]
        public partial void GenRenderbuffers(int n, int* renderbuffers);

        [GetProcAddress("glGenTextures")]
        public partial void GenTextures(int n, int* textures);

        [GetProcAddress("glGetActiveAttrib")]
        public partial void GetActiveAttrib(int program, int index, int bufSize, int* length, int* size, int* type, byte* name);

        [GetProcAddress("glGetActiveUniform")]
        public partial void GetActiveUniform(int program, int index, int bufSize, int* length, int* size, int* type, byte* name);

        [GetProcAddress("glGetAttachedShaders")]
        public partial void GetAttachedShaders(int program, int maxCount, int* count, int* shaders);

        [GetProcAddress("glGetAttribLocation")]
        public partial int GetAttribLocation(int program, byte* name);

        [GetProcAddress("glGetBooleanv")]
        public partial void GetBooleanv(int pname, bool* data);

        [GetProcAddress("glGetBufferParameteriv")]
        public partial void GetBufferParameteriv(int target, int pname, int* parameters);

        [GetProcAddress("glGetError")]
        public partial int GetError();

        [GetProcAddress("glGetFloatv")]
        public partial void GetFloatv(int pname, float* data);

        [GetProcAddress("glGetFramebufferAttachmentParameteriv")]
        public partial void GetFramebufferAttachmentParameteriv(int target, int attachment, int pname, int* parameters);

        //[GetProcAddress("glGetIntegerv")]
        //public partial void GetIntegerv(int pname, int* data);

        [GetProcAddress("glGetProgramiv")]
        public partial void GetProgramiv(int program, int pname, int* parameters);

        [GetProcAddress("glGetProgramInfoLog")]
        public partial void GetProgramInfoLog(int program, int bufSize, int* length, byte* infoLog);

        [GetProcAddress("glGetRenderbufferParameteriv")]
        public partial void GetRenderbufferParameteriv(int target, int pname, int* parameters);

        [GetProcAddress("glGetShaderiv")]
        public partial void GetShaderiv(int shader, int pname, int* parameters);

        [GetProcAddress("glGetShaderInfoLog")]
        public partial void GetShaderInfoLog(int shader, int bufSize, int* length, byte* infoLog);

        [GetProcAddress("glGetShaderPrecisionFormat")]
        public partial void GetShaderPrecisionFormat(int shadertype, int precisiontype, int* range, int* precision);

        [GetProcAddress("glGetShaderSource")]
        public partial void GetShaderSource(int shader, int bufSize, int* length, byte* source);

        //[GetProcAddress("glGetString")]
        //public partial byte* GetString(int name);

        [GetProcAddress("glGetTexParameterfv")]
        public partial void GetTexParameterfv(int target, int pname, float* parameters);

        [GetProcAddress("glGetTexParameteriv")]
        public partial void GetTexParameteriv(int target, int pname, int* parameters);

        [GetProcAddress("glGetUniformfv")]
        public partial void GetUniformfv(int program, int location, float* parameters);

        [GetProcAddress("glGetUniformiv")]
        public partial void GetUniformiv(int program, int location, int* parameters);

        [GetProcAddress("glGetUniformLocation")]
        public partial int GetUniformLocation(int program, byte* name);

        [GetProcAddress("glGetVertexAttribfv")]
        public partial void GetVertexAttribfv(int index, int pname, float* parameters);

        [GetProcAddress("glGetVertexAttribiv")]
        public partial void GetVertexAttribiv(int index, int pname, int* parameters);

        [GetProcAddress("glGetVertexAttribPointerv")]
        public partial void GetVertexAttribPointerv(int index, int pname, void** pointer);

        [GetProcAddress("glHint")]
        public partial void Hint(int target, int mode);

        [GetProcAddress("glIsBuffer")]
        public partial bool IsBuffer(int buffer);

        [GetProcAddress("glIsEnabled")]
        public partial bool IsEnabled(int cap);

        [GetProcAddress("glIsFramebuffer")]
        public partial bool IsFramebuffer(int framebuffer);

        [GetProcAddress("glIsProgram")]
        public partial bool IsProgram(int program);

        [GetProcAddress("glIsRenderbuffer")]
        public partial bool IsRenderbuffer(int renderbuffer);

        [GetProcAddress("glIsShader")]
        public partial bool IsShader(int shader);

        [GetProcAddress("glIsTexture")]
        public partial bool IsTexture(int texture);

        [GetProcAddress("glLineWidth")]
        public partial void LineWidth(float width);

        [GetProcAddress("glLinkProgram")]
        public partial void LinkProgram(int program);

        [GetProcAddress("glPixelStorei")]
        public partial void PixelStorei(int pname, int param);

        [GetProcAddress("glPolygonOffset")]
        public partial void PolygonOffset(float factor, float units);

        [GetProcAddress("glReadPixels")]
        public partial void ReadPixels(int x, int y, int width, int height, int format, int type, void* pixels);

        [GetProcAddress("glReleaseShaderCompiler")]
        public partial void ReleaseShaderCompiler();

        [GetProcAddress("glRenderbufferStorage")]
        public partial void RenderbufferStorage(int target, int internalformat, int width, int height);

        [GetProcAddress("glSampleCoverage")]
        public partial void SampleCoverage(float value, bool invert);

        [GetProcAddress("glScissor")]
        public partial void Scissor(int x, int y, int width, int height);

        [GetProcAddress("glShaderBinary")]
        public partial void ShaderBinary(int count, int* shaders, int binaryFormat, void* binary, int length);

        [GetProcAddress("glShaderSource")]
        public partial void ShaderSource(int shader, int count, byte** str, int* length);

        [GetProcAddress("glStencilFunc")]
        public partial void StencilFunc(int func, int reference, int mask);

        [GetProcAddress("glStencilFuncSeparate")]
        public partial void StencilFuncSeparate(int face, int func, int reference, int mask);

        [GetProcAddress("glStencilMask")]
        public partial void StencilMask(int mask);

        [GetProcAddress("glStencilMaskSeparate")]
        public partial void StencilMaskSeparate(int face, int mask);

        [GetProcAddress("glStencilOp")]
        public partial void StencilOp(int fail, int zfail, int zpass);

        [GetProcAddress("glStencilOpSeparate")]
        public partial void StencilOpSeparate(int face, int sfail, int dpfail, int dppass);

        [GetProcAddress("glTexImage2D")]
        public partial void TexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, void* pixels);

        [GetProcAddress("glTexParameterf")]
        public partial void TexParameterf(int target, int pname, float param);

        [GetProcAddress("glTexParameterfv")]
        public partial void TexParameterfv(int target, int pname, float* parameters);

        [GetProcAddress("glTexParameteri")]
        public partial void TexParameteri(int target, int pname, int param);

        [GetProcAddress("glTexParameteriv")]
        public partial void TexParameteriv(int target, int pname, int* parameters);

        [GetProcAddress("glTexSubImage2D")]
        public partial void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, void* pixels);

        [GetProcAddress("glUniform1f")]
        public partial void Uniform1f(int location, float v0);

        [GetProcAddress("glUniform1fv")]
        public partial void Uniform1fv(int location, int count, float* value);

        [GetProcAddress("glUniform1i")]
        public partial void Uniform1i(int location, int v0);

        [GetProcAddress("glUniform1iv")]
        public partial void Uniform1iv(int location, int count, int* value);

        [GetProcAddress("glUniform2f")]
        public partial void Uniform2f(int location, float v0, float v1);

        [GetProcAddress("glUniform2fv")]
        public partial void Uniform2fv(int location, int count, Vector2* value);

        [GetProcAddress("glUniform2i")]
        public partial void Uniform2i(int location, int v0, int v1);

        [GetProcAddress("glUniform2iv")]
        public partial void Uniform2iv(int location, int count, int* value);

        [GetProcAddress("glUniform3f")]
        public partial void Uniform3f(int location, float v0, float v1, float v2);

        [GetProcAddress("glUniform3fv")]
        public partial void Uniform3fv(int location, int count, Vector3* value);

        [GetProcAddress("glUniform3i")]
        public partial void Uniform3i(int location, int v0, int v1, int v2);

        [GetProcAddress("glUniform3iv")]
        public partial void Uniform3iv(int location, int count, int* value);

        [GetProcAddress("glUniform4f")]
        public partial void Uniform4f(int location, float v0, float v1, float v2, float v3);

        [GetProcAddress("glUniform4fv")]
        public partial void Uniform4fv(int location, int count, Vector4* value);

        [GetProcAddress("glUniform4i")]
        public partial void Uniform4i(int location, int v0, int v1, int v2, int v3);

        [GetProcAddress("glUniform4iv")]
        public partial void Uniform4iv(int location, int count, int* value);

        [GetProcAddress("glUniformMatrix2fv")]
        public partial void UniformMatrix2fv(int location, int count, bool transpose, float* value);

        [GetProcAddress("glUniformMatrix3fv")]
        public partial void UniformMatrix3fv(int location, int count, bool transpose, float* value);

        [GetProcAddress("glUniformMatrix4fv")]
        public partial void UniformMatrix4fv(int location, int count, bool transpose, Matrix4x4* value);

        [GetProcAddress("glUseProgram")]
        public partial void UseProgram(int program);

        [GetProcAddress("glValidateProgram")]
        public partial void ValidateProgram(int program);

        [GetProcAddress("glVertexAttrib1f")]
        public partial void VertexAttrib1f(int index, float x);

        [GetProcAddress("glVertexAttrib1fv")]
        public partial void VertexAttrib1fv(int index, float* v);

        [GetProcAddress("glVertexAttrib2f")]
        public partial void VertexAttrib2f(int index, float x, float y);

        [GetProcAddress("glVertexAttrib2fv")]
        public partial void VertexAttrib2fv(int index, float* v);

        [GetProcAddress("glVertexAttrib3f")]
        public partial void VertexAttrib3f(int index, float x, float y, float z);

        [GetProcAddress("glVertexAttrib3fv")]
        public partial void VertexAttrib3fv(int index, float* v);

        [GetProcAddress("glVertexAttrib4f")]
        public partial void VertexAttrib4f(int index, float x, float y, float z, float w);

        [GetProcAddress("glVertexAttrib4fv")]
        public partial void VertexAttrib4fv(int index, float* v);

        [GetProcAddress("glVertexAttribPointer")]
        public partial void VertexAttribPointer(int index, int size, int type, bool normalized, int stride, void* pointer);

        [GetProcAddress("glViewport")]
        public partial void Viewport(int x, int y, int width, int height);

        public int GenFramebuffer()
        {
            int rv = 0;
            GenFramebuffers(1, &rv);
            return rv;
        }

        public void DeleteFramebuffer(int fb)
        {
            DeleteFramebuffers(1, &fb);
        }

        public int GenRenderbuffer()
        {
            int rv = 0;
            GenRenderbuffers(1, &rv);
            return rv;
        }

        public void DeleteRenderbuffer(int renderbuffer)
        {
            DeleteRenderbuffers(1, &renderbuffer);
        }

        public int GenTexture()
        {
            int rv = 0;
            GenTextures(1, &rv);
            return rv;
        }

        public void DeleteTexture(int texture) => DeleteTextures(1, &texture);

        public void ShaderSourceString(int shader, string source)
        {
            using (var b = new Utf8Buffer(source))
            {
                var ptr = b.DangerousGetHandle();
                var len = new IntPtr(b.ByteLen);
                ShaderSource(shader, 1, (byte**)&ptr, (int*)&len);
            }
        }

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
            fixed (byte* ptr = logData)
                GetShaderInfoLog(shader, logLength, &len, ptr);
            return Encoding.UTF8.GetString(logData, 0, len);
        }

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
            fixed (byte* ptr = logData)
                GetProgramInfoLog(program, logLength, &len, ptr);
            return Encoding.UTF8.GetString(logData, 0, len);
        }

        public void BindAttribLocationString(int program, int index, string name)
        {
            using (var b = new Utf8Buffer(name))
                BindAttribLocation(program, index, (byte*)b.DangerousGetHandle());
        }

        public int GenBuffer()
        {
            int rv;
            GenBuffers(1, &rv);
            return rv;
        }

        public int GetAttribLocationString(int program, string name)
        {
            using (var b = new Utf8Buffer(name))
                return GetAttribLocation(program, (byte*)b.DangerousGetHandle());
        }

        public int GetUniformLocationString(int program, string name)
        {
            using (var b = new Utf8Buffer(name))
                return GetUniformLocation(program, (byte*)b.DangerousGetHandle());
        }

        public void DeleteBuffer(int buffer) => DeleteBuffers(1, &buffer);

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
