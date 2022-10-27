using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls
{
    public interface INavigationRouter : INotifyPropertyChanged
    {
        bool? CanGoBack { get; set; }
        IEnumerable<object?> NavigationStack { get; }
        object? CurrentView { get; }
        object? Header { get; }

        void NavigateTo(object? viewModel);
        void GoBack();
    }
}
