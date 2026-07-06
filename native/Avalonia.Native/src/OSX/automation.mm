#include "common.h"
#include "automation.h"
#include "AvnAutomationNode.h"
#include "AvnString.h"
#include "INSWindowHolder.h"
#include "AvnView.h"
#include "WindowInterfaces.h"

@implementation AvnAccessibilityElement
{
    ComPtr<IAvnAutomationPeer> _peer;
    AvnAutomationNode* _node;
    NSMutableArray* _children;
    NSArray<NSString*>* _attributeNames;
}

+ (NSAccessibilityElement *)acquire:(IAvnAutomationPeer *)peer
{
    if (peer == nullptr)
        return nil;
    
    auto instance = peer->GetNode();
    
    if (instance != nullptr)
        return dynamic_cast<AvnAutomationNode*>(instance)->GetOwner();
    
    if (peer->IsInteropPeer())
    {
        auto view = (__bridge NSAccessibilityElement*)peer->InteropPeer_GetNativeControlHandle();
        return view;
    }
    else if (peer->IsRootProvider())
    {
        auto window = peer->RootProvider_GetWindow();
        
        if (window == nullptr)
        {
            NSLog(@"IRootProvider.PlatformImpl returned null or a non-WindowBaseImpl.");
            return nil;
        }
        
        auto holder = dynamic_cast<INSViewHolder*>(window);
        auto view = holder->GetNSView();
        return (NSAccessibilityElement*)[view window];
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
    _node = new AvnAutomationNode(self);
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
        _peer.getRaw()];
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
        case AutomationList: return NSAccessibilityListRole;
        case AutomationMenu: return NSAccessibilityMenuBarRole;
        case AutomationMenuBar: return NSAccessibilityMenuBarRole;
        case AutomationMenuItem: return NSAccessibilityMenuItemRole;
        case AutomationProgressBar: return NSAccessibilityProgressIndicatorRole;
        case AutomationRadioButton: return NSAccessibilityRadioButtonRole;
        case AutomationScrollBar: return NSAccessibilityScrollBarRole;
        case AutomationSlider: return NSAccessibilitySliderRole;
        case AutomationSpinner: return NSAccessibilityIncrementorRole;
        case AutomationStatusBar: return NSAccessibilityGroupRole;
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
        case AutomationScrollViewer: return NSAccessibilityScrollAreaRole;
        case AutomationHeader: return @"AXHeading";
        case AutomationHeaderItem:  return NSAccessibilityButtonRole;
        case AutomationTable: return NSAccessibilityTableRole;
        case AutomationTitleBar: return NSAccessibilityGroupRole;
        case AutomationExpander: return NSAccessibilityDisclosureTriangleRole;
        // Treat unknown roles as generic group container items. Returning
        // NSAccessibilityUnknownRole is also possible but makes the screen
        // reader focus on the item instead of passing focus to child items.
        default: return NSAccessibilityGroupRole;
    }
}

- (NSAccessibilitySubrole)accessibilitySubrole
{
    auto controlType = _peer->GetAutomationControlType();
    switch (controlType) {
        case AutomationList: return @"AXContentList";
        case AutomationListItem: return NSAccessibilityTableRowSubrole;
    }

    auto landmarkType = _peer->GetLandmarkType();
    switch (landmarkType) {
        case LandmarkBanner: return @"AXLandmarkBanner";
        case LandmarkComplementary: return @"AXLandmarkComplementary";
        case LandmarkContentInfo: return @"AXLandmarkContentInfo";
        case LandmarkRegion: return @"AXLandmarkRegion";
        case LandmarkForm: return @"AXLandmarkForm";
        case LandmarkMain: return @"AXLandmarkMain";
        case LandmarkNavigation: return @"AXLandmarkNavigation";
        case LandmarkSearch: return @"AXLandmarkSearch";
    }

    return NSAccessibilityUnknownSubrole;
}

- (NSString *)accessibilityRoleDescription
{
    auto landmarkType = _peer->GetLandmarkType();
    switch (landmarkType) {
        case LandmarkBanner: return @"banner";
        case LandmarkComplementary: return @"complementary";
        case LandmarkContentInfo: return @"content";
        case LandmarkRegion: return @"region";
        case LandmarkForm: return @"form";
        case LandmarkMain: return @"main";
        case LandmarkNavigation: return @"navigation";
        case LandmarkSearch: return @"search";
    }
    return NSAccessibilityRoleDescription([self accessibilityRole], [self accessibilitySubrole]);
}

// Note: Apple has deprecated this API, but it's still used to set attributes not supported by NSAccessibility
- (NSArray<NSString *> *)accessibilityAttributeNames
{
    if (_attributeNames == nil)
    {
        _attributeNames = @[
            @"AXARIALive", // kAXARIALiveAttribute
        ];
    }
    return _attributeNames;
}

