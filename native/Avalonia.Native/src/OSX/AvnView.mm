//
// Created by Dan Walmsley on 05/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <AppKit/AppKit.h>
#include "AvnView.h"
#include "automation.h"
#import "WindowInterfaces.h"

@implementation AvnView
{
    ComPtr<WindowBaseImpl> _parent;
    NSTrackingArea* _area;
    bool _isLeftPressed, _isMiddlePressed, _isRightPressed, _isXButton1Pressed, _isXButton2Pressed;
    AvnInputModifiers _modifierState;
    NSEvent* _lastMouseDownEvent;
    AvnPixelSize _lastPixelSize;
    NSObject<IRenderTarget>* _currentRenderTarget;
    AvnPlatformResizeReason _resizeReason;
    AvnAccessibilityElement* _accessibilityChild;
    NSRect _cursorRect;
    NSMutableAttributedString* _text;
    NSRange _selectedRange;
    NSRange _markedRange;
    NSEvent* _lastKeyDownEvent;
}

- (void)onClosed
{
    @synchronized (self)
    {
        _parent = nullptr;
    }
}

- (NSEvent*) lastMouseDownEvent
{
    return _lastMouseDownEvent;
}

- (void) updateRenderTarget
{
    if(_currentRenderTarget) {
        [_currentRenderTarget resize:_lastPixelSize withScale:static_cast<float>([[self window] backingScaleFactor])];
        [self setNeedsDisplayInRect:[self frame]];
    }
}


-(void) setRenderTarget:(NSObject<IRenderTarget>*)target
{
    if([self layer])
    {
        [self layer].delegate = nil;
    }
    _currentRenderTarget = target;
    auto layer = [target layer];
    [self setLayer: layer];
    [layer setDelegate: self];
    layer.needsDisplayOnBoundsChange = YES;
    [self updateRenderTarget];
}

-(void)displayLayer: (CALayer*)layer
{
    [self updateLayer];
}

-(AvnView*)  initWithParent: (WindowBaseImpl*) parent
{
    self = [super init];
    [self setWantsLayer:YES];
    [self setLayerContentsPlacement: NSViewLayerContentsPlacementTopLeft];

    [self setCanDrawSubviewsIntoLayer: NO];
    [self setLayerContentsRedrawPolicy: NSViewLayerContentsRedrawDuringViewResize];

    _parent = parent;
    _area = nullptr;
    _lastPixelSize.Height = 100;
    _lastPixelSize.Width = 100;
    [self registerForDraggedTypes: @[@"public.data", GetAvnCustomDataType()]];

    _modifierState = AvnInputModifiersNone;
    
    _text = [[NSMutableAttributedString alloc] initWithString:@""];
    _markedRange = NSMakeRange(0, 0);
    _selectedRange = NSMakeRange(0, 0);
    
    return self;
}

- (BOOL)isFlipped
{
    return YES;
}

- (BOOL)wantsUpdateLayer
{
    return YES;
}

- (BOOL)isOpaque
{
    return YES;
}

- (BOOL)acceptsFirstResponder
{
    return true;
}

- (BOOL)acceptsFirstMouse:(NSEvent *)event
{
    return true;
}

- (BOOL)canBecomeKeyView
{
    return true;
}

-(void)setFrameSize:(NSSize)newSize
{
    [super setFrameSize:newSize];

    if(_area != nullptr)
    {
        [self removeTrackingArea:_area];
        _area = nullptr;
    }

    if (_parent == nullptr)
    {
        return;
    }

    NSRect rect = NSZeroRect;
    rect.size = newSize;

    NSTrackingAreaOptions options = NSTrackingActiveAlways | NSTrackingMouseMoved | NSTrackingMouseEnteredAndExited | NSTrackingEnabledDuringMouseDrag;
    _area = [[NSTrackingArea alloc] initWithRect:rect options:options owner:self userInfo:nullptr];
    [self addTrackingArea:_area];

    _parent->UpdateCursor();

    auto fsize = [self convertSizeToBacking: [self frame].size];

    if(_lastPixelSize.Width != (int)fsize.width || _lastPixelSize.Height != (int)fsize.height)
    {
        _lastPixelSize.Width = (int)fsize.width;
        _lastPixelSize.Height = (int)fsize.height;
        [self updateRenderTarget];

        auto reason = [self inLiveResize] ? ResizeUser : _resizeReason;

        _parent->BaseEvents->Resized(AvnSize{newSize.width, newSize.height}, reason);
    }
}

