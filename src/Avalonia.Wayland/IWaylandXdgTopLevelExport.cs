using System;
using System.Threading.Tasks;

namespace Avalonia.Wayland;

/// <summary>
/// UI-thread façade over a worker-thread <c>zxdg_exported_v2</c> handle. Restricts
/// the UI side to the only two operations it should care about: awaiting the
/// compositor-issued handle string and disposing the export. The underlying worker
/// object is intentionally not exposed.
/// </summary>
internal interface IWaylandXdgTopLevelExport : IDisposable
{
    /// <summary>
    /// Resolves to the raw compositor-issued handle string (no protocol prefix), or
    /// <c>null</c> if the wayland connection died before the handle was delivered.
    /// </summary>
    Task<string?> HandleTask { get; }
}
