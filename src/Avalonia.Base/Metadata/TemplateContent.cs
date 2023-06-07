using System;

namespace Avalonia.Metadata
{
    /// <summary>
    /// Defines the property that contains the object's content in markup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class TemplateContentAttribute : Attribute
    {
        public Type? TemplateResultType { get; set; }
    }
}