- (void)updateLayer
{
    AvnInsidePotentialDeadlock deadlock;
    if (_parent == nullptr)
    {
        return;
    }

    _parent->BaseEvents->RunRenderPriorityJobs();

    if (_parent == nullptr)
    {
        return;
    }

    _parent->BaseEvents->Paint();
}

- (void)drawRect:(NSRect)dirtyRect
{
    return;
}

- (AvnPoint) translateLocalPoint:(AvnPoint)pt
{
    pt.Y = [self bounds].size.height - pt.Y;
    return pt;
}

+ (AvnPoint)toAvnPoint:(CGPoint)p
{
    AvnPoint result;

    result.X = p.x;
    result.Y = p.y;

    return result;
}

- (void) viewDidChangeBackingProperties
{
    auto fsize = [self convertSizeToBacking: [self frame].size];
    _lastPixelSize.Width = (int)fsize.width;
    _lastPixelSize.Height = (int)fsize.height;
    [self updateRenderTarget];

    if(_parent != nullptr)
    {
        _parent->BaseEvents->ScalingChanged([_parent->Window backingScaleFactor]);
    }

    [super viewDidChangeBackingProperties];
}

- (bool) ignoreUserInput:(bool)trigerInputWhenDisabled
{
    if(_parent == nullptr)
    {
        return TRUE;
    }

    auto parentWindow = _parent->GetWindowProtocol();

    if(parentWindow == nil || ![parentWindow shouldTryToHandleEvents])
    {
        if(trigerInputWhenDisabled)
        {
            auto window = dynamic_cast<WindowImpl*>(_parent.getRaw());

            if(window != nullptr)
            {
                window->WindowEvents->GotInputWhenDisabled();
            }
        }

        return TRUE;
    }

    return FALSE;
}

- (void)mouseEvent:(NSEvent *)event withType:(AvnRawMouseEventType) type
{
    bool triggerInputWhenDisabled = type != Move && type != LeaveWindow;

    if([self ignoreUserInput: triggerInputWhenDisabled])
    {
        return;
    }

    auto localPoint = [self convertPoint:[event locationInWindow] toView:self];
    auto avnPoint = [AvnView toAvnPoint:localPoint];
    auto point = [self translateLocalPoint:avnPoint];
    AvnVector delta = { 0, 0};

    if(type == Wheel)
    {
        auto speed = 5;

        if([event hasPreciseScrollingDeltas])
        {
            speed = 50;
        }

        delta.X = [event scrollingDeltaX] / speed;
        delta.Y = [event scrollingDeltaY] / speed;

        if(delta.X == 0 && delta.Y == 0)
        {
            return;
        }
    }
    else if (type == Magnify)
    {
        delta.X = delta.Y = [event magnification];
    }
    else if (type == Rotate)
    {
        delta.X = delta.Y = [event rotation];
    }
    else if (type == Swipe)
    {
        delta.X = [event deltaX];
        delta.Y = [event deltaY];
    }

    uint64_t timestamp = static_cast<uint64_t>([event timestamp] * 1000);
    auto modifiers = [self getModifiers:[event modifierFlags]];

    if(type != Move ||
            (
                    [self window] != nil &&
                            (
                                    [[self window] firstResponder] == nil
                                            || ![[[self window] firstResponder] isKindOfClass: [NSView class]]
                            )
            )
            )
        [self becomeFirstResponder];

    if(_parent != nullptr)
    {
        _parent->BaseEvents->RawMouseEvent(type, timestamp, modifiers, point, delta);
    }

    [super mouseMoved:event];
}

- (BOOL) resignFirstResponder
{
    _parent->BaseEvents->LostFocus();
    return YES;
}

- (void)mouseMoved:(NSEvent *)event
{
    [self mouseEvent:event withType:Move];
}

