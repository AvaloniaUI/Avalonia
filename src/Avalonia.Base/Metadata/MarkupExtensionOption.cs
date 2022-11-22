using System;

namespace Avalonia.Metadata;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MarkupExtensionOptionAttribute : Attribute
{
    public MarkupExtensionOptionAttribute(object value)
    {
        Value = value;
    }

    public object Value { get; }

    public int Priority { get; set; } = 0;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MarkupExtensionDefaultOptionAttribute : Attribute
{

}
