#import <AppKit/AppKit.h>
#import <Cocoa/Cocoa.h>
#include "automation.h"
#include "cursor.h"
#include "AutoFitContentView.h"
#include "TopLevelImpl.h"
#include "AvnTextInputMethod.h"
#include "AvnView.h"
#include "common.h"
#include "clipboard.h"

TopLevelImpl::~TopLevelImpl() {
    View = nullptr;
}

TopLevelImpl::TopLevelImpl(const ComPtr<IAvnTopLevelEvents>& events) {
    TopLevelEvents = events;
    
    View = [[AvnView alloc] initWithParent:comself(this)];
    InputMethod = comnew<AvnTextInputMethod>(View);
}

HRESULT TopLevelImpl::GetScaling(double *ret) {
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
    @autoreleasepool {
        if (ret == nullptr)
            return E_POINTER;

        NSRect frame = [View frame];
        
        ret->Width = frame.size.width;
        ret->Height = frame.size.height;

        return S_OK;
    }
}

HRESULT TopLevelImpl::GetInputMethod(ComPtr<IAvnTextInputMethod>& retOut) {
    retOut = InputMethod;

    return S_OK;
}

HRESULT TopLevelImpl::ObtainNSViewHandle(void **ret) {
    if (ret == nullptr) {
        return E_POINTER;
    }

    *ret = (__bridge void *) View;

    return S_OK;
}

HRESULT TopLevelImpl::ObtainNSViewHandleRetained(void **ret) {
    if (ret == nullptr) {
        return E_POINTER;
    }

    *ret = (__bridge_retained void *) View;

    return S_OK;
}

HRESULT TopLevelImpl::SetCursor(const ComPtr<IAvnCursor>& cursor) {
    @autoreleasepool {
        auto avnCursor = cursor.dynamicCast<Cursor>();
        if (!avnCursor)
            return E_INVALIDARG;
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

HRESULT TopLevelImpl::CreateSoftwareRenderTarget(ComPtr<IAvnSoftwareRenderTarget>& ppv) {
    if(![NSThread isMainThread])
        return COR_E_INVALIDOPERATION;

    if (View == NULL)
        return E_FAIL;

    auto target = [[IOSurfaceRenderTarget alloc] initWithOpenGlContext: nil];
    ppv = [target createSoftwareRenderTarget];
    [View setRenderTarget: target];
    return S_OK;
}

HRESULT TopLevelImpl::CreateGlRenderTarget(const ComPtr<IAvnGlContext>& glContext, ComPtr<IAvnGlSurfaceRenderTarget>& ppv) {
    if(![NSThread isMainThread])
        return COR_E_INVALIDOPERATION;

    if (View == NULL)
        return E_FAIL;

    auto target = [[IOSurfaceRenderTarget alloc] initWithOpenGlContext: glContext];
    ppv = [target createSurfaceRenderTarget];
    [View setRenderTarget: target];
    return S_OK;
}

HRESULT TopLevelImpl::CreateMetalRenderTarget(const ComPtr<IAvnMetalDevice>& device, ComPtr<IAvnMetalRenderTarget>& ppv) {
    if(![NSThread isMainThread])
        return COR_E_INVALIDOPERATION;

    if (View == NULL)
        return E_FAIL;

    auto target = [[MetalRenderTarget alloc] initWithDevice: device];
    if (target == nil)
        return E_INVALIDARG;
    [View setRenderTarget: target];
    ppv = [target createRenderTarget];
    return S_OK;
}

HRESULT TopLevelImpl::CreateNativeControlHost(ComPtr<IAvnNativeControlHost>& retOut) {
    if (View == NULL)
        return E_FAIL;
    retOut = ::CreateNativeControlHost(View);
    return S_OK;
}

AvnView *TopLevelImpl::GetNSView() {
    return View;
}

HRESULT TopLevelImpl::Invalidate() {
    @autoreleasepool {
        [View setNeedsDisplayInRect:[View frame]];

        return S_OK;
    }
}

HRESULT TopLevelImpl::PointToClient(AvnPoint point, AvnPoint *ret) {
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
    return S_OK;
}

HRESULT TopLevelImpl::GetCurrentDisplayId (CGDirectDisplayID* ret) {
    auto window = [View window];
    *ret = [window.screen av_displayId];

    return S_OK;
}

void TopLevelImpl::UpdateAppearance() {
    
}

HRESULT TopLevelImpl::BeginDragAndDropOperation(
    AvnDragDropEffects effects,
    AvnPoint point,
    const ComPtr<IAvnClipboardDataSource>& source,
    const ComPtr<IAvnDndResultCallback>& callback,
    void* sourceHandle)
{
    if (View == NULL)
        return E_FAIL;

    auto nsevent = [NSApp currentEvent];
    auto nseventType = [nsevent type];

    // If current event isn't a mouse one (probably due to malfunctioning user app)
    // attempt to forge a new one
    if (!((nseventType >= NSEventTypeLeftMouseDown && nseventType <= NSEventTypeMouseExited)
            || (nseventType >= NSEventTypeOtherMouseDown && nseventType <= NSEventTypeOtherMouseDragged))) {
        // For TopLevelImpl, we don't have a Window so we use the View's window
        auto window = [View window];
        if (window != nil) {
            NSRect convertRect = [window convertRectToScreen:NSMakeRect(point.X, point.Y, 0.0, 0.0)];
            auto nspoint = NSMakePoint(convertRect.origin.x, convertRect.origin.y);
            CGPoint cgpoint = NSPointToCGPoint(nspoint);
            auto cgevent = CGEventCreateMouseEvent(NULL, kCGEventLeftMouseDown, cgpoint, kCGMouseButtonLeft);
            nsevent = [NSEvent eventWithCGEvent:cgevent];
            CFRelease(cgevent);
        }
    }

    auto itemCount = source->GetItemCount();
    auto draggingItems = [NSMutableArray<NSDraggingItem*> arrayWithCapacity:itemCount];
    auto dragItemImage = [NSImage imageNamed:NSImageNameMultipleDocuments];
    NSRect dragItemRect = {(float) point.X, (float) point.Y, [dragItemImage size].width, [dragItemImage size].height};

    for (auto i = 0; i < itemCount; ++i)
    {
        ComPtr<IAvnClipboardDataItem> item;
        if(S_OK != source->GetItem(i, item))
            continue;
        auto writeableItem = [[WriteableClipboardItem alloc] initWithItem:item source:source];
        auto draggingItem = [[NSDraggingItem alloc] initWithPasteboardWriter:writeableItem];
        [draggingItem setDraggingFrame:dragItemRect contents:dragItemImage];
        [draggingItems addObject:draggingItem];
    }

    int op = 0;
    int ieffects = (int) effects;
    if ((ieffects & (int) AvnDragDropEffects::Copy) != 0)
        op |= NSDragOperationCopy;
    if ((ieffects & (int) AvnDragDropEffects::Link) != 0)
        op |= NSDragOperationLink;
    if ((ieffects & (int) AvnDragDropEffects::Move) != 0)
        op |= NSDragOperationMove;

    [View beginDraggingSessionWithItems:draggingItems
                                  event:nsevent
                                 source:CreateDraggingSource((NSDragOperation) op, callback, sourceHandle)];
    return S_OK;
}

void TopLevelImpl::SetClientSize(NSSize size){
    [View setFrameSize:size];
}

extern ComPtr<IAvnTopLevel> CreateAvnTopLevel(const ComPtr<IAvnTopLevelEvents>& events)
{
    return comnew<TopLevelImpl>(events);
}