- (id)accessibilityAttributeValue:(NSAccessibilityAttributeName)attribute
{
    if ([attribute isEqualToString:@"AXARIALive" /* kAXARIALiveAttribute */])
    {
        switch (_peer->GetLiveSetting())
        {
            case LiveSettingOff: return nil;
            case LiveSettingPolite: return @"polite";
            case LiveSettingAssertive: return @"assertive";
        }
        return nil;
    }
    return nil;
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

- (NSString *)accessibilityHelp
{
    return GetNSStringAndRelease(_peer->GetHelpText()); 
}

- (NSString *)accessibilityPlaceholderValue
{
    return GetNSStringAndRelease(_peer->GetPlaceholderText());
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
    else if (_peer->GetAutomationControlType() == AutomationHeader)
    {
        return [NSNumber numberWithInt:_peer->GetHeadingLevel()];
    }

    return [super accessibilityValue];
}

- (void)setAccessibilityValue:(id)newValue
{
    if (!_peer->IsEnabled())
        return;
    if (_peer->IsValueProvider() && !_peer->ValueProvider_IsReadOnly())
    {
        if (newValue == nil)
            _peer->ValueProvider_SetValue(nil);
        else if ([newValue isKindOfClass:[NSString class]])
            _peer->ValueProvider_SetValue([(NSString*)newValue UTF8String]);
    }
    else if (_peer->IsRangeValueProvider() && !_peer->RangeValueProvider_IsReadOnly())
    {
        if ([newValue isKindOfClass:[NSNumber class]])
            _peer->RangeValueProvider_SetValue([(NSNumber*)newValue doubleValue]);
    }
}

- (BOOL)accessibilityIsAttributeSettable:(NSAccessibilityAttributeName)attribute
{
    if ([attribute isEqualToString:NSAccessibilityValueAttribute])
    {
        if (_peer->IsValueProvider())
            return !_peer->ValueProvider_IsReadOnly();
        if (_peer->IsRangeValueProvider())
            return !_peer->RangeValueProvider_IsReadOnly();
        return NO;
    }

    return [super accessibilityIsAttributeSettable:attribute];
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
    auto bounds = _peer->GetBoundingRectangle();
    return [self rectToScreen:bounds];
}

- (id)accessibilityParent
{
    auto parentPeer = _peer->GetParent();

    // An ignored ancestor never appears in any children array, so report the
    // nearest unignored ancestor to keep the parent chain consistent with the
    // spliced children chain.
    while (parentPeer != nullptr && !parentPeer->IsRootProvider() && !parentPeer->IsControlElement())
        parentPeer = parentPeer->GetParent();

    if (parentPeer == nullptr)
        return [NSApplication sharedApplication];

    // When the parent is a root provider, return the AvnView (content view)
    // rather than the AvnWindow. macOS accessibility requires that the parent
    // chain is consistent with the children chain: AvnView exposes these
    // elements as its accessibilityChildren, so the elements must report
    // AvnView as their accessibilityParent.  A mismatch causes macOS to be
    // unable to resolve AXUIElementRefs back to the correct object, which
    // makes setter calls like AXUIElementSetAttributeValue silently land on
    // AvnView instead of the target AvnAccessibilityElement.
    if (parentPeer->IsRootProvider())
    {
        auto window = parentPeer->RootProvider_GetWindow();
        if (window != nullptr)
        {
            auto holder = dynamic_cast<INSViewHolder*>(window);
            if (holder != nullptr)
                return holder->GetNSView();
        }
    }

    return [AvnAccessibilityElement acquire:parentPeer];
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

- (id)accessibilityHorizontalScrollBar
{
    if (_peer == nullptr)
        return nil;
    return [AvnAccessibilityElement acquire:_peer->ScrollProvider_GetHorizontalScrollBar()];
}

- (id)accessibilityVerticalScrollBar
{
    if (_peer == nullptr)
        return nil;
    return [AvnAccessibilityElement acquire:_peer->ScrollProvider_GetVerticalScrollBar()];
}

- (BOOL)isAccessibilityExpanded
{
    if (!_peer->IsExpandCollapseProvider())
        return NO;
    return _peer->ExpandCollapseProvider_GetIsExpanded();
}

- (void)setAccessibilityExpanded:(BOOL)accessibilityExpanded
{
    if (!_peer->IsExpandCollapseProvider() || !_peer->IsEnabled())
        return;
    if (accessibilityExpanded)
        _peer->ExpandCollapseProvider_Expand();
    else
        _peer->ExpandCollapseProvider_Collapse();
}

- (BOOL)accessibilityPerformPress
{
    if (!_peer->IsEnabled())
        return NO;
    if (_peer->IsInvokeProvider())
    {
        _peer->InvokeProvider_Invoke();
    }
    else if (_peer->IsExpandCollapseProvider())
    {
        if (_peer->ExpandCollapseProvider_GetIsExpanded())
            _peer->ExpandCollapseProvider_Collapse();
        else
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
    if (!_peer->IsRangeValueProvider() || _peer->RangeValueProvider_IsReadOnly() || !_peer->IsEnabled())
        return NO;
    auto value = _peer->RangeValueProvider_GetValue();
    value += _peer->RangeValueProvider_GetSmallChange();
    _peer->RangeValueProvider_SetValue(value);
    return YES;
}

- (BOOL)accessibilityPerformDecrement
{
    if (!_peer->IsRangeValueProvider() || _peer->RangeValueProvider_IsReadOnly() || !_peer->IsEnabled())
        return NO;
    auto value = _peer->RangeValueProvider_GetValue();
    value -= _peer->RangeValueProvider_GetSmallChange();
    _peer->RangeValueProvider_SetValue(value);
    return YES;
}

- (BOOL)accessibilityPerformShowMenu
{
    if (!_peer->IsExpandCollapseProvider() || !_peer->IsEnabled())
        return NO;
    _peer->ExpandCollapseProvider_Expand();
    return YES;
}

- (NSArray<NSAccessibilityActionName> *)accessibilityActionNames
{
    NSAccessibilityActionName scrollToVisible = @"AXScrollToVisible";
    NSArray<NSAccessibilityActionName> *base = [super accessibilityActionNames];
    if (base == nil)
        base = @[];
    if ([base containsObject:scrollToVisible])
        return base;
    return [base arrayByAddingObject:scrollToVisible];
}

- (void)accessibilityPerformAction:(NSAccessibilityActionName)action
{
    if ([action isEqualToString:@"AXScrollToVisible"])
        _peer->BringIntoView();
    else
        [super accessibilityPerformAction:action];
}

- (BOOL)isAccessibilitySelected
{
    if (_peer->IsSelectionItemProvider())
        return _peer->SelectionItemProvider_IsSelected();
    return NO;
}

- (void)setAccessibilitySelected:(BOOL)accessibilitySelected
{
    if (!_peer->IsSelectionItemProvider() || !_peer->IsEnabled())
        return;
    if (accessibilitySelected)
        _peer->SelectionItemProvider_AddToSelection();
    else
        _peer->SelectionItemProvider_RemoveFromSelection();
}

- (BOOL)isAccessibilitySelectorAllowed:(SEL)selector
{
    if (selector == @selector(setAccessibilityValue:))
    {
        return (_peer->IsValueProvider() && !_peer->ValueProvider_IsReadOnly()) ||
               (_peer->IsRangeValueProvider() && !_peer->RangeValueProvider_IsReadOnly());
    }
    else if (selector == @selector(accessibilityPerformShowMenu))
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
    else if (selector == @selector(setAccessibilitySelected:))
    {
        return _peer->IsSelectionItemProvider();
    }
    else if (selector == @selector(accessibilityPerformIncrement) ||
             selector == @selector(accessibilityPerformDecrement))
    {
        return _peer->IsRangeValueProvider() && !_peer->RangeValueProvider_IsReadOnly();
    }
    else if (selector == @selector(accessibilityMinValue) ||
             selector == @selector(accessibilityMaxValue))
    {
        return _peer->IsRangeValueProvider();
    }

    return [super isAccessibilitySelectorAllowed:selector];
}

- (NSRect)rectToScreen:(AvnRect)rect
{
    id topLevel = [self accessibilityTopLevelUIElement];

    if (![topLevel isKindOfClass:[AvnWindow class]])
        return NSZeroRect;

    auto window = (AvnWindow*)topLevel;
    auto view = [window view];

    if (view == nil)
        return NSZeroRect;

    auto nsRect = ToNSRect(rect);
    auto windowRect = [view convertRect:nsRect toView:nil];
    return [window convertRectToScreen:windowRect];
}

- (void)raiseChildrenChanged
{
    auto changed = _children ? [NSMutableSet setWithArray:_children] : [NSMutableSet set];

    [self recalculateChildren];
    
    if (_children)
        [changed addObjectsFromArray:_children];

	/*
	For future reference, upon testing with a sample SwiftUI app:

    1) Containers vanish. VStack/HStack don't appear in the accessibility tree at all, 
      only real elements (Text, Button, List) do, parented to one root AXHostingView.

    2) Changes post on the nearest real element, not the container. 
       Toggling a child two VStack/HStack levels deep fired AXLayoutChanged on the AXHostingView 
       (the nearest real element), never on the hidden containers.

    3) Real controls get their own notification. The List (AXOutline) posted AXRowCountChanged on itself.

	Apple never posts a structural notification on a non-accessibility container.
    It targets the nearest element that's actually in the tree.
    That's exactly what the code below does (templated parent, else walk to the nearest exposed ancestor).
	*/

    id target = [AvnAccessibilityElement acquire:_peer->GetTemplatedParent()];
    if (target == nil)
        target = self;
    while ([target isKindOfClass:[AvnAccessibilityElement class]] && ![(AvnAccessibilityElement*)target isAccessibilityElement])
        target = [(AvnAccessibilityElement*)target accessibilityParent];

    NSAccessibilityPostNotificationWithUserInfo(
        target,
        NSAccessibilityLayoutChangedNotification,
        @{ NSAccessibilityUIElementsKey: [changed allObjects]});
}

- (void)raisePropertyChanged:(AvnAutomationProperty)property
{
    switch (property)
    {
        case AutomationPeer_AutomationId:
            // accessibilityIdentifier is read on-demand; no VoiceOver announcement required.
            break;
        case AutomationPeer_Name:
            NSAccessibilityPostNotification(self, NSAccessibilityTitleChangedNotification);
            if (_peer->GetLiveSetting() != LiveSettingOff)
                [self raiseLiveRegionChanged];
            break;
        case ValueProvider_Value:
        case RangeValueProvider_Value:
            NSAccessibilityPostNotification(self, NSAccessibilityValueChangedNotification);
            break;
        case AutomationPeer_BoundingRectangle:
            NSAccessibilityPostNotification(self, NSAccessibilityMovedNotification);
            NSAccessibilityPostNotification(self, NSAccessibilityResizedNotification);
            break;
        case SelectionItemProvider_IsSelected:
        case SelectionProvider_Selection:
            NSAccessibilityPostNotification(self, NSAccessibilitySelectedChildrenChangedNotification);
            break;
        case ToggleProvider_ToggleState:
            NSAccessibilityPostNotification(self, NSAccessibilityValueChangedNotification);
            break;
        case ExpandCollapseProvider_ExpandCollapseState:
            NSAccessibilityPostNotification(self, NSAccessibilityValueChangedNotification);
            if (_peer->ExpandCollapseProvider_GetIsExpanded())
                NSAccessibilityPostNotification(self, (__bridge NSString *)kAXRowExpandedNotification);
            else
                NSAccessibilityPostNotification(self, (__bridge NSString *)kAXRowCollapsedNotification);
            break;
        default:
            break;
    }
}

- (void)raiseLiveRegionChanged
{
    NSAccessibilityPostNotification(self, @"AXLiveRegionChanged" /* kAXLiveRegionChangedNotification */);

    // Announce the new string
    auto name = _peer->GetName();
    if (name != nullptr)
    {
        NSAccessibilityPriorityLevel priority = NSAccessibilityPriorityLow;
        switch (_peer->GetLiveSetting())
        {
            case LiveSettingOff:
                return;
            case LiveSettingPolite:
                priority = NSAccessibilityPriorityMedium;
                break;
            case LiveSettingAssertive:
                priority = NSAccessibilityPriorityHigh;
                break;
        }

        NSDictionary <NSString *, id> *userInfo = @{
            NSAccessibilityAnnouncementKey: GetNSStringAndRelease(name),
            NSAccessibilityPriorityKey: @(priority)
        };

        id topLevel = [self accessibilityTopLevelUIElement];
        if ([topLevel isKindOfClass:[AvnWindow class]])
        {
            NSAccessibilityPostNotificationWithUserInfo(topLevel, NSAccessibilityAnnouncementRequestedNotification, userInfo);
        }
    }
}

- (void)setAccessibilityFocused:(BOOL)accessibilityFocused
{
    if (accessibilityFocused)
        _peer->SetFocus();
}

// An element reporting isAccessibilityElement == NO declares itself ignored,
// but AppKit only applies the ignored-element splice to NSView hierarchies;
// custom accessibility elements must vend unignored children themselves
// (the NSAccessibilityUnignoredChildren convention).
+ (void)appendUnignoredChildrenOf:(IAvnAutomationPeer *)peer to:(NSMutableArray *)array
{
    auto childPeers = peer->GetChildren();
    auto childCount = childPeers != nullptr ? childPeers->GetCount() : 0;

    for (int i = 0; i < childCount; ++i)
    {
        IAvnAutomationPeer* child;

        if (childPeers->Get(i, &child) != S_OK)
            continue;

        if (child->IsControlElement())
        {
            id element = [AvnAccessibilityElement acquire:child];
            if (element != nil)
                [array addObject:element];
        }
        else
        {
            [AvnAccessibilityElement appendUnignoredChildrenOf:child to:array];
        }
    }
}

- (void)recalculateChildren
{
    auto children = [NSMutableArray new];
    [AvnAccessibilityElement appendUnignoredChildrenOf:_peer.getRaw() to:children];
    _children = children.count > 0 ? children : nil;
}

@end
