using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Dialogs.Internal;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace Avalonia.Dialogs
{
    [TemplatePart("PART_QuickLinks", typeof(Control))]
    [TemplatePart("PART_Files",      typeof(ListBox))]
    public class ManagedFileChooser : TemplatedControl
    {
        private Control? _quickLinksRoot;
        private ListBox? _filesView;

        public ManagedFileChooser()
        {
            AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        }

        ManagedFileChooserViewModel? Model => DataContext as ManagedFileChooserViewModel;

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var model = (e.Source as StyledElement)?.DataContext as ManagedFileChooserItemViewModel;

            if (model == null)
            {
                return;
            }

            if (_quickLinksRoot != null)
            {
                var isQuickLink = _quickLinksRoot.IsLogicalAncestorOf(e.Source as Control);

                if (e.ClickCount == 2 || isQuickLink)
                {
                    if (model.ItemType == ManagedFileChooserItemType.File)
                    {
                        Model?.SelectSingleFile(model);
                    }
                    else
                    {
                        Model?.Navigate(model.Path);
                    }

                    e.Handled = true;
                }
            }
        }

        protected override async void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            var model = (DataContext as ManagedFileChooserViewModel);

            if (model == null)
            {
                return;
            }

            var preselected = model.SelectedItems.FirstOrDefault();

            if (preselected == null)
            {
                return;
            }

            //Let everything to settle down and scroll to selected item
            await Task.Delay(100);

            if (preselected != model.SelectedItems.FirstOrDefault())
            {
                return;
            }

            // Workaround for ListBox bug, scroll to the previous file
            var indexOfPreselected = model.Items.IndexOf(preselected);

            if ((_filesView != null) && (indexOfPreselected > 1))
            {
                _filesView.ScrollIntoView(indexOfPreselected - 1);
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _quickLinksRoot = e.NameScope.Get<Control>("PART_QuickLinks");
            _filesView = e.NameScope.Get<ListBox>("PART_Files");
        }
    }
}
