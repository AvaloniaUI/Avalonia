using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Avalonia.Controls
{
    public class NavigationRouter : INavigationRouter
    {
        private readonly Stack<object?> _backStack;
        private object? _currentPage;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanGoBack => _backStack?.Count() > 0;

        public object? CurrentPage
        {
            get => _currentPage; private set
            {
                _currentPage = value;

                OnPropertyChanged();

                OnPropertyChanged(nameof(CanGoBack));
            }
        }

        public bool AllowEmpty { get; set; }

        public bool CanGoForward => false;

        public NavigationRouter()
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

        public void OnPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
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
                OnPropertyChanged(nameof(CanGoBack));
            }
        }

        public async Task ForwardAsync() { }
    }
}
