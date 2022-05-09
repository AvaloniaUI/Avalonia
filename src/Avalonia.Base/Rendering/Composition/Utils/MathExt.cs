using System;

namespace Avalonia.Rendering.Composition.Utils
{
    static class MathExt
    {
        public static float Clamp(float value, float min, float max)
        {
            var amax = Math.Max(min, max);
            var amin = Math.Min(min, max);
            return Math.Min(Math.Max(value, amin), amax);
        }
        
        public static double Clamp(double value, double min, double max)
        {
            var amax = Math.Max(min, max);
            var amin = Math.Min(min, max);
            return Math.Min(Math.Max(value, amin), amax);
        }


    }
}