using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    public abstract class SelectingItemsControlAutomationPeer : ItemsControlAutomationPeer,
        ISelectionProvider
    {
        private ISelectionModel _selection;

        protected SelectingItemsControlAutomationPeer(SelectingItemsControl owner)
            : base(owner) 
        {
            _selection = owner.GetValue(ListBox.SelectionProperty);
            _selection.SelectionChanged += OwnerSelectionChanged;
            owner.PropertyChanged += OwnerPropertyChanged;
        }

        public bool CanSelectMultiple => GetSelectionModeCore().HasAllFlags(SelectionMode.Multiple);
        public bool IsSelectionRequired => GetSelectionModeCore().HasAllFlags(SelectionMode.AlwaysSelected);
        public IReadOnlyList<AutomationPeer> GetSelection() => GetSelectionCore() ?? Array.Empty<AutomationPeer>();

        protected virtual IReadOnlyList<AutomationPeer>? GetSelectionCore()
        {
            List<AutomationPeer>? result = null;

            if (Owner is SelectingItemsControl owner)
            {
                var selection = Owner.GetValue(ListBox.SelectionProperty);

                foreach (var i in selection.SelectedIndexes)
                {
                    var container = owner.ContainerFromIndex(i);

                    if (container is Control c && c.IsAttachedToVisualTree)
                    {
                        var peer = GetOrCreate(c);

                        if (peer is object)
                        {
                            result ??= new List<AutomationPeer>();
                            result.Add(peer);
                        }
                    }
                }

                return result;
            }

            return result;
        }

        protected virtual SelectionMode GetSelectionModeCore()
        {
            return (Owner as SelectingItemsControl)?.GetValue(ListBox.SelectionModeProperty) ?? SelectionMode.Single;
        }

        protected virtual void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ListBox.SelectionProperty)
            {
                _selection.SelectionChanged -= OwnerSelectionChanged;
                _selection = Owner.GetValue(ListBox.SelectionProperty);
                _selection.SelectionChanged += OwnerSelectionChanged;
                RaiseSelectionChanged();
            }
        }

        protected virtual void OwnerSelectionChanged(object? sender, SelectionModelSelectionChangedEventArgs e)
        {
            RaiseSelectionChanged();
        }

        private void RaiseSelectionChanged()
        {
            RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
        }
    }
}
