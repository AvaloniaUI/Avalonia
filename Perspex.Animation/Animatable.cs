// -----------------------------------------------------------------------
// <copyright file="Animatable.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Input;

    public class Animatable : InputElement
    {
        private PropertyTransitions propertyTransitions;

        public PropertyTransitions PropertyTransitions
        {
            get
            {
                if (this.propertyTransitions == null)
                {
                    this.propertyTransitions = new PropertyTransitions();
                }

                return this.propertyTransitions;
            }

            set
            {
                this.propertyTransitions = value;
            }
        }

        protected override void OnPropertyChanged(PerspexPropertyChangedEventArgs e)
        {
            if (e.Priority != BindingPriority.Animation && this.propertyTransitions != null)
            {
                var match = this.propertyTransitions.FirstOrDefault(x => x.Property == e.Property);

                if (match != null)
                {
                    Animate.Property(this, e.Property, e.OldValue, e.NewValue, match.Easing, match.Duration);
                }
            }
        }
    }
}
