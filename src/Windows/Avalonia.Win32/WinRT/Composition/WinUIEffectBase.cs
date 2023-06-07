using System;
using System.Linq;
using System.Runtime.InteropServices;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition
{
    internal abstract class WinUIEffectBase : WinRTInspectable, IGraphicsEffect,  IGraphicsEffectSource, IGraphicsEffectD2D1Interop
    {
        private IGraphicsEffectSource[]? _sources;

        public WinUIEffectBase(params IGraphicsEffectSource[] _sources)
        {
            this._sources = _sources.Select(e =>
            {
                if (e is WinUIEffectBase)
                    return e;
                return e.CloneReference();
            }).ToArray();
        }

        public IntPtr Name => IntPtr.Zero;

        public void SetName(IntPtr name)
        {
            
        }

        public abstract Guid EffectId { get; }
        public unsafe void GetNamedPropertyMapping(IntPtr name, uint* index, GRAPHICS_EFFECT_PROPERTY_MAPPING* mapping) =>
            throw new COMException("Not supported", unchecked((int)0x80004001));

        public abstract uint PropertyCount { get; }
        public abstract IPropertyValue? GetProperty(uint index);

        public IGraphicsEffectSource GetSource(uint index)
        {
            if (_sources == null || index> _sources.Length)
                throw new COMException("Invalid index", unchecked((int)0x80070057));
            return _sources[index];
        }

        public uint SourceCount => (uint)(_sources?.Length ?? 0);

        public override void OnUnreferencedFromNative()
        {
            if (_sources == null)
                return;
            
            /*foreach(var s in _sources)
                s.Dispose();*/
            _sources = null;
        }
    }

    internal class WinUIGaussianBlurEffect : WinUIEffectBase
    {
        public WinUIGaussianBlurEffect(IGraphicsEffectSource source) : base(source)
        {
        }

        private enum D2D1_GAUSSIANBLUR_OPTIMIZATION
        {
            D2D1_GAUSSIANBLUR_OPTIMIZATION_SPEED,
            D2D1_GAUSSIANBLUR_OPTIMIZATION_BALANCED,
            D2D1_GAUSSIANBLUR_OPTIMIZATION_QUALITY,
            D2D1_GAUSSIANBLUR_OPTIMIZATION_FORCE_DWORD
        };

        private enum D2D1_BORDER_MODE
        {
            D2D1_BORDER_MODE_SOFT,
            D2D1_BORDER_MODE_HARD,
            D2D1_BORDER_MODE_FORCE_DWORD
        };

        private enum D2D1GaussianBlurProp
        {
            D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION,
            D2D1_GAUSSIANBLUR_PROP_OPTIMIZATION,
            D2D1_GAUSSIANBLUR_PROP_BORDER_MODE,
            D2D1_GAUSSIANBLUR_PROP_FORCE_DWORD
        };

        public override Guid EffectId => D2DEffects.CLSID_D2D1GaussianBlur;

        public override uint PropertyCount => 3;

        public override IPropertyValue? GetProperty(uint index)
        {
            switch ((D2D1GaussianBlurProp)index)
            {
                case D2D1GaussianBlurProp.D2D1_GAUSSIANBLUR_PROP_STANDARD_DEVIATION:
                    return new WinRTPropertyValue(30.0f);

                case D2D1GaussianBlurProp.D2D1_GAUSSIANBLUR_PROP_OPTIMIZATION:
                    return new WinRTPropertyValue((uint)D2D1_GAUSSIANBLUR_OPTIMIZATION
                        .D2D1_GAUSSIANBLUR_OPTIMIZATION_BALANCED);

                case D2D1GaussianBlurProp.D2D1_GAUSSIANBLUR_PROP_BORDER_MODE:
                    return new WinRTPropertyValue((uint)D2D1_BORDER_MODE.D2D1_BORDER_MODE_HARD);
            }

            return null;
        }
    }

    internal class SaturationEffect : WinUIEffectBase
    {
        public SaturationEffect(IGraphicsEffectSource source) : base(source)
        {
        }

        private enum D2D1_SATURATION_PROP
        {
            D2D1_SATURATION_PROP_SATURATION,
            D2D1_SATURATION_PROP_FORCE_DWORD
        };

        public override Guid EffectId => D2DEffects.CLSID_D2D1Saturation;

        public override uint PropertyCount => 1;

        public override IPropertyValue? GetProperty(uint index)
        {
            switch ((D2D1_SATURATION_PROP)index)
            {
                case D2D1_SATURATION_PROP.D2D1_SATURATION_PROP_SATURATION:
                    return new WinRTPropertyValue(2.0f);
            }

            return null;
        }
    }
}
