using Avalonia;
using ReactiveUI;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RenderDemo.Pages
{
    internal class XDataGridHeaderDescriptor : ReactiveObject
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

        int colDefIndex;
        public int ColumnDefinitionIndex
        {
            get => colDefIndex;
            set => this.RaiseAndSetIfChanged(ref colDefIndex, value);
        }
    }
}