- (void)mouseDown:(NSEvent *)event
{
    _isLeftPressed = true;
    _lastMouseDownEvent = event;
    [self mouseEvent:event withType:LeftButtonDown];
}

- (void)otherMouseDown:(NSEvent *)event
{
    _lastMouseDownEvent = event;

    switch(event.buttonNumber)
    {
        case 2:
        case 3:
            _isMiddlePressed = true;
            [self mouseEvent:event withType:MiddleButtonDown];
            break;
        case 4:
            _isXButton1Pressed = true;
            [self mouseEvent:event withType:XButton1Down];
            break;
        case 5:
            _isXButton2Pressed = true;
            [self mouseEvent:event withType:XButton2Down];
            break;

        default:
            break;
    }
}

- (void)rightMouseDown:(NSEvent *)event
{
    _isRightPressed = true;
    _lastMouseDownEvent = event;
    [self mouseEvent:event withType:RightButtonDown];
}

- (void)mouseUp:(NSEvent *)event
{
    _isLeftPressed = false;
    [self mouseEvent:event withType:LeftButtonUp];
}

- (void)otherMouseUp:(NSEvent *)event
{
    switch(event.buttonNumber)
    {
        case 2:
        case 3:
            _isMiddlePressed = false;
            [self mouseEvent:event withType:MiddleButtonUp];
            break;
        case 4:
            _isXButton1Pressed = false;
            [self mouseEvent:event withType:XButton1Up];
            break;
        case 5:
            _isXButton2Pressed = false;
            [self mouseEvent:event withType:XButton2Up];
            break;

        default:
            break;
    }
}

- (void)rightMouseUp:(NSEvent *)event
{
    _isRightPressed = false;
    [self mouseEvent:event withType:RightButtonUp];
}

- (void)mouseDragged:(NSEvent *)event
{
    [self mouseEvent:event withType:Move];
    [super mouseDragged:event];
}

- (void)otherMouseDragged:(NSEvent *)event
{
    [self mouseEvent:event withType:Move];
    [super otherMouseDragged:event];
}

- (void)rightMouseDragged:(NSEvent *)event
{
    [self mouseEvent:event withType:Move];
    [super rightMouseDragged:event];
}

- (void)scrollWheel:(NSEvent *)event
{
    [self mouseEvent:event withType:Wheel];
    [super scrollWheel:event];
}

- (void)magnifyWithEvent:(NSEvent *)event
{
    [self mouseEvent:event withType:Magnify];
    [super magnifyWithEvent:event];
}

- (void)rotateWithEvent:(NSEvent *)event
{
    [self mouseEvent:event withType:Rotate];
    [super rotateWithEvent:event];
}

- (void)swipeWithEvent:(NSEvent *)event
{
    [self mouseEvent:event withType:Swipe];
    [super swipeWithEvent:event];
}

- (void)mouseEntered:(NSEvent *)event
{
    [super mouseEntered:event];
}

- (void)mouseExited:(NSEvent *)event
{
    [self mouseEvent:event withType:LeaveWindow];
    [super mouseExited:event];
}

- (void) keyboardEvent: (NSEvent *) event withType: (AvnRawKeyEventType)type
{
    if([self ignoreUserInput: false] || _parent == nullptr)
    {
        return;
    }

    auto scanCode = [event keyCode];
    auto key = VirtualKeyFromScanCode(scanCode, [event modifierFlags]);
    auto physicalKey = PhysicalKeyFromScanCode(scanCode);
    auto keySymbol = KeySymbolFromScanCode(scanCode, [event modifierFlags]);
    auto keySymbolUtf8 = keySymbol == nullptr ? nullptr : [keySymbol UTF8String];
    
    auto timestamp = static_cast<uint64_t>([event timestamp] * 1000);
    auto modifiers = [self getModifiers:[event modifierFlags]];

    _parent->BaseEvents->RawKeyEvent(type, timestamp, modifiers, key, physicalKey, keySymbolUtf8);
}

