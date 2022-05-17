//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <AppKit/AppKit.h>
#include "AutoFitContentView.h"
#include "AvnView.h"
#include "automation.h"
#include "WindowProtocol.h"

WindowImpl::WindowImpl(IAvnWindowEvents *events, IAvnGlContext *gl) : WindowBaseImpl(events, gl) {
    _isClientAreaExtended = false;
    _extendClientHints = AvnDefaultChrome;
    _fullScreenActive = false;
    _canResize = true;
    _decorations = SystemDecorationsFull;
    _transitioningWindowState = false;
    _inSetWindowState = false;
    _lastWindowState = Normal;
    _actualWindowState = Normal;
    WindowEvents = events;
    [Window disableCursorRects];
    [Window setTabbingMode:NSWindowTabbingModeDisallowed];
    [Window setCollectionBehavior:NSWindowCollectionBehaviorFullScreenPrimary];
}

void WindowImpl::HideOrShowTrafficLights() {
    if (Window == nil) {
        return;
    }

    for (id subview in Window.contentView.superview.subviews) {
        if ([subview isKindOfClass:NSClassFromString(@"NSTitlebarContainerView")]) {
            NSView *titlebarView = [subview subviews][0];
            for (id button in titlebarView.subviews) {
                if ([button isKindOfClass:[NSButton class]]) {
                    if (_isClientAreaExtended) {
                        auto wantsChrome = (_extendClientHints & AvnSystemChrome) || (_extendClientHints & AvnPreferSystemChrome);

                        [button setHidden:!wantsChrome];
                    } else {
                        [button setHidden:(_decorations != SystemDecorationsFull)];
                    }

                    [button setWantsLayer:true];
                }
            }
        }
    }
}

HRESULT WindowImpl::Show(bool activate, bool isDialog) {
    START_COM_CALL;

    @autoreleasepool {
        _isDialog = isDialog;

        bool created = Window == nullptr;

        WindowBaseImpl::Show(activate, isDialog);

        if(created)
        {
            if(_isClientAreaExtended)
            {
                [GetWindowProtocol() setIsExtended:true];
                SetExtendClientArea(true);
            }
        }

        HideOrShowTrafficLights();

        return SetWindowState(_lastWindowState);
    }
}

HRESULT WindowImpl::SetEnabled(bool enable) {
    START_COM_CALL;

    @autoreleasepool {
        [GetWindowProtocol() setEnabled:enable];
        return S_OK;
    }
}

HRESULT WindowImpl::SetParent(IAvnWindow *parent) {
    START_COM_CALL;

    @autoreleasepool {
        if (parent == nullptr)
            return E_POINTER;

        auto cparent = dynamic_cast<WindowImpl *>(parent);
        if (cparent == nullptr)
            return E_INVALIDARG;

        // If one tries to show a child window with a minimized parent window, then the parent window will be
        // restored but macOS isn't kind enough to *tell* us that, so the window will be left in a non-interactive
        // state. Detect this and explicitly restore the parent window ourselves to avoid this situation.
        if (cparent->WindowState() == Minimized)
            cparent->SetWindowState(Normal);

        [Window setCollectionBehavior:NSWindowCollectionBehaviorFullScreenAuxiliary];
        [cparent->Window addChildWindow:Window ordered:NSWindowAbove];

        UpdateStyle();

        return S_OK;
    }
}

void WindowImpl::StartStateTransition() {
    _transitioningWindowState = true;
}

void WindowImpl::EndStateTransition() {
    _transitioningWindowState = false;
}

SystemDecorations WindowImpl::Decorations() {
    return _decorations;
}

AvnWindowState WindowImpl::WindowState() {
    return _lastWindowState;
}

void WindowImpl::WindowStateChanged() {
    if (_shown && !_inSetWindowState && !_transitioningWindowState) {
        AvnWindowState state;
        GetWindowState(&state);

        if (_lastWindowState != state) {
            if (_isClientAreaExtended) {
                if (_lastWindowState == FullScreen) {
                    // we exited fs.
                    if (_extendClientHints & AvnOSXThickTitleBar) {
                        Window.toolbar = [NSToolbar new];
                        Window.toolbar.showsBaselineSeparator = false;
                    }

                    [Window setTitlebarAppearsTransparent:true];

                    [StandardContainer setFrameSize:StandardContainer.frame.size];
                } else if (state == FullScreen) {
                    // we entered fs.
                    if (_extendClientHints & AvnOSXThickTitleBar) {
                        Window.toolbar = nullptr;
                    }

                    [Window setTitlebarAppearsTransparent:false];

                    [StandardContainer setFrameSize:StandardContainer.frame.size];
                }
            }

            _lastWindowState = state;
            _actualWindowState = state;
            WindowEvents->WindowStateChanged(state);
        }
    }
}

