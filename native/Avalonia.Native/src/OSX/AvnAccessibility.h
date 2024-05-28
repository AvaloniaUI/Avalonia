#pragma once
#import <Cocoa/Cocoa.h>

// Defines the interface between AvnAutomationNode and objects which implement
// NSAccessibility such as AvnAccessibilityElement or AvnWindow.
@protocol AvnAccessibility <NSAccessibility>
@required
- (void) raiseChildrenChanged;
@optional
- (void) raiseFocusChanged;
- (void) raisePropertyChanged:(AvnAutomationProperty)property;
@end
