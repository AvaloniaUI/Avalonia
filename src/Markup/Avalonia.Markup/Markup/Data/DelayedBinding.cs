// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Logging;

namespace Avalonia.Markup.Data
{
    /// <summary>
    /// Provides delayed bindings for controls.
    /// </summary>
    /// <remarks>
    /// The XAML engine applies its bindings in a delayed manner where bindings are only applied
    /// when a control has finished initializing. This is done because applying bindings as soon
    /// as controls are created means that long-form bindings (i.e. bindings that don't use the
    /// `{Binding}` markup extension but instead use `&lt;Binding&gt;`) don't work, as the binding
    /// is applied to the property before the properties on the `Binding` object are set. Looking 
    /// at WPF it uses a similar mechanism for bindings that come from XAML.
    /// </remarks>
    public static class DelayedBinding
    {
        private static ConditionalWeakTable<IStyledElement, List<Entry>> _entries = 
            new ConditionalWeakTable<IStyledElement, List<Entry>>();

        /// <summary>
        /// Adds a delayed binding to a control.
        /// </summary>
        /// <param name="target">The control.</param>
        /// <param name="property">The property on the control to bind to.</param>
        /// <param name="binding">The binding.</param>
        public static void Add(IStyledElement target, AvaloniaProperty property, IBinding binding)
        {
            if (target.IsInitialized)
            {
                target.Bind(property, binding);
            }
            else
            {

                if (!_entries.TryGetValue(target, out List<Entry> bindings))
                {
                    bindings = new List<Entry>();
                    _entries.Add(target, bindings);

                    // TODO: Make this a weak event listener.
                    target.Initialized += ApplyBindings;
                }

                bindings.Add(new BindingEntry(property, binding));
            }
        }

        /// <summary>
        /// Adds a delayed value to a control.
        /// </summary>
        /// <param name="target">The control.</param>
        /// <param name="property">The property on the control to bind to.</param>
        /// <param name="value">A function which returns the value.</param>
        public static void Add(IStyledElement target, PropertyInfo property, Func<IStyledElement, object> value)
        {
            if (target.IsInitialized)
            {
                property.SetValue(target, value(target));
            }
            else
            {
                List<Entry> bindings;

                if (!_entries.TryGetValue(target, out bindings))
                {
                    bindings = new List<Entry>();
                    _entries.Add(target, bindings);

                    // TODO: Make this a weak event listener.
                    target.Initialized += ApplyBindings;
                }

                bindings.Add(new ClrPropertyValueEntry(property, value));
            }
        }

        /// <summary>
        /// Applies any delayed bindings to a control.
        /// </summary>
        /// <param name="control">The control.</param>
        public static void ApplyBindings(IStyledElement control)
        {
            List<Entry> entries;

            if (_entries.TryGetValue(control, out entries))
            {
                foreach (var entry in entries)
                {
                    entry.Apply(control);
                }

                _entries.Remove(control);
            }
        }

        private static void ApplyBindings(object sender, EventArgs e)
        {
            var target = (IStyledElement)sender;
            ApplyBindings(target);
            target.Initialized -= ApplyBindings;
        }

        private abstract class Entry
        {
            public abstract void Apply(IStyledElement control);
        }

        private class BindingEntry : Entry
        {
            public BindingEntry(AvaloniaProperty property, IBinding binding)
            {
                Binding = binding;
                Property = property;
            }

            public IBinding Binding { get; }
            public AvaloniaProperty Property { get; }

            public override void Apply(IStyledElement control)
            {
                control.Bind(Property, Binding);
            }
        }

        private class ClrPropertyValueEntry : Entry
        {
            public ClrPropertyValueEntry(PropertyInfo property, Func<IStyledElement, object> value)
            {
                Property = property;
                Value = value;
            }

            public PropertyInfo Property { get; }
            public Func<IStyledElement, object> Value { get; }

            public override void Apply(IStyledElement control)
            {
                try
                {
                    Property.SetValue(control, Value(control));
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error)?.Log(
                        LogArea.Property,
                        control,
                        "Error setting {Property} on {Target}: {Exception}",
                        Property.Name,
                        control,
                        e);
                }
            }
        }
    }
}
