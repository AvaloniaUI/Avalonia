#include "common.h"
#include "window.h"
#include "KeyTransform.h"

class WindowBaseImpl : public ComSingleObject<IAvnWindowBase, &IID_IAvnWindowBase>, public INSWindowHolder
{
public:
    AvnView* View;
    AvnWindow* Window;
    ComPtr<IAvnWindowBaseEvents> BaseEvents;
    AvnPoint lastPositionSet;
    WindowBaseImpl(IAvnWindowBaseEvents* events)
    {
        BaseEvents = events;
        View = [[AvnView alloc] initWithParent:this];
        Window = [[AvnWindow alloc] initWithParent:this];
        
        lastPositionSet.X = 100;
        lastPositionSet.Y = 100;
        
        [Window setStyleMask:NSWindowStyleMaskBorderless];
        [Window setBackingType:NSBackingStoreBuffered];
        [Window setContentView: View];
    }
    
    virtual AvnWindow* GetNSWindow()
    {
        return Window;
    }
    
    virtual HRESULT Show()
    {
        SetPosition(lastPositionSet);
        UpdateStyle();
        [Window makeKeyAndOrderFront:Window];
        return S_OK;
    }
    
    virtual HRESULT Hide ()
    {
        if(Window != nullptr)
        {
            [Window orderOut:Window];
        }
        return S_OK;
    }
    
    virtual HRESULT Activate ()
    {
        if(Window != nullptr)
        {
            [Window makeKeyWindow];
        }
        
        return S_OK;
    }
    
    virtual HRESULT SetTopMost (bool value)
    {
        [Window setLevel: value ? NSFloatingWindowLevel : NSNormalWindowLevel];
        
        return S_OK;
    }
    
    virtual HRESULT Close()
    {
        [Window close];
        return S_OK;
    }
    
    virtual HRESULT GetClientSize(AvnSize* ret)
    {
        if(ret == nullptr)
            return E_POINTER;
        auto frame = [View frame];
        ret->Width = frame.size.width;
        ret->Height = frame.size.height;
        return S_OK;
    }
    
    virtual HRESULT GetMaxClientSize(AvnSize* ret)
    {
        if(ret == nullptr)
            return E_POINTER;
        
        auto size = [NSScreen.screens objectAtIndex:0].frame.size;
        
        ret->Height = size.height;
        ret->Width = size.width;
        
        return S_OK;
    }
    
    virtual HRESULT GetScaling (double* ret)
    {
        if(ret == nullptr)
            return E_POINTER;
        
        if(Window == nullptr)
        {
            *ret = 1;
            return S_OK;
        }
        
        *ret = [Window backingScaleFactor];
        return S_OK;
    }
    
    virtual HRESULT Resize(double x, double y)
    {
        [Window setContentSize:NSSize{x, y}];
        return S_OK;
    }
    
    virtual void Invalidate (AvnRect rect)
    {
        [View setNeedsDisplayInRect:[View frame]];
    }
    
    virtual void BeginMoveDrag ()
    {
        auto lastEvent = [View lastMouseDownEvent];
        
        if(lastEvent == nullptr)
        {
            return;
        }
        
        [Window performWindowDragWithEvent:lastEvent];
    }
    
    
    virtual HRESULT GetPosition (AvnPoint* ret)
    {
        if(ret == nullptr)
        {
            return E_POINTER;
        }
        
        auto frame = [Window frame];
        
        ret->X = frame.origin.x;
        ret->Y = frame.origin.y + frame.size.height;
        
        *ret = ConvertPointY(*ret);
        
        return S_OK;
    }
    
    virtual void SetPosition (AvnPoint point)
    {
        lastPositionSet = point;
        [Window setFrameTopLeftPoint:ToNSPoint(ConvertPointY(point))];
    }
    
    virtual HRESULT PointToClient (AvnPoint point, AvnPoint* ret)
    {
        if(ret == nullptr)
        {
            return E_POINTER;
        }
        
        point = ConvertPointY(point);
        auto viewPoint = [Window convertScreenToBase:ToNSPoint(point)];
        
        *ret = [View translateLocalPoint:ToAvnPoint(viewPoint)];
        
        return S_OK;
    }
    
