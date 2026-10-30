//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <AppKit/AppKit.h>
#include "AutoFitContentView.h"
#include "AvnView.h"
#include "automation.h"
#include "WindowProtocol.h"
#include "WindowImpl.h"

// Tells WindowImpl when AppKit moves the standard window buttons. AppKit retiles the titlebar from many
// code paths, including deferred layout passes, so frame notifications are the only reliable hook.
@interface AvnTrafficLightFrameObserver : NSObject
-(AvnTrafficLightFrameObserver*) initWithParent: (const ComPtr<WindowImpl>&) parent;
-(void) observeViews: (NSArray<NSView*>*) views;
@end

@implementation AvnTrafficLightFrameObserver
{
    ComObjectWeakPtr<WindowImpl> _parent;
    NSArray<NSView*>* _views;
}

-(AvnTrafficLightFrameObserver*) initWithParent: (const ComPtr<WindowImpl>&) parent
{
    self = [super init];
    _parent = parent;
    return self;
}

-(void) observeViews: (NSArray<NSView*>*) views
{
    // AppKit recreates the standard buttons when the style mask changes.
    if ([views isEqualToArray:_views]) {
        return;
    }

    auto center = [NSNotificationCenter defaultCenter];
    [center removeObserver:self name:NSViewFrameDidChangeNotification object:nil];
    _views = views;

    for (NSView* view in views) {
        [center addObserver:self selector:@selector(viewFrameDidChange:) name:NSViewFrameDidChangeNotification object:view];
    }
}

-(void) viewFrameDidChange: (NSNotification*) notification
{
    auto parent = _parent.tryGet();
    if (parent) {
        parent->InvalidateTrafficLightLayout();
    }
}
@end

WindowImpl::WindowImpl(const ComPtr<IAvnWindowEvents>& events) : TopLevelImpl(events), WindowBaseImpl(events, false) {
    _isEnabled = true;
    _isClientAreaExtended = false;
    _fullScreenActive = false;
    _canResize = true;
    _canMinimize = true;
    _canMaximize = true;
    _decorations = SystemDecorationsFull;
    _transitioningWindowState = false;
    _inSetWindowState = false;
    _lastWindowState = Normal;
    _actualWindowState = Normal;
    _lastTitle = @"";
    _trafficLightState = TrafficLightState::System;
    _isUpdatingTrafficLightLayout = false;
    _trafficLightPosition = { 0, 0 };
    _trafficLightSize = { 0, 0 };
    _trafficLightGroup = {};
    _trafficLightFrameObserver = [[AvnTrafficLightFrameObserver alloc] initWithParent:comself(this)];
    Parent = nullptr;
    WindowEvents = events;

    [Window setHasShadow:true];
    
    OnInitialiseNSWindow();
}

void WindowImpl::OnInitialiseNSWindow(){
    [GetWindowProtocol() setCanBecomeKeyWindow:true];
    
    [Window disableCursorRects];
    [Window setTabbingMode:NSWindowTabbingModeDisallowed];
    [Window setCollectionBehavior:NSWindowCollectionBehaviorFullScreenPrimary];

    [Window setTitle:_lastTitle];
    
    if(_isClientAreaExtended)
    {
        [GetWindowProtocol() setIsExtended:true];
        SetExtendClientArea(true);
    }
}

HRESULT WindowImpl::Show(bool activate, bool isDialog) {
    @autoreleasepool {
        _isModal = isDialog;

        WindowBaseImpl::Show(activate, isDialog);
        GetWindowState(&_actualWindowState);

        if(IsZoomed()) {
            _lastWindowState = _actualWindowState;
        }

        return SetWindowState(_lastWindowState);
    }
}

HRESULT WindowImpl::SetEnabled(bool enable) {
    @autoreleasepool {
        _isEnabled = enable;
        [GetWindowProtocol() setEnabled:enable];
        UpdateAppearance();
        return S_OK;
    }
}

void WindowImpl::BringToFront()
{
    if(Window != nullptr)
    {
        if ([Window isVisible] && ![Window isMiniaturized])
        {
            if(IsModal())
            {
                Activate();
            }
            else
            {
                [Window orderFront:nullptr];
            }
        }
        
        [Window invalidateShadow];
        ZOrderChildWindows();
    }
}

