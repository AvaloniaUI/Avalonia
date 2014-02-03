namespace Perspex
{
    using System;
    using Perspex.Controls;

    public class ControlTemplate
    {
        public ControlTemplate(Func<TemplatedControl, Visual> build)
        {
            this.Build = build;
        }

        public Func<TemplatedControl, Visual> Build
        {
            get;
            private set;
        }

        public static ControlTemplate Create<TControl>(Func<TControl, Visual> build)
            where TControl : TemplatedControl
        {
            return new ControlTemplate(c => build((TControl)c));
        }
    }
}
