//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <AppKit/AppKit.h>
#import <Cocoa/Cocoa.h>
#include "common.h"
#include "AvnView.h"
#include "menu.h"
#include "automation.h"
#include "cursor.h"
#include "ResizeScope.h"
#include "AutoFitContentView.h"
#import "WindowProtocol.h"
#import "WindowInterfaces.h"
#include "WindowBaseImpl.h"
#include "AvnTextInputMethod.h"
#include "AvnView.h"


WindowBaseImpl::~WindowBaseImpl() {
    View = nullptr;
    Window = nullptr;
}

WindowBaseImpl::WindowBaseImpl(IAvnWindowBaseEvents *events, bool usePanel) {
    _shown = false;
    _inResize = false;
    BaseEvents = events;
    View = [[AvnView alloc] initWithParent:this];
    InputMethod = new AvnTextInputMethod(View);
    StandardContainer = [[AutoFitContentView new] initWithContent:View];

    lastPositionSet = { 0, 0 };
    hasPosition = false;
    lastSize = NSSize { 100, 100 };
    lastMaxSize = NSSize { CGFLOAT_MAX, CGFLOAT_MAX};
    lastMinSize = NSSize { 0, 0 };
    lastMenu = nullptr;
    
    CreateNSWindow(usePanel);
    
    [Window setContentView:StandardContainer];
    [Window setBackingType:NSBackingStoreBuffered];
    [Window setContentMinSize:lastMinSize];
    [Window setContentMaxSize:lastMaxSize];
    [Window setOpaque:false];
}

HRESULT WindowBaseImpl::ObtainNSViewHandle(void **ret) {
    START_COM_CALL;

    if (ret == nullptr) {
        return E_POINTER;
    }

    *ret = (__bridge void *) View;

    return S_OK;
}

HRESULT WindowBaseImpl::ObtainNSViewHandleRetained(void **ret) {
    START_COM_CALL;

    if (ret == nullptr) {
        return E_POINTER;
    }

    *ret = (__bridge_retained void *) View;

    return S_OK;
}

NSWindow *WindowBaseImpl::GetNSWindow() {
    return Window;
}

AvnView *WindowBaseImpl::GetNSView() {
    return View;
}

HRESULT WindowBaseImpl::ObtainNSWindowHandleRetained(void **ret) {
    START_COM_CALL;

    if (ret == nullptr) {
        return E_POINTER;
    }

    *ret = (__bridge_retained void *) Window;

    return S_OK;
}

HRESULT WindowBaseImpl::Show(bool activate, bool isDialog) {
    START_COM_CALL;

    @autoreleasepool {
        [Window setContentSize:lastSize];
        
        if(hasPosition)
        {
            SetPosition(lastPositionSet);
        } else
        {
            [Window center];
        }

        // When showing a window, disallow fullscreen because if the window is being
        // shown while another window is fullscreen, macOS will briefly transition this
        // new window to fullscreen and then it will be transitioned back. This breaks
        // input events for a short time as described in this issue:
        //
        // https://yt.avaloniaui.net/issue/OUTSYSTEMS-40
        //
        // We restore the collection behavior at the end of this method.
        auto collectionBehavior = [Window collectionBehavior];
        [Window setCollectionBehavior:collectionBehavior & ~NSWindowCollectionBehaviorFullScreenPrimary];

        UpdateStyle();
        
        [Window invalidateShadow];

        if (ShouldTakeFocusOnShow() && activate) {
            [Window orderFront:Window];
            [Window makeKeyAndOrderFront:Window];
            [Window makeFirstResponder:View];
            [NSApp activateIgnoringOtherApps:YES];
        } else {
            [Window orderFront:Window];
        }

        _shown = true;
        [Window setCollectionBehavior:collectionBehavior];
        return S_OK;
    }
}

bool WindowBaseImpl::IsShown ()
{
    return _shown;
}

bool WindowBaseImpl::ShouldTakeFocusOnShow() {
    return true;
}

HRESULT WindowBaseImpl::ObtainNSWindowHandle(void **ret) {
    START_COM_CALL;

    if (ret == nullptr) {
        return E_POINTER;
    }

    *ret = (__bridge void *) Window;

    return S_OK;
}

HRESULT WindowBaseImpl::Hide() {
    START_COM_CALL;

    @autoreleasepool {
        if (Window != nullptr) {
            auto frame = [Window frame];

            AvnPoint point;
            point.X = frame.origin.x;
            point.Y = frame.origin.y + frame.size.height;

            lastPositionSet = ConvertPointY(point);
            hasPosition = true;
            [Window orderOut:Window];
        }

        return S_OK;
    }
}

