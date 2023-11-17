using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Data.Core;
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
    internal static class DelayedBinding
    {
        private static readonly ConditionalWeakTable<StyledElement, List<Entry>> _entries = new();

        /// <summary>
        /// Adds a delayed binding to a control.
        /// </summary>
        /// <param name="target">The control.</param>
        /// <param name="property">The property on the control to bind to.</param>
        /// <param name="binding">The binding.</param>
        public static void Add(StyledElement target, AvaloniaProperty property, IBinding binding)
        {
            if (target.IsInitialized)
            {
                target.Bind(property, binding);
            }
            else
            {

                if (!_entries.TryGetValue(target, out var bindings))
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
        public static void Add(StyledElement target, IPropertyInfo property, Func<StyledElement, object?> value)
        {
            if (target.IsInitialized)
            {
                property.Set(target, value(target));
            }
            else
            {
                if (!_entries.TryGetValue(target, out var bindings))
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
        public static void ApplyBindings(StyledElement control)
        {
            if (_entries.TryGetValue(control, out var entries))
            {
                foreach (var entry in entries)
                {
                    entry.Apply(control);
                }

                _entries.Remove(control);
            }
        }

        private static void ApplyBindings(object? sender, EventArgs e)
        {
            var target = (StyledElement)sender!;
            ApplyBindings(target);
            target.Initialized -= ApplyBindings;
        }

        private abstract class Entry
        {
            public abstract void Apply(StyledElement control);
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

            public override void Apply(StyledElement control)
            {
                control.Bind(Property, Binding);
            }
        }

        private class ClrPropertyValueEntry : Entry
        {
            public ClrPropertyValueEntry(IPropertyInfo property, Func<StyledElement, object?> value)
            {
                Property = property;
                Value = value;
            }

            public IPropertyInfo Property { get; }
            public Func<StyledElement, object?> Value { get; }

            public override void Apply(StyledElement control)
            {
                try
                {
                    Property.Set(control, Value(control));
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, LogArea.Property)?.Log(
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
