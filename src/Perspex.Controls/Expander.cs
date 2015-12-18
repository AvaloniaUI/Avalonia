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
        static Expander()
        {
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Down, ":ExpandDirectionDown");
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Up, ":ExpandDirectionUp");
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Left, ":ExpandDirectionLeft");
            PseudoClass(ExpandDirectionProperty, d => d == ExpandDirection.Right, ":ExpandDirectionRight");

            PseudoClass(IsExpandedProperty, ":expanded");

            IsExpandedProperty.Changed.AddClassHandler<Expander>(x => x.OnIsExpandedChanged);
        }

        protected virtual void OnIsExpandedChanged(PerspexPropertyChangedEventArgs e)
        {
            IVisual visualContent = Presenter;
            if (Content != null && ContentTransition != null && visualContent != null)
            {
                bool forward = ExpandDirection == ExpandDirection.Left || ExpandDirection == ExpandDirection.Up;
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

        public static readonly PerspexProperty<bool> IsExpandedProperty = PerspexProperty.Register<Expander, bool>(nameof(IsExpanded), true);

        public bool IsExpanded
        {
            get { return GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        public static readonly PerspexProperty<ExpandDirection> ExpandDirectionProperty = PerspexProperty.Register<Expander, ExpandDirection>(nameof(ExpandDirection), ExpandDirection.Down);

        public ExpandDirection ExpandDirection
        {
            get { return GetValue(ExpandDirectionProperty); }
            set { SetValue(ExpandDirectionProperty, value); }
        }

        public static readonly PerspexProperty<IPageTransition> ContentTransitionProperty = PerspexProperty.Register<Expander, IPageTransition>(nameof(ContentTransition));

        public IPageTransition ContentTransition
        {
            get { return GetValue(ContentTransitionProperty); }
            set { SetValue(ContentTransitionProperty, value); }
        }
    }
}