namespace Avalonia.Styling.Activators
{
    internal sealed class PlatformActivator : MediaQueryActivatorBase
    {
        private readonly string _argument;

        public PlatformActivator(Visual visual, string argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && PlatformMediaQuery.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }
}
