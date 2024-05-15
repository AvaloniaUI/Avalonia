//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#ifndef AVALONIA_NATIVE_OSX_WINDOWBASEIMPL_H
#define AVALONIA_NATIVE_OSX_WINDOWBASEIMPL_H

#include "rendertarget.h"
#include "INSWindowHolder.h"
#include "AvnTextInputMethod.h"
#include <list>

@class AutoFitContentView;
@class AvnMenu;
@protocol AvnWindowProtocol;

class WindowBaseImpl : public virtual ComObject,
                       public virtual IAvnWindowBase,
                       public INSWindowHolder {

public:
    FORWARD_IUNKNOWN()

BEGIN_INTERFACE_MAP()
        INTERFACE_MAP_ENTRY(IAvnWindowBase, IID_IAvnWindowBase)
    END_INTERFACE_MAP()

    virtual ~WindowBaseImpl();

    WindowBaseImpl(IAvnWindowBaseEvents *events, bool usePanel = false, bool isPopup = false);

    virtual HRESULT ObtainNSWindowHandle(void **ret) override;

    virtual HRESULT ObtainNSWindowHandleRetained(void **ret) override;

    virtual HRESULT ObtainNSViewHandle(void **ret) override;

    virtual HRESULT ObtainNSViewHandleRetained(void **ret) override;

    virtual NSWindow *GetNSWindow() override;

    virtual AvnView *GetNSView() override;

    virtual HRESULT Show(bool activate, bool isDialog) override;

    virtual bool IsShown ();

    virtual bool ShouldTakeFocusOnShow();

    virtual HRESULT Hide() override;

    virtual HRESULT Activate() override;

    virtual HRESULT SetTopMost(bool value) override;

    virtual HRESULT Close() override;

    virtual HRESULT GetClientSize(AvnSize *ret) override;

    virtual HRESULT GetFrameSize(AvnSize *ret) override;

    virtual HRESULT GetScaling(double *ret) override;

    virtual HRESULT SetMinMaxSize(AvnSize minSize, AvnSize maxSize) override;

    virtual HRESULT Resize(double x, double y, AvnPlatformResizeReason reason) override;

    virtual HRESULT Invalidate(__attribute__((unused)) AvnRect rect) override;

    virtual HRESULT SetMainMenu(IAvnMenu *menu) override;

    virtual HRESULT BeginMoveDrag() override;

    virtual HRESULT BeginResizeDrag(__attribute__((unused)) AvnWindowEdge edge) override;

    virtual HRESULT GetPosition(AvnPoint *ret) override;

    virtual HRESULT SetPosition(AvnPoint point) override;

    virtual HRESULT PointToClient(AvnPoint point, AvnPoint *ret) override;

    virtual HRESULT PointToScreen(AvnPoint point, AvnPoint *ret) override;

    virtual HRESULT SetCursor(IAvnCursor *cursor) override;
                           
    virtual HRESULT SetParent (IAvnWindowBase* parent) override;

    virtual void UpdateCursor();

    virtual HRESULT CreateSoftwareRenderTarget(IAvnSoftwareRenderTarget **ppv) override;

    virtual HRESULT CreateGlRenderTarget(IAvnGlContext* glContext, IAvnGlSurfaceRenderTarget **ppv) override;

    virtual HRESULT CreateMetalRenderTarget(IAvnMetalDevice* device, IAvnMetalRenderTarget **ppv) override;

    virtual HRESULT CreateNativeControlHost(IAvnNativeControlHost **retOut) override;

    virtual HRESULT SetTransparencyMode(AvnWindowTransparencyMode mode) override;

    virtual HRESULT SetFrameThemeVariant(AvnPlatformThemeVariant variant) override;

    virtual HRESULT BeginDragAndDropOperation(AvnDragDropEffects effects, AvnPoint point,
            IAvnClipboard *clipboard, IAvnDndResultCallback *cb,
            void *sourceHandle) override;

    virtual bool IsModal();

    id<AvnWindowProtocol> GetWindowProtocol ();
                           
    virtual void BringToFront ();
                           
    virtual HRESULT GetInputMethod(IAvnTextInputMethod **retOut) override;

    virtual bool CanZoom() { return false; }
                           
protected:
    std::list<WindowBaseImpl*> _children;
    bool _isModal;
                           
    virtual NSWindowStyleMask CalculateStyleMask() = 0;
    virtual void UpdateStyle();

private:
    void CreateNSWindow (bool usePanel, bool isPopup);
    void CleanNSWindow ();
    NSCursor *cursor;
    bool hasPosition;
    NSSize lastSize;
    NSSize lastMinSize;
    NSSize lastMaxSize;
    AvnMenu* lastMenu;
    bool _inResize;

protected:
    AvnPoint lastPositionSet;
    AutoFitContentView *StandardContainer;
    bool _shown;

public:
    NSObject <IRenderTarget> *currentRenderTarget;
    NSWindow * Window;
    ComPtr<IAvnWindowBaseEvents> BaseEvents;
    ComPtr<AvnTextInputMethod> InputMethod;
    AvnView *View;
    WindowBaseImpl* Parent;
};

#endif //AVALONIA_NATIVE_OSX_WINDOWBASEIMPL_H
