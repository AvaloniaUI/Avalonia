using System;

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
            return GetFormattedException(typeof(EglErrors), funcName, egl.GetError());
        }

        public static OpenGlException GetFormattedException(string funcName, GlInterface gl)
        {
            return GetFormattedException(typeof(GlErrors), funcName, gl.GetError());
        }

        private static OpenGlException GetFormattedException(Type consts, string funcName, int errorCode)
        {
            try
            {
                string errorName = Enum.GetName(consts, errorCode);
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
