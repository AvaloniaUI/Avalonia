using System;
using System.Reactive.Linq;
using Avalonia.Animation.Animators;
using Avalonia.Media;

namespace Avalonia.Animation
{
    public class TransformOperationsTransition : Transition<ITransform>
    {
        private static readonly TransformOperationsAnimator _operationsAnimator =  new TransformOperationsAnimator();

        public override IObservable<ITransform> DoTransition(IObservable<double> progress,
            ITransform oldValue,
            ITransform newValue)
        {
            var oldTransform = TransformOperationsAnimator.EnsureOperations(oldValue);
            var newTransform = TransformOperationsAnimator.EnsureOperations(newValue);

            return progress
                .Select(p =>
                {
                    var f = Easing.Ease(p);

                    return _operationsAnimator.Interpolate(f, oldTransform, newTransform);
                });
        }
    }
}
