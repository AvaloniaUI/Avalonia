using System;
using Avalonia.OpenGL.Egl;

namespace Avalonia.OpenGL
{
    public class OpenGlException : Exception
    {
        public int? ErrorCode { get; }

        public OpenGlException(string? message) : base(message)
        {
        }

        private OpenGlException(string? message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public static OpenGlException GetFormattedException(string funcName, EglInterface egl)
        {
            return GetFormattedEglException(funcName, egl.GetError());
        }

        public static OpenGlException GetFormattedException(string funcName, GlInterface gl)
        {
            var err = gl.GetError();
            return GetFormattedException(funcName, (GlErrors)err, err);
        }

        public static OpenGlException GetFormattedException(string funcName, int errorCode) =>
            GetFormattedException(funcName, (GlErrors)errorCode, errorCode);

        public static OpenGlException GetFormattedEglException(string funcName, int errorCode) =>
            GetFormattedException(funcName, (EglErrors)errorCode,errorCode);

        private static OpenGlException GetFormattedException<T>(string funcName, T errorCode, int intErrorCode) where T : struct, Enum
        {
            try
            {
                var errorName = Enum.GetName(errorCode);
                return new OpenGlException(
                    $"{funcName} failed with error {errorName} (0x{errorCode.ToString("X")})", intErrorCode);
            }
            catch (ArgumentException)
            {
                return new OpenGlException($"{funcName} failed with error 0x{errorCode.ToString("X")}", intErrorCode);
            }
        }
    }
}
