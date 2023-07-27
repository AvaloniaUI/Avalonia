#include "common.h"
#include "automation.h"
#include "AvnString.h"
#include "INSWindowHolder.h"
#include "AvnView.h"

@interface AvnAccessibilityElement (Events)
- (void) raiseChildrenChanged;
@end

@interface AvnRootAccessibilityElement : AvnAccessibilityElement
- (AvnView *) ownerView;
- (AvnRootAccessibilityElement *) initWithPeer:(IAvnAutomationPeer *) peer owner:(AvnView*) owner;
- (void) raiseFocusChanged;
@end

class AutomationNode : public ComSingleObject<IAvnAutomationNode, &IID_IAvnAutomationNode>
{
public:
    FORWARD_IUNKNOWN()

    AutomationNode(AvnAccessibilityElement* owner)
    {
        _owner = owner;
    }
    
    AvnAccessibilityElement* GetOwner()
    {
        return _owner;
    }
    
    virtual void Dispose() override
    {
        _owner = nil;
    }
    
    virtual void ChildrenChanged () override
    {
        [_owner raiseChildrenChanged];
    }
    
    virtual void PropertyChanged (AvnAutomationProperty property) override
    {
        
    }
    
    virtual void FocusChanged () override
    {
        [(AvnRootAccessibilityElement*)_owner raiseFocusChanged];
    }
    
private:
    __strong AvnAccessibilityElement* _owner;
};

@implementation AvnAccessibilityElement
{
    IAvnAutomationPeer* _peer;
    AutomationNode* _node;
    NSMutableArray* _children;
}

+ (AvnAccessibilityElement *)acquire:(IAvnAutomationPeer *)peer
{
    if (peer == nullptr)
        return nil;
    
    auto instance = peer->GetNode();
    
    if (instance != nullptr)
        return dynamic_cast<AutomationNode*>(instance)->GetOwner();
    
    if (peer->IsRootProvider())
    {
        auto window = peer->RootProvider_GetWindow();
        
        if (window == nullptr)
        {
            NSLog(@"IRootProvider.PlatformImpl returned null or a non-WindowBaseImpl.");
            return nil;
        }
        
        auto holder = dynamic_cast<INSWindowHolder*>(window);
        auto view = holder->GetNSView();
        return [[AvnRootAccessibilityElement alloc] initWithPeer:peer owner:view];
    }
    else
    {
        return [[AvnAccessibilityElement alloc] initWithPeer:peer];
    }
}

- (AvnAccessibilityElement *)initWithPeer:(IAvnAutomationPeer *)peer
{
    self = [super init];
    _peer = peer;
    _node = new AutomationNode(self);
    _peer->SetNode(_node);
    return self;
}

- (void)dealloc
{
    if (_node)
        delete _node;
    _node = nullptr;
}

- (NSString *)description
{
    return [NSString stringWithFormat:@"%@ '%@' (%p)",
        GetNSStringAndRelease(_peer->GetClassName()),
        GetNSStringAndRelease(_peer->GetName()),
        _peer];
}

- (IAvnAutomationPeer *)peer
{
    return _peer;
}

- (BOOL)isAccessibilityElement
{
    return _peer->IsControlElement();
}

- (NSAccessibilityRole)accessibilityRole
{
    auto controlType = _peer->GetAutomationControlType();
    
    switch (controlType) {
        case AutomationButton: return NSAccessibilityButtonRole;
        case AutomationCalendar: return NSAccessibilityGridRole;
        case AutomationCheckBox: return NSAccessibilityCheckBoxRole;
        case AutomationComboBox: return NSAccessibilityPopUpButtonRole;
        case AutomationComboBoxItem: return NSAccessibilityMenuItemRole;
        case AutomationEdit: return NSAccessibilityTextFieldRole;
        case AutomationHyperlink: return NSAccessibilityLinkRole;
        case AutomationImage: return NSAccessibilityImageRole;
        case AutomationListItem: return NSAccessibilityRowRole;
        case AutomationList: return NSAccessibilityTableRole;
        case AutomationMenu: return NSAccessibilityMenuBarRole;
        case AutomationMenuBar: return NSAccessibilityMenuBarRole;
        case AutomationMenuItem: return NSAccessibilityMenuItemRole;
        case AutomationProgressBar: return NSAccessibilityProgressIndicatorRole;
        case AutomationRadioButton: return NSAccessibilityRadioButtonRole;
        case AutomationScrollBar: return NSAccessibilityScrollBarRole;
        case AutomationSlider: return NSAccessibilitySliderRole;
        case AutomationSpinner: return NSAccessibilityIncrementorRole;
        case AutomationStatusBar: return NSAccessibilityTableRole;
        case AutomationTab: return NSAccessibilityTabGroupRole;
        case AutomationTabItem: return NSAccessibilityRadioButtonRole;
        case AutomationText: return NSAccessibilityStaticTextRole;
        case AutomationToolBar: return NSAccessibilityToolbarRole;
        case AutomationToolTip: return NSAccessibilityPopoverRole;
        case AutomationTree: return NSAccessibilityOutlineRole;
        case AutomationTreeItem: return NSAccessibilityCellRole;
        case AutomationCustom: return NSAccessibilityUnknownRole;
        case AutomationGroup: return NSAccessibilityGroupRole;
        case AutomationThumb: return NSAccessibilityHandleRole;
        case AutomationDataGrid: return NSAccessibilityGridRole;
        case AutomationDataItem: return NSAccessibilityCellRole;
        case AutomationDocument: return NSAccessibilityStaticTextRole;
        case AutomationSplitButton: return NSAccessibilityPopUpButtonRole;
        case AutomationWindow: return NSAccessibilityWindowRole;
        case AutomationPane: return NSAccessibilityGroupRole;
        case AutomationHeader: return NSAccessibilityGroupRole;
        case AutomationHeaderItem:  return NSAccessibilityButtonRole;
        case AutomationTable: return NSAccessibilityTableRole;
        case AutomationTitleBar: return NSAccessibilityGroupRole;
        // Treat unknown roles as generic group container items. Returning
        // NSAccessibilityUnknownRole is also possible but makes the screen
       // reader focus on the item instead of passing focus to child items.
        default: return NSAccessibilityGroupRole;
    }
}

