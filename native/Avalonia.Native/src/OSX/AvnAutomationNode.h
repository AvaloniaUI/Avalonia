#pragma once
#include "avalonia-native.h"
#include "AvnAccessibility.h"

// Defines a means for managed code to raise accessibility events.
class AvnAutomationNode : public ComSingleObject<IAvnAutomationNode, &IID_IAvnAutomationNode>
{
public:
    FORWARD_IUNKNOWN()
    AvnAutomationNode(id <AvnAccessibility> owner) { _owner = owner; }
    id <AvnAccessibility> GetOwner() { return _owner; }
    void SetOwner(id <AvnAccessibility> owner) { _owner = owner; }
    void ClearOwner(id <AvnAccessibility> owner)
    {
        if (_owner == owner)
            _owner = nil;
    }
    virtual void Dispose() override
    {
        if (_disposed)
            return;

        _disposed = true;
        _owner = nil;
        Release();
    }
    virtual void ChildrenChanged () override { [_owner raiseChildrenChanged]; }
    virtual void PropertyChanged (AvnAutomationProperty property) override { [_owner raisePropertyChanged:property]; }
    virtual void FocusChanged () override { [_owner raiseFocusChanged]; }
private:
    __weak id <AvnAccessibility> _owner;
    bool _disposed = false;
};
