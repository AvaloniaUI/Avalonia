using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BindingTest.ViewModels
{
    public class NestedCommandViewModel : ReactiveObject
    {
        public NestedCommandViewModel()
        {
            Command = ReactiveCommand.Create(() => { });
        }

        public ICommand Command { get; }
    }
}
