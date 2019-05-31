using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls.Repeaters
{
    internal class RecyclePool
    {
        public static readonly AttachedProperty<IDataTemplate> OriginTemplateProperty =
            AvaloniaProperty.RegisterAttached<Control, IDataTemplate>("OriginTemplate", typeof(RecyclePool));

        private static ConditionalWeakTable<IDataTemplate, RecyclePool> s_pools = new ConditionalWeakTable<IDataTemplate, RecyclePool>();
        private readonly Dictionary<string, List<ElementInfo>> _elements = new Dictionary<string, List<ElementInfo>>();

        public static RecyclePool GetPoolInstance(IDataTemplate dataTemplate)
        {
            s_pools.TryGetValue(dataTemplate, out var result);
            return result;
        }

        public static void SetPoolInstance(IDataTemplate dataTemplate, RecyclePool value) => s_pools.Add(dataTemplate, value);

        public void PutElement(IControl element, string key, IControl owner)
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

        public IControl TryGetElement(string key, IControl owner)
        {
            if (_elements.TryGetValue(key, out var elements))
            {
                if (elements.Count > 0)
                {
                    // Prefer an element from the same owner or with no owner so that we don't incur
                    // the enter/leave cost during recycling.
                    // TODO: prioritize elements with the same owner to those without an owner.
                    var elementInfo = elements.FirstOrDefault(x => x.Owner == owner) ?? elements.LastOrDefault();
                    elements.Remove(elementInfo);

                    var ownerAsPanel = EnsureOwnerIsPanelOrNull(owner);
                    if (elementInfo.Owner != null && elementInfo.Owner != ownerAsPanel)
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

        private IPanel EnsureOwnerIsPanelOrNull(IControl owner)
        {
            if (owner is IPanel panel)
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
            public ElementInfo(IControl element, IPanel owner)
            {
                Element = element;
                Owner = owner;
            }

            public IControl Element { get; }
            public IPanel Owner { get;}
        }
    }
}
