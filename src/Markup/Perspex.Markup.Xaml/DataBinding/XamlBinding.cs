// -----------------------------------------------------------------------
// <copyright file="XamlBinding.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.DataBinding
{
    using System;
    using System.Diagnostics;
    using ChangeTracking;
    using OmniXaml.TypeConversion;

    public class XamlBinding
    {
        private readonly ITypeConverterProvider typeConverterProvider;

        public PerspexObject Target { get; set; }

        public PerspexProperty TargetProperty { get; set; }

        public PropertyPath SourcePropertyPath { get; set; }

        public BindingMode BindingMode { get; set; }

        public XamlBinding(ITypeConverterProvider typeConverterProvider)
        {
            this.typeConverterProvider = typeConverterProvider;
        }

        public void Bind(object dataContext)
        {
            if (dataContext == null)
            {
                return;
            }

            try
            {
                if (this.BindingMode == BindingMode.TwoWay)
                {
                    var changeSynchronizer = new DataContextChangeSynchronizer(
                        this.Target,
                        this.TargetProperty,
                        this.SourcePropertyPath,
                        dataContext,
                        this.typeConverterProvider);

                    changeSynchronizer.SubscribeUIToModel();
                    changeSynchronizer.SubscribeModelToUI();
                }

                if (this.BindingMode == BindingMode.OneWay)
                {
                    var subscriptionHandler = new DataContextChangeSynchronizer(
                        this.Target,
                        this.TargetProperty,
                        this.SourcePropertyPath,
                        dataContext,
                        this.typeConverterProvider);

                    subscriptionHandler.SubscribeUIToModel();
                }

                if (this.BindingMode == BindingMode.OneWayToSource)
                {
                    var subscriptionHandler = new DataContextChangeSynchronizer(this.Target, this.TargetProperty, this.SourcePropertyPath, dataContext, this.typeConverterProvider);
                    subscriptionHandler.SubscribeModelToUI();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}