    virtual HRESULT PointToScreen (AvnPoint point, AvnPoint* ret)
    {
        if(ret == nullptr)
        {
            return E_POINTER;
        }
        
        auto cocoaViewPoint =  ToNSPoint([View translateLocalPoint:point]);
        auto cocoaScreenPoint = [Window convertBaseToScreen:cocoaViewPoint];
        *ret = ConvertPointY(ToAvnPoint(cocoaScreenPoint));
        
        return S_OK;
    }
    
    virtual HRESULT ThreadSafeSetSwRenderedFrame(AvnFramebuffer* fb, IUnknown* dispose)
    {
        [View setSwRenderedFrame: fb dispose: dispose];
        return S_OK;
    }
    
protected:
    virtual NSWindowStyleMask GetStyle()
    {
        return NSWindowStyleMaskBorderless;
    }
    
    void UpdateStyle()
    {
        [Window setStyleMask:GetStyle()];
    }
    
    virtual void OnResized ()
    {
        
    }
};

NSArray* AllLoopModes = [NSArray arrayWithObjects: NSDefaultRunLoopMode, NSEventTrackingRunLoopMode, NSModalPanelRunLoopMode, NSRunLoopCommonModes, NSConnectionReplyMode, nil];

@implementation AvnView
{
    ComPtr<WindowBaseImpl> _parent;
    ComPtr<IUnknown> _swRenderedFrame;
    AvnFramebuffer _swRenderedFrameBuffer;
    bool _queuedDisplayFromThread;
    NSTrackingArea* _area;
    bool _isLeftPressed, _isMiddlePressed, _isRightPressed, _isMouseOver;
    NSEvent* _lastMouseDownEvent;
    bool _lastKeyHandled;
}

- (NSEvent*) lastMouseDownEvent
{
    return _lastMouseDownEvent;
}

-(AvnView*)  initWithParent: (WindowBaseImpl*) parent
{
    self = [super init];
    _parent = parent;
    _area = nullptr;
    return self;
}

- (BOOL)isOpaque
{
    return false;
}

- (BOOL)acceptsFirstResponder
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
    
    NSRect rect = NSZeroRect;
    rect.size = newSize;
    
    NSTrackingAreaOptions options = NSTrackingActiveAlways | NSTrackingMouseMoved | NSTrackingEnabledDuringMouseDrag;
    _area = [[NSTrackingArea alloc] initWithRect:rect options:options owner:self userInfo:nullptr];
    [self addTrackingArea:_area];
    
    _parent->BaseEvents->Resized(AvnSize{newSize.width, newSize.height});
}

- (void) drawFb: (AvnFramebuffer*) fb
{
    auto colorSpace = CGColorSpaceCreateDeviceRGB();
    auto dataProvider = CGDataProviderCreateWithData(NULL, fb->Data, fb->Height*fb->Stride, NULL);

    
    auto image = CGImageCreate(fb->Width, fb->Height, 8, 32, fb->Stride, colorSpace, kCGBitmapByteOrderDefault | kCGImageAlphaPremultipliedLast,
                               dataProvider, nullptr, false, kCGRenderingIntentDefault);
    
    auto ctx = [NSGraphicsContext currentContext];
    
    [ctx saveGraphicsState];
    auto cgc = [ctx CGContext];
    
    CGContextDrawImage(cgc, CGRect{0,0, fb->Width/(fb->Dpi.X/96), fb->Height/(fb->Dpi.Y/96)}, image);
    CGImageRelease(image);
    CGColorSpaceRelease(colorSpace);
    CGDataProviderRelease(dataProvider);
    
    [ctx restoreGraphicsState];

}

