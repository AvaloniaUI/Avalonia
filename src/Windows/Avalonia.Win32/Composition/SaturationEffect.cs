using System;
using Windows.Graphics.Effects;


namespace Avalonia.Win32
{
    class SaturationEffect : EffectBase
    {
        public SaturationEffect(IGraphicsEffect source) : base(source)
        {
        }

        enum D2D1_SATURATION_PROP
        {
            D2D1_SATURATION_PROP_SATURATION,
            D2D1_SATURATION_PROP_FORCE_DWORD
        };

        public override Guid EffectId => D2DEffects.CLSID_D2D1Saturation;

        public override uint PropertyCount => 1;

        public override object GetProperty(uint index)
        {
            switch ((D2D1_SATURATION_PROP)index)
            {
                case D2D1_SATURATION_PROP.D2D1_SATURATION_PROP_SATURATION:
                    return 2.0f;
            }

            return null;
        }
    }
}

