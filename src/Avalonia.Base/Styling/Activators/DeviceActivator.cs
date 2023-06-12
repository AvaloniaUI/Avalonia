namespace Avalonia.Styling.Activators
{
    internal sealed class IsOsActivator : MediaQueryActivatorBase
    {
        private readonly string _argument;

        public IsOsActivator(Visual visual, string argument) : base(visual)
        {
            _argument = argument;
        }

        protected override bool EvaluateIsActive() => CurrentMediaInfoProvider != null && IsOsMediaQuery.Evaluate(CurrentMediaInfoProvider, _argument).IsMatch;
    }
}