HRESULT WindowBaseImpl::Activate() {
    START_COM_CALL;

    @autoreleasepool {
        if (Window != nullptr) {
            [Window makeKeyAndOrderFront:nil];
            [NSApp activateIgnoringOtherApps:YES];
        }
    }

    return S_OK;
}

HRESULT WindowBaseImpl::SetTopMost(bool value) {
    START_COM_CALL;

    @autoreleasepool {
        [Window setLevel:value ? NSFloatingWindowLevel : NSNormalWindowLevel];

        return S_OK;
    }
}

HRESULT WindowBaseImpl::Close() {
    START_COM_CALL;

    @autoreleasepool {
        if (Window != nullptr) {
            auto window = Window;
            Window = nullptr;

            try {
                // Seems to throw sometimes on application exit.
                [window close];
            }
            catch (NSException *) {}
        }

        return S_OK;
    }
}

HRESULT WindowBaseImpl::GetClientSize(AvnSize *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr)
            return E_POINTER;

        ret->Width = lastSize.width;
        ret->Height = lastSize.height;

        return S_OK;
    }
}

HRESULT WindowBaseImpl::GetFrameSize(AvnSize *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr)
            return E_POINTER;

        if(Window != nullptr && _shown){
            auto frame = [Window frame];
            ret->Width = frame.size.width;
            ret->Height = frame.size.height;
        }

        return S_OK;
    }
}

HRESULT WindowBaseImpl::GetScaling(double *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr)
            return E_POINTER;

        if (Window == nullptr) {
            *ret = 1;
            return S_OK;
        }

        *ret = [Window backingScaleFactor];
        return S_OK;
    }
}

HRESULT WindowBaseImpl::SetMinMaxSize(AvnSize minSize, AvnSize maxSize) {
    START_COM_CALL;

    @autoreleasepool {
        lastMinSize = ToNSSize(minSize);
        lastMaxSize = ToNSSize(maxSize);

        if(Window != nullptr) {
            [Window setContentMinSize:lastMinSize];
            [Window setContentMaxSize:lastMaxSize];
        }

        return S_OK;
    }
}

HRESULT WindowBaseImpl::Resize(double x, double y, AvnPlatformResizeReason reason) {
    if (_inResize) {
        return S_OK;
    }

    _inResize = true;

    START_COM_CALL;
    auto resizeBlock = ResizeScope(View, reason);

    @autoreleasepool {
        auto maxSize = lastMaxSize;
        auto minSize = lastMinSize;

        if (x < minSize.width) {
            x = minSize.width;
        }

        if (y < minSize.height) {
            y = minSize.height;
        }

        if (x > maxSize.width) {
            x = maxSize.width;
        }

        if (y > maxSize.height) {
            y = maxSize.height;
        }

        @try {
            if(x != lastSize.width || y != lastSize.height)
            {
                if (!_shown) {
                    auto screenSize = [Window screen].visibleFrame.size;

                    if (x > screenSize.width) {
                        x = screenSize.width;
                    }

                    if (y > screenSize.height) {
                        y = screenSize.height;
                    }
                }

                lastSize = NSSize{x, y};

                [Window setContentSize:lastSize];
                [Window invalidateShadow];
            }
        }
        @finally {
            _inResize = false;
        }

        return S_OK;
    }
}

HRESULT WindowBaseImpl::Invalidate(__attribute__((unused)) AvnRect rect) {
    START_COM_CALL;

    @autoreleasepool {
        [View setNeedsDisplayInRect:[View frame]];

        return S_OK;
    }
}

HRESULT WindowBaseImpl::SetMainMenu(IAvnMenu *menu) {
    START_COM_CALL;

    auto nativeMenu = dynamic_cast<AvnAppMenu *>(menu);

    lastMenu = nativeMenu->GetNative();

    if(Window != nullptr) {
        [GetWindowProtocol() applyMenu:lastMenu];

        if ([Window isKeyWindow]) {
            [GetWindowProtocol() showWindowMenuWithAppMenu];
        }
    }

    return S_OK;
}

HRESULT WindowBaseImpl::BeginMoveDrag() {
    START_COM_CALL;

    @autoreleasepool {
        auto lastEvent = [View lastMouseDownEvent];

        if (lastEvent == nullptr) {
            return S_OK;
        }

        [Window performWindowDragWithEvent:lastEvent];

        return S_OK;
    }
}

HRESULT WindowBaseImpl::BeginResizeDrag(__attribute__((unused)) AvnWindowEdge edge) {
    START_COM_CALL;

    return S_OK;
}