bool WindowImpl::UndecoratedIsMaximized() {
    auto windowSize = [Window frame];
    auto available = [Window screen].visibleFrame;
    return CGRectEqualToRect(windowSize, available);
}

bool WindowImpl::IsZoomed() {
    return _decorations == SystemDecorationsFull ? [Window isZoomed] : UndecoratedIsMaximized();
}

void WindowImpl::DoZoom() {
    switch (_decorations) {
        case SystemDecorationsNone:
        case SystemDecorationsBorderOnly:
            [Window setFrame:[Window screen].visibleFrame display:true];
            break;


        case SystemDecorationsFull:
            [Window performZoom:Window];
            break;
    }
}

HRESULT WindowImpl::SetCanResize(bool value) {
    START_COM_CALL;

    @autoreleasepool {
        _canResize = value;
        UpdateStyle();
        return S_OK;
    }
}

HRESULT WindowImpl::SetDecorations(SystemDecorations value) {
    START_COM_CALL;

    @autoreleasepool {
        auto currentWindowState = _lastWindowState;
        _decorations = value;

        if (_fullScreenActive) {
            return S_OK;
        }

        UpdateStyle();

        HideOrShowTrafficLights();

        switch (_decorations) {
            case SystemDecorationsNone:
                [Window setHasShadow:NO];
                [Window setTitleVisibility:NSWindowTitleHidden];
                [Window setTitlebarAppearsTransparent:YES];

                if (currentWindowState == Maximized) {
                    if (!UndecoratedIsMaximized()) {
                        DoZoom();
                    }
                }
                break;

            case SystemDecorationsBorderOnly:
                [Window setHasShadow:YES];
                [Window setTitleVisibility:NSWindowTitleHidden];
                [Window setTitlebarAppearsTransparent:YES];

                if (currentWindowState == Maximized) {
                    if (!UndecoratedIsMaximized()) {
                        DoZoom();
                    }
                }
                break;

            case SystemDecorationsFull:
                [Window setHasShadow:YES];
                [Window setTitleVisibility:NSWindowTitleVisible];
                [Window setTitlebarAppearsTransparent:NO];
                [Window setTitle:_lastTitle];

                if (currentWindowState == Maximized) {
                    auto newFrame = [Window contentRectForFrameRect:[Window frame]].size;

                    [View setFrameSize:newFrame];
                }
                break;
        }

        return S_OK;
    }
}

HRESULT WindowImpl::SetTitle(char *utf8title) {
    START_COM_CALL;

    @autoreleasepool {
        _lastTitle = [NSString stringWithUTF8String:(const char *) utf8title];
        [Window setTitle:_lastTitle];

        return S_OK;
    }
}

HRESULT WindowImpl::SetTitleBarColor(AvnColor color) {
    START_COM_CALL;

    @autoreleasepool {
        float a = (float) color.Alpha / 255.0f;
        float r = (float) color.Red / 255.0f;
        float g = (float) color.Green / 255.0f;
        float b = (float) color.Blue / 255.0f;

        auto nscolor = [NSColor colorWithSRGBRed:r green:g blue:b alpha:a];

        // Based on the titlebar color we have to choose either light or dark
        // OSX doesnt let you set a foreground color for titlebar.
        if ((r * 0.299 + g * 0.587 + b * 0.114) > 186.0f / 255.0f) {
            [Window setAppearance:[NSAppearance appearanceNamed:NSAppearanceNameVibrantLight]];
        } else {
            [Window setAppearance:[NSAppearance appearanceNamed:NSAppearanceNameVibrantDark]];
        }

        [Window setTitlebarAppearsTransparent:true];
        [Window setBackgroundColor:nscolor];
    }

    return S_OK;
}

HRESULT WindowImpl::GetWindowState(AvnWindowState *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        if (([Window styleMask] & NSWindowStyleMaskFullScreen) == NSWindowStyleMaskFullScreen) {
            *ret = FullScreen;
            return S_OK;
        }

        if ([Window isMiniaturized]) {
            *ret = Minimized;
            return S_OK;
        }

        if (IsZoomed()) {
            *ret = Maximized;
            return S_OK;
        }

        *ret = Normal;

        return S_OK;
    }
}

HRESULT WindowImpl::TakeFocusFromChildren() {
    START_COM_CALL;

    @autoreleasepool {
        if (Window == nil)
            return S_OK;
        if ([Window isKeyWindow])
            [Window makeFirstResponder:View];

        return S_OK;
    }
}

