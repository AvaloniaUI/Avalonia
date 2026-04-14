using Avalonia.Input;

namespace Avalonia.Controls.Platform
{
    public class PlatformFeedback
    {
        public static readonly AttachedProperty<FeedbackType> FeedbackTypeProperty =
            AvaloniaProperty.RegisterAttached<PlatformFeedback, InputElement, FeedbackType>("FeedbackType", defaultValue: FeedbackType.None);

        public static void SetFeedbackType(InputElement control, FeedbackType feedbackType)
        {
            control.SetValue(FeedbackTypeProperty, feedbackType);
        }

        public static FeedbackType GetFeedbackType(InputElement control)
        {
            return control.GetValue(FeedbackTypeProperty);
        }
    }

    internal static class PlatformFeedbackExtensions
    {
        internal static void PerformFeedback(this InputElement inputElement, FeedbackEffect feedbackEffect)
        {
            var feedback = PlatformFeedback.GetFeedbackType(inputElement);
            if (feedback != FeedbackType.None &&
                TopLevel.GetTopLevel(inputElement)?.PlatformImpl?.TryGetFeature<IPlatformFeedback>() is { } platformFeedBack)
            {
                platformFeedBack.Play(feedbackEffect, feedback);
            }
        }
    }
}
