//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#ifndef AVALONIA_NATIVE_OSX_WINDOWIMPL_H
#define AVALONIA_NATIVE_OSX_WINDOWIMPL_H

#import "WindowBaseImpl.h"
#include "IWindowStateChanged.h"
class WindowImpl : public virtual WindowBaseImpl, public virtual IAvnWindow, public IWindowStateChanged
{
public:
    FORWARD_IUNKNOWN()
BEGIN_INTERFACE_MAP()
        INHERIT_INTERFACE_MAP(WindowBaseImpl)
        INTERFACE_MAP_ENTRY(IAvnWindow, IID_IAvnWindow)
    END_INTERFACE_MAP()
    virtual ~WindowImpl()
    {
    }

    ComPtr<IAvnWindowEvents> WindowEvents;

    WindowImpl(IAvnWindowEvents* events);

    virtual HRESULT Show (bool activate, bool isDialog) override;

    virtual HRESULT SetEnabled (bool enable) override;

    void StartStateTransition () override ;

    void EndStateTransition () override ;

    SystemDecorations Decorations () override ;

    AvnWindowState WindowState () override ;

    void WindowStateChanged () override ;

    bool UndecoratedIsMaximized ();

    bool IsZoomed ();

    void DoZoom();

    virtual HRESULT SetCanResize(bool value) override;
    
    virtual HRESULT SetCanMinimize(bool value) override;
    
    virtual HRESULT SetCanMaximize(bool value) override;

    virtual HRESULT SetDecorations(SystemDecorations value) override;

    virtual HRESULT SetTitle (char* utf8title) override;

    virtual HRESULT SetTitleBarColor(AvnColor color) override;

    virtual HRESULT GetWindowState (AvnWindowState*ret) override;

    virtual HRESULT TakeFocusFromChildren () override;

    virtual HRESULT SetExtendClientArea (bool enable) override;

    virtual HRESULT GetExtendTitleBarHeight (double*ret) override;

    virtual HRESULT SetExtendTitleBarHeight (double value) override;

    virtual HRESULT SetTrafficLightPosition (AvnPoint position, bool useCustomLayout) override;
    
    virtual HRESULT GetWindowZOrder (long* zOrder) override;

    void EnterFullScreenMode ();

    void ExitFullScreenMode ();

    virtual HRESULT SetWindowState (AvnWindowState state) override;
    
    virtual HRESULT SetWindowState (AvnWindowState state, bool shouldResize);

    virtual bool IsModal() override;
    
    bool IsOwned();
    
    virtual void BringToFront () override;
    
    bool CanBecomeKeyWindow ();

    bool CanZoom() override { return _isEnabled && _canMaximize; }

    bool IsTransitioningWindowState() { return _transitioningWindowState; }

    void UpdateTrafficLightLayout();

protected:
    virtual NSWindowStyleMask CalculateStyleMask() override;
    virtual void UpdateAppearance() override;

private:
    enum class TrafficLightLayout
    {
        System,
        Custom
    };

    struct TrafficLightViews
    {
        NSView* Container;
        NSButton* Close;
        NSButton* Miniaturize;
        NSButton* Zoom;
    };

    struct TrafficLightFrames
    {
        NSRect Container;
        NSRect Close;
        NSRect Miniaturize;
        NSRect Zoom;
    };

    void UpdateWindowedTrafficLightLayout();
    void RestoreTrafficLightLayout();
    bool TryGetTrafficLightViews(TrafficLightViews* views);
    void UpdateTrafficLightTrackingAreas(const TrafficLightViews& views);

    void ZOrderChildWindows();
    void OnInitialiseNSWindow();
    NSString *_lastTitle;
    bool _isEnabled;
    bool _canResize;
    bool _canMinimize;
    bool _canMaximize;
    bool _fullScreenActive;
    SystemDecorations _decorations;
    AvnWindowState _lastWindowState;
    AvnWindowState _actualWindowState;
    bool _inSetWindowState;
    NSRect _preZoomSize;
    bool _transitioningWindowState;
    bool _isClientAreaExtended;
    bool _isModal;
    bool _hasDefaultTrafficLightFrames;
    TrafficLightLayout _trafficLightLayout;
    AvnPoint _trafficLightPosition;
    TrafficLightFrames _defaultTrafficLightFrames;
};

#endif //AVALONIA_NATIVE_OSX_WINDOWIMPL_H