- (void)flagsChanged:(NSEvent *)event
{
    auto newModifierState = [self getModifiers:[event modifierFlags]];

    bool isAltCurrentlyPressed = (_modifierState & Alt) == Alt;
    bool isControlCurrentlyPressed = (_modifierState & Control) == Control;
    bool isShiftCurrentlyPressed = (_modifierState & Shift) == Shift;
    bool isCommandCurrentlyPressed = (_modifierState & Windows) == Windows;

    bool isAltPressed = (newModifierState & Alt) == Alt;
    bool isControlPressed = (newModifierState & Control) == Control;
    bool isShiftPressed = (newModifierState & Shift) == Shift;
    bool isCommandPressed = (newModifierState & Windows) == Windows;

    if (isAltPressed && !isAltCurrentlyPressed)
    {
        [self keyboardEvent:event withType:KeyDown];
    }
    else if (isAltCurrentlyPressed && !isAltPressed)
    {
        [self keyboardEvent:event withType:KeyUp];
    }

    if (isControlPressed && !isControlCurrentlyPressed)
    {
        [self keyboardEvent:event withType:KeyDown];
    }
    else if (isControlCurrentlyPressed && !isControlPressed)
    {
        [self keyboardEvent:event withType:KeyUp];
    }

    if (isShiftPressed && !isShiftCurrentlyPressed)
    {
        [self keyboardEvent:event withType:KeyDown];
    }
    else if(isShiftCurrentlyPressed && !isShiftPressed)
    {
        [self keyboardEvent:event withType:KeyUp];
    }

    if(isCommandPressed && !isCommandCurrentlyPressed)
    {
        [self keyboardEvent:event withType:KeyDown];
    }
    else if(isCommandCurrentlyPressed && ! isCommandPressed)
    {
        [self keyboardEvent:event withType:KeyUp];
    }

    _modifierState = newModifierState;

    [[self inputContext] handleEvent:event];
    [super flagsChanged:event];
}

- (bool) handleKeyDown: (NSTimeInterval) timestamp withKey:(AvnKey)key withPhysicalKey:(AvnPhysicalKey)physicalKey withModifiers:(AvnInputModifiers)modifiers withKeySymbol:(NSString*)keySymbol {
    return _parent->BaseEvents->RawKeyEvent(KeyDown, timestamp, modifiers, key, physicalKey, [keySymbol UTF8String]);
}

- (void)keyDown:(NSEvent *)event
{
    if([self ignoreUserInput: false] || _parent == nullptr)
    {
        return;
    }
    
    _lastKeyDownEvent = event;
    
    auto timestamp = static_cast<uint64_t>([event timestamp] * 1000);
    
    auto scanCode = [event keyCode];
    auto key = VirtualKeyFromScanCode(scanCode, [event modifierFlags]);
    auto physicalKey = PhysicalKeyFromScanCode(scanCode);
    auto keySymbol = KeySymbolFromScanCode(scanCode, [event modifierFlags]);
    
    auto modifiers = [self getModifiers:[event modifierFlags]];
    
    //InputMethod is active
    if(_parent->InputMethod->IsActive()){
        auto hasInputModifier = modifiers != AvnInputModifiersNone;
        
        //Handle keyDown first if an input modifier is present
        if(hasInputModifier){
            if([self handleKeyDown:timestamp withKey:key withPhysicalKey:physicalKey withModifiers:modifiers withKeySymbol:keySymbol]){
                //User code has handled the event
                _lastKeyDownEvent = nullptr;
                
                return;
            }
        }
        
        if([[self inputContext] handleEvent:event] == NO){
            //KeyDown has not been consumed by the input context
                
            //Only raise a keyDown if we don't have a modifier
            if(!hasInputModifier){
                [self handleKeyDown:timestamp withKey:key withPhysicalKey:physicalKey withModifiers:modifiers withKeySymbol:keySymbol];
            }
        }
        
    }
    //InputMethod not active
    else{
        auto keyDownHandled = [self handleKeyDown:timestamp withKey:key withPhysicalKey:physicalKey withModifiers:modifiers withKeySymbol:keySymbol];
            
        //Raise text input event for unhandled key down
        if(!keyDownHandled){
            if(keySymbol != nullptr && key != AvnKeyEnter){
                auto timestamp = static_cast<uint64_t>([event timestamp] * 1000);
                
                _parent->BaseEvents->RawTextInputEvent(timestamp, [keySymbol UTF8String]);
            }
        }
    }
    
    _lastKeyDownEvent = nullptr;
}

