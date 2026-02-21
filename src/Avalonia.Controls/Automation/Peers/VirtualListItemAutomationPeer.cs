using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Utils;

namespace Avalonia.Automation.Peers
{
    /// <summary>
    /// An item in a <see cref="ListBox"/> that is not in the visible viewport. This allows screen
    /// readers to be aware of the number of items, and to enable navigation to items outside the
    /// viewport. When the item becomes visible, this class will be replaced in the automation tree
    /// with the actual automation peer.
    /// </summary>
    internal class VirtualListItemAutomationPeer(ListBox listBox, int index)
        : AutomationPeer, ISelectionItemProvider
    {
        public bool IsSelected => listBox.Selection.SelectedIndexes.Contains(index);

        public ISelectionProvider? SelectionContainer
        {
            get
            {
                var peer = listBox.GetOrCreateAutomationPeer();
                return peer.GetProvider<ISelectionProvider>();
            }
        }

        public void Select()
        {
            EnsureEnabled();

            listBox.SelectedIndex = index;
            BringIntoView();
        }

        public void AddToSelection()
        {
            EnsureEnabled();

            if (listBox.GetValue(ListBox.SelectionProperty) is ISelectionModel selectionModel)
            {
                selectionModel.Select(index);
                BringIntoView();
            }
        }

        public void RemoveFromSelection()
        {
            EnsureEnabled();

            if (listBox.GetValue(ListBox.SelectionProperty) is ISelectionModel selectionModel)
            {
                selectionModel.Deselect(index);
            }
        }

        protected override void BringIntoViewCore()
        {
            var container = listBox.ContainerFromIndex(index);
            container?.BringIntoView();
        }

        protected override string? GetAcceleratorKeyCore() => null;
        protected override string? GetAccessKeyCore() => null;
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;
        protected override string GetAutomationIdCore() => $"ListBoxItem_{index}";

        protected override Rect GetBoundingRectangleCore()
        {
            var container = listBox.ContainerFromIndex(index);
            var peer = container?.GetOrCreateAutomationPeer();
            return peer?.GetBoundingRectangle() ?? default;
        }

        protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore() => [];
        protected override string GetClassNameCore() => "ListBoxItem";
        protected override AutomationPeer? GetLabeledByCore() => null;

        protected override string? GetNameCore()
        {
            using var textBindingEvaluator = BindingEvaluator<string?>.TryCreate(listBox.DisplayMemberBinding);
            var item = listBox.Items.ElementAtOrDefault(index);
            return textBindingEvaluator?.Evaluate(item);
        }

        protected override AutomationPeer? GetParentCore() => listBox.GetOrCreateAutomationPeer();
        protected override bool HasKeyboardFocusCore() => false;
        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
        protected override bool IsEnabledCore() => listBox.IsEnabled;
        protected override bool IsKeyboardFocusableCore() => true;

        protected override void SetFocusCore()
        {
            listBox.SelectedIndex = index;
            BringIntoView();
        }

        protected override bool ShowContextMenuCore() => false;
        protected internal override bool TrySetParent(AutomationPeer? parent) => true;
    }
}
