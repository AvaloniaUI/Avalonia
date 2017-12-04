using System;

namespace Avalonia.Remote.Protocol
{
    public interface IMessageTypeResolver
    {
        Type GetByGuid(Guid id);
        Guid GetGuid(Type type);
    }
}