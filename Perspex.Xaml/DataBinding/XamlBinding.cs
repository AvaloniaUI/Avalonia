namespace Perspex.Xaml.DataBinding
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
                if (BindingMode == BindingMode.TwoWay)
                {
                    var changeSynchronizer = new DataContextChangeSynchronizer(
                        Target,
                        TargetProperty,
                        SourcePropertyPath,
                        dataContext,
                        typeConverterProvider);

                    changeSynchronizer.SubscribeUIToModel();
                    changeSynchronizer.SubscribeModelToUI();
                }

                if (BindingMode == BindingMode.OneWay)
                {
                    var subscriptionHandler = new DataContextChangeSynchronizer(
                        Target,
                        TargetProperty,
                        SourcePropertyPath,
                        dataContext,
                        typeConverterProvider);

                    subscriptionHandler.SubscribeUIToModel();                
                }

                if (BindingMode == BindingMode.OneWayToSource)
                {
                    var subscriptionHandler = new DataContextChangeSynchronizer(Target, TargetProperty, SourcePropertyPath, dataContext, typeConverterProvider);
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