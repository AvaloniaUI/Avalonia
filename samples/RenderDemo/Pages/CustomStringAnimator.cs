using System;
using Avalonia.Animation;
using Avalonia.Animation.Animators;

namespace RenderDemo.Pages
{
    public class CustomStringAnimator : InterpolatingAnimator<string>
    {
        public override string Interpolate(double progress, string oldValue, string newValue)
        {
            if (newValue.Length == 0) return "";
            var step = 1.0 / newValue.Length;
            var length = (int)(progress / step);
            var result = newValue.Substring(0, length + 1);
            return result;
        }
    }
}
