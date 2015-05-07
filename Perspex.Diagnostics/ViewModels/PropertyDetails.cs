// -----------------------------------------------------------------------
// <copyright file="PropertyDetails.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System;
    using ReactiveUI;

    internal class PropertyDetails : ReactiveObject
    {
        private object value;

        private string priority;

        private string diagnostic;

        public PropertyDetails(PerspexObject o, PerspexProperty property)
        {
            this.Name = property.IsAttached ?
                string.Format("[{0}.{1}]", property.OwnerType.Name, property.Name) :
                property.Name;
            this.IsAttached = property.IsAttached;

            // TODO: Unsubscribe when view model is deactivated.
            o.GetObservable(property).Subscribe(x =>
            {
                var diagnostic = o.GetDiagnostic(property);
                this.Value = diagnostic.Value ?? "(null)";
                this.Priority = (diagnostic.Priority != BindingPriority.Unset) ?
                    diagnostic.Priority.ToString() :
                    diagnostic.Property.Inherits ? "Inherited" : "Unset";
                this.Diagnostic = diagnostic.Diagnostic;
            });
        }

        public string Name { get; }

        public bool IsAttached { get; }

        public string Priority
        {
            get { return this.priority; }
            private set { this.RaiseAndSetIfChanged(ref this.priority, value); }
        }

        public string Diagnostic
        {
            get { return this.diagnostic; }
            private set { this.RaiseAndSetIfChanged(ref this.diagnostic, value); }
        }

        public object Value
        {
            get { return this.value; }
            private set { this.RaiseAndSetIfChanged(ref this.value, value); }
        }
    }
}