void WindowImpl::ZOrderChildWindows()
{
    for(auto iterator = _children.begin(); iterator != _children.end(); iterator++)
    {
        auto child = (*iterator).tryGet();
        if (!child) continue;
        
        auto window = child->Window;
        
        // #9565: Only bring window to front if it's on the currently active space
        if ([window isOnActiveSpace]) {
            child->BringToFront();
        }
    }
}

bool WindowImpl::CanBecomeKeyWindow()
{
    for(auto iterator = _children.begin(); iterator != _children.end(); iterator++)
    {
        auto child = (*iterator).tryGet();
        if(child && child->IsModal())
        {
            return false;
        }
    }
    
    return true;
}

void WindowImpl::StartStateTransition() {
    _transitioningWindowState = true;
    UpdateAppearance();
}

void WindowImpl::EndStateTransition() {
    _transitioningWindowState = false;
    UpdateAppearance();

    // Ensure correct order of child windows after fullscreen transition.
    ZOrderChildWindows();
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
                    [Window setTitlebarAppearsTransparent:true];

                    [StandardContainer setFrameSize:StandardContainer.frame.size];
                } else if (state == FullScreen) {
                    // we entered fs.
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
    if (_decorations == SystemDecorationsNone ||
        _decorations == SystemDecorationsBorderOnly ||
        _canResize == false ||
        _canMaximize == false) {
        [Window setFrame:[Window screen].visibleFrame display:true];
    } else {
        [Window performZoom:Window];
    }
}

HRESULT WindowImpl::SetCanResize(bool value) {
    @autoreleasepool {
        _canResize = value;
        UpdateAppearance();
        return S_OK;
    }
}

HRESULT WindowImpl::SetCanMinimize(bool value) {
    _canMinimize = value;
    UpdateAppearance();
    return S_OK;
}

HRESULT WindowImpl::SetCanMaximize(bool value) {
    _canMaximize = value;
    UpdateAppearance();
    return S_OK;
}

