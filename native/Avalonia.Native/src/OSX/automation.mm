#include "common.h"
#include "automation.h"
#include "AvnString.h"
#include "INSWindowHolder.h"
#include "AvnView.h"
#include "WindowInterfaces.h"

@implementation AvnAccessibilityElement
{
    IAvnAutomationPeer* _peer;
    AutomationNode* _node;
    NSMutableArray* _children;
}

+ (id)acquire:(IAvnAutomationPeer *)peer
{
    if (peer == nullptr)
        return nil;
    
    auto instance = peer->GetNode();
    
    if (instance != nullptr)
        return dynamic_cast<AutomationNode*>(instance)->GetOwner();
    
    if (peer->IsRootProvider())
    {
        auto window = peer->RootProvider_GetWindow();
        auto holder = dynamic_cast<INSWindowHolder*>(window);
        auto view = holder->GetNSView();
        return [view window];
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
    else if (_peer->IsTextProvider())
    {
        // Only return the first 100 characters as text can be long.
        return GetNSStringAndRelease(_peer->TextProvider_GetText(0, 100));
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
    auto bounds = _peer->GetBoundingRectangle();
    return [self rectToScreen:bounds];
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
    id topLevel = [self accessibilityTopLevelUIElement];
    return [topLevel isKindOfClass:[NSWindow class]] ? topLevel : nil;
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

- (NSRect)accessibilityFrameForRange:(NSRange)range
{
    if (_peer->IsTextProvider())
    {
        auto bounds = _peer->TextProvider_GetBounds((int)range.location, (int)range.length);
        return [self rectToScreen:bounds];
    }
    
    return [super accessibilityFrameForRange:range];
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

- (NSInteger)accessibilityInsertionPointLineNumber
{
    if (_peer->IsTextProvider())
        return _peer->TextProvider_GetCaretLineNumber();
    return [super accessibilityNumberOfCharacters];
}

- (NSInteger)accessibilityLineForIndex:(NSInteger)index
{
    if (_peer->IsTextProvider())
        return _peer->TextProvider_GetLineForIndex((int)index);
    return [super accessibilityLineForIndex:index];
}

- (NSInteger)accessibilityNumberOfCharacters
{
    if (_peer->IsTextProvider())
        return _peer->TextProvider_GetTextLength();
    return [super accessibilityNumberOfCharacters];
}

- (NSString *)accessibilityPlaceholderValue
{
    if (_peer->IsTextProvider())
        return GetNSStringAndRelease(_peer->TextProvider_GetPlaceholderText());
    return [super accessibilityPlaceholderValue];
}

- (NSRange)accessibilityRangeForLine:(NSInteger)line
{
    if (_peer->IsTextProvider())
    {
        int start;
        int length;
        _peer->TextProvider_GetLineRange((int)line, &start, &length);
        return NSMakeRange(start, length);
    }
    return [super accessibilityRangeForLine:line];
}

- (NSString *)accessibilitySelectedText
{
    if (_peer->IsTextProvider())
        return GetNSStringAndRelease(_peer->TextProvider_GetSelectedText());
    return [super accessibilitySelectedText];
}

- (NSRange)accessibilitySelectedTextRange
{
    if (_peer->IsTextProvider())
    {
        int start;
        int length;
        _peer->TextProvider_GetSelection(&start, &length);
        return NSMakeRange(start, length);
    }
    
    return [super accessibilitySelectedTextRange];
}

- (void)setAccessibilitySelectedTextRange:(NSRange)accessibilitySelectedTextRange
{
    if (_peer->IsTextProvider())
        _peer->TextProvider_Select((int)accessibilitySelectedTextRange.location, (int)accessibilitySelectedTextRange.length);
    return [super setAccessibilitySelectedTextRange:accessibilitySelectedTextRange];
}

- (NSString *)accessibilityStringForRange:(NSRange)range
{
    if (_peer->IsTextProvider())
        return GetNSStringAndRelease(_peer->TextProvider_GetText((int)range.location, (int)range.length));
    return [super accessibilitySelectedText];
}

- (NSRange)accessibilityVisibleCharacterRange
{
    if (_peer->IsTextProvider())
    {
        auto length = _peer->TextProvider_GetTextLength();
        return NSMakeRange(0, length);
    }
    
    return [super accessibilityVisibleCharacterRange];
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

- (void)raisePropertyChanged:(AvnAutomationProperty)property
{
    switch (property)
    {
        case TextProvider_SelectionChanged:
            NSAccessibilityPostNotification(self, NSAccessibilitySelectedTextChangedNotification);
            break;
        default:
            // Nothing to do.
            break;
    }
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

