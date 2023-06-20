using System;

namespace Avalonia.Remote.Protocol
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AvaloniaRemoteMessageGuidAttribute : Attribute
    {
        public Guid Guid { get; }

        public AvaloniaRemoteMessageGuidAttribute(string guid)
        {
            Guid = Guid.Parse(guid);
        }
    }
}