HRESULT WindowBaseImpl::GetPosition(AvnPoint *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        if(Window != nullptr) {
            auto frame = [Window frame];

            ret->X = frame.origin.x;
            ret->Y = frame.origin.y + frame.size.height;

            *ret = ConvertPointY(*ret);
        } else
        {
            *ret = lastPositionSet;
        }

        return S_OK;
    }
}

HRESULT WindowBaseImpl::SetPosition(AvnPoint point) {
    START_COM_CALL;

    @autoreleasepool {
        lastPositionSet = point;
        hasPosition = true;

        if(Window != nullptr) {
            [Window setFrameTopLeftPoint:ToNSPoint(ConvertPointY(point))];
        }

        return S_OK;
    }
}

HRESULT WindowBaseImpl::PointToClient(AvnPoint point, AvnPoint *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        point = ConvertPointY(point);
        NSRect convertRect = [Window convertRectFromScreen:NSMakeRect(point.X, point.Y, 0.0, 0.0)];
        auto viewPoint = NSMakePoint(convertRect.origin.x, convertRect.origin.y);

        *ret = [View translateLocalPoint:ToAvnPoint(viewPoint)];

        return S_OK;
    }
}

HRESULT WindowBaseImpl::PointToScreen(AvnPoint point, AvnPoint *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        auto cocoaViewPoint = ToNSPoint([View translateLocalPoint:point]);
        NSRect convertRect = [Window convertRectToScreen:NSMakeRect(cocoaViewPoint.x, cocoaViewPoint.y, 0.0, 0.0)];
        auto cocoaScreenPoint = NSPointFromCGPoint(NSMakePoint(convertRect.origin.x, convertRect.origin.y));
        *ret = ConvertPointY(ToAvnPoint(cocoaScreenPoint));

        return S_OK;
    }
}

HRESULT WindowBaseImpl::SetCursor(IAvnCursor *cursor) {
    START_COM_CALL;

    @autoreleasepool {
        Cursor *avnCursor = dynamic_cast<Cursor *>(cursor);
        this->cursor = avnCursor->GetNative();
        UpdateCursor();

        if (avnCursor->IsHidden()) {
            [NSCursor hide];
        } else {
            [NSCursor unhide];
        }

        return S_OK;
    }
}

void WindowBaseImpl::UpdateCursor() {
    if (cursor != nil) {
        [cursor set];
    }
}

HRESULT WindowBaseImpl::CreateSoftwareRenderTarget(IAvnSoftwareRenderTarget **ppv) {
    START_COM_CALL;

    if(![NSThread isMainThread])
        return COR_E_INVALIDOPERATION;

    if (View == NULL)
        return E_FAIL;

    auto target = [[IOSurfaceRenderTarget alloc] initWithOpenGlContext: nil];
    *ppv = [target createSoftwareRenderTarget];
    [View setRenderTarget: target];
    return S_OK;
}

HRESULT WindowBaseImpl::CreateGlRenderTarget(IAvnGlContext* glContext, IAvnGlSurfaceRenderTarget **ppv) {
    START_COM_CALL;

    if(![NSThread isMainThread])
        return COR_E_INVALIDOPERATION;

    if (View == NULL)
        return E_FAIL;

    auto target = [[IOSurfaceRenderTarget alloc] initWithOpenGlContext: glContext];
    *ppv = [target createSurfaceRenderTarget];
    [View setRenderTarget: target];
    return S_OK;
}

HRESULT WindowBaseImpl::CreateMetalRenderTarget(IAvnMetalDevice* device, IAvnMetalRenderTarget **ppv) {
    START_COM_CALL;

    if(![NSThread isMainThread])
        return COR_E_INVALIDOPERATION;

    if (View == NULL)
        return E_FAIL;

    auto target = [[MetalRenderTarget alloc] initWithDevice: device];
    [View setRenderTarget: target];
    [target getRenderTarget: ppv];
    return S_OK;
}

HRESULT WindowBaseImpl::CreateNativeControlHost(IAvnNativeControlHost **retOut) {
    START_COM_CALL;

    if (View == NULL)
        return E_FAIL;
    *retOut = ::CreateNativeControlHost(View);
    return S_OK;
}

HRESULT WindowBaseImpl::SetTransparencyMode(AvnWindowTransparencyMode mode) {
    START_COM_CALL;

    [Window setBackgroundColor: (mode != Transparent ? [NSColor windowBackgroundColor] : [NSColor clearColor])];
    [StandardContainer ShowBlur: mode == Blur];

    return S_OK;
}

