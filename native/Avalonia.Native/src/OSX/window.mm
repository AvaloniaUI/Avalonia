#import <AppKit/AppKit.h>
#include "common.h"
#import "window.h"
#include "KeyTransform.h"
#include "menu.h"
#include "rendertarget.h"
#include "automation.h"
#import "WindowBaseImpl.h"
#include "WindowImpl.h"

@implementation AutoFitContentView
{
    NSVisualEffectView* _titleBarMaterial;
    NSBox* _titleBarUnderline;
    NSView* _content;
    NSVisualEffectView* _blurBehind;
    double _titleBarHeightHint;
    bool _settingSize;
}

-(AutoFitContentView* _Nonnull) initWithContent:(NSView *)content
{
    _titleBarHeightHint = -1;
    _content = content;
    _settingSize = false;

    [self setAutoresizesSubviews:true];
    [self setWantsLayer:true];
    
    _titleBarMaterial = [NSVisualEffectView new];
    [_titleBarMaterial setBlendingMode:NSVisualEffectBlendingModeWithinWindow];
    [_titleBarMaterial setMaterial:NSVisualEffectMaterialTitlebar];
    [_titleBarMaterial setWantsLayer:true];
    _titleBarMaterial.hidden = true;
    
    _titleBarUnderline = [NSBox new];
    _titleBarUnderline.boxType = NSBoxSeparator;
    _titleBarUnderline.fillColor = [NSColor underPageBackgroundColor];
    _titleBarUnderline.hidden = true;
    
    [self addSubview:_titleBarMaterial];
    [self addSubview:_titleBarUnderline];
    
    _blurBehind = [NSVisualEffectView new];
    [_blurBehind setBlendingMode:NSVisualEffectBlendingModeBehindWindow];
    [_blurBehind setMaterial:NSVisualEffectMaterialLight];
    [_blurBehind setWantsLayer:true];
    _blurBehind.hidden = true;
    
    [_blurBehind setAutoresizingMask:NSViewWidthSizable | NSViewHeightSizable];
    [_content setAutoresizingMask:NSViewWidthSizable | NSViewHeightSizable];
    
    [self addSubview:_blurBehind];
    [self addSubview:_content];
    
    [self setWantsLayer:true];
    return self;
}

-(void) ShowBlur:(bool)show
{
    _blurBehind.hidden = !show;
}

-(void) ShowTitleBar: (bool) show
{
    _titleBarMaterial.hidden = !show;
    _titleBarUnderline.hidden = !show;
}

-(void) SetTitleBarHeightHint: (double) height
{
    _titleBarHeightHint = height;
    
    [self setFrameSize:self.frame.size];
}

-(void)setFrameSize:(NSSize)newSize
{
    if(_settingSize)
    {
        return;
    }
    
    _settingSize = true;
    [super setFrameSize:newSize];
    
    auto window = objc_cast<AvnWindow>([self window]);
    
    // TODO get actual titlebar size
    
    double height = _titleBarHeightHint == -1 ? [window getExtendedTitleBarHeight] : _titleBarHeightHint;
    
    NSRect tbar;
    tbar.origin.x = 0;
    tbar.origin.y = newSize.height - height;
    tbar.size.width = newSize.width;
    tbar.size.height = height;
    
    [_titleBarMaterial setFrame:tbar];
    tbar.size.height = height < 1 ? 0 : 1;
    [_titleBarUnderline setFrame:tbar];

    _settingSize = false;
}
@end

@implementation AvnView
{
    ComPtr<WindowBaseImpl> _parent;
    NSTrackingArea* _area;
    bool _isLeftPressed, _isMiddlePressed, _isRightPressed, _isXButton1Pressed, _isXButton2Pressed;
    AvnInputModifiers _modifierState;
    NSEvent* _lastMouseDownEvent;
    bool _lastKeyHandled;
    AvnPixelSize _lastPixelSize;
    NSObject<IRenderTarget>* _renderTarget;
    AvnPlatformResizeReason _resizeReason;
    AvnAccessibilityElement* _accessibilityChild;
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
    [_renderTarget resize:_lastPixelSize withScale:static_cast<float>([[self window] backingScaleFactor])];
    [self setNeedsDisplayInRect:[self frame]];
}

