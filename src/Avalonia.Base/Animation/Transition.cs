using System;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines how a property should be animated using a transition.
    /// </summary>
    public abstract class Transition<T> : TransitionBase
    {
        static Transition()
        {
            PropertyProperty.Changed.AddClassHandler<Transition<T>>((x, e) => x.OnPropertyPropertyChanged(e));
        }

        private void OnPropertyPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if ((e.NewValue is AvaloniaProperty newValue) && !newValue.PropertyType.IsAssignableFrom(typeof(T)))
                throw new InvalidCastException
                    ($"Invalid property type \"{typeof(T).Name}\" for this transition: {GetType().Name}.");
        }

        /// <summary>
        /// Apply interpolation to the property.
        /// </summary>
        internal abstract IObservable<T> DoTransition(IObservable<double> progress, T oldValue, T newValue);

        internal override IDisposable Apply(Animatable control, IClock clock, object? oldValue, object? newValue)
        {
            if (Property is null)
                throw new InvalidOperationException("Transition has no property specified.");

            var transition = DoTransition(new TransitionInstance(clock, Delay, Duration), (T)oldValue!, (T)newValue!);
            return control.Bind<T>((AvaloniaProperty<T>)Property, transition, Data.BindingPriority.Animation);
        }
    }
}
