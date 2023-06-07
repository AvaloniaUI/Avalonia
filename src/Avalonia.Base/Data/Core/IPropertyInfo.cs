using System;
using Avalonia.Metadata;

namespace Avalonia.Data.Core
{
    [NotClientImplementable]
    public interface IPropertyInfo
    {
        string Name { get; }
        object? Get(object target);
        void Set(object target, object? value);
        bool CanSet { get; }
        bool CanGet { get; }
        Type PropertyType { get; }
    }
}
