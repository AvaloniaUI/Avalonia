using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    public class NavigationRouter : INavigationRouter
    {
        private bool? _canGoBack;
        private Stack<object?> _navigationStack;
        private object? _currentView;
        private object? _header;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool? CanGoBack
        {
            get => (_canGoBack == null || (bool)_canGoBack) && (NavigationStack?.Count() > 1); set
            {
                _canGoBack = value;

                OnPropertyChanged();
            }
        }
        public IEnumerable<object?> NavigationStack => _navigationStack;

        public object? CurrentView
        {
            get => _currentView; private set
            {
                _currentView = value;

                OnPropertyChanged();
            }
        }

        public object? Header
        {
            get => _header; set
            {
                _header = value;

                OnPropertyChanged();
            }
        }

        public NavigationRouter()
        {
            _navigationStack = new Stack<object?>();
        }

        public void GoBack()
        {
            if (CanGoBack != null && (bool)CanGoBack)
            {
                _navigationStack?.Pop();

                CurrentView = _navigationStack?.Peek();
            }
        }

        public void NavigateTo(object? viewModel)
        {
            _navigationStack.Push(viewModel);
            CurrentView = viewModel;
        }

        public void OnPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
}
