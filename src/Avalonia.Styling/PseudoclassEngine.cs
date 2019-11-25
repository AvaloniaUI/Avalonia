using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace Avalonia
{
    internal static class PseudoclassEngine
    {
        private static Dictionary<Type, List<IEntry>> _entries = new Dictionary<Type, List<IEntry>>();
        private static Dictionary<AvaloniaProperty, List<IEntry>> _properties = new Dictionary<AvaloniaProperty, List<IEntry>>();
        private static Dictionary<Type, List<IEntry>> _cache = new Dictionary<Type, List<IEntry>>();

        public static void Created(StyledElement element)
        {
            var t = element.GetType();

            if (!_cache.TryGetValue(t, out var list))
            {
                list = new List<IEntry>();

                foreach (var i in _entries)
                {
                    if (i.Key.IsAssignableFrom(t))
                    {
                        list.AddRange(i.Value);
                    }
                }

                _cache.Add(t, list);
            }

            foreach (var i in list)
            {
                i.Update(element);
            }
        }

        public static void Register<T>(
            Type type,
            AvaloniaProperty<T> property,
            Func<T, bool> selector,
            string className)
        {
            if (!_entries.TryGetValue(type, out var list))
            {
                list = new List<IEntry>();
                _entries.Add(type, list);
            }

            var entry = new Entry<T>(type, property, selector, className);
            list.Add(entry);
            
            if (!_properties.TryGetValue(property, out list))
            {
                list = new List<IEntry>();
                _properties.Add(property, list);
                property.Changed.Subscribe(ValueChanged);
            }

            list.Add(entry);
        }

        private static void ValueChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is StyledElement element &&
                _properties.TryGetValue(e.Property, out var list))
            {
                var t = e.Sender.GetType();

                foreach (var i in list)
                {
                    if (i.Type.IsAssignableFrom(t))
                    {
                        i.Update(element);
                    }
                }
            }
        }

        private interface IEntry
        {
            Type Type { get; }
            void Update(StyledElement element);
        }

        private class Entry<T> : IEntry
        {
            public Entry(
                Type type,
                AvaloniaProperty<T> property,
                Func<T, bool> selector,
                string className)
            {
                Type = type;
                Property = property;
                Selector = selector;
                ClassName = className;
            }

            public Type Type { get; }
            public AvaloniaProperty<T> Property { get; }
            public Func<T, bool> Selector { get; }
            public string ClassName { get; }

            public void Update(StyledElement element)
            {
                var value = element.GetValue(Property);

                if (Selector(value))
                {
                    ((IPseudoClasses)element.Classes).Add(ClassName);
                }
                else
                {
                    ((IPseudoClasses)element.Classes).Remove(ClassName);
                }
            }
        }
    }
}
