using Portable.Xaml;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    public class AvaloniaXamlObjectWriter : XamlObjectWriter
    {
        public static AvaloniaXamlObjectWriter Create(XamlSchemaContext schemaContext, object instance)
        {
            var writerSettings = new XamlObjectWriterSettings();
            var nameScope = new AvaloniaNameScope { Instance = instance };
            writerSettings.ExternalNameScope = nameScope;
            writerSettings.RegisterNamesOnExternalNamescope = true;
            writerSettings.RootObjectInstance = instance;

            return new AvaloniaXamlObjectWriter(schemaContext, writerSettings, nameScope);
        }

        private AvaloniaXamlObjectWriter(
            XamlSchemaContext schemaContext,
            XamlObjectWriterSettings settings,
            AvaloniaNameScope nameScope
            )
            : base(schemaContext, settings)
        {
            _nameScope = nameScope;
        }

        protected override void OnAfterBeginInit(object value)
        {
            base.OnAfterBeginInit(value);
        }

        protected override void OnAfterEndInit(object value)
        {
            base.OnAfterEndInit(value);
        }

        protected override void OnAfterProperties(object value)
        {
            base.OnAfterProperties(value);

            //AfterEndInit is not called as it supports only
            //Portable.Xaml.ComponentModel.ISupportInitialize
            //and we have Avalonia.ISupportInitialize so we need some hacks
            _endEditValue = value;
        }

        protected override void OnBeforeProperties(object value)
        {
            //OnAfterBeginInit is not called as it supports only
            //Portable.Xaml.ComponentModel.ISupportInitialize
            //and we have Avalonia.ISupportInitialize so we need some hacks
            HandleBeginInit(value);

            base.OnBeforeProperties(value);
        }

        public override void WriteEndObject()
        {
            base.WriteEndObject();

            //AfterEndInit is not called as it supports only
            //Portable.Xaml.ComponentModel.ISupportInitialize
            //and we have Avalonia.ISupportInitialize so we need some hacks
            HandleEndEdit(_endEditValue);
            _endEditValue = null;
        }

        private object _endEditValue;

        private AvaloniaNameScope _nameScope;

        private void HandleBeginInit(object value)
        {
            (value as Avalonia.ISupportInitialize)?.BeginInit();
        }

        private void HandleEndEdit(object value)
        {
            (value as Avalonia.ISupportInitialize)?.EndInit();
        }

        private void HandleFinished()
        {
            if(_nameScope != null &&  Result != null)
            {
                _nameScope.RegisterOnNameScope(Result);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                HandleFinished();
            }

            base.Dispose(disposing);
        }
    }
}