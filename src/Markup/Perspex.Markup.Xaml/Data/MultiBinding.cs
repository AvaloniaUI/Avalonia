// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using OmniXaml.TypeConversion;
using Perspex.Controls;
using Perspex.Markup.Data;
using Perspex.Metadata;

namespace Perspex.Markup.Xaml.Data
{
    public class MultiBinding : IBinding
    {
        private readonly ITypeConverterProvider _typeConverterProvider;

        public MultiBinding()
        {
        }

        public MultiBinding(ITypeConverterProvider typeConverterProvider)
        {
            _typeConverterProvider = typeConverterProvider;
        }

        [Content]
        public IList<Binding> Bindings { get; } = new List<Binding>();
        public IMultiValueConverter Converter { get; set; }
        public BindingMode Mode { get; set; }
        public BindingPriority Priority { get; set; }
        public RelativeSource RelativeSource { get; set; }
        public string SourcePropertyPath { get; set; }

        public void Bind(IObservablePropertyBag instance, PerspexProperty property)
        {
            var subject = CreateSubject(instance, property);

            if (subject != null)
            {
                Bind(instance, property, subject);
            }
        }

        public ISubject<object> CreateSubject(
            IObservablePropertyBag instance, 
            PerspexProperty property)
        {
            if (Converter == null)
            {
                throw new NotSupportedException("MultiBinding without Converter not currently supported.");
            }

            var result = new Subject<object>();
            var children = Bindings.Select(x => x.CreateExpressionSubject(instance, property));
            var input = Observable.CombineLatest(children).Select(x =>
                Converter.Convert(x, property.PropertyType, null, CultureInfo.CurrentUICulture));
            input.Subscribe(result);
            return result;
        }

        internal void Bind(IObservablePropertyBag target, PerspexProperty property, ISubject<object> subject)
        {
            var mode = Mode == BindingMode.Default ?
                property.DefaultBindingMode : Mode;

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    target.Bind(property, subject, Priority);
                    break;
                case BindingMode.TwoWay:
                    throw new NotSupportedException("TwoWay MultiBinding not currently supported.");
                case BindingMode.OneTime:
                    target.GetObservable(Control.DataContextProperty).Subscribe(dataContext =>
                    {
                        subject.Take(1).Subscribe(x => target.SetValue(property, x, Priority));
                    });                    
                    break;
                case BindingMode.OneWayToSource:
                    target.GetObservable(property).Subscribe(subject);
                    break;
            }
        }
    }
}