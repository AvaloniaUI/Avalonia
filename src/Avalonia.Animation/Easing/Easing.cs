using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Base class for all Easing classes.
    /// </summary>
    [TypeConverter(typeof(EasingTypeConverter))]
    public abstract class Easing : IEasing
    {
        /// <inheritdoc/>
        public abstract double Ease(double progress);

        static Dictionary<string, Type> _easingTypes;

        static readonly Type s_thisType = typeof(Easing);

        /// <summary>
        /// Parses a Easing type string.
        /// </summary>
        /// <param name="e">The Easing type string.</param>
        /// <returns>Returns the instance of the parsed type.</returns>
        public static Easing Parse(string e)
        {
            if (e.Contains(','))
            {
                var k = e.Split(',');

                if (k.Count() != 4)
                {
                    throw new FormatException($"SplineEasing only accepts exactly 4 arguments.");
                }
                
                var splineEase = new SplineEasing();

                var setterArray = new Action<double>[4]
                {
                    (x) => splineEase.X1 = x,

                    (x) => splineEase.Y1 = x,

                    (x) => splineEase.X2 = x,

                    (x) => splineEase.Y2 = x
                };

                for (int i = 0; i < 4; i++)
                {
                    if (double.TryParse(k[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var x))
                    {
                        setterArray[i](x);
                    }
                    else
                    {
                        throw new FormatException($"Parameter string \"{k[i]}\" is not a double.");
                    }
                }

                return splineEase;
            }

            if (_easingTypes == null)
            {
                _easingTypes = new Dictionary<string, Type>();

                // Fetch the built-in easings.
                var derivedTypes = typeof(Easing).Assembly.GetTypes()
                                      .Where(p => p.Namespace == s_thisType.Namespace)
                                      .Where(p => p.IsSubclassOf(s_thisType))
                                      .Select(p => p).ToList();

                foreach (var easingType in derivedTypes)
                    _easingTypes.Add(easingType.Name, easingType);
            }

            if (_easingTypes.ContainsKey(e))
            {
                var type = _easingTypes[e];
                return (Easing)Activator.CreateInstance(type);
            }
            else
            {
                throw new FormatException($"Easing \"{e}\" was not found in {s_thisType.Namespace} namespace.");
            }
        }
    }
}
