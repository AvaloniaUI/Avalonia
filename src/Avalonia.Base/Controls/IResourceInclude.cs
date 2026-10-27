namespace Avalonia.Controls;

/// <summary>
/// Resource provider, which loads its resource dictionary on demand, such as ResourceInclude.
/// </summary>
internal interface IResourceInclude : IResourceProvider
{
    /// <summary>
    /// Gets the loaded resource dictionary.
    /// </summary>
    IResourceDictionary Loaded { get; }
}
