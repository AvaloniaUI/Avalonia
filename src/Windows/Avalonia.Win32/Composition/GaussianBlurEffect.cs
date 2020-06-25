using System;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Windows.UI.Composition;


namespace Avalonia.Win32
{
    class GaussianBlurEffect : IGraphicsEffect, IGraphicsEffectSource, global::Windows.Graphics.Effects.Interop.IGraphicsEffectD2D1Interop
    {
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

        public string Name { get; set; }

        public Guid EffectId => D2DEffects.CLSID_D2D1GaussianBlur;

        public uint PropertyCount => 3;

        public uint SourceCount => 1;

        public uint GetNamedPropertyMapping(string name, out GRAPHICS_EFFECT_PROPERTY_MAPPING mapping)
        {
            throw new NotImplementedException();
        }

        public object GetProperty(uint index)
        {
            switch ((D2D1GaussianBlurProp)index)
            {
                case D2D1GaussianBlurProp.D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION:
                    return 30.0f;

                case D2D1GaussianBlurProp.D2D1_GAUSSIANBLUR_PROP_OPTIMIZATION:
                    return (UInt32)D2D1_GAUSSIANBLUR_OPTIMIZATION.D2D1_GAUSSIANBLUR_OPTIMIZATION_SPEED;

                case D2D1GaussianBlurProp.D2D1_GAUSSIANBLUR_PROP_BORDER_MODE:
                    return (UInt32)D2D1_BORDER_MODE.D2D1_BORDER_MODE_HARD;
            }

            return null;
        }

        private IGraphicsEffectSource _source = new CompositionEffectSourceParameter("backdrop");

        public IGraphicsEffectSource GetSource(uint index)
        {
            if (index == 0)
            {
                return _source;
            }
            else
            {
                return null;
            }
        }
    }
}

