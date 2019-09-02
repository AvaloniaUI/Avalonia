using ReactiveUI;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RenderDemo.Pages
{
    internal class XDataGridHeaderDescriptor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaiseAndSetIfChanged<T>(ref T prop, T value, [CallerMemberName] string callee = null) where T : IEquatable<T>
        {
            if (callee is null || (prop?.Equals(value) ?? true)) return;
            prop = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(callee));
        }

        int index;
        public int Index
        {
            get => index;
            set => this.RaiseAndSetIfChanged(ref index, value);
        }

        string headerText;
        public string HeaderText
        {
            get => headerText;
            set => this.RaiseAndSetIfChanged(ref headerText, value);
        }

        string propName;
        public string PropertyName
        {
            get => propName;
            set => this.RaiseAndSetIfChanged(ref propName, value);
        }

        int colDefIndex;
        public int ColumnDefinitionIndex
        {
            get => colDefIndex;
            set => this.RaiseAndSetIfChanged(ref colDefIndex, value);
        }
    }
}
