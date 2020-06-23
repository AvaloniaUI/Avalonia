using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Avalonia.Controls
{
    public enum ExpandDirection
    {
        Down,
        Up,
        Left,
        Right
    }

    public class Expander : HeaderedContentControl
    {
        public static readonly StyledProperty<IPageTransition> ContentTransitionProperty =
            AvaloniaProperty.Register<Expander, IPageTransition>(nameof(ContentTransition));

        public static readonly StyledProperty<ExpandDirection> ExpandDirectionProperty =
            AvaloniaProperty.Register<Expander, ExpandDirection>(nameof(ExpandDirection), ExpandDirection.Down);

        public static readonly DirectProperty<Expander, bool> IsExpandedProperty =
            AvaloniaProperty.RegisterDirect<Expander, bool>(
                nameof(IsExpanded),
                o => o.IsExpanded,
                (o, v) => o.IsExpanded = v,
                defaultBindingMode: Data.BindingMode.TwoWay);

        private bool _isExpanded;

        static Expander()
        {
            IsExpandedProperty.Changed.AddClassHandler<Expander>((x, e) => x.OnIsExpandedChanged(e));
        }

        public Expander()
        {
            UpdatePseudoClasses(ExpandDirection);
        }

        public IPageTransition ContentTransition
        {
            get => GetValue(ContentTransitionProperty);
            set => SetValue(ContentTransitionProperty, value);
        }

        public ExpandDirection ExpandDirection
        {
            get => GetValue(ExpandDirectionProperty);
            set => SetValue(ExpandDirectionProperty, value);
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set 
            { 
                SetAndRaise(IsExpandedProperty, ref _isExpanded, value);
                PseudoClasses.Set(":expanded", value);
            }
        }

        protected virtual void OnIsExpandedChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (Content != null && ContentTransition != null && Presenter is Visual visualContent)
            {
                bool forward = ExpandDirection == ExpandDirection.Left ||
                                ExpandDirection == ExpandDirection.Up;
                if (IsExpanded)
                {
                    ContentTransition.Start(null, visualContent, forward);
                }
                else
                {
                    ContentTransition.Start(visualContent, null, !forward);
                }
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ExpandDirectionProperty)
            {
                UpdatePseudoClasses(change.NewValue.GetValueOrDefault<ExpandDirection>());
            }
        }

        private void UpdatePseudoClasses(ExpandDirection d)
        {
            PseudoClasses.Set(":up", d == ExpandDirection.Up);
            PseudoClasses.Set(":down", d == ExpandDirection.Down);
            PseudoClasses.Set(":left", d == ExpandDirection.Left);
            PseudoClasses.Set(":right", d == ExpandDirection.Right);
        }
    }
}
