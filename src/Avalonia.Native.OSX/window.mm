#include "common.h"

class WindowBaseImpl;

@interface AvnView : NSView
-(AvnView*)  initWithParent: (WindowBaseImpl*) parent;
-(NSEvent*)  lastMouseDownEvent;
-(AvnPoint)translateLocalPoint:(AvnPoint)pt;
@end

@interface AvnWindow : NSWindow <NSWindowDelegate>
-(AvnWindow*) initWithParent: (WindowBaseImpl*) parent;
-(void) setCanBecomeKeyAndMain;
@end

class WindowBaseImpl : public ComSingleObject<IAvnWindowBase, &IID_IAvnWindowBase>
{
public:
    AvnView* View;
    AvnWindow* Window;
    ComPtr<IAvnWindowBaseEvents> BaseEvents;
    WindowBaseImpl(IAvnWindowBaseEvents* events)
    {
        BaseEvents = events;
        View = [[AvnView alloc] initWithParent:this];
        Window = [[AvnWindow alloc] initWithParent:this];
        [Window setStyleMask:NSWindowStyleMaskBorderless];
        [Window setBackingType:NSBackingStoreBuffered];
        [Window setContentView: View];
    }
    
    virtual HRESULT Show()
    {
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
        
        AvnPoint bottomLeft;
        bottomLeft.X = frame.origin.x;
        bottomLeft.Y = frame.origin.y + frame.size.height;
        
        *ret = ConvertPointY(bottomLeft);
        
        return S_OK;
    }
    
    virtual void SetPosition (AvnPoint point)
    {
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
    
protected:
    virtual NSWindowStyleMask GetStyle()
    {
        return NSWindowStyleMaskBorderless;
    }
    
    void UpdateStyle()
    {
        [Window setStyleMask:GetStyle()];
    }
};

@implementation AvnView
{
    ComPtr<WindowBaseImpl> _parent;
    NSTrackingArea* _area;
    bool _isLeftPressed, _isMiddlePressed, _isRightPressed, _isMouseOver;
    NSEvent* _lastMouseDownEvent;
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

- (void)drawRect:(NSRect)dirtyRect
{
    auto logicalSize = [self frame].size;
    auto pixelSize = [self convertSizeToBacking:logicalSize];
    int w = pixelSize.width;
    int h = pixelSize.height;
    int stride = w * 4;
    void*ptr = malloc(h * stride);
    _parent->BaseEvents->SoftwareDraw(ptr, stride, w, h, AvnSize{logicalSize.width, logicalSize.height});
    
    auto colorSpace = CGColorSpaceCreateDeviceRGB();
    auto bctx = CGBitmapContextCreate(ptr, w, h, 8, stride, colorSpace, kCGBitmapByteOrder32Big | kCGImageAlphaPremultipliedLast);
    auto image = CGBitmapContextCreateImage(bctx);
    CGContextRelease(bctx);
    CGColorSpaceRelease(colorSpace);
    
    auto ctx = [NSGraphicsContext currentContext];
    
    [ctx saveGraphicsState];
    auto cgc = [ctx CGContext];
    
    CGContextDrawImage(cgc, CGRect{0,0, logicalSize.width, logicalSize.height}, image);
    CGImageRelease(image);
    
    [ctx restoreGraphicsState];
    free(ptr);
}

- (AvnPoint)translateLocalPoint:(AvnPoint)pt
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

- (void)mouseEvent:(NSEvent *)event withType:(AvnRawMouseEventType) type
{
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
@end


@implementation AvnWindow
{
    ComPtr<WindowBaseImpl> _parent;
    bool _canBecomeKeyAndMain;
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
    [super becomeKeyWindow];
    _parent->BaseEvents->Activated();
}

-(void)resignKeyWindow
{
    _parent->BaseEvents->Deactivated();
    [super resignKeyWindow];
}

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

class WindowImpl : public WindowBaseImpl, public IAvnWindow
{
private:
    bool _canResize = true;
    bool _hasDecorations = true;
    
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
    
protected:
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
