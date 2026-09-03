using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public partial class AvaloniaView : IUIAccessibilityContainer
    {
        [Export("accessibilityContainerType")]
        public UIAccessibilityContainerType AccessibilityContainerType => 
            _accessWrapper.AccessibilityContainerType;

        [Export("accessibilityElementCount")]
        public nint AccessibilityElementCount() => 
            _accessWrapper.AccessibilityElementCount();

        [Export("accessibilityElementAtIndex:")]
        public NSObject GetAccessibilityElementAt(nint index) =>
            _accessWrapper.GetAccessibilityElementAt(index);

        [Export("indexOfAccessibilityElement:")]
        public nint GetIndexOfAccessibilityElement(NSObject element) => 
            _accessWrapper.GetIndexOfAccessibilityElement(element);
    }
}
