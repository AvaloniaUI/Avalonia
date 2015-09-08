





namespace Perspex.Markup.Xaml.DataBinding
{
    using System;
    using System.Diagnostics;
    using ChangeTracking;
    using OmniXaml.TypeConversion;

    public class XamlBinding
    {
        private readonly ITypeConverterProvider typeConverterProvider;
        private DataContextChangeSynchronizer changeSynchronizer;

        public XamlBinding(ITypeConverterProvider typeConverterProvider)
        {
            this.typeConverterProvider = typeConverterProvider;
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

                this.changeSynchronizer = new DataContextChangeSynchronizer(bindingSource, bindingTarget, this.typeConverterProvider);

                if (this.BindingMode == BindingMode.TwoWay)
                {
                    this.changeSynchronizer.StartUpdatingTargetWhenSourceChanges();
                    this.changeSynchronizer.StartUpdatingSourceWhenTargetChanges();
                }

                if (this.BindingMode == BindingMode.OneWay)
                {
                    this.changeSynchronizer.StartUpdatingTargetWhenSourceChanges();
                }

                if (this.BindingMode == BindingMode.OneWayToSource)
                {
                    this.changeSynchronizer.StartUpdatingSourceWhenTargetChanges();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}