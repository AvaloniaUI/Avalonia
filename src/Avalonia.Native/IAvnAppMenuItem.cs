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
        private CompositeDisposable _propertyDisposables = new CompositeDisposable();
        private IDisposable _currentActionDisposable;

        public NativeMenuItemBase ManagedMenuItem { get; set; }

        private void UpdateTitle(string title)
        {
            using (var buffer = new Utf8Buffer(string.IsNullOrWhiteSpace(title) ? "" : title))
            {
                Title = buffer.DangerousGetHandle();
            }
        }

        private void UpdateIsChecked (bool isChecked)
        {
            IsChecked = isChecked;
        }

        private void UpdateGesture(Input.KeyGesture gesture)
        {
            // todo ensure backend can cope with setting null gesture.
            using (var buffer = new Utf8Buffer(gesture == null ? "" : OsxUnicodeKeys.ConvertOSXSpecialKeyCodes(gesture.Key)))
            {
                SetGesture(buffer.DangerousGetHandle(), (AvnInputModifiers)gesture.KeyModifiers);
            }            
        }

        private void UpdateAction (NativeMenuItem item)
        {
            _currentActionDisposable?.Dispose();

            var action = new PredicateCallback(() =>
            {
                if (item.Command != null || item.HasClickHandlers)
                {
                    return item.Enabled;
                }

                return false;
            });

            var callback = new MenuActionCallback(() => { item.RaiseClick(); });

            _currentActionDisposable = Disposable.Create(() =>
            {
                action.Dispose();
                callback.Dispose();
            });

            SetAction(action, callback);
        }

        internal void Initialise()
        {
            _propertyDisposables.Add(Disposable.Create(() => ManagedMenuItem.GetObservable(NativeMenuItem.HeaderProperty)
                .Subscribe(x => UpdateTitle(x))));

            _propertyDisposables.Add(Disposable.Create(() => ManagedMenuItem.GetObservable(NativeMenuItem.GestureProperty)
                .Subscribe(x => UpdateGesture(x))));

            _propertyDisposables.Add(Disposable.Create(() => ManagedMenuItem.GetObservable(NativeMenuItem.CommandProperty)
                .Subscribe(x => UpdateAction(ManagedMenuItem as NativeMenuItem))));

            _propertyDisposables.Add(Disposable.Create(() => ManagedMenuItem.GetObservable(NativeMenuItem.IsCheckedProperty)
                .Subscribe(x => UpdateIsChecked(x))));
        }

        internal void Deinitialise ()
        {
            if(_subMenu != null)
            {
                _subMenu.Update(null, null, null);
                _subMenu = null;
            }

            _propertyDisposables?.Dispose();
            _currentActionDisposable?.Dispose();
        }

        internal IDisposable Update(AvaloniaNativeMenuExporter exporter, IAvaloniaNativeFactory factory, NativeMenuItem item)
        {
            var disposables = new CompositeDisposable();

            _exporter = exporter;

            ManagedMenuItem = item;

            UpdateTitle(item.Header);

            UpdateGesture(item.Gesture);

            UpdateAction(ManagedMenuItem as NativeMenuItem);

            UpdateIsChecked(item.IsChecked);

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
    }
}
