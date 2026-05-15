using Avalonia.Input;

namespace Avalonia.Controls.Platform
{
    public class PlatformFeedback
    {
        /// <summary>
        /// Defines the FeedbackType attached property.
        /// </summary>
        public static readonly AttachedProperty<FeedbackType> FeedbackTypeProperty =
            AvaloniaProperty.RegisterAttached<PlatformFeedback, InputElement, FeedbackType>("FeedbackType", defaultValue: FeedbackType.None);

        /// <summary>
        /// Sets the value of the attached FeedbackType property.
        /// </summary>
        /// <param name="control">The attached control</param>
        /// <param name="feedbackType">The feedback type</param>
        public static void SetFeedbackType(InputElement control, FeedbackType feedbackType)
        {
            control.SetValue(FeedbackTypeProperty, feedbackType);
        }

        /// <summary>
        /// Gets the value of the attached FeedbackType property.
        /// </summary>
        /// <param name="control">The feedback type</param>
        /// <returns></returns>
        public static FeedbackType GetFeedbackType(InputElement control)
        {
            return control.GetValue(FeedbackTypeProperty);
        }
    }

    public static class PlatformFeedbackExtensions
    {
        /// <summary>
        /// Performs the specified <see cref="FeedbackAction"/> on this <see cref="InputElement"/>. The type of feedback to perform is defined in the <see cref="PlatformFeedback.FeedbackTypeProperty"/>
        /// </summary>
        /// <param name="inputElement">The element to trigger the feedback effect on</param>
        /// <param name="feedbackAction">The feedback action relating to the action that triggered it</param>
        public static void PerformFeedback(this InputElement inputElement, FeedbackAction feedbackAction)
        {
            var feedback = PlatformFeedback.GetFeedbackType(inputElement);
            if (feedback != FeedbackType.None &&
                TopLevel.GetTopLevel(inputElement)?.PlatformImpl?.TryGetFeature<IPlatformFeedback>() is { } platformFeedBack)
            {
                platformFeedBack.Perform(feedbackAction, feedback);
            }
        }
    }
}
