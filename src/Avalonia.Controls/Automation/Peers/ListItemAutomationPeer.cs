using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;

namespace Avalonia.Automation.Peers
{
    public class ListItemAutomationPeer : ContentControlAutomationPeer,
        ISelectionItemProvider
    {
        private int? _index;

        public int Index
        {
            get
            {
                if (_index.HasValue)
                    return _index.Value;

                if (Owner.Parent is ItemsControl parent)
                    return parent.IndexFromContainer(Owner);

                return -1;
            }

            set => _index = value;
        }

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
                var index = Index;

                if (index != -1)
                    parent.SelectedIndex = index;
            }
        }

        public void AddToSelection()
        {
            EnsureEnabled();

            if (Owner.Parent is ItemsControl parent &&
                parent.GetValue(ListBox.SelectionProperty) is ISelectionModel selectionModel)
            {
                var index = Index;

                if (index != -1)
                    selectionModel.Select(index);
            }
        }

        public void RemoveFromSelection()
        {
            EnsureEnabled();

            if (Owner.Parent is ItemsControl parent &&
                parent.GetValue(ListBox.SelectionProperty) is ISelectionModel selectionModel)
            {
                var index = Index;

                if (index != -1)
                    selectionModel.Deselect(index);
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ListItem;
        }

        protected override string? GetAutomationIdCore()
        {
            var index = Index;

            if (index != -1)
                return base.GetAutomationIdCore();

            return $"{nameof(ListBoxItem)}: {index}";
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
    }
}
