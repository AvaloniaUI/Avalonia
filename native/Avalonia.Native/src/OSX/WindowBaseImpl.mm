//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <AppKit/AppKit.h>
#include "common.h"
#import "window.h"
#import "AvnView.h"
#include "menu.h"
#include "automation.h"
#import "cursor.h"
#include "ResizeScope.h"
#import "AutoFitContentView.h"

WindowBaseImpl::WindowBaseImpl(IAvnWindowBaseEvents *events, IAvnGlContext *gl) {
    _shown = false;
    _inResize = false;
    BaseEvents = events;
    _glContext = gl;
    renderTarget = [[IOSurfaceRenderTarget alloc] initWithOpenGlContext:gl];
    View = [[AvnView alloc] initWithParent:this];
    StandardContainer = [[AutoFitContentView new] initWithContent:View];

    Window = [[AvnWindow alloc] initWithParent:this];
    [Window setContentView:StandardContainer];

    lastPositionSet.X = 100;
    lastPositionSet.Y = 100;
    _lastTitle = @"";

    [Window setStyleMask:NSWindowStyleMaskBorderless];
    [Window setBackingType:NSBackingStoreBuffered];

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

AvnWindow *WindowBaseImpl::GetNSWindow() {
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
        SetPosition(lastPositionSet);
        UpdateStyle();

        [Window setTitle:_lastTitle];

        if (ShouldTakeFocusOnShow() && activate) {
            [Window orderFront:Window];
            [Window makeKeyAndOrderFront:Window];
            [Window makeFirstResponder:View];
            [NSApp activateIgnoringOtherApps:YES];
        } else {
            [Window orderFront:Window];
        }

        _shown = true;

        return S_OK;
    }
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
            [Window orderOut:Window];
            [Window restoreParentWindow];
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

        auto frame = [View frame];
        ret->Width = frame.size.width;
        ret->Height = frame.size.height;

        return S_OK;
    }
}

HRESULT WindowBaseImpl::GetFrameSize(AvnSize *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr)
            return E_POINTER;

        auto frame = [Window frame];
        ret->Width = frame.size.width;
        ret->Height = frame.size.height;

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
        [Window setContentMinSize:ToNSSize(minSize)];
        [Window setContentMaxSize:ToNSSize(maxSize)];

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
        auto maxSize = [Window contentMaxSize];
        auto minSize = [Window contentMinSize];

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
            if (!_shown) {
                BaseEvents->Resized(AvnSize{x, y}, reason);
            }

            [Window setContentSize:NSSize{x, y}];
            [Window invalidateShadow];
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

    auto nsmenu = nativeMenu->GetNative();

    [Window applyMenu:nsmenu];

    if ([Window isKeyWindow]) {
        [Window showWindowMenuWithAppMenu];
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

        auto frame = [Window frame];

        ret->X = frame.origin.x;
        ret->Y = frame.origin.y + frame.size.height;

        *ret = ConvertPointY(*ret);

        return S_OK;
    }
}

HRESULT WindowBaseImpl::SetPosition(AvnPoint point) {
    START_COM_CALL;

    @autoreleasepool {
        lastPositionSet = point;
        [Window setFrameTopLeftPoint:ToNSPoint(ConvertPointY(point))];

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

HRESULT WindowBaseImpl::ThreadSafeSetSwRenderedFrame(AvnFramebuffer *fb, IUnknown *dispose) {
    START_COM_CALL;

    [View setSwRenderedFrame:fb dispose:dispose];
    return S_OK;
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

HRESULT WindowBaseImpl::CreateGlRenderTarget(IAvnGlSurfaceRenderTarget **ppv) {
    START_COM_CALL;

    if (View == NULL)
        return E_FAIL;
    *ppv = [renderTarget createSurfaceRenderTarget];
    return static_cast<HRESULT>(*ppv == nil ? E_FAIL : S_OK);
}

HRESULT WindowBaseImpl::CreateNativeControlHost(IAvnNativeControlHost **retOut) {
    START_COM_CALL;

    if (View == NULL)
        return E_FAIL;
    *retOut = ::CreateNativeControlHost(View);
    return S_OK;
}

HRESULT WindowBaseImpl::SetBlurEnabled(bool enable) {
    START_COM_CALL;

    [StandardContainer ShowBlur:enable];

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

bool WindowBaseImpl::IsDialog() {
    return false;
}

NSWindowStyleMask WindowBaseImpl::GetStyle() {
    return NSWindowStyleMaskBorderless;
}

void WindowBaseImpl::UpdateStyle() {
    [Window setStyleMask:GetStyle()];
}