- (void)drawRect:(NSRect)dirtyRect
{
    _parent->BaseEvents->RunRenderPriorityJobs();
    @synchronized (self) {
        if(_swRenderedFrame != NULL)
        {
            [self drawFb: &_swRenderedFrameBuffer];
            return;
        }
    }
    
    auto logicalSize = [self frame].size;
    auto pixelSize = [self convertSizeToBacking:logicalSize];
    int w = pixelSize.width;
    int h = pixelSize.height;
    int stride = w * 4;
    void*ptr = malloc(h * stride);
    AvnFramebuffer fb = {
        .Data = ptr,
        .Stride = stride,
        .Width = w,
        .Height = h,
        .PixelFormat = kAvnRgba8888,
        .Dpi = AvnVector { .X = w / logicalSize.width * 96, .Y = h / logicalSize.height * 96}
    };
    _parent->BaseEvents->SoftwareDraw(&fb);
    [self drawFb: &fb];
    free(ptr);
}

-(void) redrawSelf
{
    @autoreleasepool
    {
        @synchronized(self)
        {
            if(!_queuedDisplayFromThread)
                return;
            _queuedDisplayFromThread = false;
        }
        [self setNeedsDisplayInRect:[self frame]];
        [self display];
        
    }
}

-(void) setSwRenderedFrame: (AvnFramebuffer*) fb dispose: (IUnknown*) dispose
{
    @autoreleasepool {
        @synchronized (self) {
            _swRenderedFrame = dispose;
            _swRenderedFrameBuffer = *fb;
            if(!_queuedDisplayFromThread)
            {
                _queuedDisplayFromThread = true;
                [self performSelector:@selector(redrawSelf) onThread:[NSThread mainThread] withObject:NULL waitUntilDone:false modes: AllLoopModes];
            }
        }
    }
}

- (AvnPoint) translateLocalPoint:(AvnPoint)pt
{
    pt.Y = [self bounds].size.height - pt.Y;
    return pt;
}

- (AvnPoint)toAvnPoint:(CGPoint)p
{
    AvnPoint result;
    
    result.X = p.x;
    result.Y = p.y;
    
    return result;
}

- (void) viewDidChangeBackingProperties
{
    _parent->BaseEvents->ScalingChanged([_parent->Window backingScaleFactor]);
    [super viewDidChangeBackingProperties];
}

- (void)mouseEvent:(NSEvent *)event withType:(AvnRawMouseEventType) type
{
    [self becomeFirstResponder];
    auto localPoint = [self convertPoint:[event locationInWindow] toView:self];
    auto avnPoint = [self toAvnPoint:localPoint];
    auto point = [self translateLocalPoint:avnPoint];
    AvnVector delta;
    
    if(type == Wheel)
    {
        delta.X = [event scrollingDeltaX] / 50;
        delta.Y = [event scrollingDeltaY] / 50;
        
        if(delta.X == 0 && delta.Y == 0)
        {
            return;
        }
    }
    
    auto timestamp = [event timestamp] * 1000;
    auto modifiers = [self getModifiers:[event modifierFlags]];
    
    [self becomeFirstResponder];
    _parent->BaseEvents->RawMouseEvent(type, timestamp, modifiers, point, delta);
    [super mouseMoved:event];
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
    _lastMouseDownEvent = nullptr;
    
    [super mouseDown:event];
}

- (void)otherMouseDown:(NSEvent *)event
{
    _isMiddlePressed = true;
    _lastMouseDownEvent = event;
    [self mouseEvent:event withType:MiddleButtonDown];
    _lastMouseDownEvent = nullptr;
    
    [super otherMouseDown:event];
}

- (void)rightMouseDown:(NSEvent *)event
{
    _isRightPressed = true;
    _lastMouseDownEvent = event;
    [self mouseEvent:event withType:RightButtonDown];
    _lastMouseDownEvent = nullptr;
    
    [super rightMouseDown:event];
}

- (void)mouseUp:(NSEvent *)event
{
    _isLeftPressed = false;
    [self mouseEvent:event withType:LeftButtonUp];
    
    [super mouseUp:event];
}

- (void)otherMouseUp:(NSEvent *)event
{
    _isMiddlePressed = false;
    [self mouseEvent:event withType:MiddleButtonUp];
    
    [super otherMouseUp:event];
}

