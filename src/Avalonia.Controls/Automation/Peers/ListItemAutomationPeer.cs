using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;

namespace Avalonia.Automation.Peers
{
    public class ListItemAutomationPeer : ContentControlAutomationPeer,
        ISelectionItemProvider
    {
        public ListItemAutomationPeer(ContentControl owner)
            : base(owner)
        {
        }

        public bool IsSelected => Owner.GetValue(ListBoxItem.IsSelectedProperty);

        public ISelectionProvider? SelectionContainer
        {
            get
            {
                if (Owner.Parent is Control parent)
                {
                    var parentPeer = GetOrCreate(parent);
                    return parentPeer.GetProvider<ISelectionProvider>();
                }

                return null;
            }
        }

        public void Select()
        {
            EnsureEnabled();

            if (Owner.Parent is SelectingItemsControl parent)
            {
                var index = parent.IndexFromContainer(Owner);

                if (index != -1)
                    parent.SelectedIndex = index;

                // When an accessibility tool selects a ComboBox dropdown item,
                // also close the dropdown to match real click behavior.
                if (parent is ComboBox { IsDropDownOpen: true } comboBox)
                    comboBox.IsDropDownOpen = false;
            }
        }

        void ISelectionItemProvider.AddToSelection()
        {
            EnsureEnabled();

            if (Owner.Parent is ItemsControl parent &&
                parent.GetValue(ListBox.SelectionProperty) is ISelectionModel selectionModel)
            {
                var index = parent.IndexFromContainer(Owner);

                if (index != -1)
                    selectionModel.Select(index);
            }
        }

        void ISelectionItemProvider.RemoveFromSelection()
        {
            EnsureEnabled();

            if (Owner.Parent is ItemsControl parent &&
                parent.GetValue(ListBox.SelectionProperty) is ISelectionModel selectionModel)
            {
                var index = parent.IndexFromContainer(Owner);

                if (index != -1)
                    selectionModel.Deselect(index);
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ListItem;
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
    }
}
