// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using OmniXaml.TypeConversion;
using Perspex.Markup.Binding;

namespace Perspex.Markup.Xaml.Binding
{
    public class XamlBinding
    {
        private readonly ITypeConverterProvider _typeConverterProvider;

        public XamlBinding(ITypeConverterProvider typeConverterProvider)
        {
            _typeConverterProvider = typeConverterProvider;
        }

        public PerspexObject Target { get; set; }

        public PerspexProperty TargetProperty { get; set; }

        public object Source { get; set; }

        public string SourcePropertyPath { get; set; }

        public BindingMode BindingMode { get; set; }

        public void Bind()
        {
            var path = SourcePropertyPath;
            var source = Source;

            if (source == null)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    path = "DataContext." + path;
                }

                source = Target;
            }

            var observable = new ExpressionObserver(source, path);
            var mode = BindingMode == BindingMode.Default ? 
                TargetProperty.DefaultBindingMode : BindingMode;

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    Target.Bind(TargetProperty, observable.Select(x => x.Value));
                    break;
                case BindingMode.TwoWay:
                    Target.BindTwoWay(TargetProperty, new ExpressionSubject(observable));
                    break;
                case BindingMode.OneTime:
                    throw new NotImplementedException();
                case BindingMode.OneWayToSource:
                    Target.GetObservable(TargetProperty).Subscribe(new ExpressionSubject(observable));
                    break;
            }
        }
    }
}