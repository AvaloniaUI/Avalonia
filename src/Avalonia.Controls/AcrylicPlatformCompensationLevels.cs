namespace Avalonia.Controls
{
    /// <summary>
    /// Defines compensation levels for the platform depending on the transparency level.
    /// It controls the base opacity level of the 'tracing paper' layer that compensates
    /// for low blur radius.
    /// </summary>
    public struct AcrylicPlatformCompensationLevels
    {
        public double TransparentLevel { get; }

        public double BlurLevel { get; }

        public double AcrylicBlurLevel { get; }
    }
}
