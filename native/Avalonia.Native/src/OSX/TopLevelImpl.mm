#import <AppKit/AppKit.h>
#import <Cocoa/Cocoa.h>
#include "automation.h"
#include "cursor.h"
#include "AutoFitContentView.h"
#include "TopLevelImpl.h"
#include "AvnTextInputMethod.h"
#include "AvnView.h"

TopLevelImpl::~TopLevelImpl() {
    View = nullptr;
}

TopLevelImpl::TopLevelImpl(IAvnTopLevelEvents *events) {
    TopLevelEvents = events;
    
    View = [[AvnView alloc] initWithParent:this];
    InputMethod = new AvnTextInputMethod(View);
}

HRESULT TopLevelImpl::GetScaling(double *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr)
            return E_POINTER;

        if ([View window] == nullptr) {
            *ret = 1;
            return S_OK;
        }

        *ret = [[View window] backingScaleFactor];
        
        return S_OK;
    }
}

HRESULT TopLevelImpl::GetClientSize(AvnSize *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr)
            return E_POINTER;

        NSRect frame = [View frame];
        
        ret->Width = frame.size.width;
        ret->Height = frame.size.height;

        return S_OK;
    }
}

HRESULT TopLevelImpl::GetInputMethod(IAvnTextInputMethod **retOut) {
    START_COM_CALL;

    *retOut = InputMethod;

    return S_OK;
}

HRESULT TopLevelImpl::ObtainNSViewHandle(void **ret) {
    START_COM_CALL;

    if (ret == nullptr) {
        return E_POINTER;
    }

    *ret = (__bridge void *) View;

    return S_OK;
}

HRESULT TopLevelImpl::ObtainNSViewHandleRetained(void **ret) {
    START_COM_CALL;

    if (ret == nullptr) {
        return E_POINTER;
    }

    *ret = (__bridge_retained void *) View;

    return S_OK;
}

HRESULT TopLevelImpl::SetCursor(IAvnCursor *cursor) {
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

void TopLevelImpl::UpdateCursor() {
    if (cursor != nil) {
        [cursor set];
    }
}

HRESULT TopLevelImpl::CreateSoftwareRenderTarget(IAvnSoftwareRenderTarget **ppv) {
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

HRESULT TopLevelImpl::CreateGlRenderTarget(IAvnGlContext* glContext, IAvnGlSurfaceRenderTarget **ppv) {
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

HRESULT TopLevelImpl::CreateMetalRenderTarget(IAvnMetalDevice* device, IAvnMetalRenderTarget **ppv) {
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

HRESULT TopLevelImpl::CreateNativeControlHost(IAvnNativeControlHost **retOut) {
    START_COM_CALL;

    if (View == NULL)
        return E_FAIL;
    *retOut = ::CreateNativeControlHost(View);
    return S_OK;
}

AvnView *TopLevelImpl::GetNSView() {
    return View;
}

HRESULT TopLevelImpl::Invalidate() {
    START_COM_CALL;

    @autoreleasepool {
        [View setNeedsDisplayInRect:[View frame]];

        return S_OK;
    }
}

HRESULT TopLevelImpl::PointToClient(AvnPoint point, AvnPoint *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        auto window = [View window];
        
        if(window == nullptr){
            ret = &point;
            
            return S_OK;
        }
        
        auto frame = [View frame];
        
        auto viewRect = [View convertRect:frame toView:nil];
        
        auto viewScreenRect = [window convertRectToScreen:viewRect];
        
        auto primaryDisplayHeight = NSMaxY([[[NSScreen screens] firstObject] frame]);
        
        //Window coord are bottom to top so we need to adjust by primaryScreenHeight
        auto viewScreenLocation = NSMakePoint(viewScreenRect.origin.x, primaryDisplayHeight - viewScreenRect.origin.y - frame.size.height);
        
        //Substract client point from screen position of the view
        auto localPoint = NSMakePoint(point.X - viewScreenLocation.x, point.Y - viewScreenLocation.y);
        
        point = ToAvnPoint(localPoint);

        *ret = point;

        return S_OK;
    }
}

HRESULT TopLevelImpl::PointToScreen(AvnPoint point, AvnPoint *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }
        
        auto window = [View window];
        
        if(window == nullptr){
            ret = &point;
            
            return S_OK;
        }
        
        auto frame = [View frame];
        
        //Get rect inside current window
        auto viewRect = [View convertRect:frame toView:nil];
        
        //Get screen rect of the view
        auto viewScreenRect = [window convertRectToScreen:viewRect];
               
        auto primaryDisplayHeight = NSMaxY([[[NSScreen screens] firstObject] frame]);
        
        //Window coord are bottom to top so we need to adjust by primaryScreenHeight
        auto viewScreenLocation = NSMakePoint(viewScreenRect.origin.x, primaryDisplayHeight - viewScreenRect.origin.y - frame.size.height);

        //Add client point to screen position of the view
        auto screenPoint = ToAvnPoint(NSMakePoint(viewScreenLocation.x + point.X, viewScreenLocation.y + point.Y));
        
        *ret = screenPoint;

        return S_OK;
    }
}

HRESULT TopLevelImpl::SetTransparencyMode(AvnWindowTransparencyMode mode) {
    START_COM_CALL;

    return S_OK;
}

HRESULT TopLevelImpl::GetCurrentDisplayId (CGDirectDisplayID* ret) {
    START_COM_CALL;

    auto window = [View window];
    *ret = [window.screen av_displayId];

    return S_OK;
}

void TopLevelImpl::UpdateAppearance() {
    
}

void TopLevelImpl::SetClientSize(NSSize size){
    [View setFrameSize:size];
}

extern IAvnTopLevel* CreateAvnTopLevel(IAvnTopLevelEvents* events)
{
    @autoreleasepool
    {
        IAvnTopLevel* ptr = (IAvnTopLevel*)new TopLevelImpl(events);
        return ptr;
    }
}
