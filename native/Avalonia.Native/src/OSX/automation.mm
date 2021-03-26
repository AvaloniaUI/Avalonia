#import "automation.h"
#include "common.h"
#include "AvnString.h"
#include "window.h"

class AutomationNode : public ComSingleObject<IAvnAutomationNode, &IID_IAvnAutomationNode>,
    public INSAccessibilityHolder
{
private:
    NSAccessibilityElement* _node;
public:
    FORWARD_IUNKNOWN()

    AutomationNode(NSAccessibilityElement* node)
    {
        _node = node;
    }

    AutomationNode(IAvnAutomationPeer* peer)
    {
        _node = [[AvnAutomationNode alloc] initWithPeer: peer];
    }
    
    virtual NSObject* GetNSAccessibility() override
    {
        return _node;
    }
};

@implementation AvnAutomationNode
{
    IAvnAutomationPeer* _peer;
    NSMutableArray* _children;
}

- (AvnAutomationNode *)initWithPeer:(IAvnAutomationPeer *)peer
{
    self = [super init];
    _peer = peer;
    return self;
}

- (BOOL)isAccessibilityElement
{
    return _peer->IsControlElement();
}

- (NSAccessibilityRole)accessibilityRole
{
    auto controlType = _peer->GetAutomationControlType();
    
    switch (controlType) {
        case AutomationButton:
            return NSAccessibilityButtonRole;
        case AutomationCheckBox:
            return NSAccessibilityCheckBoxRole;
        case AutomationComboBox:
            return NSAccessibilityPopUpButtonRole;
        case AutomationGroup:
        case AutomationPane:
            return NSAccessibilityGroupRole;
        case AutomationSlider:
            return NSAccessibilitySliderRole;
        case AutomationTab:
            return NSAccessibilityTabGroupRole;
        case AutomationTabItem:
            return NSAccessibilityRadioButtonRole;
        case AutomationWindow:
            return NSAccessibilityWindowRole;
        default:
            return NSAccessibilityUnknownRole;
    }
}

- (NSString *)accessibilityIdentifier
{
    return GetNSStringAndRelease(_peer->GetAutomationId());
}

- (NSString *)accessibilityTitle
{
    return GetNSStringAndRelease(_peer->GetName());
}

- (NSArray *)accessibilityChildren
{
    if (_children == nullptr && _peer != nullptr)
    {
        auto childPeers = _peer->GetChildren();
        auto childCount = childPeers != nullptr ? childPeers->GetCount() : 0;

        if (childCount > 0)
        {
            _children = [[NSMutableArray alloc] initWithCapacity:childCount];
            
            for (int i = 0; i < childCount; ++i)
            {
                IAvnAutomationPeer* child;
                
                if (childPeers->Get(i, &child) == S_OK)
                {
                    NSObject* element = ::GetAccessibilityElement(child->GetNode());
                    [_children addObject:element];
                }
            }
        }
    }
    
    return _children;
}

- (NSRect)accessibilityFrame
{
    auto view = [self getAvnView];
    auto window = [self getAvnWindow];

    if (view != nullptr)
    {
        auto bounds = ToNSRect(_peer->GetBoundingRectangle());
        auto windowBounds = [view convertRect:bounds toView:nil];
        auto screenBounds = [window convertRectToScreen:windowBounds];
        return screenBounds;
    }
    
    return NSRect();
}

- (id)accessibilityParent
{
    auto parentPeer = _peer->GetParent();
    
    if (parentPeer != nullptr)
    {
        return GetAccessibilityElement(parentPeer);
    }
    
    return [NSApplication sharedApplication];
}

- (id)accessibilityTopLevelUIElement
{
    return GetAccessibilityElement([self getRootNode]);
}

- (id)accessibilityWindow
{
    return [self accessibilityTopLevelUIElement];
}

- (BOOL)accessibilityPerformPress
{
    _peer->InvokeProvider_Invoke();
    return YES;
}

- (BOOL)isAccessibilitySelectorAllowed:(SEL)selector
{
    if (selector == @selector(accessibilityPerformPress))
    {
        return _peer->IsInvokeProvider();
    }
    
    return [super isAccessibilitySelectorAllowed:selector];
}

- (IAvnAutomationNode*)getRootNode
{
    auto rootPeer = _peer->GetRootPeer();
    return rootPeer != nullptr ? rootPeer->GetNode() : nullptr;
}

- (IAvnWindowBase*)getWindow
{
    auto rootNode = [self getRootNode];

    if (rootNode != nullptr)
    {
        IAvnWindowBase* window;
        if (rootNode->QueryInterface(&IID_IAvnWindow, (void**)&window) == S_OK)
        {
            return window;
        }
    }
    
    return nullptr;
}

- (AvnWindow*) getAvnWindow
{
    auto window = [self getWindow];
    return dynamic_cast<INSWindowHolder*>(window)->GetNSWindow();
}

- (AvnView*) getAvnView
{
    auto window = [self getWindow];
    return dynamic_cast<INSWindowHolder*>(window)->GetNSView();
}

@end

extern IAvnAutomationNode* CreateAutomationNode(IAvnAutomationPeer* peer)
{
    @autoreleasepool
    {
        return new AutomationNode(peer);
    }
}

extern NSObject* GetAccessibilityElement(IAvnAutomationPeer* peer)
{
    auto node = peer != nullptr ? peer->GetNode() : nullptr;
    return GetAccessibilityElement(node);
}

extern NSObject* GetAccessibilityElement(IAvnAutomationNode* node)
{
    auto holder = dynamic_cast<INSAccessibilityHolder*>(node);
    return holder != nullptr ? holder->GetNSAccessibility() : nil;
}
