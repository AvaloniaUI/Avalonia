// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
    public class RecyclePool
    {
        internal static readonly AttachedProperty<IDataTemplate> OriginTemplateProperty =
            AvaloniaProperty.RegisterAttached<RecyclePool, Control, IDataTemplate>("OriginTemplate");

        internal static readonly AttachedProperty<string> ReuseKeyProperty =
            AvaloniaProperty.RegisterAttached<RecyclePool, Control, string>("ReuseKey", string.Empty);

        private static ConditionalWeakTable<IDataTemplate, RecyclePool> s_pools = new ConditionalWeakTable<IDataTemplate, RecyclePool>();
        private readonly Dictionary<string, List<ElementInfo>> _elements = new Dictionary<string, List<ElementInfo>>();

        public static RecyclePool? GetPoolInstance(IDataTemplate dataTemplate)
        {
            s_pools.TryGetValue(dataTemplate, out var result);
            return result;
        }

        public static void SetPoolInstance(IDataTemplate dataTemplate, RecyclePool value) => s_pools.Add(dataTemplate, value);

        public void PutElement(Control element, string key, Control? owner)
        {
            var ownerAsPanel = EnsureOwnerIsPanelOrNull(owner);
            var elementInfo = new ElementInfo(element, ownerAsPanel);

            if (!_elements.TryGetValue(key, out var pool))
            {
                pool = new List<ElementInfo>();
                _elements.Add(key, pool);
            }

            pool.Add(elementInfo);
        }

        public Control? TryGetElement(string key, Control? owner)
        {
            if (_elements.TryGetValue(key, out var elements))
            {
                if (elements.Count > 0)
                {
                    // Prefer an element from the same owner or with no owner so that we don't incur
                    // the enter/leave cost during recycling.
                    // TODO: prioritize elements with the same owner to those without an owner.
                    var elementInfo = elements.FirstOrDefault(x => x.Owner == owner) ?? elements.LastOrDefault();
                    elements.Remove(elementInfo!);

                    var ownerAsPanel = EnsureOwnerIsPanelOrNull(owner);
                    if (elementInfo!.Owner != null && elementInfo.Owner != ownerAsPanel)
                    {
                        // Element is still under its parent. remove it from its parent.
                        var panel = elementInfo.Owner;
                        if (panel != null)
                        {
                            int childIndex = panel.Children.IndexOf(elementInfo.Element);
                            if (childIndex == -1)
                            {
                                throw new KeyNotFoundException("ItemsRepeater's child not found in its Children collection.");
                            }

                            panel.Children.RemoveAt(childIndex);
                        }
                    }

                    return elementInfo.Element;
                }
            }

            return null;
        }

        internal string GetReuseKey(Control element) => element.GetValue(ReuseKeyProperty);
        internal void SetReuseKey(Control element, string value) => element.SetValue(ReuseKeyProperty, value);

        private Panel? EnsureOwnerIsPanelOrNull(Control? owner)
        {
            if (owner is Panel panel)
            {
                return panel;
            }
            else if (owner != null)
            {
                throw new InvalidOperationException("Owner must be IPanel or null.");
            }

            return null;
        }

        private class ElementInfo
        {
            public ElementInfo(Control element, Panel? owner)
            {
                Element = element;
                Owner = owner;
            }
            
            public Control Element { get; }
            public Panel? Owner { get;}
        }
    }
}