HRESULT WindowImpl::SetDecorations(SystemDecorations value) {
    @autoreleasepool {
        auto currentWindowState = _lastWindowState;
        _decorations = value;

        if (_fullScreenActive) {
            return S_OK;
        }

        UpdateAppearance();

        if (_isClientAreaExtended) {
            [StandardContainer ShowTitleBar:_decorations == SystemDecorationsFull];
        }

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
                [Window setTitle:_lastTitle];

                if (!_isClientAreaExtended) {
                    [Window setTitleVisibility:NSWindowTitleVisible];
                    [Window setTitlebarAppearsTransparent:NO];
                }

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
    @autoreleasepool {
        _lastTitle = [NSString stringWithUTF8String:(const char *) utf8title];
        [Window setTitle:_lastTitle];

        return S_OK;
    }
}

HRESULT WindowImpl::SetTitleBarColor(AvnColor color) {
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

HRESULT WindowImpl::GetWindowZOrder(long* zOrder) {
    @autoreleasepool {
        if (zOrder == nullptr) {
            return E_POINTER;
        }

        // negate the value to match expected z-order in Avalonia
        // (top-most window should have the highest z-order value)
        *zOrder = -[Window orderedIndex];
        return S_OK;
    }
}

HRESULT WindowImpl::TakeFocusFromChildren() {
    @autoreleasepool {
        if (Window == nil)
            return S_OK;
        if ([Window isKeyWindow])
            [Window makeFirstResponder:View];

        return S_OK;
    }
}

HRESULT WindowImpl::SetExtendClientArea(bool enable) {
    @autoreleasepool {
        _isClientAreaExtended = enable;

        if(Window != nullptr) {
            if (enable) {
                Window.titleVisibility = NSWindowTitleHidden;

                [Window setTitlebarAppearsTransparent:true];

                [StandardContainer ShowTitleBar:_decorations == SystemDecorationsFull];

                Window.toolbar = nullptr;
            } else {
                Window.titleVisibility = NSWindowTitleVisible;
                Window.toolbar = nullptr;
                [Window setTitlebarAppearsTransparent:false];
                View.layer.zPosition = 0;
            }

            [GetWindowProtocol() setIsExtended:enable];
            UpdateAppearance();
        }

        return S_OK;
    }
}

HRESULT WindowImpl::GetExtendTitleBarHeight(double *ret) {
    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        *ret = [GetWindowProtocol() getExtendedTitleBarHeight];

        return S_OK;
    }
}

HRESULT WindowImpl::SetExtendTitleBarHeight(double value) {
    @autoreleasepool {
        [StandardContainer SetTitleBarHeightHint:value];
        return S_OK;
    }
}

HRESULT WindowImpl::SetTrafficLightPosition(AvnPoint position, bool useCustomLayout) {
    @autoreleasepool {
        _trafficLightPosition = position;
        TransitionTrafficLightState(useCustomLayout ? ResolveRequestedTrafficLightState() : TrafficLightState::System);
        return S_OK;
    }
}

HRESULT WindowImpl::GetTrafficLightSize(AvnSize* ret) {
    @autoreleasepool {
        if (ret == nullptr) {
            return E_POINTER;
        }

        *ret = CalculateTrafficLightSize();
        return S_OK;
    }
}

AvnFlowDirection WindowImpl::GetTitlebarFlowDirection() {
    return Window.windowTitlebarLayoutDirection == NSUserInterfaceLayoutDirectionRightToLeft
        ? AvnFlowDirectionRightToLeft
        : AvnFlowDirectionLeftToRight;
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
    return SetWindowState(state, true);
}

HRESULT WindowImpl::SetWindowState(AvnWindowState state, bool shouldResize) {
    @autoreleasepool {
        auto currentState = _actualWindowState;
        _lastWindowState = state;

        if (Window == nullptr) {
            return S_OK;
        }

        if (_actualWindowState == state) {
            return S_OK;
        }

        _inSetWindowState = true;

        if (currentState == Normal) {
            _preZoomSize = [Window frame];
        }

        if (_shown) {
            _actualWindowState = _lastWindowState;

            if (shouldResize) {
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
            }

            WindowEvents->WindowStateChanged(_actualWindowState);
        }


        _inSetWindowState = false;

        return S_OK;
    }
}

bool WindowImpl::IsModal() {
    return _isModal;
}

bool WindowImpl::IsOwned() {
    return Parent.tryGet() != nullptr;
}

NSWindowStyleMask WindowImpl::CalculateStyleMask() {
    // Use the current style mask and only clear the flags we're going to be modifying.
    unsigned long s = [Window styleMask] &
        ~(NSWindowStyleMaskFullSizeContentView |
          NSWindowStyleMaskTitled |
          NSWindowStyleMaskClosable |
          NSWindowStyleMaskResizable |
          NSWindowStyleMaskMiniaturizable |
          NSWindowStyleMaskTexturedBackground);
    
    switch (_decorations) {
        case SystemDecorationsNone:
            s = s | NSWindowStyleMaskFullSizeContentView;
            break;

        case SystemDecorationsBorderOnly:
            s = s | NSWindowStyleMaskTitled | NSWindowStyleMaskFullSizeContentView;
            
            if (_canResize && _isEnabled) {
                s = s | NSWindowStyleMaskResizable;
            }
            break;

        case SystemDecorationsFull:
            s = s | NSWindowStyleMaskTitled | NSWindowStyleMaskClosable;

            if ((_canResize && _isEnabled) || _transitioningWindowState) {
                s = s | NSWindowStyleMaskResizable;
            }
            break;
    }

    if (_canMinimize && !IsOwned()) {
        s |= NSWindowStyleMaskMiniaturizable;
    }

    if (_isClientAreaExtended) {
        s |= NSWindowStyleMaskFullSizeContentView | NSWindowStyleMaskTexturedBackground;
    }
    return s;
}

void WindowImpl::UpdateAppearance() {
    WindowBaseImpl::UpdateAppearance();
    
    if (Window == nil) {
        return;
    }

    bool hasTrafficLights = (_decorations == SystemDecorationsFull);
    
    NSButton* closeButton = [Window standardWindowButton:NSWindowCloseButton];
    NSButton* miniaturizeButton = [Window standardWindowButton:NSWindowMiniaturizeButton];
    NSButton* zoomButton = [Window standardWindowButton:NSWindowZoomButton];

    [closeButton setHidden:!hasTrafficLights];
    [closeButton setEnabled:_isEnabled];
    [miniaturizeButton setHidden:!hasTrafficLights];
    [miniaturizeButton setEnabled:_isEnabled && _canMinimize];
    [zoomButton setHidden:!hasTrafficLights];
    [zoomButton setEnabled:CanZoom() || (([Window styleMask] & NSWindowStyleMaskFullScreen) != 0 && _isEnabled)];

    // Style and button visibility changes can make AppKit lay out the titlebar again.
    UpdateTrafficLightLayout();
}

void WindowImpl::InvalidateTrafficLightLayout() {
    // Only moved buttons need to be put back, and our own frame changes also post notifications.
    if (_trafficLightState != TrafficLightState::Custom || _isUpdatingTrafficLightLayout) {
        return;
    }

    // AppKit ignores frame changes made while a view posts its own frame notification, so the layout is
    // reapplied in the next layout pass. The content view is laid out after the titlebar and before display.
    [View setNeedsLayout:YES];
}

void WindowImpl::UpdateTrafficLightLayout() {
    // Window state changes and AppKit retiles keep the requested position.
    TransitionTrafficLightState(_trafficLightState == TrafficLightState::System
        ? TrafficLightState::System
        : ResolveRequestedTrafficLightState());
}

void WindowImpl::TransitionTrafficLightState(TrafficLightState state) {
    if (Window == nil) {
        return;
    }

    _isUpdatingTrafficLightLayout = true;

    TrafficLightViews views;
    bool hasViews = TryGetTrafficLightViews(&views);
    if (!hasViews && state == TrafficLightState::Custom) {
        // The position stays requested until AppKit creates the buttons.
        state = TrafficLightState::Suspended;
    }

    if (hasViews) {
        [_trafficLightFrameObserver observeViews:@[views.Container, views.Close, views.Miniaturize, views.Zoom]];

        // AppKit retiles one button at a time, so the group is only measured while AppKit owns the buttons.
        if (_trafficLightState != TrafficLightState::Custom) {
            _trafficLightGroup = MeasureTrafficLightGroup(views);
        }

        if (state == TrafficLightState::Custom) {
            ApplyCustomTrafficLightLayout(views);
        } else if (_trafficLightState == TrafficLightState::Custom) {
            RestoreSystemTrafficLightLayout(views);
        }
    }

    _trafficLightState = state;
    _isUpdatingTrafficLightLayout = false;

    // Notify after the update, so a handler can set a new position.
    AvnSize size = CalculateTrafficLightSize();
    if (size.Width != _trafficLightSize.Width || size.Height != _trafficLightSize.Height) {
        _trafficLightSize = size;
        WindowEvents->TrafficLightSizeChanged(size);
    }
}

WindowImpl::TrafficLightState WindowImpl::ResolveRequestedTrafficLightState() {
    return CanCustomizeTrafficLightLayout() ? TrafficLightState::Custom : TrafficLightState::Suspended;
}

bool WindowImpl::CanCustomizeTrafficLightLayout() {
    // The buttons can only be placed inside the client area of an extended, fully decorated window.
    // AppKit owns them in every other state, including native fullscreen.
    return _isClientAreaExtended &&
        _decorations == SystemDecorationsFull &&
        ([Window styleMask] & NSWindowStyleMaskFullScreen) == 0;
}

WindowImpl::TrafficLightGroup WindowImpl::MeasureTrafficLightGroup(const TrafficLightViews& views) {
    // Measure the native group instead of assuming fixed button sizes or spacing.
    NSRect groupFrame = NSUnionRect(views.Close.frame,
        NSUnionRect(views.Miniaturize.frame, views.Zoom.frame));
    auto originInGroup = [&](NSButton* button) {
        return NSMakePoint(NSMinX(button.frame) - NSMinX(groupFrame), NSMinY(button.frame) - NSMinY(groupFrame));
    };

    return {
        groupFrame.size,
        originInGroup(views.Close),
        originInGroup(views.Miniaturize),
        originInGroup(views.Zoom)
    };
}

AvnSize WindowImpl::CalculateTrafficLightSize() {
    TrafficLightViews views;
    if (!CanCustomizeTrafficLightLayout() || !TryGetTrafficLightViews(&views)) {
        return {};
    }

    // AppKit retiles one button at a time, so live frames are only consistent while AppKit owns the buttons.
    auto group = _trafficLightState == TrafficLightState::Custom
        ? _trafficLightGroup
        : MeasureTrafficLightGroup(views);
    return { group.Size.width, group.Size.height };
}

void WindowImpl::ApplyCustomTrafficLightLayout(const TrafficLightViews& views) {
    NSRect containerFrame = views.Container.frame;
    // Equal bottom padding keeps the titlebar tracking geometry around the moved buttons.
    containerFrame.size.height = _trafficLightGroup.Size.height + 2 * _trafficLightPosition.Y;
    containerFrame.origin.y = NSHeight(Window.frame) - NSHeight(containerFrame);
    [views.Container setFrame:containerFrame];

    // Write absolute origins from the measured group shape. AppKit may be midway through retiling,
    // so the current frames can't be used as a reference.
    NSRect titlebarBounds = views.Titlebar.bounds;
    bool isRightToLeft = GetTitlebarFlowDirection() == AvnFlowDirectionRightToLeft;
    NSPoint groupOrigin = NSMakePoint(
        isRightToLeft
            ? NSWidth(titlebarBounds) - _trafficLightPosition.X - _trafficLightGroup.Size.width
            : _trafficLightPosition.X,
        NSHeight(titlebarBounds) - _trafficLightPosition.Y - _trafficLightGroup.Size.height);
    auto moveButton = [&](NSButton* button, NSPoint originInGroup) {
        [button setFrameOrigin:NSMakePoint(groupOrigin.x + originInGroup.x, groupOrigin.y + originInGroup.y)];
    };

    moveButton(views.Close, _trafficLightGroup.CloseOrigin);
    moveButton(views.Miniaturize, _trafficLightGroup.MiniaturizeOrigin);
    moveButton(views.Zoom, _trafficLightGroup.ZoomOrigin);

    UpdateTrafficLightTrackingAreas(views);
}

void WindowImpl::RestoreSystemTrafficLightLayout(const TrafficLightViews& views) {
    // Changing the title visibility makes AppKit retile the titlebar for the current window size.
    // Cached frames would be stale after a resize.
    NSWindowTitleVisibility visibility = Window.titleVisibility;
    Window.titleVisibility = visibility == NSWindowTitleVisible ? NSWindowTitleHidden : NSWindowTitleVisible;
    Window.titleVisibility = visibility;

    UpdateTrafficLightTrackingAreas(views);
}

bool WindowImpl::TryGetTrafficLightViews(TrafficLightViews* views) {
    if (Window == nil || views == nullptr) {
        return false;
    }

    // Standard buttons can be removed and recreated, so always fetch the live views.
    NSButton* closeButton = [Window standardWindowButton:NSWindowCloseButton];
    NSButton* miniaturizeButton = [Window standardWindowButton:NSWindowMiniaturizeButton];
    NSButton* zoomButton = [Window standardWindowButton:NSWindowZoomButton];
    NSView* titlebar = closeButton.superview;

    if (closeButton == nil ||
        miniaturizeButton == nil ||
        zoomButton == nil ||
        titlebar == nil ||
        miniaturizeButton.superview != titlebar ||
        zoomButton.superview != titlebar ||
        titlebar.superview == nil) {
        return false;
    }

    views->Container = titlebar.superview;
    views->Titlebar = titlebar;
    views->Close = closeButton;
    views->Miniaturize = miniaturizeButton;
    views->Zoom = zoomButton;
    return true;
}

void WindowImpl::UpdateTrafficLightTrackingAreas(const TrafficLightViews& views) {
    // Moving frames alone can leave AppKit's hover and hit-test geometry stale.
    [views.Container updateTrackingAreas];
    [views.Close updateTrackingAreas];
    [views.Miniaturize updateTrackingAreas];
    [views.Zoom updateTrackingAreas];
}

extern ComPtr<IAvnWindow> CreateAvnWindow(const ComPtr<IAvnWindowEvents>& events)
{
    return comnew<WindowImpl>(events);
}
