//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#ifndef AVALONIA_NATIVE_OSX_WINDOWBASEIMPL_H
#define AVALONIA_NATIVE_OSX_WINDOWBASEIMPL_H

#include "rendertarget.h"
#include "INSWindowHolder.h"
#include "AvnTextInputMethod.h"
#include "TopLevelImpl.h"
#include <list>

@class AvnMenu;
@protocol AvnWindowProtocol;

class WindowBaseImpl : public virtual TopLevelImpl,
                       public virtual IAvnWindowBase,
                       public INSWindowHolder {

public:
    FORWARD_IUNKNOWN()

    BEGIN_INTERFACE_MAP()
        INHERIT_INTERFACE_MAP(TopLevelImpl)
        INTERFACE_MAP_ENTRY(IAvnWindowBase, IID_IAvnWindowBase)
    END_INTERFACE_MAP()

    virtual ~WindowBaseImpl();

    WindowBaseImpl(IAvnWindowBaseEvents *events, bool usePanel = false);

    virtual HRESULT ObtainNSWindowHandle(void **ret) override;

    virtual HRESULT ObtainNSWindowHandleRetained(void **ret) override;

    virtual NSWindow *GetNSWindow() override;

    virtual HRESULT Show(bool activate, bool isDialog) override;

    virtual bool IsShown ();

    virtual bool ShouldTakeFocusOnShow();

    virtual HRESULT Hide() override;

    virtual HRESULT Activate() override;

    virtual HRESULT SetTopMost(bool value) override;

    virtual HRESULT Close() override;

    virtual HRESULT GetFrameSize(AvnSize *ret) override;

    virtual HRESULT SetMinMaxSize(AvnSize minSize, AvnSize maxSize) override;

    virtual HRESULT Resize(double x, double y, AvnPlatformResizeReason reason) override;

    virtual HRESULT SetMainMenu(IAvnMenu *menu) override;

    virtual HRESULT BeginMoveDrag() override;

    virtual HRESULT BeginResizeDrag(__attribute__((unused)) AvnWindowEdge edge) override;

    virtual HRESULT GetPosition(AvnPoint *ret) override;

    virtual HRESULT SetPosition(AvnPoint point) override;

    virtual HRESULT SetFrameThemeVariant(AvnPlatformThemeVariant variant) override;

    virtual HRESULT SetTransparencyMode(AvnWindowTransparencyMode mode) override;
                           
    virtual bool IsModal();

    id<AvnWindowProtocol> GetWindowProtocol ();
                           
    virtual void BringToFront ();

    virtual bool CanZoom() { return false; }
                           
    virtual HRESULT SetParent(IAvnWindowBase* parent) override;
                           
protected:
    virtual NSWindowStyleMask CalculateStyleMask() = 0;
    virtual void UpdateAppearance() override;
    virtual void SetClientSize(NSSize size) override;

private:
    void CreateNSWindow (bool isDialog);
    void CleanNSWindow ();

    bool hasPosition;
    NSSize lastSize;
    NSSize lastMinSize;
    NSSize lastMaxSize;
    AvnMenu* lastMenu;
    bool _inResize;

protected:
    AutoFitContentView *StandardContainer;
    AvnPoint lastPositionSet;
    bool _shown;
    std::list<WindowBaseImpl*> _children;

public:
    ComObjectWeakPtr<WindowBaseImpl> Parent = nullptr;
    NSWindow * Window;
    ComPtr<IAvnWindowBaseEvents> BaseEvents;
};

#endif //AVALONIA_NATIVE_OSX_WINDOWBASEIMPL_H
