using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Avalonia.Controls.ApplicationLifetimes
{
    [NotClientImplementable]
    public interface IMultiSceneApplicationLifetime : IApplicationLifetime
    {
        IReadOnlyList<Scene> Scenes { get; }
    }

    [NotClientImplementable]
    public interface IApplicationLifetimeSaveRestoreFeature
    {
        // Creation parameters or restored checkpoint, available before view creation.
        IReadOnlyDictionary<string, object?> InitialState { get; }

        // Identifies which of those two sources supplied InitialState.
        bool IsRestored { get; }

        // Synchronously captures a new checkpoint.
        event EventHandler<ApplicationLifetimeStateEventArgs>? SaveRequested;

        // Marks the previously captured state as outdated.
        void InvalidateState();
    }

    public sealed class ApplicationLifetimeStateEventArgs(RoutedEvent routedEvent) : RoutedEventArgs(routedEvent)
    {
        public IDictionary<string, object?>? State { get; init; }
    }
}
