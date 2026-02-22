using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Utils;

namespace Avalonia.Automation.Peers
{
    public class ListBoxAutomationPeer : SelectingItemsControlAutomationPeer
    {
        public ListBoxAutomationPeer(ListBox owner)
            : base(owner)
        {
        }

        protected override void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            base.OwnerPropertyChanged(sender, e);

            if (e.Property == ItemsControl.ItemCountProperty)
                InvalidateChildren();
        }

        public new ListBox Owner => (ListBox)base.Owner;

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            if (Owner.ItemCount == 0)
            {
                return null;
            }

            var children = new List<AutomationPeer>();

            for (var i = 0; i < Owner.ItemCount; i++)
            {
                var container = Owner.ContainerFromIndex(i);

                if (container == null)
                {
                    children.Add(new UnrealizedListItemAutomationPeer(this, i));
                }
                else
                {
                    var peer = GetOrCreate(container);
                    if (peer is ListItemAutomationPeer listItemPeer)
                        listItemPeer.Index = i;
                    children.Add(peer);
                }
            }

            return children;
        }

        internal class UnrealizedListItemAutomationPeer(ListBoxAutomationPeer owner, int index)
            : UnrealizedElementAutomationPeer, ISelectionItemProvider
        {
            private ListBox _listBox => owner.Owner;

            public bool IsSelected => _listBox.Selection.SelectedIndexes.Contains(index);

            public ISelectionProvider? SelectionContainer => owner.GetProvider<ISelectionProvider>();

            public void Select()
            {
                EnsureEnabled();

                _listBox.SelectedIndex = index;
                BringIntoView();
            }

            public void AddToSelection()
            {
                EnsureEnabled();

                if (_listBox.GetValue(ListBox.SelectionProperty) is ISelectionModel selectionModel)
                {
                    selectionModel.Select(index);
                    BringIntoView();
                }
            }

            public void RemoveFromSelection()
            {
                EnsureEnabled();

                if (_listBox.GetValue(ListBox.SelectionProperty) is ISelectionModel selectionModel)
                {
                    selectionModel.Deselect(index);
                }
            }

            protected override void BringIntoViewCore()
            {
                var container = _listBox.ContainerFromIndex(index);
                container?.BringIntoView();
            }

            protected override string? GetAcceleratorKeyCore() => null;
            protected override string? GetAccessKeyCore() => null;
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;
            protected override string GetAutomationIdCore() => $"{nameof(ListBoxItem)}: {index}";

            protected override Rect GetBoundingRectangleCore()
            {
                var container = _listBox.ContainerFromIndex(index);
                var peer = container?.GetOrCreateAutomationPeer();
                return peer?.GetBoundingRectangle() ?? base.GetBoundingRectangleCore();
            }

            protected override string GetClassNameCore() => nameof(ListBoxItem);
            protected override AutomationPeer? GetLabeledByCore() => null;

            protected override string? GetNameCore()
            {
                using var textBindingEvaluator = BindingEvaluator<string?>.TryCreate(_listBox.DisplayMemberBinding);
                var item = _listBox.Items.ElementAtOrDefault(index);
                return textBindingEvaluator?.Evaluate(item);
            }

            protected override AutomationPeer? GetParentCore() => owner;
            protected override bool IsContentElementCore() => true;
            protected override bool IsControlElementCore() => true;
            protected override bool IsEnabledCore() => _listBox.IsEnabled;
            protected override bool IsKeyboardFocusableCore() => true;

            protected override void SetFocusCore()
            {
                _listBox.SelectedIndex = index;
                BringIntoView();
            }
        }
    }
}