- (NSString *)accessibilityIdentifier
{
    return GetNSStringAndRelease(_peer->GetAutomationId());
}

- (NSString *)accessibilityTitle
{
    // StaticText exposes its text via the value property.
    if (_peer->GetAutomationControlType() != AutomationText)
    {
        return GetNSStringAndRelease(_peer->GetName());
    }
    
    return [super accessibilityTitle];
}

- (id)accessibilityValue
{
    if (_peer->IsRangeValueProvider())
    {
        return [NSNumber numberWithDouble:_peer->RangeValueProvider_GetValue()];
    }
    else if (_peer->IsToggleProvider())
    {
        switch (_peer->ToggleProvider_GetToggleState()) {
            case 0: return [NSNumber numberWithBool:NO];
            case 1: return [NSNumber numberWithBool:YES];
            default: return [NSNumber numberWithInt:2];
        }
    }
    else if (_peer->IsValueProvider())
    {
        return GetNSStringAndRelease(_peer->ValueProvider_GetValue());
    }
    else if (_peer->GetAutomationControlType() == AutomationText)
    {
        return GetNSStringAndRelease(_peer->GetName());
    }

    return [super accessibilityValue];
}

- (id)accessibilityMinValue
{
    if (_peer->IsRangeValueProvider())
    {
        return [NSNumber numberWithDouble:_peer->RangeValueProvider_GetMinimum()];
    }
    
    return [super accessibilityMinValue];
}

- (id)accessibilityMaxValue
{
    if (_peer->IsRangeValueProvider())
    {
        return [NSNumber numberWithDouble:_peer->RangeValueProvider_GetMaximum()];
    }
    
    return [super accessibilityMaxValue];
}

- (BOOL)isAccessibilityEnabled
{
    return _peer->IsEnabled();
}

- (BOOL)isAccessibilityFocused
{
    return _peer->HasKeyboardFocus();
}

- (NSArray *)accessibilityChildren
{
    if (_children == nullptr && _peer != nullptr)
        [self recalculateChildren];
    return _children;
}

- (NSRect)accessibilityFrame
{
    id topLevel = [self accessibilityTopLevelUIElement];
    auto result = NSZeroRect;

    if ([topLevel isKindOfClass:[AvnRootAccessibilityElement class]])
    {
        auto root = (AvnRootAccessibilityElement*)topLevel;
        auto view = [root ownerView];
        
        if (view)
        {
            auto window = [view window];
            auto bounds = ToNSRect(_peer->GetBoundingRectangle());
            auto windowBounds = [view convertRect:bounds toView:nil];
            auto screenBounds = [window convertRectToScreen:windowBounds];
            result = screenBounds;
        }
    }

    return result;
}

- (id)accessibilityParent
{
    auto parentPeer = _peer->GetParent();
    return parentPeer ? [AvnAccessibilityElement acquire:parentPeer] : [NSApplication sharedApplication];
}

- (id)accessibilityTopLevelUIElement
{
    auto rootPeer = _peer->GetRootPeer();
    return [AvnAccessibilityElement acquire:rootPeer];
}

- (id)accessibilityWindow
{
    auto rootPeer = _peer->GetVisualRoot();
    return [AvnAccessibilityElement acquire:rootPeer];
}

- (BOOL)isAccessibilityExpanded
{
    if (!_peer->IsExpandCollapseProvider())
        return NO;
    return _peer->ExpandCollapseProvider_GetIsExpanded();
}

- (void)setAccessibilityExpanded:(BOOL)accessibilityExpanded
{
    if (!_peer->IsExpandCollapseProvider())
        return;
    if (accessibilityExpanded)
        _peer->ExpandCollapseProvider_Expand();
    else
        _peer->ExpandCollapseProvider_Collapse();
}