-(AvnView*)  initWithParent: (WindowBaseImpl*) parent
{
    self = [super init];
    _renderTarget = parent->renderTarget;
    [self setWantsLayer:YES];
    [self setLayerContentsRedrawPolicy: NSViewLayerContentsRedrawDuringViewResize];
    
    _parent = parent;
    _area = nullptr;
    _lastPixelSize.Height = 100;
    _lastPixelSize.Width = 100;
    [self registerForDraggedTypes: @[@"public.data", GetAvnCustomDataType()]];
    
    _modifierState = AvnInputModifiersNone;
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

- (void)setLayer:(CALayer *)layer
{
    [_renderTarget setNewLayer: layer];
    [super setLayer: layer];
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

-(void) setSwRenderedFrame: (AvnFramebuffer*) fb dispose: (IUnknown*) dispose
{
    @autoreleasepool {
        [_renderTarget setSwFrame:fb];
        dispose->Release();
    }
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
    auto parentWindow = objc_cast<AvnWindow>([self window]);
    
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
    bool triggerInputWhenDisabled = type != Move;
    
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
    
    uint32 timestamp = static_cast<uint32>([event timestamp] * 1000);
    auto modifiers = [self getModifiers:[event modifierFlags]];
    
    if(type != AvnRawMouseEventType::Move ||
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
    if([self ignoreUserInput: false])
    {
        return;
    }
    
    auto key = s_KeyMap[[event keyCode]];
    
    uint32_t timestamp = static_cast<uint32_t>([event timestamp] * 1000);
    auto modifiers = [self getModifiers:[event modifierFlags]];
     
    if(_parent != nullptr)
    {
        _lastKeyHandled = _parent->BaseEvents->RawKeyEvent(type, timestamp, modifiers, key);
    }
}

- (BOOL)performKeyEquivalent:(NSEvent *)event
{
    bool result = _lastKeyHandled;
    
    _lastKeyHandled = false;
    
    return result;
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

- (void)keyDown:(NSEvent *)event
{
    [self keyboardEvent:event withType:KeyDown];
    [[self inputContext] handleEvent:event];
    [super keyDown:event];
}

- (void)keyUp:(NSEvent *)event
{
    [self keyboardEvent:event withType:KeyUp];
    [super keyUp:event];
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
    return _lastKeyHandled;
}

- (NSRange)markedRange
{
    return NSMakeRange(NSNotFound, 0);
}

- (NSRange)selectedRange
{
    return NSMakeRange(NSNotFound, 0);
}

- (void)setMarkedText:(id)string selectedRange:(NSRange)selectedRange replacementRange:(NSRange)replacementRange
{
    
}

- (void)unmarkText
{
    
}

- (NSArray<NSString *> *)validAttributesForMarkedText
{
    return [NSArray new];
}

- (NSAttributedString *)attributedSubstringForProposedRange:(NSRange)range actualRange:(NSRangePointer)actualRange
{
    return [NSAttributedString new];
}

- (void)insertText:(id)string replacementRange:(NSRange)replacementRange
{
    if(!_lastKeyHandled)
    {
        if(_parent != nullptr)
        {
            _lastKeyHandled = _parent->BaseEvents->RawTextInputEvent(0, [string UTF8String]);
        }
    }
}

- (NSUInteger)characterIndexForPoint:(NSPoint)point
{
    return 0;
}

- (NSRect)firstRectForCharacterRange:(NSRange)range actualRange:(NSRangePointer)actualRange
{
    CGRect result = { 0 };
    
    return result;
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

@end


@implementation AvnWindow
{
    ComPtr<WindowBaseImpl> _parent;
    bool _canBecomeKeyAndMain;
    bool _closed;
    bool _isEnabled;
    bool _isExtended;
    AvnMenu* _menu;
}

-(void) setIsExtended:(bool)value;
{
    _isExtended = value;
}

-(bool) isDialog
{
    return _parent->IsDialog();
}

-(double) getExtendedTitleBarHeight
{
    if(_isExtended)
    {
        for (id subview in self.contentView.superview.subviews)
        {
            if ([subview isKindOfClass:NSClassFromString(@"NSTitlebarContainerView")])
            {
                NSView *titlebarView = [subview subviews][0];

                return (double)titlebarView.frame.size.height;
            }
        }

        return -1;
    }
    else
    {
        return 0;
    }
}

- (void)performClose:(id)sender
{
    if([[self delegate] respondsToSelector:@selector(windowShouldClose:)])
    {
        if(![[self delegate] windowShouldClose:self]) return;
    }
    else if([self respondsToSelector:@selector(windowShouldClose:)])
    {
        if(![self windowShouldClose:self]) return;
    }

    [self close];
}

- (void)pollModalSession:(nonnull NSModalSession)session
{
    auto response = [NSApp runModalSession:session];
    
    if(response == NSModalResponseContinue)
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            [self pollModalSession:session];
        });
    }
    else if (!_closed)
    {
        [self orderOut:self];
        [NSApp endModalSession:session];
    }
}

-(void) showWindowMenuWithAppMenu
{
    if(_menu != nullptr)
    {
        auto appMenuItem = ::GetAppMenuItem();
        
        if(appMenuItem != nullptr)
        {
            auto appMenu = [appMenuItem menu];
            
            [appMenu removeItem:appMenuItem];
            
            [_menu insertItem:appMenuItem atIndex:0];
            
            [_menu setHasGlobalMenuItem:true];
        }
        
        [NSApp setMenu:_menu];
    }
    else
    {
        [self showAppMenuOnly];
    }
}

-(void) showAppMenuOnly
{
    auto appMenuItem = ::GetAppMenuItem();
    
    if(appMenuItem != nullptr)
    {
        auto appMenu = ::GetAppMenu();
        
        auto nativeAppMenu = dynamic_cast<AvnAppMenu*>(appMenu);
        
        [[appMenuItem menu] removeItem:appMenuItem];
        
        if(_menu != nullptr)
        {
            [_menu setHasGlobalMenuItem:false];
        }
        
        [nativeAppMenu->GetNative() addItem:appMenuItem];
        
        [NSApp setMenu:nativeAppMenu->GetNative()];
    }
}

-(void) applyMenu:(AvnMenu *)menu
{
    if(menu == nullptr)
    {
        menu = [AvnMenu new];
    }
    
    _menu = menu;
}

-(void) setCanBecomeKeyAndMain
{
    _canBecomeKeyAndMain = true;
}

-(AvnWindow*)  initWithParent: (WindowBaseImpl*) parent
{
    self = [super init];
    [self setReleasedWhenClosed:false];
    _parent = parent;
    [self setDelegate:self];
    _closed = false;
    _isEnabled = true;
    
    [self backingScaleFactor];
    [self setOpaque:NO];
    [self setBackgroundColor: [NSColor clearColor]];
    _isExtended = false;
    return self;
}

- (BOOL)windowShouldClose:(NSWindow *)sender
{
    auto window = dynamic_cast<WindowImpl*>(_parent.getRaw());
    
    if(window != nullptr)
    {
        return !window->WindowEvents->Closing();
    }
    
    return true;
}

- (void)windowDidChangeBackingProperties:(NSNotification *)notification
{
    [self backingScaleFactor];
}

- (void)windowWillClose:(NSNotification *)notification
{
    _closed = true;
    if(_parent)
    {
        ComPtr<WindowBaseImpl> parent = _parent;
        _parent = NULL;
        [self restoreParentWindow];
        parent->BaseEvents->Closed();
        [parent->View onClosed];
    }
}

-(BOOL)canBecomeKeyWindow
{
    if (_canBecomeKeyAndMain)
    {
        // If the window has a child window being shown as a dialog then don't allow it to become the key window.
        for(NSWindow* uch in [self childWindows])
        {
            auto ch = objc_cast<AvnWindow>(uch);
            if(ch == nil)
                continue;
            if (ch.isDialog)
                return false;
        }
        
        return true;
    }
    
    return false;
}

-(BOOL)canBecomeMainWindow
{
    return _canBecomeKeyAndMain;
}

-(bool)shouldTryToHandleEvents
{
    return _isEnabled;
}

-(void) setEnabled:(bool)enable
{
    _isEnabled = enable;
}

-(void)becomeKeyWindow
{
    [self showWindowMenuWithAppMenu];
    
    if(_parent != nullptr)
    {
        _parent->BaseEvents->Activated();
    }

    [super becomeKeyWindow];
}

-(void) restoreParentWindow;
{
    auto parent = objc_cast<AvnWindow>([self parentWindow]);
    if(parent != nil)
    {
        [parent removeChildWindow:self];
    }
}

- (void)windowDidMiniaturize:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}

- (void)windowDidDeminiaturize:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}

