using System;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

#nullable enable

namespace Avalonia.Controls
{
    public class VirtualizingStackPanel : VirtualizingStackBase, IPanel
    {
        /// <summary>
        /// Defines the <see cref="VirtualizationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            AvaloniaProperty.Register<ItemsPresenter, ItemVirtualizationMode>(
                nameof(VirtualizationMode),
                defaultValue: ItemVirtualizationMode.Smooth);

        private IItemsPresenter? _presenter;

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
            var (generator, items) = GetGeneratorAndItems();
            var e = generator.Realize(this, index, items[index]);

            if (e.Parent is null)
                Children.Add(e);
            else if (e.Parent != this)
                throw new InvalidOperationException("Realized element has unexpected Parent.");

            return e;

        }

        protected override void UnrealizeElement(IControl element, int index)
        {
            var (generator, items) = GetGeneratorAndItems();
            generator.Unrealize(element, index, items[index]);
        }

        protected override void UpdateElementIndex(IControl element, int index)
        {
            throw new NotImplementedException();
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e); 
            _presenter = e.Parent as ItemsPresenter;
            base.Items = _presenter?.ItemsView ?? ItemsSourceView.Empty;
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

        private (IItemContainerGenerator, ItemsSourceView) GetGeneratorAndItems()
        {
            if (_presenter?.ItemContainerGenerator is null || _presenter.ItemsView is null)
                throw new NotSupportedException(
                    $"{nameof(VirtualizingStackPanel)} must be hosted in an {nameof(IItemsPresenter)} " +
                    "with a generator and items.");
            return (_presenter.ItemContainerGenerator, _presenter.ItemsView);
        }
    }
}