- (BOOL)accessibilityPerformPress
{
    if (_peer->IsInvokeProvider())
    {
        _peer->InvokeProvider_Invoke();
    }
    else if (_peer->IsExpandCollapseProvider())
    {
        _peer->ExpandCollapseProvider_Expand();
    }
    else if (_peer->IsToggleProvider())
    {
        _peer->ToggleProvider_Toggle();
    }
    return YES;
}

- (BOOL)accessibilityPerformIncrement
{
    if (!_peer->IsRangeValueProvider())
        return NO;
    auto value = _peer->RangeValueProvider_GetValue();
    value += _peer->RangeValueProvider_GetSmallChange();
    _peer->RangeValueProvider_SetValue(value);
    return YES;
}

- (BOOL)accessibilityPerformDecrement
{
    if (!_peer->IsRangeValueProvider())
        return NO;
    auto value = _peer->RangeValueProvider_GetValue();
    value -= _peer->RangeValueProvider_GetSmallChange();
    _peer->RangeValueProvider_SetValue(value);
    return YES;
}

- (BOOL)accessibilityPerformShowMenu
{
    if (!_peer->IsExpandCollapseProvider())
        return NO;
    _peer->ExpandCollapseProvider_Expand();
    return YES;
}

- (BOOL)isAccessibilitySelected
{
    if (_peer->IsSelectionItemProvider())
        return _peer->SelectionItemProvider_IsSelected();
    return NO;
}

- (BOOL)isAccessibilitySelectorAllowed:(SEL)selector
{
    if (selector == @selector(accessibilityPerformShowMenu))
    {
        return _peer->IsExpandCollapseProvider() && _peer->ExpandCollapseProvider_GetShowsMenu();
    }
    else if (selector == @selector(isAccessibilityExpanded))
    {
        return _peer->IsExpandCollapseProvider();
    }
    else if (selector == @selector(accessibilityPerformPress))
    {
        return _peer->IsInvokeProvider() || _peer->IsExpandCollapseProvider() || _peer->IsToggleProvider();
    }
    else if (selector == @selector(accessibilityPerformIncrement) ||
             selector == @selector(accessibilityPerformDecrement) ||
             selector == @selector(accessibilityMinValue) ||
             selector == @selector(accessibilityMaxValue))
    {
        return _peer->IsRangeValueProvider();
    }
    
    return [super isAccessibilitySelectorAllowed:selector];
}

- (void)raiseChildrenChanged
{
    auto changed = _children ? [NSMutableSet setWithArray:_children] : [NSMutableSet set];

    [self recalculateChildren];
    
    if (_children)
        [changed addObjectsFromArray:_children];

    NSAccessibilityPostNotificationWithUserInfo(
        self,
        NSAccessibilityLayoutChangedNotification,
        @{ NSAccessibilityUIElementsKey: [changed allObjects]});
}

- (void)raisePropertyChanged
{
}

- (void)setAccessibilityFocused:(BOOL)accessibilityFocused
{
    if (accessibilityFocused)
        _peer->SetFocus();
}

- (void)recalculateChildren
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
                auto element = [AvnAccessibilityElement acquire:child];
                [_children addObject:element];
            }
        }
    }
    else
    {
        _children = nil;
    }
}

@end

@implementation AvnRootAccessibilityElement
{
    AvnView* _owner;
}

- (AvnRootAccessibilityElement *)initWithPeer:(IAvnAutomationPeer *)peer owner:(AvnView *)owner
{
    self = [super initWithPeer:peer];
    _owner = owner;

    // Seems we need to raise a focus changed notification here if we have focus
    auto focusedPeer = [self peer]->RootProvider_GetFocus();
    id focused = [AvnAccessibilityElement acquire:focusedPeer];

    if (focused)
        NSAccessibilityPostNotification(focused, NSAccessibilityFocusedUIElementChangedNotification);
    
    return self;
}

- (AvnView *)ownerView
{
    return _owner;
}

- (id)accessibilityFocusedUIElement
{
    auto focusedPeer = [self peer]->RootProvider_GetFocus();
    return [AvnAccessibilityElement acquire:focusedPeer];
}

- (id)accessibilityHitTest:(NSPoint)point
{
    auto clientPoint = [[_owner window] convertPointFromScreen:point];
    auto localPoint = [_owner translateLocalPoint:ToAvnPoint(clientPoint)];
    auto hit = [self peer]->RootProvider_GetPeerFromPoint(localPoint);
    return [AvnAccessibilityElement acquire:hit];
}

- (id)accessibilityParent
{
    return _owner;
}

- (void)raiseFocusChanged
{
    id focused = [self accessibilityFocusedUIElement];
    NSAccessibilityPostNotification(focused, NSAccessibilityFocusedUIElementChangedNotification);
}

// Although this method is marked as deprecated we get runtime warnings if we don't handle it.
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-implementations"
- (void)accessibilityPerformAction:(NSAccessibilityActionName)action
{
    [_owner accessibilityPerformAction:action];
}
#pragma clang diagnostic pop

@end