- (void)windowDidResize:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}

- (void)windowWillExitFullScreen:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->StartStateTransition();
    }
}

- (void)windowDidExitFullScreen:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->EndStateTransition();
        
        if(parent->Decorations() != SystemDecorationsFull && parent->WindowState() == Maximized)
        {
            NSRect screenRect = [[self screen] visibleFrame];
            [self setFrame:screenRect display:YES];
        }
        
        if(parent->WindowState() == Minimized)
        {
            [self miniaturize:nullptr];
        }
        
        parent->WindowStateChanged();
    }
}

- (void)windowWillEnterFullScreen:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->StartStateTransition();
    }
}

- (void)windowDidEnterFullScreen:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->EndStateTransition();
        parent->WindowStateChanged();
    }
}

- (BOOL)windowShouldZoom:(NSWindow *)window toFrame:(NSRect)newFrame
{
    return true;
}

-(void)resignKeyWindow
{
    if(_parent)
        _parent->BaseEvents->Deactivated();
    
    [self showAppMenuOnly];
    
    [super resignKeyWindow];
}

- (void)windowDidMove:(NSNotification *)notification
{
    AvnPoint position;
    
    if(_parent != nullptr)
    {
        auto cparent = dynamic_cast<WindowImpl*>(_parent.getRaw());
        
        if(cparent != nullptr)
        {
            if(cparent->WindowState() == Maximized)
            {
                cparent->SetWindowState(Normal);
            }
        }
        
        _parent->GetPosition(&position);
        _parent->BaseEvents->PositionChanged(position);
    }
}

