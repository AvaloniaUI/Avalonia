// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Data;

namespace Perspex.Diagnostics
{
    /// <summary>
    /// Defines diagnostic extensions on <see cref="PerspexObject"/>s.
    /// </summary>
    public static class PerspexObjectExtensions
    {
        /// <summary>
        /// Gets a diagnostic for a <see cref="PerspexProperty"/> on a <see cref="PerspexObject"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>
        /// A <see cref="PerspexPropertyValue"/> that can be used to diagnose the state of the
        /// property on the object.
        /// </returns>
        public static PerspexPropertyValue GetDiagnostic(this PerspexObject o, PerspexProperty property)
        {
            var set = o.GetSetValues();

            PriorityValue value;

            if (set.TryGetValue(property, out value))
            {
                return new PerspexPropertyValue(
                    property,
                    o.GetValue(property),
                    (BindingPriority)value.ValuePriority,
                    value.GetDiagnostic());
            }
            else
            {
                return new PerspexPropertyValue(
                    property,
                    o.GetValue(property),
                    BindingPriority.Unset,
                    "Unset");
            }
        }
    }
}
