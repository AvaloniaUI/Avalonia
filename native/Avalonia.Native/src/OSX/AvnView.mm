//
// Created by Dan Walmsley on 05/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <AppKit/AppKit.h>
#include "AvnView.h"
#include "automation.h"
#import "WindowInterfaces.h"
#import "WindowImpl.h"

@implementation AvnView
{
    ComObjectWeakPtr<TopLevelImpl> _parent;
    NSTrackingArea* _area;
    AvnInputModifiers _modifierState;
    NSEvent* _lastMouseDownEvent;
    AvnPixelSize _lastPixelSize;
    NSObject<IRenderTarget>* _currentRenderTarget;
    AvnPlatformResizeReason _resizeReason;
    NSRect _cursorRect;
    NSMutableAttributedString* _text;
    NSRange _selectedRange;
    NSRange _markedRange;
    NSEvent* _lastKeyDownEvent;
    NSMutableArray* _accessibilityChildren;
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
        AvnPixelSize size { MAX(_lastPixelSize.Width, 1), MAX(_lastPixelSize.Height, 1) };
        [_currentRenderTarget resize:size withScale:static_cast<float>([[self window] backingScaleFactor])];
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

-(AvnView*)  initWithParent: (TopLevelImpl*) parent
{
    self = [super init];
    [self setWantsLayer:YES];
    [self setLayerContentsPlacement: NSViewLayerContentsPlacementTopLeft];

    [self setCanDrawSubviewsIntoLayer: NO];
    [self setLayerContentsRedrawPolicy: NSViewLayerContentsRedrawDuringViewResize];

    _parent = parent;

    // NSTrackingInVisibleRect makes AppKit follow the visible bounds of the view.
    // Because of this, the tracking area does not need to change on resize.
    // Do not remove and add the area again on each live-resize tick. If you do, AppKit
    // queues synthetic enter and exit events with stale locations. These events corrupt
    // the pointer position after a resize and force it to the top left.
    NSTrackingAreaOptions options = NSTrackingActiveAlways | NSTrackingMouseMoved |
        NSTrackingMouseEnteredAndExited | NSTrackingEnabledDuringMouseDrag | NSTrackingInVisibleRect;
    _area = [[NSTrackingArea alloc] initWithRect:NSZeroRect options:options owner:self userInfo:nullptr];
    [self addTrackingArea:_area];

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

    auto parent = _parent.tryGet();
    if (parent == nullptr)
    {
        return;
    }

    parent->UpdateCursor();

    auto fsize = [self convertSizeToBacking: [self frame].size];

    if(_lastPixelSize.Width != (int)fsize.width || _lastPixelSize.Height != (int)fsize.height)
    {
        _lastPixelSize.Width = (int)fsize.width;
        _lastPixelSize.Height = (int)fsize.height;
        [self updateRenderTarget];

        auto reason = [self inLiveResize] ? ResizeUser : _resizeReason;

        parent->TopLevelEvents->Resized(FromNSSize(newSize), reason);
    }
}

- (void)updateLayer
{
    AvnInsidePotentialDeadlock deadlock;
    auto parent = _parent.tryGet();
    if (parent == nullptr)
    {
        return;
    }

    parent->TopLevelEvents->RunRenderPriorityJobs();

    parent = _parent.tryGet();
    if (parent == nullptr)
    {
        return;
    }

    parent->TopLevelEvents->Paint();
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

- (void) viewDidChangeBackingProperties
{
    auto fsize = [self convertSizeToBacking: [self frame].size];
    _lastPixelSize.Width = (int)fsize.width;
    _lastPixelSize.Height = (int)fsize.height;
    [self updateRenderTarget];

    auto parent = _parent.tryGet();
    if(parent != nullptr)
    {
        parent->TopLevelEvents->ScalingChanged([[self window] backingScaleFactor]);
    }

    [super viewDidChangeBackingProperties];
}

- (bool) ignoreUserInput:(bool)trigerInputWhenDisabled
{
    auto parent = _parent.tryGet();
    if(parent == nullptr)
    {
        return TRUE;
    }
    
    id<AvnWindowProtocol> parentWindow = nullptr;

    if([[self window] conformsToProtocol:@protocol(AvnWindowProtocol)]){
        parentWindow = (id<AvnWindowProtocol>)[self window];
    }

    if(parentWindow != nullptr && ![parentWindow shouldTryToHandleEvents])
    {
        if(trigerInputWhenDisabled)
        {
            auto windowImpl = _parent.tryGetWithCast<WindowImpl>();
            
            if(windowImpl == nullptr){
                return FALSE;
            }
            
            windowImpl->WindowEvents->GotInputWhenDisabled();
        }

        return TRUE;
    }

    return FALSE;
}

static void ConvertTilt(NSPoint tilt, float* xTilt, float* yTilt)
{
    *xTilt =  tilt.x * 90;
    *yTilt = -tilt.y * 90;
}

- (void)mouseEvent:(NSEvent *)event withType:(AvnRawMouseEventType) type
{
    bool triggerInputWhenDisabled = type != Move && type != LeaveWindow;

    if([self ignoreUserInput: triggerInputWhenDisabled])
    {
        return;
    }

    // AppKit synthesizes enter and exit events for the tracked geometry.
    // They can have stale locations. Use the current pointer location instead.
    NSPoint eventLocation = event.type == NSEventTypeMouseEntered || event.type == NSEventTypeMouseExited
        ? [[self window] mouseLocationOutsideOfEventStream]
        : [event locationInWindow];

    auto viewLocation = [self convertPoint:NSMakePoint(0, 0) toView:nil];
    
    auto localPoint = NSMakePoint(eventLocation.x - viewLocation.x, viewLocation.y - eventLocation.y);
    
    auto point = ToAvnPoint(localPoint);
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

    float pressure = 0.5f;
    float xTilt = 0.0f;
    float yTilt = 0.0f;
    AvnPointerDeviceType pointerType = AvnPointerDeviceType::Mouse;

    switch (event.type)
    {
        case NSEventTypeLeftMouseDown:
        case NSEventTypeLeftMouseDragged:
        case NSEventTypeRightMouseDown:
        case NSEventTypeRightMouseDragged:
        case NSEventTypeOtherMouseDown:
        case NSEventTypeOtherMouseDragged:
            switch (event.subtype)
            {
                case NSEventSubtypeTabletPoint:
                    pointerType = AvnPointerDeviceType::Pen;
                    pressure = event.pressure;
                    ConvertTilt(event.tilt, &xTilt, &yTilt);
                    break;
                case NSEventSubtypeTabletProximity:
                    pointerType = AvnPointerDeviceType::Pen;
                    pressure = 0.0f;
                    break;
                default:
                    break;
            }
            break;
        case NSEventTypeLeftMouseUp:
        case NSEventTypeRightMouseUp:
        case NSEventTypeOtherMouseUp:
        case NSEventTypeMouseMoved:
            switch (event.subtype)
            {
                case NSEventSubtypeTabletPoint:
                    pointerType = AvnPointerDeviceType::Pen;
                    pressure = 0.0f;
                    ConvertTilt(event.tilt, &xTilt, &yTilt);
                    break;
                case NSEventSubtypeTabletProximity:
                    pointerType = AvnPointerDeviceType::Pen;
                    pressure = 0.0f;
                    break;
                default:
                    break;
            }
            break;
        default:
            break;
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
    {
        auto windowBase = _parent.tryGetWithCast<WindowBaseImpl>();
        
        if(windowBase != nullptr){
            auto parent = windowBase->Parent.tryGet();
            
            if(parent != nullptr){
                auto parentWindow = parent->Window;
                
                if(parentWindow != nullptr)
                    [parentWindow makeFirstResponder:parent->View];
            }
        } else{
            [self becomeFirstResponder];
        }
    }

    if([self hasMarkedText] &&
       (type == LeftButtonDown || type == RightButtonDown || type == MiddleButtonDown ||
        type == XButton1Down || type == XButton2Down) &&
       [self inputContext] != nil)
    {
        [[self inputContext] handleEvent:event];
    }

    auto parent = _parent.tryGet();
    if(parent != nullptr)
    {
        parent->TopLevelEvents->RawMouseEvent(type, pointerType, timestamp, modifiers, point, delta, pressure, xTilt, yTilt);
    }

    // This handler is shared by all mouse event types. Only real mouse-moved
    // events must continue up the responder chain as mouseMoved:
    if (event.type == NSEventTypeMouseMoved)
        [super mouseMoved:event];
}

- (BOOL) resignFirstResponder
{
    auto window = [self window];
    if (window != nullptr && window.keyWindow)
    {
        [self onLostFocus];
    }
    
    return YES;
}

- (void)viewWillMoveToWindow:(NSWindow *)newWindow
{
    auto oldWindow = [self window];
    if (oldWindow == newWindow)
    {
        // viewWillMoveToWindow can be called with the same window when the view hierarchy changes
        return;
    }

    if (oldWindow != nullptr)
    {
        [[NSNotificationCenter defaultCenter]
            removeObserver:self
            name:@"NSWindowDidResignKeyNotification"
            object: oldWindow];
    }

    if (newWindow != nullptr)
    {
        [[NSNotificationCenter defaultCenter]
            addObserver:self
            selector:@selector(windowDidResignKey:)
            name:@"NSWindowDidResignKeyNotification"
            object: newWindow];
    }
}

- (void)windowDidResignKey:(NSNotification*)notification
{
    auto window = [self window];
    if (window != nullptr && notification.object == window && [window firstResponder] == self)
    {
        [self onLostFocus];
    }
}

- (void)onLostFocus
{
    auto parent = _parent.tryGet();
    if (parent)
        parent->TopLevelEvents->LostFocus();
}

- (void)mouseMoved:(NSEvent *)event
{
    [self mouseEvent:event withType:Move];
}

- (void)mouseDown:(NSEvent *)event
{
    _lastMouseDownEvent = event;
    [self mouseEvent:event withType:LeftButtonDown];
}

- (void)otherMouseDown:(NSEvent *)event
{
    _lastMouseDownEvent = event;

    switch(event.buttonNumber)
    {
        case 2:
            [self mouseEvent:event withType:MiddleButtonDown];
            break;
        case 3:
            [self mouseEvent:event withType:XButton1Down];
            break;
        case 4:
            [self mouseEvent:event withType:XButton2Down];
            break;

        default:
            break;
    }
}

- (void)rightMouseDown:(NSEvent *)event
{
    _lastMouseDownEvent = event;
    [self mouseEvent:event withType:RightButtonDown];
}

- (void)mouseUp:(NSEvent *)event
{
    [self mouseEvent:event withType:LeftButtonUp];
}

- (void)otherMouseUp:(NSEvent *)event
{
    switch(event.buttonNumber)
    {
        case 2:
            [self mouseEvent:event withType:MiddleButtonUp];
            break;
        case 3:
            [self mouseEvent:event withType:XButton1Up];
            break;
        case 4:
            [self mouseEvent:event withType:XButton2Up];
            break;

        default:
            break;
    }
}

- (void)rightMouseUp:(NSEvent *)event
{
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
    // If the pointer is not inside the view, the enter event is false. Ignore it.
    NSPoint location = [self convertPoint:[[self window] mouseLocationOutsideOfEventStream] fromView:nil];
    if (NSPointInRect(location, self.bounds))
        [self mouseEvent:event withType:Move];
    [super mouseEntered:event];
}

- (void)mouseExited:(NSEvent *)event
{
    [self mouseEvent:event withType:LeaveWindow];
    [super mouseExited:event];
}

- (void) keyboardEvent: (NSEvent *) event withType: (AvnRawKeyEventType)type
{
    auto parent = _parent.tryGet();
    if([self ignoreUserInput: false] || parent == nullptr)
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

    parent->TopLevelEvents->RawKeyEvent(type, timestamp, modifiers, key, physicalKey, keySymbolUtf8);
}

- (void)setModifiers:(NSEventModifierFlags)modifierFlags
{
    _modifierState = [self getModifiers:modifierFlags];
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
    auto parent = _parent.tryGet();
    return parent->TopLevelEvents->RawKeyEvent(KeyDown, timestamp, modifiers, key, physicalKey, [keySymbol UTF8String]);
}

- (void)keyDown:(NSEvent *)event
{
    auto parent = _parent.tryGet();
    if([self ignoreUserInput: false] || parent == nullptr)
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
    if(parent->InputMethod->IsActive()){
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
                
                parent->TopLevelEvents->RawTextInputEvent(timestamp, [keySymbol UTF8String]);
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

    NSUInteger pressedButtons = [NSEvent pressedMouseButtons];
        
    if (pressedButtons & (1 << 0))  // Left mouse button
        rv |= LeftMouseButton;
    if (pressedButtons & (1 << 1))  // Right mouse button
        rv |= RightMouseButton;
    if (pressedButtons & (1 << 2))  // Middle mouse button
        rv |= MiddleMouseButton;
    if (pressedButtons & (1 << 3))  // X1 button
        rv |= XButton1MouseButton;
    if (pressedButtons & (1 << 4))  // X2 button
        rv |= XButton2MouseButton;

    return (AvnInputModifiers)rv;
}

// Clamps a range so that it can never be used to index outside of _text.
// Ranges reaching us from AppKit or from the managed side are not guaranteed to be valid.
- (NSRange)clampRangeToText:(NSRange)range
{
    if (range.location == NSNotFound)
        return NSMakeRange(NSNotFound, 0);

    NSUInteger length = _text.length;

    if (range.location > length)
        return NSMakeRange(length, 0);

    // Avoids the overflow of location + length that a plain bounds check would have.
    return NSMakeRange(range.location, MIN(range.length, length - range.location));
}

- (BOOL)hasMarkedText
{
    return _markedRange.location != NSNotFound && _markedRange.length > 0;
}

- (NSRange)markedRange
{
    // From the docs: returns {NSNotFound, 0} if there is no marked range.
    if (![self hasMarkedText])
        return NSMakeRange(NSNotFound, 0);

    // The preedit isn't necessarily part of the surrounding text we got from the managed side,
    // so only the location is clamped here. An overlong length is handled by
    // attributedSubstringForProposedRange:actualRange:, as the docs require.
    return NSMakeRange(MIN(_markedRange.location, _text.length), _markedRange.length);
}

- (NSRange)selectedRange
{
    return [self clampRangeToText:_selectedRange];
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

    if (markedText == nil)
    {
        markedText = @"";
    }

    auto parent = _parent.tryGet();

    // Delete any replaced range
    auto finalReplacementRange = [self clampRangeToText:replacementRange];

    if (finalReplacementRange.location != NSNotFound && parent != nullptr && parent->InputMethod->IsActive())
    {
        parent->InputMethod->Client->SelectInSurroundingText((int)finalReplacementRange.location, (int)(finalReplacementRange.location + finalReplacementRange.length));
        uint64_t timestamp = static_cast<uint64_t>([NSDate timeIntervalSinceReferenceDate] * 1000);
        parent->TopLevelEvents->RawKeyEvent(KeyDown, timestamp, AvnInputModifiersNone, AvnKeyBack, AvnPhysicalKeyNone, "\b");
        parent->TopLevelEvents->RawKeyEvent(KeyUp, timestamp, AvnInputModifiersNone, AvnKeyBack, AvnPhysicalKeyNone, "\b");
    }

    auto markedLocation = [self selectedRange].location;

    _markedRange = NSMakeRange(markedLocation == NSNotFound ? 0 : markedLocation, [markedText length]);

    if (parent != nullptr && parent->InputMethod->IsActive())
    {
        const char* utf8MarkedText = [markedText UTF8String];
        parent->InputMethod->Client->SetPreeditText((char*)(utf8MarkedText != nullptr ? utf8MarkedText : ""));
    }
}

- (void)unmarkText
{
    auto parent = _parent.tryGet();
    if(parent != nullptr && parent->InputMethod->IsActive()){
        parent->InputMethod->Client->SetPreeditText(nullptr);
    }

    auto selectionLocation = [self selectedRange].location;

    _markedRange = NSMakeRange(selectionLocation == NSNotFound ? 0 : selectionLocation, 0);

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
    // From the docs: an implementation of this method should be prepared for aRange to be out of bounds.
    // In this case, you should return the intersection of the document's range and aRange.
    // If the location of aRange is completely outside of the document's range, return nil.
    // actualRange is an out parameter: it is uninitialized on entry and must only be written to.
    NSRange docRange = NSMakeRange(0, _text.length);
    NSRange finalRange = NSIntersectionRange(range, docRange);

    if (finalRange.length == 0)
    {
        if (actualRange) {
            *actualRange = NSMakeRange(NSNotFound, 0);
        }

        return nil;
    }

    if (actualRange) {
        *actualRange = finalRange;
    }

    return [_text attributedSubstringFromRange:finalRange];
}

- (void)insertText:(id)string replacementRange:(NSRange)replacementRange
{
    auto parent = _parent.tryGet();
    if(parent == nullptr){
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

    if (text == nil)
    {
        text = @"";
    }

    auto finalReplacementRange = [self clampRangeToText:replacementRange];

    if (finalReplacementRange.location != NSNotFound &&
        ![self hasMarkedText] &&
        parent->InputMethod->IsActive())
    {
        parent->InputMethod->Client->SelectInSurroundingText((int)finalReplacementRange.location, (int)(finalReplacementRange.location + finalReplacementRange.length));
    }

    [self unmarkText];

    uint64_t timestamp = static_cast<uint64_t>([NSDate timeIntervalSinceReferenceDate] * 1000);

    const char* utf8Text = [text UTF8String];

    parent->TopLevelEvents->RawTextInputEvent(timestamp, utf8Text != nullptr ? utf8Text : "");
}

- (NSUInteger)characterIndexForPoint:(NSPoint)point
{
    return NSNotFound;
}

- (NSRect)firstRectForCharacterRange:(NSRange)range actualRange:(NSRangePointer)actualRange
{
    // actualRange is an out parameter: it is uninitialized on entry and must only be written to.
    // We only ever report a single rect, so the requested range is echoed back clamped to the document.
    if (actualRange) {
        *actualRange = [self clampRangeToText:range];
    }

    auto parent = _parent.tryGet();
    if(parent == nullptr || !parent->InputMethod->IsActive()){
        return NSZeroRect;
    }

    return _cursorRect;
}

- (NSDragOperation)triggerAvnDragEvent: (AvnDragEventType) type info: (id <NSDraggingInfo>)info
{
    NSPoint eventLocation = [info draggingLocation];
    auto viewLocation = [self convertPoint:NSMakePoint(0, 0) toView:nil];
    auto localPoint = NSMakePoint(eventLocation.x - viewLocation.x, viewLocation.y - eventLocation.y);
    auto point = ToAvnPoint(localPoint);
    auto modifiers = [self getModifiers:[[NSApp currentEvent] modifierFlags]];
    NSDragOperation nsop = [info draggingSourceOperationMask];

    auto effects = ConvertDragDropEffects(nsop);
    auto parent = _parent.tryGet();
    if (!parent)
      return NSDragOperationNone;
    int reffects = (int)parent->TopLevelEvents
            ->DragEvent(type, point, modifiers, effects,
                    CreateClipboard([info draggingPasteboard]),
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

- (NSArray *)accessibilityChildren
{
    if (_accessibilityChildren == nil)
        [self recalculateAccessibiltyChildren];
    return _accessibilityChildren;
}

- (id _Nullable) accessibilityHitTest:(NSPoint)point
{
    if (![[self window] isKindOfClass:[AvnWindow class]])
        return self;

    auto window = (AvnWindow*)[self window];
    auto peer = [window automationPeer];

    if (peer == nullptr || !peer->IsRootProvider())
        return nil;

    auto clientPoint = [window convertPointFromScreen:point];
    auto localPoint = [self translateLocalPoint:ToAvnPoint(clientPoint)];
    auto hit = peer->RootProvider_GetPeerFromPoint(localPoint);
    return [AvnAccessibilityElement acquire:hit];
}

- (void)raiseAccessibilityChildrenChanged
{
    auto changed = _accessibilityChildren ? [NSMutableSet setWithArray:_accessibilityChildren] : [NSMutableSet set];

    [self recalculateAccessibiltyChildren];

    if (_accessibilityChildren)
        [changed addObjectsFromArray:_accessibilityChildren];

    NSAccessibilityPostNotificationWithUserInfo(
        self,
        NSAccessibilityLayoutChangedNotification,
        @{ NSAccessibilityUIElementsKey: [changed allObjects]});
}

- (void)recalculateAccessibiltyChildren
{
    _accessibilityChildren = [[NSMutableArray alloc] init];

    if (![[self window] isKindOfClass:[AvnWindow class]])
    {
        return;
    }

    // The accessibility children of the Window are exposed as children
    // of the AvnView.
    auto window = (AvnWindow*)[self window];
    auto peer = [window automationPeer];
    if (peer == nullptr)
    {
        return;
    }
    auto childPeers = peer->GetChildren();
    auto childCount = childPeers != nullptr ? childPeers->GetCount() : 0;

    if (childCount > 0)
    {
        for (int i = 0; i < childCount; ++i)
        {
            IAvnAutomationPeer* child;

            if (childPeers->Get(i, &child) == S_OK)
            {
                id element = [AvnAccessibilityElement acquire:child];
                [_accessibilityChildren addObject:element];
            }
        }
    }
}

- (void) setText:(NSString *)text{
    [[_text mutableString] setString:text != nil ? text : @""];

    // The document changed, so the stored ranges can now point outside of it.
    _selectedRange = [self clampRangeToText:_selectedRange];

    if (_markedRange.location != NSNotFound)
    {
        _markedRange = NSMakeRange(MIN(_markedRange.location, _text.length), _markedRange.length);
    }
}

- (void) setSelection:(int)start :(int)end{
    if (end < start)
    {
        auto temp = start;
        start = end;
        end = temp;
    }

    auto length = (int)_text.length;

    start = MAX(0, MIN(start, length));
    end = MAX(start, MIN(end, length));

    _selectedRange = NSMakeRange((NSUInteger)start, (NSUInteger)(end - start));
}

- (void) resetInputMethod{
    auto parent = _parent.tryGet();

    if(parent != nullptr && parent->InputMethod->IsActive()){
        parent->InputMethod->Client->SetPreeditText(nullptr);
    }

    _markedRange = NSMakeRange(_selectedRange.location, 0);

    if([self inputContext]) {
        [[self inputContext] discardMarkedText];
    }
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
