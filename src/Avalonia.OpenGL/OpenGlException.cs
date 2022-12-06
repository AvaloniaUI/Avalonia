using System;
using Avalonia.OpenGL.Egl;

namespace Avalonia.OpenGL
{
    public class OpenGlException : Exception
    {
        public int? ErrorCode { get; private set; }

        public OpenGlException(string message) : base(message)
        {
        }

        private OpenGlException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public static OpenGlException GetFormattedException(string funcName, EglInterface egl)
        {
            return GetFormattedException<EglErrors>(funcName, egl.GetError());
        }

        public static OpenGlException GetFormattedException(string funcName, GlInterface gl)
        {
            return GetFormattedException<GlErrors>(funcName, gl.GetError());
        }

        public static OpenGlException GetFormattedEglException(string funcName, int errorCode) =>
            GetFormattedException<EglErrors>(funcName, errorCode);

        private static OpenGlException GetFormattedException<T>(string funcName, int errorCode)
        {
            try
            {
                string errorName = Enum.GetName(typeof(T), errorCode);
                return new OpenGlException(
                    $"{funcName} failed with error {errorName} (0x{errorCode.ToString("X")})", errorCode);
            }
            catch (ArgumentException)
            {
                return new OpenGlException($"{funcName} failed with error 0x{errorCode.ToString("X")}", errorCode);
            }
        }
    }
}
