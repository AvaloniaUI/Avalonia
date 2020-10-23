using System;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;


namespace Avalonia.Win32
{
    abstract class EffectBase : IGraphicsEffect,  IGraphicsEffectSource, global::Windows.Graphics.Effects.Interop.IGraphicsEffectD2D1Interop
    {
        private IGraphicsEffectSource[] _sources;

        public EffectBase(params IGraphicsEffectSource[] sources)
        {
            _sources = sources;
        }

        private IGraphicsEffectSource _source;

        public virtual string Name { get; set; }

        public abstract Guid EffectId { get; }

        public abstract uint PropertyCount { get; }

        public uint SourceCount => (uint)_sources.Length;

        public IGraphicsEffectSource GetSource(uint index)
        {
            if(index < _sources.Length)
            {
                return _sources[index];
            }

            return null;
        }

        public uint GetNamedPropertyMapping(string name, out GRAPHICS_EFFECT_PROPERTY_MAPPING mapping)
        {
            throw new NotImplementedException();
        }

        public abstract object GetProperty(uint index);
    }
}

