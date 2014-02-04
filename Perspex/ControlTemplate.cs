namespace Perspex
{
    using System;
    using Perspex.Controls;

    public class ControlTemplate
    {
        public ControlTemplate(Func<TemplatedControl, Control> build)
        {
            this.Build = build;
        }

        public Func<TemplatedControl, Control> Build
        {
            get;
            private set;
        }

        public static ControlTemplate Create<TControl>(Func<TControl, Control> build)
            where TControl : TemplatedControl
        {
            return new ControlTemplate(c => build((TControl)c));
        }
    }
}
