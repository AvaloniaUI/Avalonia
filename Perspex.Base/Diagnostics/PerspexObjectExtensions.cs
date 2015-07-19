// --------------------------------------------------------------------
// <copyright file="PerspexObjectExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    public static class PerspexObjectExtensions
    {
        public static PerspexPropertyValue GetDiagnostic(this PerspexObject o, PerspexProperty property)
        {
            var set = o.GetSetValues();

            PriorityValue value;

            if (set.TryGetValue(property, out value))
            {
                return new PerspexPropertyValue(
                    property,
                    value.Value,
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
