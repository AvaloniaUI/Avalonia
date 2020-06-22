using System;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a platform-specific embeddable window implementation.
    /// </summary>
    public interface IEmbeddableWindowImpl : ITopLevelImpl
    {
        event Action LostFocus;
    }
}
