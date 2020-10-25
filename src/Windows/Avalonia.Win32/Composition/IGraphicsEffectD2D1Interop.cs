using System;
using System.Runtime.InteropServices;
using WinRT;

namespace Windows.Graphics.Effects.Interop
{
    [WindowsRuntimeType]
    [Guid("2FC57384-A068-44D7-A331-30982FCF7177")]
    public interface IGraphicsEffectD2D1Interop
    {
        Guid EffectId { get; }

        uint GetNamedPropertyMapping(string name, out GRAPHICS_EFFECT_PROPERTY_MAPPING mapping);

        object GetProperty(uint index);

        uint PropertyCount { get; }

        IGraphicsEffectSource GetSource(uint index);

        uint SourceCount { get; }
    }
}

