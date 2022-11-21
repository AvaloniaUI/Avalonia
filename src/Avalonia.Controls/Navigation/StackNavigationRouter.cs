using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Avalonia.Controls
{
    public class StackNavigationRouter : INavigationRouter
    {
        private readonly Stack<object?> _backStack;
        private object? _currentPage;

        public event EventHandler<NavigatedEventArgs>? Navigated;

        public bool CanGoBack => _backStack?.Count() > 0;

        public object? CurrentPage
        {
            get => _currentPage; private set
            {
                var oldContent = _currentPage;

                _currentPage = value;

                Navigated?.Invoke(this, new NavigatedEventArgs(oldContent, value));
            }
        }

        public bool AllowEmpty { get; set; }

        public bool CanGoForward => false;

        public StackNavigationRouter()
        {
            _backStack = new Stack<object?>();
        }

        public async Task BackAsync()
        {
            if (CanGoBack || AllowEmpty)
            {
                CurrentPage = _backStack?.Pop();
            }
        }

        public async Task NavigateToAsync(object? viewModel, NavigationMode navigationMode)
        {
            if(viewModel == null)
            {
                return;
            }

            if (CurrentPage != null)
            {
                switch (navigationMode)
                {
                    case NavigationMode.Normal:
                        _backStack.Push(CurrentPage);
                        break;
                    case NavigationMode.Clear:
                        _backStack.Clear();
                        break;
                }
            }

            CurrentPage = viewModel;
        }

        public async Task ClearAsync()
        {
            _backStack?.Clear();

            if(AllowEmpty)
            {
                CurrentPage = null;
            }
            else
            {
                Navigated?.Invoke(this, new NavigatedEventArgs(CurrentPage, CurrentPage));
            }
        }

        public async Task ForwardAsync() { }
    }
}
