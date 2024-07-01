#pragma once
#include "avalonia-native.h"
#include "AvnAccessibility.h"

// Defines a means for managed code to raise accessibility events.
class AvnAutomationNode : public ComSingleObject<IAvnAutomationNode, &IID_IAvnAutomationNode>
{
public:
    FORWARD_IUNKNOWN()
    AvnAutomationNode(id <AvnAccessibility> owner) { _owner = owner; }
    AvnAccessibilityElement* GetOwner() { return _owner; }
    virtual void Dispose() override { _owner = nil; }
    virtual void ChildrenChanged () override { [_owner raiseChildrenChanged]; }
    virtual void PropertyChanged (AvnAutomationProperty property) override { [_owner raisePropertyChanged:property]; }
    virtual void FocusChanged () override { [_owner raiseFocusChanged]; }
private:
    __strong id <AvnAccessibility> _owner;
};
