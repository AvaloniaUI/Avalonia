using System;
using System.Diagnostics;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;

#nullable enable

namespace Avalonia.Controls
{
    public class VirtualizingStackPanel : VirtualizingStackBase<object?>, IPanel
    {
        public static readonly StyledProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            ItemsPresenter.VirtualizationModeProperty.AddOwner<VirtualizingStackPanel>();

        private ItemsPresenter? _presenter;

        public ItemVirtualizationMode VirtualizationMode
        {
            get => GetValue(VirtualizationModeProperty);
            set => SetValue(VirtualizationModeProperty, value);
        }

        Controls IPanel.Children => base.Children;

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_presenter is null)
                return default;
            return base.MeasureOverride(availableSize);
        }

        protected override IControl RealizeElement(int index)
        {
            Debug.Assert(_presenter is not null);
            return _presenter!.ItemContainerGenerator.Realize(this, index, _presenter.ItemsView[index]);
        }

        protected override void UnrealizeElement(IControl element, int index)
        {
            throw new NotImplementedException();
        }

        protected override void UpdateElementIndex(IControl element, int index)
        {
            throw new NotImplementedException();
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _presenter = e.Parent as ItemsPresenter;
            base.Items = ItemsSourceView.GetOrCreate(_presenter?.Items);
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            _presenter = null;
            base.Items = ItemsSourceView.Empty;
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == VirtualizationModeProperty)
                UnrealizeAllElements();
        }
    }
}
