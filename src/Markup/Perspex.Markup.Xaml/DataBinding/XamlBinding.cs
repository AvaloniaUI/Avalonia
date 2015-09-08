// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using OmniXaml.TypeConversion;
using Perspex.Markup.Xaml.DataBinding.ChangeTracking;

namespace Perspex.Markup.Xaml.DataBinding
{
    public class XamlBinding
    {
        private readonly ITypeConverterProvider _typeConverterProvider;
        private DataContextChangeSynchronizer _changeSynchronizer;

        public XamlBinding(ITypeConverterProvider typeConverterProvider)
        {
            _typeConverterProvider = typeConverterProvider;
        }

        public PerspexObject Target { get; set; }

        public PerspexProperty TargetProperty { get; set; }

        public PropertyPath SourcePropertyPath { get; set; }

        public BindingMode BindingMode { get; set; }

        public void BindToDataContext(object dataContext)
        {
            if (dataContext == null)
            {
                return;
            }

            try
            {
                var bindingSource = new DataContextChangeSynchronizer.BindingSource(this.SourcePropertyPath, dataContext);
                var bindingTarget = new DataContextChangeSynchronizer.BindingTarget(this.Target, this.TargetProperty);

                _changeSynchronizer = new DataContextChangeSynchronizer(bindingSource, bindingTarget, _typeConverterProvider);

                if (this.BindingMode == BindingMode.TwoWay)
                {
                    _changeSynchronizer.StartUpdatingTargetWhenSourceChanges();
                    _changeSynchronizer.StartUpdatingSourceWhenTargetChanges();
                }

                if (this.BindingMode == BindingMode.OneWay)
                {
                    _changeSynchronizer.StartUpdatingTargetWhenSourceChanges();
                }

                if (this.BindingMode == BindingMode.OneWayToSource)
                {
                    _changeSynchronizer.StartUpdatingSourceWhenTargetChanges();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}