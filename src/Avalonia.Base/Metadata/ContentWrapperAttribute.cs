using System;

namespace Avalonia.Metadata
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ContentWrapperAttribute : Attribute
    {
        public ContentWrapperAttribute(Type contentWrapper)
        {
            ContentWrapper = contentWrapper;
        }

        public Type ContentWrapper { get; private set; }
    }
}
