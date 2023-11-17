using System.ComponentModel;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    [TypeConverter(typeof(TransformConverter))]
    [NotClientImplementable]
    public interface ITransform
    {
        Matrix Value { get; }
    }
}
