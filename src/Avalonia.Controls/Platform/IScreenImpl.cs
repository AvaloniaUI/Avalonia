using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls.Utils;
using Avalonia.Metadata;
using Avalonia.Threading;
#pragma warning disable CS1591 // Private API doesn't require XML documentation. 

namespace Avalonia.Platform
{
    [Unstable]
    public interface IScreenImpl
    {
        int ScreenCount { get; }
        IReadOnlyList<Screen> AllScreens { get; }
        Action? Changed { get; set; }
        Screen? ScreenFromWindow(IWindowBaseImpl window);
        Screen? ScreenFromTopLevel(ITopLevelImpl topLevel);
        Screen? ScreenFromPoint(PixelPoint point);
        Screen? ScreenFromRect(PixelRect rect);
        Task<bool> RequestScreenDetails();
    }

    [PrivateApi]
    public abstract class ScreensBaseImpl<TKey, TScreen>(IEqualityComparer<TKey>? screenKeyComparer) : IScreenImpl
        where TKey : notnull
        where TScreen : Screen
    {
        private readonly Dictionary<TKey, TScreen> _allScreensByKey = screenKeyComparer is not null ?
            new Dictionary<TKey, TScreen>(screenKeyComparer) :
            new Dictionary<TKey, TScreen>();
        private TScreen[]? _allScreens;
        private int? _screenCount;
        private bool? _screenDetailsRequestGranted;
        private DispatcherOperation? _onChangeOperation;

        protected ScreensBaseImpl() : this(null)
        {
            
        }

        public int ScreenCount => _screenCount ??= GetScreenCount();

        public IReadOnlyList<Screen> AllScreens
        {
            get
            {
                if (_allScreens == null)
                {
                    var screens = GetAllScreenKeys();

                    _allScreens = new TScreen[screens.Count];

                    foreach (var oldScreenKey in _allScreensByKey.Keys)
                    {
                        if (!screens.Contains(oldScreenKey))
                        {
                            _allScreensByKey.Remove(oldScreenKey);
                        }
                    }

                    int i = 0;
                    foreach (var newScreen in screens)
                    {
                        if (_allScreensByKey.TryGetValue(newScreen, out var oldScreen))
                        {
                            RefreshScreen(oldScreen);
                            _allScreens[i] = oldScreen;
                        }
                        else
                        {
                            var screen = CreateScreenFromKey(newScreen);
                            RefreshScreen(screen);
                            _allScreensByKey[newScreen] = screen;
                            _allScreens[i] = screen;
                        }

                        i++;
                    }
                }

                return _allScreens;
            }
        }
        
        public Action? Changed { get; set; }

        public Screen? ScreenFromWindow(IWindowBaseImpl window) => ScreenFromTopLevel(window);

        public Screen? ScreenFromTopLevel(ITopLevelImpl topLevel) => ScreenFromTopLevelCore(topLevel);

        public Screen? ScreenFromPoint(PixelPoint point) => ScreenFromPointCore(point);

        public Screen? ScreenFromRect(PixelRect rect) => ScreenFromRectCore(rect);

        public void OnChanged()
        {
            _onChangeOperation?.Abort();
            _onChangeOperation = Dispatcher.UIThread.InvokeAsync(() =>
            {
                _screenCount = null;
                _allScreens = null;
                Changed?.Invoke();
            }, DispatcherPriority.Input);
        }

        public async Task<bool> RequestScreenDetails()
        {
            _screenDetailsRequestGranted ??= await RequestScreenDetailsCore();

            return _screenDetailsRequestGranted.Value;
        }

        protected bool TryGetScreen(TKey key,  [MaybeNullWhen(false)] out TScreen screen)
        {
            _ = AllScreens; // ensure it's up to date.
            return _allScreensByKey.TryGetValue(key, out screen);
        }

        protected virtual int GetScreenCount() => AllScreens.Count;
        protected abstract IReadOnlyList<TKey> GetAllScreenKeys();
        protected abstract TScreen CreateScreenFromKey(TKey key);
        protected virtual void RefreshScreen(TScreen screen)
        {
        }
        protected virtual Task<bool> RequestScreenDetailsCore() => Task.FromResult(true);

        protected virtual Screen? ScreenFromTopLevelCore(ITopLevelImpl topLevel)
        {
            if (topLevel is IWindowImpl window)
            {
                return ScreenHelper.ScreenFromWindow(window, AllScreens);
            }

            return null;
        }

        protected virtual Screen? ScreenFromPointCore(PixelPoint point) => ScreenHelper.ScreenFromPoint(point, AllScreens);

        protected virtual Screen? ScreenFromRectCore(PixelRect rect) => ScreenHelper.ScreenFromRect(rect, AllScreens);
    }
}
