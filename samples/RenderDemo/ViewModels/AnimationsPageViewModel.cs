using System;
using ReactiveUI;
using Avalonia.Animation;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Collections;
using System.Reactive.Linq;
using Avalonia.Markup.Xaml.Templates;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Collections.Specialized;
using System.Collections;

namespace RenderDemo.ViewModels
{
     public class SampleData
    {
        [DisplayName("Index")]
        // [ColumnWidth(50)]
        public int Index { get; set; }

        [DisplayName("Allow")]
        // [ColumnWidth(50)]
        public bool Allow { get; set; }

        [DisplayName("Some Text")]
        // [ColumnWidth(100)]
        public string SomeText { get; set; }

        [DisplayName("Random Number 1")]
        // [ColumnWidth(150)]
        public string RndNum1 { get; set; }

        [DisplayName("Random Number 2")]
        // [ColumnWidth(150)]
        public string RndNum2 { get; set; }

        [DisplayName("Random Number 3")]
        // [ColumnWidth(150)]
        public string RndNum3 { get; set; }
    }


    // [AttributeUsage(System.AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    // public sealed class ColumnWidthAttribute : Attribute
    // {
    //     readonly double _width;

    //     public ColumnWidthAttribute(double width)
    //     {
    //         this._width = width;
    //     }

    //     public double Width
    //     {
    //         get { return _width; }
    //     }
    // }

    public static class LinqExtensions
    {

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
            // this.WhenAnyValue(x => x.DataType)
            //     .Where(x => x != null)
            //     .Subscribe(AutoGenerateHeaders);

            var k = new List<SampleData>();
            var r = new Random();

            for (int i = 0; i < 2_000; i++)
            {
                var l = new SampleData()
                {
                    Index = i,
                    Allow = r.NextDouble() > 0.5,
                    SomeText = RandomString(10),
                    RndNum1 = r.NextDouble().ToString(),
                    RndNum2 = r.NextDouble().ToString(),
                    RndNum3 = r.NextDouble().ToString(),
                };

                k.Add(l);
            }

            var o = new List<object>();
            foreach (var xzx in k.OrderBy(x => x.Index))
                o.Add(xzx);

            DataRows = o.ToList();
            // DataType = typeof(SampleData);
        }



        private Func<T, object> GetOrderByExpression<T>(string sortColumn, Type dataType)
        {
            Func<T, object> orderByExpr = null;

            if (!String.IsNullOrEmpty(sortColumn))
            {
                Type sponsorResultType = dataType;

                if (sponsorResultType.GetProperties().Any(prop => prop.Name == sortColumn))
                {
                    PropertyInfo pinfo = sponsorResultType.GetProperty(sortColumn);
                    orderByExpr = (data => pinfo.GetValue(data, null));
                }
            }
            return orderByExpr;
        }


        // Type dataType;
        // public Type DataType
        // {
        //     get => dataType;
        //     internal set => this.RaiseAndSetIfChanged(ref dataType, value);
        // }


        // XDataGridHeaderDescriptors[] dataHeaderDescriptors;
        // public XDataGridHeaderDescriptors[] DataHeaderDescriptors
        // {
        //     get => dataHeaderDescriptors;
        //     set => this.RaiseAndSetIfChanged(ref dataHeaderDescriptors, value);
        // }

        List<dynamic> dataRows;

        public List<dynamic> DataRows
        {
            get => dataRows;
            set
            {
                this.RaiseAndSetIfChanged(ref dataRows, value);
            }
        }
    }
}
