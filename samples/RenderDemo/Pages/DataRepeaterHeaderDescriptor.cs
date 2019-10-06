using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RenderDemo.Pages
{
    public class DataRepeaterHeaderDescriptor : ReactiveObject
    {
        private string _headerText;
        public string HeaderText
        {
            get => _headerText;
            set => this.RaiseAndSetIfChanged(ref _headerText, value);
        }

        private string _propName;
        public string PropertyName
        {
            get => _propName;
            set => this.RaiseAndSetIfChanged(ref _propName, value);
        }

        private double _headerWidth;
        public double HeaderWidth
        {
            get => _headerWidth;
            set =>
             this.RaiseAndSetIfChanged(ref _headerWidth, value);
        }

        public bool IsSortable
        {
            get;
            internal set;
        }

        internal Func<object, object> GetterDelegate { get; set; }
    }
}