- (AvnPoint) translateLocalPoint:(AvnPoint)pt
{
    pt.Y = [self frame].size.height - pt.Y;
    return pt;
}

- (void)sendEvent:(NSEvent *)event
{
    [super sendEvent:event];
    
    /// This is to detect non-client clicks. This can only be done on Windows... not popups, hence the dynamic_cast.
    if(_parent != nullptr && dynamic_cast<WindowImpl*>(_parent.getRaw()) != nullptr)
    {
        switch(event.type)
        {
            case NSEventTypeLeftMouseDown:
            {
                AvnView* view = _parent->View;
                NSPoint windowPoint = [event locationInWindow];
                NSPoint viewPoint = [view convertPoint:windowPoint fromView:nil];
                
                if (!NSPointInRect(viewPoint, view.bounds))
                {
                    auto avnPoint = [AvnView toAvnPoint:windowPoint];
                    auto point = [self translateLocalPoint:avnPoint];
                    AvnVector delta = { 0, 0 };
                   
                    _parent->BaseEvents->RawMouseEvent(NonClientLeftButtonDown, static_cast<uint32>([event timestamp] * 1000), AvnInputModifiersNone, point, delta);
                }
            }
            break;
                
            case NSEventTypeMouseEntered:
            {
                _parent->UpdateCursor();
            }
            break;
                
            case NSEventTypeMouseExited:
            {
                [[NSCursor arrowCursor] set];
            }
            break;
                
            default:
                break;
        }
    }
}

@end

class PopupImpl : public virtual WindowBaseImpl, public IAvnPopup
{
private:
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    INTERFACE_MAP_ENTRY(IAvnPopup, IID_IAvnPopup)
    END_INTERFACE_MAP()
    virtual ~PopupImpl(){}
    ComPtr<IAvnWindowEvents> WindowEvents;
    PopupImpl(IAvnWindowEvents* events, IAvnGlContext* gl) : WindowBaseImpl(events, gl)
    {
        WindowEvents = events;
        [Window setLevel:NSPopUpMenuWindowLevel];
    }
protected:
    virtual NSWindowStyleMask GetStyle() override
    {
        return NSWindowStyleMaskBorderless;
    }
    
    virtual HRESULT Resize(double x, double y, AvnPlatformResizeReason reason) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if (Window != nullptr)
            {
                [Window setContentSize:NSSize{x, y}];
            
                [Window setFrameTopLeftPoint:ToNSPoint(ConvertPointY(lastPositionSet))];
            }
            
            return S_OK;
        }
    }
public:
    virtual bool ShouldTakeFocusOnShow() override
    {
        return false;
    }
};

extern IAvnPopup* CreateAvnPopup(IAvnWindowEvents*events, IAvnGlContext* gl)
{
    @autoreleasepool
    {
        IAvnPopup* ptr = dynamic_cast<IAvnPopup*>(new PopupImpl(events, gl));
        return ptr;
    }
}

extern IAvnWindow* CreateAvnWindow(IAvnWindowEvents*events, IAvnGlContext* gl)
{
    @autoreleasepool
    {
        IAvnWindow* ptr = (IAvnWindow*)new WindowImpl(events, gl);
        return ptr;
    }
}
