using System;
using ReactiveUI;
using Avalonia.Animation;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Collections;
using System.Reactive.Linq;
using Avalonia.Markup.Xaml.Templates;

namespace RenderDemo.ViewModels
{

    public class XDataGridHeaderDescriptors
    {
        public int Index { get; set; }
        public string HeaderText { get; set; }
        public string PropertyName { get; set; }
        public string SharedSizeGroup => PropertyName + Index;
        public double ColumnWidth { get; internal set; }
        internal int ColumnDefinitionIndex { get; set; }
    }

    public class SampleData
    {
        [DisplayName("Index")]
        [ColumnWidth(50)]
        public int Index { get; set; }

        [DisplayName("Allow")]
        [ColumnWidth(50)]
        public bool Allow { get; set; }

        [DisplayName("Some Text")]
        [ColumnWidth(100)]
        public string SomeText { get; set; }

        [DisplayName("Random Number 1")]
        [ColumnWidth(150)]
        public string RndNum1 { get; set; }

        [DisplayName("Random Number 2")]
        [ColumnWidth(150)]
        public string RndNum2 { get; set; }

        [DisplayName("Random Number 3")]
        [ColumnWidth(150)]
        public string RndNum3 { get; set; }
    }


    [AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ColumnWidthAttribute : Attribute
    {
        readonly double _width;

        public ColumnWidthAttribute(double width)
        {
            this._width = width;
        }

        public double Width
        {
            get { return _width; }
        }
    }


    public class AnimationsPageViewModel : ReactiveObject
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public AnimationsPageViewModel()
        {
            this.WhenAnyValue(x => x.DataType)
                .Where(x => x != null)
                .Subscribe(AutoGenerateHeaders);

            var k = new AvaloniaList<object>();
            var r = new Random();

            for (int i = 0; i < 2_000; i++)
            {
                var l = new SampleData()
                {
                    Index = i,
                    Allow = r.Next(0,1) == 1,
                    SomeText = RandomString(10),
                    RndNum1 = r.NextDouble().ToString(),
                    RndNum2 = r.NextDouble().ToString(),
                    RndNum3 = r.NextDouble().ToString(),
                };

                k.Add(l);
            }

            DataType = typeof(SampleData);
            DataRows = k;
        }

        private void AutoGenerateHeaders(Type obj)
        {
            int i = 0;
            var xdghList = new List<XDataGridHeaderDescriptors>();

            foreach (var property in obj.GetProperties())
            {
                var dispNameAttrib = (DisplayNameAttribute)property
                                        .GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                        .SingleOrDefault();

                var colWidthAttrib = (ColumnWidthAttribute)property
                                        .GetCustomAttributes(typeof(ColumnWidthAttribute), false)
                                        .SingleOrDefault();

                if (dispNameAttrib is null | colWidthAttrib is null)
                    continue;

                var dName = dispNameAttrib.DisplayName;

                var xdgh = new XDataGridHeaderDescriptors()
                {
                    Index = i,
                    HeaderText = dName,
                    PropertyName = property.Name,
                    ColumnWidth = colWidthAttrib.Width
                };

                i++;

                xdghList.Add(xdgh);
            }

            DataHeaderDescriptors = xdghList.ToArray();
        }

        Type dataType;
        public Type DataType
        {
            get => dataType;
            set => this.RaiseAndSetIfChanged(ref dataType, value);
        }

        XDataGridHeaderDescriptors[] dataHeaderDescriptors;
        public XDataGridHeaderDescriptors[] DataHeaderDescriptors
        {
            get => dataHeaderDescriptors;
            set => this.RaiseAndSetIfChanged(ref dataHeaderDescriptors, value);
        }

        IAvaloniaList<object> dataRows;

        public IAvaloniaList<object> DataRows
        {
            get => dataRows;
            set => this.RaiseAndSetIfChanged(ref dataRows, value);
        }
    }
}
