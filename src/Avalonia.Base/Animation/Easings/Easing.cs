using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.SourceGenerator;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Base class for all Easing classes.
    /// </summary>
    [TypeConverter(typeof(EasingTypeConverter))]
    public abstract partial class Easing : IEasing
    {
        /// <inheritdoc/>
        public abstract double Ease(double progress);

        private const string Namespace = "Avalonia.Animation.Easings";

        [SubtypesFactory(typeof(Easing), Namespace)]
        private static partial bool TryCreateEasingInstance(string type, [NotNullWhen(true)] out Easing? instance);

        /// <summary>
        /// Parses a Easing type string.
        /// </summary>
        /// <param name="e">The Easing type string.</param>
        /// <returns>Returns the instance of the parsed type.</returns>
        public static Easing Parse(string e)
        {
#if NETSTANDARD2_0
            if (e.Contains(","))
#else
            if (e.Contains(','))
#endif
            {
                return new SplineEasing(KeySpline.Parse(e, CultureInfo.InvariantCulture));
            }

            return TryCreateEasingInstance(e, out var easing)
                ? easing
                : throw new FormatException($"Easing \"{e}\" was not found in {Namespace} namespace.");
        }
    }
}