HRESULT WindowBaseImpl::SetFrameThemeVariant(AvnPlatformThemeVariant variant) {
    START_COM_CALL;

    NSAppearanceName appearanceName;
    if (@available(macOS 10.14, *))
    {
        appearanceName = variant == AvnPlatformThemeVariant::Dark ? NSAppearanceNameDarkAqua : NSAppearanceNameAqua;
    }
    else
    {
        appearanceName = variant == AvnPlatformThemeVariant::Dark ? NSAppearanceNameVibrantDark : NSAppearanceNameAqua;
    }

    [Window setAppearance: [NSAppearance appearanceNamed: appearanceName]];

    return S_OK;
}

HRESULT WindowBaseImpl::BeginDragAndDropOperation(AvnDragDropEffects effects, AvnPoint point, IAvnClipboard *clipboard, IAvnDndResultCallback *cb, void *sourceHandle) {
    START_COM_CALL;

    auto item = TryGetPasteboardItem(clipboard);
    [item setString:@"" forType:GetAvnCustomDataType()];
    if (item == nil)
        return E_INVALIDARG;
    if (View == NULL)
        return E_FAIL;

    auto nsevent = [NSApp currentEvent];
    auto nseventType = [nsevent type];

    // If current event isn't a mouse one (probably due to malfunctioning user app)
    // attempt to forge a new one
    if (!((nseventType >= NSEventTypeLeftMouseDown && nseventType <= NSEventTypeMouseExited)
            || (nseventType >= NSEventTypeOtherMouseDown && nseventType <= NSEventTypeOtherMouseDragged))) {
        NSRect convertRect = [Window convertRectToScreen:NSMakeRect(point.X, point.Y, 0.0, 0.0)];
        auto nspoint = NSMakePoint(convertRect.origin.x, convertRect.origin.y);
        CGPoint cgpoint = NSPointToCGPoint(nspoint);
        auto cgevent = CGEventCreateMouseEvent(NULL, kCGEventLeftMouseDown, cgpoint, kCGMouseButtonLeft);
        nsevent = [NSEvent eventWithCGEvent:cgevent];
        CFRelease(cgevent);
    }

    auto dragItem = [[NSDraggingItem alloc] initWithPasteboardWriter:item];

    auto dragItemImage = [NSImage imageNamed:NSImageNameMultipleDocuments];
    NSRect dragItemRect = {(float) point.X, (float) point.Y, [dragItemImage size].width, [dragItemImage size].height};
    [dragItem setDraggingFrame:dragItemRect contents:dragItemImage];

    int op = 0;
    int ieffects = (int) effects;
    if ((ieffects & (int) AvnDragDropEffects::Copy) != 0)
        op |= NSDragOperationCopy;
    if ((ieffects & (int) AvnDragDropEffects::Link) != 0)
        op |= NSDragOperationLink;
    if ((ieffects & (int) AvnDragDropEffects::Move) != 0)
        op |= NSDragOperationMove;
    [View beginDraggingSessionWithItems:@[dragItem] event:nsevent
                                 source:CreateDraggingSource((NSDragOperation) op, cb, sourceHandle)];
    return S_OK;
}

bool WindowBaseImpl::IsModal() {
    return false;
}

void WindowBaseImpl::UpdateStyle() {
    [Window setStyleMask:CalculateStyleMask()];
}

void WindowBaseImpl::CleanNSWindow() {
    if(Window != nullptr) {
        [GetWindowProtocol() disconnectParent];
        [Window close];
        Window = nullptr;
    }
}

void WindowBaseImpl::CreateNSWindow(bool usePanel) {
    if (usePanel) {
        Window = [[AvnPanel alloc] initWithParent:this contentRect:NSRect{0, 0, lastSize} styleMask:NSWindowStyleMaskBorderless];
        [Window setHidesOnDeactivate:false];
    } else {
        Window = [[AvnWindow alloc] initWithParent:this contentRect:NSRect{0, 0, lastSize} styleMask:NSWindowStyleMaskBorderless];
    }
}

id <AvnWindowProtocol> WindowBaseImpl::GetWindowProtocol() {
    if(Window == nullptr)
    {
        return nullptr;
    }

    return (id <AvnWindowProtocol>) Window;
}

void WindowBaseImpl::BringToFront()
{
    // do nothing.
}

HRESULT WindowBaseImpl::GetInputMethod(IAvnTextInputMethod **retOut) {
    START_COM_CALL;

    *retOut = InputMethod;

    return S_OK;
}

extern IAvnWindow* CreateAvnWindow(IAvnWindowEvents*events)
{
    @autoreleasepool
    {
        IAvnWindow* ptr = (IAvnWindow*)new WindowImpl(events);
        return ptr;
    }
}
