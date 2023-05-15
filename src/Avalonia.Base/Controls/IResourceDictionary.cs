using System.Collections.Generic;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public interface IResourceDictionary : IResourceProvider, IDictionary<object, object?>
    {
        /// <summary>
        /// Gets a collection of child resource dictionaries.
        /// </summary>
        IList<IResourceProvider> MergedDictionaries { get; }

        /// <summary>
        /// Gets a collection of merged resource dictionaries that are specifically keyed and composed to address theme scenarios.
        /// </summary>
        IDictionary<ThemeVariant, IThemeVariantProvider> ThemeDictionaries { get; }
    }
}
