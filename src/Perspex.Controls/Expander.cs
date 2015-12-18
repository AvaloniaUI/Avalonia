using Perspex.Animation;
using Perspex.Controls.Primitives;

namespace Perspex.Controls
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
        public static readonly PerspexProperty<bool> IsExpandedProperty =
            PerspexProperty.Register<Expander, bool>(nameof(IsExpanded), true);

        public static readonly PerspexProperty<ExpandDirection> ExpandDirectionProperty =
            PerspexProperty.Register<Expander, ExpandDirection>(nameof(ExpandDirection), ExpandDirection.Down);

        public static readonly PerspexProperty<IPageTransition> ContentTransitionProperty =
            PerspexProperty.Register<Expander, IPageTransition>(nameof(ContentTransition));

        static Expander()
        {
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Down, ":down");
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Up, ":up");
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Left, ":left");
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Right, ":right");

            PseudoClass(IsExpandedProperty, ":expanded");

            IsExpandedProperty.Changed.AddClassHandler<Expander>(x => x.OnIsExpandedChanged);
        }

        public bool IsExpanded
        {
            get { return GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public ExpandDirection ExpandDirection
        {
            get { return GetValue(ExpandDirectionProperty); }
            set { SetValue(ExpandDirectionProperty, value); }
        }

        public IPageTransition ContentTransition
        {
            get { return GetValue(ContentTransitionProperty); }
            set { SetValue(ContentTransitionProperty, value); }
        }

        protected virtual void OnIsExpandedChanged(PerspexPropertyChangedEventArgs e)
        {
            IVisual visualContent = Presenter;

            if (Content != null && ContentTransition != null && visualContent != null)
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