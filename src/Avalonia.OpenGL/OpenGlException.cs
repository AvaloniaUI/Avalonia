using System;
using System.Reflection;

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
            return GetFormattedException(typeof(EglConsts).GetFields(), funcName, 0x3000, 0x301F, egl.GetError());
        }

        public static OpenGlException GetFormattedException(string funcName, GlInterface gl)
        {
            return GetFormattedException(typeof(GlConsts).GetFields(), funcName, 0x0500, 0x0505, gl.GetError());
        }

        private static OpenGlException GetFormattedException(
            FieldInfo[] fields, string funcName, int minValue, int maxValue, int errorCode)
        {
            foreach (var field in fields)
            {
                int value = (int)field.GetValue(null);
                if (value < minValue || value > maxValue)
                {
                    continue;
                }

                if (value == errorCode)
                {
                    return new OpenGlException(
                        $"{funcName} failed with error {field.Name} (0x{errorCode.ToString("X")})", errorCode);
                }
            }

            return new OpenGlException($"{funcName} failed with error 0x{errorCode.ToString("X")}", errorCode);
        }
    }
}
