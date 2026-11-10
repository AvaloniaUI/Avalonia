using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Controls.ApplicationLifetimes
{
    [NotClientImplementable]
    public interface IMultiSceneApplicationLifetime : IApplicationLifetime
    {
        // Currently connected scene lifetimes.
        IReadOnlyList<IMultiSceneApplicationLifetime> Scenes { get; }

        // Requests a new scene with the supplied creation parameters.
        // Completes when the platform accepts the request.
        Task RequestSceneAsync(
            IReadOnlyDictionary<string, object?> parameters);
    }

    [NotClientImplementable]
    public interface IApplicationLifetimeSaveRestoreFeature
    {
        // Creation parameters or restored checkpoint, available before view creation.
        IReadOnlyDictionary<string, object?> InitialState { get; }

        // Identifies which of those two sources supplied InitialState.
        bool IsRestored { get; }

        // Synchronously captures a new checkpoint.
        event EventHandler<ApplicationLifetimeSaveEventArgs>? SaveRequested;

        // Marks the previously captured state as outdated.
        void InvalidateState();
    }

    public sealed class ApplicationLifetimeSaveEventArgs : EventArgs
    {
        // A fresh output bag for this checkpoint.
        public IDictionary<string, object?>? State { get; }
    }
}