- (void)keyUp:(NSEvent *)event
{
    [self keyboardEvent:event withType:KeyUp];
    [super keyUp:event];
}

- (void) doCommandBySelector:(SEL)selector{
    if(_lastKeyDownEvent != nullptr){
        [self keyboardEvent:_lastKeyDownEvent withType:KeyDown];
    }
}

- (AvnInputModifiers)getModifiers:(NSEventModifierFlags)mod
{
    unsigned int rv = 0;

    if (mod & NSEventModifierFlagControl)
        rv |= Control;
    if (mod & NSEventModifierFlagShift)
        rv |= Shift;
    if (mod & NSEventModifierFlagOption)
        rv |= Alt;
    if (mod & NSEventModifierFlagCommand)
        rv |= Windows;

    if (_isLeftPressed)
        rv |= LeftMouseButton;
    if (_isMiddlePressed)
        rv |= MiddleMouseButton;
    if (_isRightPressed)
        rv |= RightMouseButton;
    if (_isXButton1Pressed)
        rv |= XButton1MouseButton;
    if (_isXButton2Pressed)
        rv |= XButton2MouseButton;

    return (AvnInputModifiers)rv;
}

- (BOOL)hasMarkedText
{
    return _markedRange.length > 0;
}

- (NSRange)markedRange
{
    return _markedRange;
}

- (NSRange)selectedRange
{
    return _selectedRange;
}

- (void)setMarkedText:(id)string selectedRange:(NSRange)selectedRange replacementRange:(NSRange)replacementRange
{
    NSString* markedText;
        
    if([string isKindOfClass:[NSAttributedString class]])
    {
        markedText = [string string];
    }
    else
    {
        markedText = (NSString*) string;
    }
    
    _markedRange = NSMakeRange(_selectedRange.location, [markedText length]);
        
    if(_parent->InputMethod->IsActive()){
        _parent->InputMethod->Client->SetPreeditText((char*)[markedText UTF8String]);
    }
}

- (void)unmarkText
{
    if(_parent->InputMethod->IsActive()){
        _parent->InputMethod->Client->SetPreeditText(nullptr);
    }
    
    _markedRange = NSMakeRange(_selectedRange.location, 0);
    
    if([self inputContext]) {
        [[self inputContext] discardMarkedText];
    }
}

- (NSArray<NSString *> *)validAttributesForMarkedText
{
    return [NSArray new];
}

- (NSAttributedString *)attributedSubstringForProposedRange:(NSRange)range actualRange:(NSRangePointer)actualRange
{
    if(actualRange){
        range = *actualRange;
    }
    
    NSAttributedString* subString = [_text attributedSubstringFromRange:range];
    
    return subString;
}

- (void)insertText:(id)string replacementRange:(NSRange)replacementRange
{
    if(_parent == nullptr){
        return;
    }
    
    NSString* text;
        
    if([string isKindOfClass:[NSAttributedString class]])
    {
        text = [string string];
    }
    else
    {
        text = (NSString*) string;
    }
    
    [self unmarkText];
        
    uint64_t timestamp = static_cast<uint64_t>([NSDate timeIntervalSinceReferenceDate] * 1000);
        
    _parent->BaseEvents->RawTextInputEvent(timestamp, [text UTF8String]);
}

- (NSUInteger)characterIndexForPoint:(NSPoint)point
{
    return NSNotFound;
}

- (NSRect)firstRectForCharacterRange:(NSRange)range actualRange:(NSRangePointer)actualRange
{
    if(!_parent->InputMethod->IsActive()){
        return NSZeroRect;
    }
    
    return _cursorRect;
}

