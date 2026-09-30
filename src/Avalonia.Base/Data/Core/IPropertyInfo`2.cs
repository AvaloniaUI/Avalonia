using System;
using Avalonia.Metadata;

namespace Avalonia.Data.Core;

[NotClientImplementable]
public interface IPropertyInfo<TSource, TValue> : IPropertyInfo
    where TSource : class
{
    TValue Get(TSource target);
    void Set(TSource target, TValue value);
}
