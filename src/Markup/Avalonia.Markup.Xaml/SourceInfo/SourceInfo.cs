using System;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.SourceInfo
{
    /// <summary>
    /// Represents source location information for an element within a XAML or code file.
    /// </summary>
    /// <remarks>
    /// This struct is typically used to store the line, column, and file path of a control or object
    /// to enable navigation or diagnostics (for example, jumping to a definition in Visual Studio).
    /// </remarks>
    public readonly struct SourceInfo : IEquatable<SourceInfo>
    {
        /// <summary>
        /// Gets the 1-based line number in the source file where the element is defined.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// Gets the 1-based column number in the source file where the element is defined.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Gets the full file path of the source file containing the element, or <c>null</c> if unavailable.
        /// </summary>
        public string? FilePath { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceInfo"/> struct
        /// with a specified line and column but no file path.
        /// </summary>
        /// <param name="line">The line number of the source element.</param>
        /// <param name="column">The column number of the source element.</param>
        public SourceInfo(int line, int column)
        {
            Line = line;
            Column = column;
            FilePath = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceInfo"/> struct
        /// with a specified line, column, and file path.
        /// </summary>
        /// <param name="line">The line number of the source element.</param>
        /// <param name="column">The column number of the source element.</param>
        /// <param name="filePath">The full path of the source file.</param>
        public SourceInfo(int line, int column, string? filePath)
        {
            Line = line;
            Column = column;
            FilePath = filePath;
        }

        /// <summary>
        /// Returns a string that represents the current <see cref="SourceInfo"/>.
        /// </summary>
        /// <returns>
        /// A formatted string in the form <c>"FilePath:Line,Column"</c>,
        /// or <c>"(unknown):Line,Column"</c> if the file path is not set.
        /// </returns>
        public override string ToString()
        {
            var file = string.IsNullOrEmpty(FilePath) ? "(unknown)" : System.IO.Path.GetFileName(FilePath);
            return $"{file}:{Line},{Column}";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="SourceInfo"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is a <see cref="SourceInfo"/> with the same
        /// <see cref="Line"/>, <see cref="Column"/>, and <see cref="FilePath"/> values; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            return obj is SourceInfo info &&
                   Line == info.Line &&
                   Column == info.Column &&
                   FilePath == info.FilePath;
        }

        /// <summary>
        /// Returns a hash code for this <see cref="SourceInfo"/>.
        /// </summary>
        /// <returns>An integer hash code computed from <see cref="Line"/>, <see cref="Column"/>, and <see cref="FilePath"/>.</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (Line.GetHashCode());
            hash = hash * 31 + (Column.GetHashCode());
            hash = hash * 31 + (FilePath?.GetHashCode() ?? 0);
            return hash;
        }

        /// <summary>
        /// Determines whether two <see cref="SourceInfo"/> instances are equal.
        /// </summary>
        /// <param name="left">Left-hand operand.</param>
        /// <param name="right">Right-hand operand.</param>
        /// <returns><c>true</c> when both operands represent the same source location; otherwise <c>false</c>.</returns>
        public static bool operator ==(SourceInfo left, SourceInfo right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="SourceInfo"/> instances are not equal.
        /// </summary>
        /// <param name="left">Left-hand operand.</param>
        /// <param name="right">Right-hand operand.</param>
        /// <returns><c>true</c> when operands do not represent the same source location; otherwise <c>false</c>.</returns>
        public static bool operator !=(SourceInfo left, SourceInfo right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Indicates whether the current instance is equal to another <see cref="SourceInfo"/>.
        /// </summary>
        /// <param name="other">The other <see cref="SourceInfo"/> to compare with.</param>
        /// <returns><c>true</c> when both instances represent the same source location; otherwise <c>false</c>.</returns>
        public bool Equals(SourceInfo other)
        {
            return Line == other.Line && Column == other.Column && FilePath == other.FilePath;
        }
    }


    /// <summary>
    /// Provides an attached property for storing <see cref="SourceInfo"/> metadata on Avalonia controls.
    /// </summary>
    /// <remarks>
    /// This class is primarily used by the XAML compiler or runtime tooling to associate 
    /// source location information (file path, line, and column) with UI elements.
    /// <para/>
    /// The <see cref="SourceInfo"/> property is typically populated automatically by the Avalonia XAML compiler:
    /// <list type="number">
    /// <item>
    /// When running in <b>design mode</b> — to enable designer tools to map rendered elements back to source XAML.
    /// </item>
    /// <item>
    /// When running in a <b>debug configuration</b> — allowing runtime inspection or navigation
    /// back to the originating XAML source (this can be overridden by setting the 
    /// <c>AvaloniaXamlCreateSourceInfo</c> build property).
    /// </item>
    /// </list>
    /// </remarks>
    public static class Source
    {
        /// <summary>
        /// Defines the attached <see cref="SourceInfo"/> property that stores the source location
        /// information for a control.
        /// </summary>
        public static readonly AttachedProperty<SourceInfo> SourceInfoProperty =
            AvaloniaProperty.RegisterAttached<AvaloniaObject, SourceInfo>(
                "SourceInfo",
                typeof(Source));

        /// <summary>
        /// Gets the <see cref="SourceInfo"/> value associated with the specified visual.
        /// </summary>
        /// <param name="v">The control from which to retrieve the source information.</param>
        /// <returns>
        /// The <see cref="SourceInfo"/> that describes where the control was defined in XAML.
        /// If no value is set, the default (empty) <see cref="SourceInfo"/> is returned.
        /// </returns>
        public static SourceInfo GetSourceInfo(AvaloniaObject v)
        {
            return v.GetValue(SourceInfoProperty);
        }

        /// <summary>
        /// Sets the <see cref="SourceInfo"/> value for the specified control.
        /// </summary>
        /// <param name="v">The control to associate with the source information.</param>
        /// <param name="value">The <see cref="SourceInfo"/> describing the control’s origin in XAML.</param>
        /// <remarks>
        /// Normally this method is invoked automatically by the XAML compiler or design-time tools.
        /// You generally do not need to set this property manually unless you are generating controls dynamically.
        /// </remarks>
        public static void SetSourceInfo(AvaloniaObject v, SourceInfo value)
        {
            v.SetValue(SourceInfoProperty, value);
        }
    }

    /// <summary>
    /// Attribute applied to types generated from XAML to record the original source file name.
    /// </summary>
    /// <remarks>
    /// The XAML compiler may add this attribute to generated classes so design-time tooling
    /// can map a runtime type back to its originating XAML file.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class XamlSourceInfoAttribute : Attribute
    {
        /// <summary>
        /// Gets the source file name that produced the type.
        /// </summary>
        public string SourceFileName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XamlSourceInfoAttribute"/> class.
        /// </summary>
        /// <param name="sourceFileName">The source file name associated with the generated type.</param>
        public XamlSourceInfoAttribute(string sourceFileName)
        {
            SourceFileName = sourceFileName;
        }
    }
}