- (NSDragOperation)triggerAvnDragEvent: (AvnDragEventType) type info: (id <NSDraggingInfo>)info
{
    auto localPoint = [self convertPoint:[info draggingLocation] toView:self];
    auto avnPoint = [AvnView toAvnPoint:localPoint];
    auto point = [self translateLocalPoint:avnPoint];
    auto modifiers = [self getModifiers:[[NSApp currentEvent] modifierFlags]];
    NSDragOperation nsop = [info draggingSourceOperationMask];

    auto effects = ConvertDragDropEffects(nsop);
    int reffects = (int)_parent->BaseEvents
            ->DragEvent(type, point, modifiers, effects,
                    CreateClipboard([info draggingPasteboard], nil),
                    GetAvnDataObjectHandleFromDraggingInfo(info));

    NSDragOperation ret = static_cast<NSDragOperation>(0);

    // Ensure that the managed part didn't add any new effects
    reffects = (int)effects & reffects;

    // OSX requires exactly one operation
    if((reffects & (int)AvnDragDropEffects::Copy) != 0)
        ret = NSDragOperationCopy;
    else if((reffects & (int)AvnDragDropEffects::Move) != 0)
        ret = NSDragOperationMove;
    else if((reffects & (int)AvnDragDropEffects::Link) != 0)
        ret = NSDragOperationLink;
    if(ret == 0)
        ret = NSDragOperationNone;
    return ret;
}

- (NSDragOperation)draggingEntered:(id <NSDraggingInfo>)sender
{
    return [self triggerAvnDragEvent: AvnDragEventType::Enter info:sender];
}

- (NSDragOperation)draggingUpdated:(id <NSDraggingInfo>)sender
{
    return [self triggerAvnDragEvent: AvnDragEventType::Over info:sender];
}

- (void)draggingExited:(id <NSDraggingInfo>)sender
{
    [self triggerAvnDragEvent: AvnDragEventType::Leave info:sender];
}

- (BOOL)prepareForDragOperation:(id <NSDraggingInfo>)sender
{
    return [self triggerAvnDragEvent: AvnDragEventType::Over info:sender] != NSDragOperationNone;
}

- (BOOL)performDragOperation:(id <NSDraggingInfo>)sender
{
    return [self triggerAvnDragEvent: AvnDragEventType::Drop info:sender] != NSDragOperationNone;
}

- (void)concludeDragOperation:(nullable id <NSDraggingInfo>)sender
{

}

- (AvnPlatformResizeReason)getResizeReason
{
    return _resizeReason;
}

- (void)setResizeReason:(AvnPlatformResizeReason)reason
{
    _resizeReason = reason;
}

- (AvnAccessibilityElement *) accessibilityChild
{
    if (_accessibilityChild == nil)
    {
        auto peer = _parent->BaseEvents->GetAutomationPeer();

        if (peer == nil)
            return nil;

        _accessibilityChild = [AvnAccessibilityElement acquire:peer];
    }

    return _accessibilityChild;
}

- (NSArray *)accessibilityChildren
{
    auto child = [self accessibilityChild];
    return NSAccessibilityUnignoredChildrenForOnlyChild(child);
}

- (id)accessibilityHitTest:(NSPoint)point
{
    return [[self accessibilityChild] accessibilityHitTest:point];
}

- (id)accessibilityFocusedUIElement
{
    return [[self accessibilityChild] accessibilityFocusedUIElement];
}

- (void) setText:(NSString *)text{
    [[_text mutableString] setString:text];
}

- (void) setSelection:(int)start :(int)end{
    _selectedRange = NSMakeRange(start, end - start);
}

- (void) setCursorRect:(AvnRect)rect{
    NSRect cursorRect = ToNSRect(rect);
    NSRect windowRectOnScreen = [[self window] convertRectToScreen:self.frame];
    
    windowRectOnScreen.size = cursorRect.size;
    windowRectOnScreen.origin = NSMakePoint(windowRectOnScreen.origin.x + cursorRect.origin.x, windowRectOnScreen.origin.y + self.frame.size.height - cursorRect.origin.y - cursorRect.size.height);
    
    _cursorRect = windowRectOnScreen;
    
    if([self inputContext]) {
        [[self inputContext] invalidateCharacterCoordinates];
    }
}

@end
