using System;
using Avalonia.LogicalTree;

namespace Avalonia.Styling.Activators
{
    internal sealed class MinWidthActivator : MediaQueryActivatorBase
    {
        private readonly double _argument;

        public MinWidthActivator(Visual visual, double argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MinWidthMediaSelector.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }

    internal sealed class MaxWidthActivator : MediaQueryActivatorBase
    {
        private readonly double _argument;

        public MaxWidthActivator(Visual visual, double argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MaxWidthMediaSelector.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }

    internal sealed class MinHeightActivator : MediaQueryActivatorBase
    {
        private readonly double _argument;

        public MinHeightActivator(Visual visual, double argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MinHeightMediaSelector.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }

    internal sealed class MaxHeightActivator : MediaQueryActivatorBase
    {
        private readonly double _argument;

        public MaxHeightActivator(Visual visual, double argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MaxHeightMediaSelector.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }
}
