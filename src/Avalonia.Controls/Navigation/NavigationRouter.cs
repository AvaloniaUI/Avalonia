using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    public class NavigationRouter : INavigationRouter
    {
        private readonly Stack<object?> _navigationStack;
        private bool? _canGoBack;
        private object? _currentView;

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

        public NavigationRouter()
        {
            _navigationStack = new Stack<object?>();
        }

        public async Task GoBack()
        {
            if (CanGoBack != null && (bool)CanGoBack)
            {
                _navigationStack?.Pop();

                CurrentView = _navigationStack?.Peek();
            }
        }

        public async Task NavigateTo(object? viewModel)
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
