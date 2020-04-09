using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Platform.Interop;

namespace Avalonia.Native.Interop
{
    public partial class IAvnAppMenuItem
    {
        private IAvnAppMenu _subMenu;
        private AvaloniaNativeMenuExporter _exporter;

        public NativeMenuItemBase ManagedMenuItem { get; set; }

        internal IDisposable Update(AvaloniaNativeMenuExporter exporter, IAvaloniaNativeFactory factory, NativeMenuItem item)
        {
            var disposables = new CompositeDisposable();

            _exporter = exporter;

            ManagedMenuItem = item;

            ManagedMenuItem.PropertyChanged += OnMenuItemPropertyChanged;

            disposables.Add(Disposable.Create(() => ManagedMenuItem.PropertyChanged -= OnMenuItemPropertyChanged));

            if(!string.IsNullOrWhiteSpace(item.Header))
            {
                using (var buffer = new Utf8Buffer(item.Header))
                {
                    Title = buffer.DangerousGetHandle();
                }
            }

            if (item.Gesture != null)
            {
                using (var buffer = new Utf8Buffer(OsxUnicodeKeys.ConvertOSXSpecialKeyCodes(item.Gesture.Key)))
                {
                    SetGesture(buffer.DangerousGetHandle(), (AvnInputModifiers)item.Gesture.KeyModifiers);
                }
            }

            SetAction(new PredicateCallback(() =>
            {
                if (item.Command != null || item.HasClickHandlers)
                {
                    return item.Enabled;
                }

                return false;
            }), new MenuActionCallback(() => { item.RaiseClick(); }));

            if (item.Menu != null)
            {
                if (_subMenu == null)
                {
                    _subMenu = factory.CreateMenu();
                }

                SetSubMenu(_subMenu);
                
                disposables.Add(_subMenu.Update(exporter, factory, item.Menu, item.Header));
            }

            if (item.Menu == null && _subMenu != null)
            {
                // todo remove submenu.

                // needs implementing on native side also.
            }

            return disposables;
        }

        private void OnMenuItemPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            _exporter.QueueReset();
        }
    }
}
