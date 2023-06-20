// ReSharper disable once CheckNamespace

using System;
using System.ComponentModel;
using Avalonia.Metadata;

namespace Avalonia.Media;

[TypeConverter(typeof(EffectConverter))]
[NotClientImplementable]
public interface IEffect
{
    
}

public interface IMutableEffect : IEffect
{
    /// <summary>
    /// Creates an immutable clone of the effect.
    /// </summary>
    /// <returns>The immutable clone.</returns>
    internal IImmutableEffect ToImmutable();
}

public interface IImmutableEffect : IEffect, IEquatable<IEffect>
{
    
}