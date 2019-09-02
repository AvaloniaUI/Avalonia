using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReactiveUI;
using RenderDemo.ViewModels;
using System;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Controls.Templates;
using System.Linq.Expressions;
using Avalonia.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.Collections;

namespace RenderDemo.Pages
{


    public class XDataGridCell : ContentControl
    {

    }


    public class XDataGridHeaderRenderer : Grid
    {


        public XDataGridHeaderRenderer()
        {
            this.AttachedToVisualTree += VisualAttached;
            this.DetachedFromVisualTree += VisualDetached;
            var headerDesc = XDataGrid.GetHeaderDescriptors(this);
            headerDesc.CollectionChanged += DescriptorsCollectionChanged;
        }

        private void DescriptorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

        }

        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        private void DescriptorsChanged(XDataGridHeaderDescriptors obj)
        {
            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                var colDefHeaderCell = new ColumnDefinition(new GridLength(100)); //temporary

                this.ColumnDefinitions.Add(colDefHeaderCell);

                var rowValue = headerDesc.PropertyName;

                var boundCellContent = new XDataGridCell();

                boundCellContent.Content = rowValue;

                Grid.SetColumn(boundCellContent, headerDesc.ColumnDefinitionIndex);

                this.Children.Add(boundCellContent);
            }
        }

        private void VisualDetached(object sender, VisualTreeAttachmentEventArgs e)
        {

        }

        private void VisualAttached(object sender, VisualTreeAttachmentEventArgs e)
        {

        }
    }

    public class XDataGridRowRenderer : Grid
    {


        public XDataGridRowRenderer()
        {
            this.AttachedToVisualTree += VisualAttached;
            this.DetachedFromVisualTree += VisualDetached;
            var headerDesc = XDataGrid.GetHeaderDescriptors(this);
            headerDesc.CollectionChanged += DescriptorsCollectionChanged;
        }

        private void DescriptorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

        }

        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        private void DescriptorsChanged(XDataGridHeaderDescriptors obj)
        {
            for (int i = 0; i < obj.Count; i++)
            {
                var headerDesc = obj[i];

                var colDefHeaderCell = new ColumnDefinition(new GridLength(100)); //temporary

                this.ColumnDefinitions.Add(colDefHeaderCell);

                var rowValue = GetPropValue(this.DataContext, headerDesc.PropertyName);

                var boundCellContent = new XDataGridCell();

                boundCellContent.Content = rowValue;

                Grid.SetColumn(boundCellContent, headerDesc.ColumnDefinitionIndex);

                this.Children.Add(boundCellContent);
            }
        }

        private void VisualDetached(object sender, VisualTreeAttachmentEventArgs e)
        {

        }

        private void VisualAttached(object sender, VisualTreeAttachmentEventArgs e)
        {

        }
    }


    internal class XDataGridHeaderDescriptors : AvaloniaList<XDataGridHeaderDescriptor>
    {

    }

    internal static class ReflectionUtils
    {

        public static Type GetItemType(this IEnumerable list)
        {
            Type listType = list.GetType();
            Type itemType = null;

            // if it's a generic enumerable, we get the generic type

            // Unfortunately, if data source is fed from a bare IEnumerable, TypeHelper will report an element type of object,
            // which is not particularly interesting.  We deal with it further on.
            if (listType.IsEnumerableType())
            {
                itemType = listType.GetEnumerableItemType();
            }

            // Bare IEnumerables mean that result type will be object.  In that case, we try to get something more interesting
            if (itemType == null || itemType == typeof(object))
            {
                // We haven't located a type yet.. try a different approach.
                // Does the list have anything in it?

                IEnumerator en = list.GetEnumerator();
                if (en.MoveNext() && en.Current != null)
                {
                    return en.Current.GetType();
                }
            }

            // if we're null at this point, give up
            return itemType;
        }

        public static Type FindGenericType(Type definition, Type type)
        {
            while ((type != null) && (type != typeof(object)))
            {
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == definition))
                {
                    return type;
                }
                if (definition.IsInterface)
                {
                    foreach (Type type2 in type.GetInterfaces())
                    {
                        Type type3 = FindGenericType(definition, type2);
                        if (type3 != null)
                        {
                            return type3;
                        }
                    }
                }
                type = type.BaseType;
            }
            return null;
        }


        public static Type GetEnumerableItemType(this Type enumerableType)
        {
            Type type = FindGenericType(typeof(IEnumerable<>), enumerableType);
            if (type != null)
            {
                return type.GetGenericArguments()[0];
            }
            return enumerableType;
        }


        public static bool IsEnumerableType(this Type enumerableType)
        {
            return (FindGenericType(typeof(IEnumerable<>), enumerableType) != null);
        }
    }


    internal class XDataGridHeaderDescriptor : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaiseAndSetIfChanged<T>(ref T prop, T value, [CallerMemberName] string callee = null) where T : IEquatable<T>
        {
            if (callee is null || prop.Equals(value)) return;
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

    public class XDataGrid : TemplatedControl
    {
        internal static readonly AttachedProperty<XDataGridHeaderDescriptors> HeaderDescriptorsProperty =
            AvaloniaProperty.RegisterAttached<XDataGrid, Control, XDataGridHeaderDescriptors>("HeaderDescriptors", inherits: true);

        internal static XDataGridHeaderDescriptors GetHeaderDescriptors(Control element)
        {
            return element.GetValue(HeaderDescriptorsProperty);
        }


        internal static void SetHeaderDescriptors(Control element, XDataGridHeaderDescriptors value)
        {
            element.SetValue(HeaderDescriptorsProperty, value);
        }




        public static readonly DirectProperty<XDataGrid, IEnumerable> ItemsProperty =
            AvaloniaProperty.RegisterDirect<XDataGrid, IEnumerable>(
                nameof(Items),
                o => o.Items,
                (o, v) => o.Items = v);
        private IEnumerable _items;

        public IEnumerable Items
        {
            get { return _items; }
            set
            {
                if (value is null) return;

                DataType = value.GetItemType();

                DoAutoGeneratedHeaders(DataType);

                SetAndRaise(ItemsProperty, ref _items, value);
            }
        }

        private void DoAutoGeneratedHeaders(Type DataType)
        {
            int i = 0;
            var xdghList = new XDataGridHeaderDescriptors();

            foreach (var property in DataType.GetProperties())
            {
                var dispNameAttrib = (DisplayNameAttribute)property
                                        .GetCustomAttributes(typeof(DisplayNameAttribute), false)
                                        .SingleOrDefault();

                // var colWidthAttrib = (ColumnWidthAttribute)property
                //                         .GetCustomAttributes(typeof(ColumnWidthAttribute), false)
                //                         .SingleOrDefault();

                if (dispNameAttrib is null)
                    continue;

                var dName = dispNameAttrib.DisplayName;

                var xdgh = new XDataGridHeaderDescriptor()
                {
                    Index = i,
                    HeaderText = dName,
                    PropertyName = property.Name,
                    // ColumnWidth = colWidthAttrib.Width
                };

                i++;

                xdghList.Add(xdgh);
            }


            XDataGrid.SetHeaderDescriptors(this, xdghList);
        }



        public static readonly DirectProperty<XDataGrid, Type> DataTypeProperty =
            AvaloniaProperty.RegisterDirect<XDataGrid, Type>(
                nameof(DataType),
                o => o.DataType,
                (o, v) => o.DataType = v);

        private Type _dataType;

        public Type DataType
        {
            get { return _dataType; }
            set
            {
                if (value is null) return;

                SetAndRaise(DataTypeProperty, ref _dataType, value);
            }
        }

        public XDataGrid()
        {
            
        }
    }
}
