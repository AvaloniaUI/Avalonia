// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.ViewModels
{
    using WellKNownProperties = List<WellKnownProperty>;
    using WellKNownPropertiesReg = Dictionary<Func<object, bool>, List<WellKnownProperty>>;

    internal class WellKnownProperty
    {
        private static IObservable<object> LayoutUpdatedEvent(object ctrl) => Observable.FromEventPattern(ctrl, nameof(Layoutable.LayoutUpdated));

        private static WellKNownPropertiesReg _registry = new WellKNownPropertiesReg
        {
            {
                o => true,
                new WellKNownProperties
                {
                    new WellKnownProperty()
                    {
                        Name = "ToString()",
                        Type = typeof(string),
                        Getter = o => o.ToString(),
                        Changed = o => Observable.Never<object>()
                    }
                }
            },
            {
                o => o is Style,
                new WellKNownProperties
                {
                    new WellKnownProperty()
                    {
                        Name = "Selector",
                        Type = typeof(Selector),
                        Getter = o => (o as Style).Selector.ToString(),
                        Changed = o => Observable.Never<object>()
                    }
                }
            },
            {
                o => o is Control,
                new WellKNownProperties
                {
                    new WellKnownProperty()
                    {
                        Name = nameof(StyledElement.Classes),
                        Type = typeof(string),
                        Getter = o => string.Join(" ", (o as StyledElement).Classes),
                        Setter = (o, v) => (o as StyledElement).Classes = Classes.Parse((v??"").ToString()),
                        Changed = o => Observable.FromEventPattern((o as StyledElement).Classes, nameof(StyledElement.Classes.CollectionChanged))
                    },
                    new WellKnownProperty()
                    {
                        Name = "Layout State",
                        Type = typeof(string),
                        Getter = o =>
                        {
                            var l = o as ILayoutable;
                            return $"measured: {l.IsMeasureValid} -> {l.PreviousMeasure} arranged: {l.IsArrangeValid} -> {l.PreviousArrange}";
                        },
                        Changed = o => LayoutUpdatedEvent(o)
                    },
                }
            },
            {
                o => o is Grid,
                new WellKNownProperties
                {
                    new WellKnownProperty()
                    {
                        Name = nameof(Grid.ColumnDefinitions),
                        Type = typeof(string),
                        Getter = o => string.Join(",", (o as Grid).ColumnDefinitions.Select(c=>c.Width.ToString())),
                        Setter = (o,v) => (o as Grid).ColumnDefinitions = ColumnDefinitions.Parse(v?.ToString()??""),
                        Changed = o => Observable.Never<object>()
                    },
                    new WellKnownProperty()
                    {
                        Name = nameof(Grid.ColumnDefinitions) + " (Actual)",
                        Type = typeof(string),
                        Getter = o => string.Join(",", (o as Grid).ColumnDefinitions.Select(c=>c.ActualWidth.ToString())),
                        Changed = o => LayoutUpdatedEvent(o)
                    },
                    new WellKnownProperty()
                    {
                        Name = nameof(Grid.RowDefinitions),
                        Type = typeof(string),
                        Getter = o => string.Join(",", (o as Grid).RowDefinitions.Select(c=>c.Height.ToString())),
                        Setter = (o,v) => (o as Grid).RowDefinitions = RowDefinitions.Parse(v?.ToString()??""),
                        Changed = o => Observable.Never<object>()
                    },
                    new WellKnownProperty()
                    {
                        Name = nameof(Grid.RowDefinitions) + " (Actual)",
                        Type = typeof(string),
                        Getter = o => string.Join(",", (o as Grid).RowDefinitions.Select(c=>c.ActualHeight.ToString())),
                        Changed = o => LayoutUpdatedEvent(o)
                    },
                }
            },
        };

        public static IEnumerable<WellKnownProperty> Get(AvaloniaObject obj)
        {
            return _registry.Where(v => v.Key(obj)).SelectMany(v => v.Value);
        }

        public string Name;
        public Type Type;
        public Func<object, object> Getter;
        public Action<object, object> Setter;
        public Func<object, IObservable<object>> Changed;
    }

    internal class ControlDetailsViewModel : ViewModelBase, IDisposable
    {
        private object _control;

        public ControlDetailsViewModel(object avObject)
        {
            if (avObject is AvaloniaObject avaloniaObject)
            {
                var props = WellKnownProperty.Get(avaloniaObject)
                    .Select(x => new PropertyDetails(avaloniaObject, x))
                    .ToList();

                var avProps = AvaloniaPropertyRegistry.Instance.GetRegistered(avaloniaObject)
                    .Select(x => new PropertyDetails(avaloniaObject, x))
                    .OrderBy(x => x.IsAttached)
                    .ThenBy(x => x.Name)
                    .ToList();

                props.AddRange(avProps);

                if (avObject is Control c)
                {
                    if (c.Parent != null)
                    {
                        var attached = AvaloniaPropertyRegistry.Instance.GetRegistered((AvaloniaObject)c.Parent)
                                .Where(p => p.IsAttached)
                                .Select(x => new PropertyDetails(avaloniaObject, x))
                                .OrderBy(x => x.Name)
                                .ToList();

                        props.AddRange(attached);
                    }
                }

                if (avObject is Style style)
                {
                    WellKnownProperty forSetter(ISetter setter)
                    {
                        if (setter is Setter sett)
                        {
                            return new WellKnownProperty()
                            {
                                Name = sett.Property.Name,
                                Type = sett.Property.PropertyType,
                                Getter = o => sett.Value,
                                Setter = (o,v) => sett.Value = v,
                                Changed = o => Observable.Never<object>()
                            };
                        }

                        return new WellKnownProperty()
                        {
                            Name = setter.GetType().Name,
                            Type = setter.GetType(),
                            Getter = o => setter.ToString(),
                            Changed = o => Observable.Never<object>()
                        };
                    }

                    var setters = style.Setters.Select(s => new PropertyDetails(style, forSetter(s))).ToList();

                    props.AddRange(setters);
                }

                Properties = props;
            }

            _control = avObject;
        }

        public IEnumerable<PropertyDetails> Properties
        {
            get;
            private set;
        }

        public void Dispose()
        {
            foreach (var d in Properties)
            {
                d.Dispose();
            }
        }
    }
}
