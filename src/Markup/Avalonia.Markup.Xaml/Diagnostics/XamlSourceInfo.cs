using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.Diagnostics
{
    /// <summary>
    /// Represents source location information for an element within a XAML or code file.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global //This class is instantiated through the XAML compiler.
    public record XamlSourceInfo : IEquatable<XamlSourceInfo>
    {
        private static ConditionalWeakTable<object, XamlSourceInfo?> s_sourceInfo = [];

        /// <summary>
        /// Gets the full path of the source file containing the element, or <c>null</c> if unavailable.
        /// </summary>
        public Uri SourceUri { get; }
        
        /// <summary>
        /// Gets the 1-based line number in the source file where the element is defined.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets the 1-based column number in the source file where the element is defined.
        /// </summary>
        public int LinePosition { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="XamlSourceInfo"/> class
        /// with a specified line, column, and file path.
        /// </summary>
        /// <param name="line">The line number of the source element.</param>
        /// <param name="column">The column number of the source element.</param>
        /// <param name="filePath">The full path of the source file.</param>
        public XamlSourceInfo(int line, int column, string? filePath)
        {
            LineNumber = line;
            LinePosition = column;
            SourceUri = new Uri(filePath ?? string.Empty);
        }

        /// <summary>
        /// Associates XAML source information with the specified object for debugging or diagnostic purposes.
        /// </summary>
        /// <remarks>This method is typically used to enable enhanced debugging or diagnostics by tracking
        /// the origin of XAML elements at runtime. If the same object is passed multiple times, the most recent source
        /// information will overwrite any previous value.</remarks>
        /// <param name="obj">The object to associate with the XAML source information. Cannot be null.</param>
        /// <param name="info">The XAML source information to associate with the object, or null to remove any existing association.</param>
        public static void SetXamlSourceInfo(object obj, XamlSourceInfo? info)
        {
            s_sourceInfo.AddOrUpdate(obj, info);
        }

        /// <summary>
        /// Retrieves the XAML source information associated with the specified object, if available.
        /// </summary>
        /// <param name="obj">The object for which to obtain XAML source information. Cannot be null.</param>
        /// <returns>A <see cref="XamlSourceInfo"/> instance containing the XAML source information for the specified object, or
        /// <see langword="null"/> if no source information is available.</returns>
        public static XamlSourceInfo? GetXamlSourceInfo(object obj)
        {
            s_sourceInfo.TryGetValue(obj, out var info);

            return info;
        }

        /// <summary>
        /// Returns a string that represents the current <see cref="XamlSourceInfo"/>.
        /// </summary>
        /// <returns>
        /// A formatted string in the form <c>"FilePath:Line,Column"</c>,
        /// or <c>"(unknown):Line,Column"</c> if the file path is not set.
        /// </returns>
        public override string ToString()
        {
            return $"{SourceUri.OriginalString}:{LineNumber},{LinePosition}";
        }
    }
}
