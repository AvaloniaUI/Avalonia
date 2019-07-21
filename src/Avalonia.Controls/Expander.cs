using Avalonia.Animation;
using Avalonia.Controls.Primitives;

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
            PseudoClass<Expander, ExpandDirection>(ExpandDirectionProperty, d => d == ExpandDirection.Down, ":down");
            PseudoClass<Expander, ExpandDirection>(ExpandDirectionProperty, d => d == ExpandDirection.Up, ":up");
            PseudoClass<Expander, ExpandDirection>(ExpandDirectionProperty, d => d == ExpandDirection.Left, ":left");
            PseudoClass<Expander, ExpandDirection>(ExpandDirectionProperty, d => d == ExpandDirection.Right, ":right");

            PseudoClass<Expander>(IsExpandedProperty, ":expanded");

            IsExpandedProperty.Changed.AddClassHandler<Expander>(x => x.OnIsExpandedChanged);
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
            set { SetAndRaise(IsExpandedProperty, ref _isExpanded, value); }
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
    }
}
