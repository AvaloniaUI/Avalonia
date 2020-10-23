using System;
using Windows.Graphics.Effects;

namespace Avalonia.Win32
{
    class GaussianBlurEffect : EffectBase
    {
        public GaussianBlurEffect(IGraphicsEffectSource source) : base(source)
        {
        }

        enum D2D1_GAUSSIANBLUR_OPTIMIZATION
        {
            D2D1_GAUSSIANBLUR_OPTIMIZATION_SPEED,
            D2D1_GAUSSIANBLUR_OPTIMIZATION_BALANCED,
            D2D1_GAUSSIANBLUR_OPTIMIZATION_QUALITY,
            D2D1_GAUSSIANBLUR_OPTIMIZATION_FORCE_DWORD
        };

        enum D2D1_BORDER_MODE
        {
            D2D1_BORDER_MODE_SOFT,
            D2D1_BORDER_MODE_HARD,
            D2D1_BORDER_MODE_FORCE_DWORD
        };

        enum D2D1GaussianBlurProp
        {
            D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION,
            D2D1_GAUSSIANBLUR_PROP_OPTIMIZATION,
            D2D1_GAUSSIANBLUR_PROP_BORDER_MODE,
            D2D1_GAUSSIANBLUR_PROP_FORCE_DWORD
        };

        public override Guid EffectId => D2DEffects.CLSID_D2D1GaussianBlur;

        public override uint PropertyCount => 3;

        public override object GetProperty(uint index)
        {
            switch ((D2D1GaussianBlurProp)index)
            {
                case D2D1GaussianBlurProp.D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION:
                    return 30.0f;

                case D2D1GaussianBlurProp.D2D1_GAUSSIANBLUR_PROP_OPTIMIZATION:
                    return (uint)D2D1_GAUSSIANBLUR_OPTIMIZATION.D2D1_GAUSSIANBLUR_OPTIMIZATION_BALANCED;

                case D2D1GaussianBlurProp.D2D1_GAUSSIANBLUR_PROP_BORDER_MODE:
                    return (uint)D2D1_BORDER_MODE.D2D1_BORDER_MODE_HARD;
            }

            return null;
        }
    }
}

