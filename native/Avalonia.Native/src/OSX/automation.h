#pragma once

#import <Cocoa/Cocoa.h>
#include "common.h"

class IAvnAutomationPeer;

// Defines the interface between AutomationNode and objects which implement
// NSAccessibility such as AvnAccessibilityElement or AvnWindow.
@protocol AvnAccessibility <NSAccessibility>
@required
- (void) raiseChildrenChanged;
@optional
- (void) raiseFocusChanged;
- (void) raisePropertyChanged:(AvnAutomationProperty)property;
@end

// A node representing an Avalonia control in the native accessibility tree.
@interface AvnAccessibilityElement : NSAccessibilityElement <AvnAccessibility>
+ (AvnAccessibilityElement *) acquire:(IAvnAutomationPeer *) peer;
@end

// Defines a means for managed code to raise accessibility events.
class AutomationNode : public ComSingleObject<IAvnAutomationNode, &IID_IAvnAutomationNode>
{
public:
    FORWARD_IUNKNOWN()
    AutomationNode(id <AvnAccessibility> owner) { _owner = owner; }
    AvnAccessibilityElement* GetOwner() { return _owner; }
    virtual void Dispose() override { _owner = nil; }
    virtual void ChildrenChanged () override { [_owner raiseChildrenChanged]; }
    virtual void PropertyChanged (AvnAutomationProperty property) override { [_owner raisePropertyChanged:property]; }
    virtual void FocusChanged () override { [_owner raiseFocusChanged]; }
private:
    __strong id <AvnAccessibility> _owner;
};