- (void)rightMouseUp:(NSEvent *)event
{
    _isRightPressed = false;
    [self mouseEvent:event withType:RightButtonUp];
    
    [super rightMouseUp:event];
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

- (void)mouseEntered:(NSEvent *)event
{
    _isMouseOver = true;
    [super mouseEntered:event];
}

- (void)mouseExited:(NSEvent *)event
{
    _isMouseOver = false;
    [self mouseEvent:event withType:LeaveWindow];
    [super mouseExited:event];
} 

- (void) keyboardEvent: (NSEvent *) event withType: (AvnRawKeyEventType)type
{ 
    auto key = s_KeyMap[[event keyCode]];
    
    auto timestamp = [event timestamp] * 1000;
    auto modifiers = [self getModifiers:[event modifierFlags]];
     
    _lastKeyHandled = _parent->BaseEvents->RawKeyEvent(type, timestamp, modifiers, key);
}

- (BOOL)performKeyEquivalent:(NSEvent *)event
{
    return _lastKeyHandled;
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
        _lastKeyHandled = _parent->BaseEvents->RawTextInputEvent(0, [string UTF8String]);
    }
}

- (NSUInteger)characterIndexForPoint:(NSPoint)point
{
    return 0;
}

- (NSRect)firstRectForCharacterRange:(NSRange)range actualRange:(NSRangePointer)actualRange
{
    CGRect result;
    
    return result;
}
@end


@implementation AvnWindow
{
    ComPtr<WindowBaseImpl> _parent;
    bool _canBecomeKeyAndMain;
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
    else
    {
        [self orderOut:self];
        [NSApp endModalSession:session];
    }
}

-(void) setCanBecomeKeyAndMain
{
    _canBecomeKeyAndMain = true;
}

-(AvnWindow*)  initWithParent: (WindowBaseImpl*) parent
{
    self = [super init];
    _parent = parent;
    [self setDelegate:self];
    return self;
}

-(BOOL)canBecomeKeyWindow
{
    return _canBecomeKeyAndMain;
}

-(BOOL)canBecomeMainWindow
{
    return _canBecomeKeyAndMain;
}

-(void)becomeKeyWindow
{
    _parent->BaseEvents->Activated();
    [super becomeKeyWindow];
}

- (BOOL)windowShouldZoom:(NSWindow *)window toFrame:(NSRect)newFrame
{
    return true;
}

-(void)resignKeyWindow
{
    _parent->BaseEvents->Deactivated();
    [super resignKeyWindow];
}

- (void)windowDidMove:(NSNotification *)notification
{
    AvnPoint position;
    _parent->GetPosition(&position);
    _parent->BaseEvents->PositionChanged(position);
}

// TODO this breaks resizing.
/*- (void)windowDidResize:(NSNotification *)notification
{
    
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}*/
@end

class PopupImpl : public WindowBaseImpl, public IAvnPopup
{
private:
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    INTERFACE_MAP_ENTRY(IAvnPopup, IID_IAvnPopup)
    END_INTERFACE_MAP()
    ComPtr<IAvnWindowEvents> WindowEvents;
    PopupImpl(IAvnWindowEvents* events) : WindowBaseImpl(events)
    {
        WindowEvents = events;
        [Window setLevel:NSPopUpMenuWindowLevel];
    }
    
protected:
    virtual NSWindowStyleMask GetStyle()
    {
        return NSWindowStyleMaskBorderless;
    }
};

extern IAvnPopup* CreateAvnPopup(IAvnWindowEvents*events)
{
    IAvnPopup* ptr = dynamic_cast<IAvnPopup*>(new PopupImpl(events));
    return ptr;
}

class ModalDisposable : public ComUnknownObject
{
    NSModalSession _session;
    AvnWindow* _window;
    
    void Dispose ()
    {
        [_window orderOut:_window];
        [NSApp endModalSession:_session];
    }
    
public:
    ModalDisposable(AvnWindow* window, NSModalSession session)
    {
        _session = session;
        _window = window;
    }
    
    virtual ~ModalDisposable()
    {
        Dispose();
    }
};

