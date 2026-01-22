using System;

namespace Avalonia.Input;

// TODO13: remove
/// <summary>
/// This interface is not used anymore.
/// Use <see cref="IDataTransfer"/> or <see cref="IAsyncDataTransfer"/> instead.
/// </summary>
[Obsolete($"Use {nameof(IDataTransfer)} or {nameof(IAsyncDataTransfer)} instead", true)]
public interface IDataObject;
