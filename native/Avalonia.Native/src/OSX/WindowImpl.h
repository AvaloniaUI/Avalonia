//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#ifndef AVALONIA_NATIVE_OSX_WINDOWIMPL_H
#define AVALONIA_NATIVE_OSX_WINDOWIMPL_H

#import "WindowBaseImpl.h"
#include "IWindowStateChanged.h"
#include <list>

class WindowImpl : public virtual WindowBaseImpl, public virtual IAvnWindow, public IWindowStateChanged
{
private:
    bool _isEnabled;
    bool _canResize;
    bool _fullScreenActive;
    SystemDecorations _decorations;
    AvnWindowState _lastWindowState;
    AvnWindowState _actualWindowState;
    bool _inSetWindowState;
    NSRect _preZoomSize;
    bool _transitioningWindowState;
    bool _isClientAreaExtended;
    bool _isModal;
    WindowImpl* _parent;
    std::list<WindowImpl*> _children;
    AvnExtendClientAreaChromeHints _extendClientHints;

    FORWARD_IUNKNOWN()
BEGIN_INTERFACE_MAP()
        INHERIT_INTERFACE_MAP(WindowBaseImpl)
        INTERFACE_MAP_ENTRY(IAvnWindow, IID_IAvnWindow)
    END_INTERFACE_MAP()
    virtual ~WindowImpl()
    {
    }

    ComPtr<IAvnWindowEvents> WindowEvents;

    WindowImpl(IAvnWindowEvents* events, IAvnGlContext* gl);

    virtual HRESULT Show (bool activate, bool isDialog) override;

    virtual HRESULT SetEnabled (bool enable) override;

    virtual HRESULT SetParent (IAvnWindow* parent) override;

    void StartStateTransition () override ;

    void EndStateTransition () override ;

    SystemDecorations Decorations () override ;

    AvnWindowState WindowState () override ;

    void WindowStateChanged () override ;

    bool UndecoratedIsMaximized ();

    bool IsZoomed ();

    void DoZoom();

    virtual HRESULT SetCanResize(bool value) override;

    virtual HRESULT SetDecorations(SystemDecorations value) override;

    virtual HRESULT SetTitle (char* utf8title) override;

    virtual HRESULT SetTitleBarColor(AvnColor color) override;

    virtual HRESULT GetWindowState (AvnWindowState*ret) override;

    virtual HRESULT TakeFocusFromChildren () override;

    virtual HRESULT SetExtendClientArea (bool enable) override;

    virtual HRESULT SetExtendClientAreaHints (AvnExtendClientAreaChromeHints hints) override;

    virtual HRESULT GetExtendTitleBarHeight (double*ret) override;

    virtual HRESULT SetExtendTitleBarHeight (double value) override;

    void EnterFullScreenMode ();

    void ExitFullScreenMode ();

    virtual HRESULT SetWindowState (AvnWindowState state) override;

    virtual bool IsModal() override;
    
    bool IsOwned();
    
    virtual void BringToFront () override;
    
    bool CanBecomeKeyWindow ();

    bool CanZoom() override { return _isEnabled && _canResize; }
    
protected:
    virtual NSWindowStyleMask CalculateStyleMask() override;
    void UpdateStyle () override;

private:
    void ZOrderChildWindows();
    void OnInitialiseNSWindow();
    NSString *_lastTitle;
};

#endif //AVALONIA_NATIVE_OSX_WINDOWIMPL_H