class WindowImpl : public WindowBaseImpl, public IAvnWindow, public IWindowStateChanged
{
private:
    bool _canResize = true;
    bool _hasDecorations = true;
    CGRect _lastUndecoratedFrame;
    AvnWindowState _lastWindowState;
    
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    INTERFACE_MAP_ENTRY(IAvnWindow, IID_IAvnWindow)
    END_INTERFACE_MAP()
    ComPtr<IAvnWindowEvents> WindowEvents;
    WindowImpl(IAvnWindowEvents* events) : WindowBaseImpl(events)
    {
        WindowEvents = events;
        [Window setCanBecomeKeyAndMain];
    }
    
    virtual HRESULT ShowDialog (IUnknown**ppv)
    {
        @autoreleasepool
        {
            if(ppv == nullptr)
            {
                return E_POINTER;
            }
            
            auto session = [NSApp beginModalSessionForWindow:Window];
            auto disposable = new ModalDisposable(Window, session);
            *ppv = disposable;
            
            SetPosition(lastPositionSet);
            UpdateStyle();
            
            [Window pollModalSession:session];
            
            return S_OK;
        }
    }
    
    void WindowStateChanged ()
    {
        AvnWindowState state;
        GetWindowState(&state);
        WindowEvents->WindowStateChanged(state);
    }
    
    bool UndecoratedIsMaximized ()
    {
        return CGRectEqualToRect([Window frame], [Window screen].visibleFrame);
    }
    
    bool IsZoomed ()
    {
        return _hasDecorations ? [Window isZoomed] : UndecoratedIsMaximized();
    }
    
    void DoZoom()
    {
        if (_hasDecorations)
        {
            [Window performZoom:Window];
        }
        else
        {
            if (!UndecoratedIsMaximized())
            {
                _lastUndecoratedFrame = [Window frame];
            }
            
            [Window zoom:Window];
        }
    }
    
    virtual HRESULT SetCanResize(bool value)
    {
        _canResize = value;
        UpdateStyle();
        return S_OK;
    }
    
    virtual HRESULT SetHasDecorations(bool value)
    {
        _hasDecorations = value;
        UpdateStyle();
        return S_OK;
    }
    
    virtual HRESULT GetWindowState (AvnWindowState*ret)
    {
        if(ret == nullptr)
        {
            return E_POINTER;
        }
        
        if([Window isMiniaturized])
        {
            *ret = Minimized;
            return S_OK;
        }
        
        if([Window isZoomed])
        {
            *ret = Maximized;
            return S_OK;
        }
        
        *ret = Normal;
        
        return S_OK;
    }
    
    virtual HRESULT SetWindowState (AvnWindowState state)
    {
        switch (state) {
            case Maximized:
                if([Window isMiniaturized])
                {
                    [Window deminiaturize:Window];
                }
                
                if(!IsZoomed())
                {
                    DoZoom();
                }
                break;
                
            case Minimized:
                [Window miniaturize:Window];
                break;
                
            default:
                if([Window isMiniaturized])
                {
                    [Window deminiaturize:Window];
                }
                
                if(IsZoomed())
                {
                    DoZoom();
                }
                break;
        }
        
        return S_OK;
    }
    
protected:
    virtual void OnResized ()
    {
        auto windowState = [Window isMiniaturized] ? Minimized
        : (IsZoomed() ? Maximized : Normal);
        
        if (windowState != _lastWindowState)
        {
            _lastWindowState = windowState;
            
            WindowEvents->WindowStateChanged(windowState);
        }
    }
    
    virtual NSWindowStyleMask GetStyle()
    {
        unsigned long s = NSWindowStyleMaskBorderless;
        if(_hasDecorations)
            s = s | NSWindowStyleMaskTitled | NSWindowStyleMaskClosable | NSWindowStyleMaskMiniaturizable;
        if(_canResize)
            s = s | NSWindowStyleMaskResizable;
        return s;
    }
};

extern IAvnWindow* CreateAvnWindow(IAvnWindowEvents*events)
{
    IAvnWindow* ptr = dynamic_cast<IAvnWindow*>(new WindowImpl(events));
    return ptr;
}
