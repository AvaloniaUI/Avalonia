using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RenderDemo.Pages
{
    public class XDataGridHeaderDescriptor : ReactiveObject
    {
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
 
        double headerWidth;
        public double HeaderWidth
        {
            get => headerWidth;
            set => this.RaiseAndSetIfChanged(ref headerWidth, value);
        }
    }
}
