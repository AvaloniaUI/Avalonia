using System;
using Avalonia.LogicalTree;
using Avalonia.Platform;

namespace Avalonia.Styling.Activators
{
    internal sealed class MinWidthActivator : MediaQueryActivatorBase
    {
        private readonly double _argument;

        public MinWidthActivator(Visual visual, double argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MinWidthMediaQuery.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }

    internal sealed class MaxWidthActivator : MediaQueryActivatorBase
    {
        private readonly double _argument;

        public MaxWidthActivator(Visual visual, double argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MaxWidthMediaQuery.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }

    internal sealed class MinHeightActivator : MediaQueryActivatorBase
    {
        private readonly double _argument;

        public MinHeightActivator(Visual visual, double argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MinHeightMediaQuery.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }

    internal sealed class MaxHeightActivator : MediaQueryActivatorBase
    {
        private readonly double _argument;

        public MaxHeightActivator(Visual visual, double argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && MaxHeightMediaQuery.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }

    internal sealed class OrientationActivator : MediaQueryActivatorBase
    {
        private readonly DeviceOrientation _argument;

        public OrientationActivator(Visual visual, DeviceOrientation argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && OrientationMediaQuery.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }
}