HRESULT WindowImpl::SetExtendClientArea(bool enable) {
    START_COM_CALL;

    @autoreleasepool {
        _isClientAreaExtended = enable;

        if(Window != nullptr) {
            if (enable) {
                Window.titleVisibility = NSWindowTitleHidden;

                [Window setTitlebarAppearsTransparent:true];

                auto wantsTitleBar = (_extendClientHints & AvnSystemChrome) || (_extendClientHints & AvnPreferSystemChrome);

                if (wantsTitleBar) {
                    [StandardContainer ShowTitleBar:true];
                } else {
                    [StandardContainer ShowTitleBar:false];
                }

                if (_extendClientHints & AvnOSXThickTitleBar) {
                    Window.toolbar = [NSToolbar new];
                    Window.toolbar.showsBaselineSeparator = false;
                } else {
                    Window.toolbar = nullptr;
                }
            } else {
                Window.titleVisibility = NSWindowTitleVisible;
                Window.toolbar = nullptr;
                [Window setTitlebarAppearsTransparent:false];
                View.layer.zPosition = 0;
            }

            [GetWindowProtocol() setIsExtended:enable];

            HideOrShowTrafficLights();

            UpdateStyle();
        }

        return S_OK;
    }
}

HRESULT WindowImpl::SetExtendClientAreaHints(AvnExtendClientAreaChromeHints hints) {
    START_COM_CALL;

    @autoreleasepool {
        _extendClientHints = hints;

        SetExtendClientArea(_isClientAreaExtended);
        return S_OK;
    }
}

HRESULT WindowImpl::GetExtendTitleBarHeight(double *ret) {
    START_COM_CALL;

    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        *ret = [GetWindowProtocol() getExtendedTitleBarHeight];

        return S_OK;
    }
}

HRESULT WindowImpl::SetExtendTitleBarHeight(double value) {
    START_COM_CALL;

    @autoreleasepool {
        [StandardContainer SetTitleBarHeightHint:value];
        return S_OK;
    }
}

void WindowImpl::EnterFullScreenMode() {
    _fullScreenActive = true;

    [Window setTitle:_lastTitle];
    [Window toggleFullScreen:nullptr];
}

void WindowImpl::ExitFullScreenMode() {
    [Window toggleFullScreen:nullptr];

    _fullScreenActive = false;

    SetDecorations(_decorations);
}

HRESULT WindowImpl::SetWindowState(AvnWindowState state) {
    START_COM_CALL;

    @autoreleasepool {
        if (Window == nullptr) {
            return S_OK;
        }

        if (_actualWindowState == state) {
            return S_OK;
        }

        _inSetWindowState = true;

        auto currentState = _actualWindowState;
        _lastWindowState = state;

        if (currentState == Normal) {
            _preZoomSize = [Window frame];
        }

        if (_shown) {
            switch (state) {
                case Maximized:
                    if (currentState == FullScreen) {
                        ExitFullScreenMode();
                    }

                    lastPositionSet.X = 0;
                    lastPositionSet.Y = 0;

                    if ([Window isMiniaturized]) {
                        [Window deminiaturize:Window];
                    }

                    if (!IsZoomed()) {
                        DoZoom();
                    }
                    break;

                case Minimized:
                    if (currentState == FullScreen) {
                        ExitFullScreenMode();
                    } else {
                        [Window miniaturize:Window];
                    }
                    break;

                case FullScreen:
                    if ([Window isMiniaturized]) {
                        [Window deminiaturize:Window];
                    }

                    EnterFullScreenMode();
                    break;

                case Normal:
                    if ([Window isMiniaturized]) {
                        [Window deminiaturize:Window];
                    }

                    if (currentState == FullScreen) {
                        ExitFullScreenMode();
                    }

                    if (IsZoomed()) {
                        if (_decorations == SystemDecorationsFull) {
                            DoZoom();
                        } else {
                            [Window setFrame:_preZoomSize display:true];
                            auto newFrame = [Window contentRectForFrameRect:[Window frame]].size;

                            [View setFrameSize:newFrame];
                        }

                    }
                    break;
            }

            _actualWindowState = _lastWindowState;
            WindowEvents->WindowStateChanged(_actualWindowState);
        }


        _inSetWindowState = false;

        return S_OK;
    }
}

bool WindowImpl::IsDialog() {
    return _isDialog;
}

NSWindowStyleMask WindowImpl::GetStyle() {
    unsigned long s = this->_isDialog ? NSWindowStyleMaskUtilityWindow : NSWindowStyleMaskBorderless;

    switch (_decorations) {
        case SystemDecorationsNone:
            s = s | NSWindowStyleMaskFullSizeContentView;
            break;

        case SystemDecorationsBorderOnly:
            s = s | NSWindowStyleMaskTitled | NSWindowStyleMaskFullSizeContentView;
            break;

        case SystemDecorationsFull:
            s = s | NSWindowStyleMaskTitled | NSWindowStyleMaskClosable | NSWindowStyleMaskBorderless;

            if (_canResize) {
                s = s | NSWindowStyleMaskResizable;
            }
            break;
    }

    if ([Window parentWindow] == nullptr) {
        s |= NSWindowStyleMaskMiniaturizable;
    }

    if (_isClientAreaExtended) {
        s |= NSWindowStyleMaskFullSizeContentView | NSWindowStyleMaskTexturedBackground;
    }
    return s;
}
