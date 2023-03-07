//
// Created by Dan Walmsley on 06/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//


#import <AppKit/AppKit.h>
#import "WindowProtocol.h"
#import "WindowBaseImpl.h"

#ifdef IS_NSPANEL
#define BASE_CLASS NSPanel
#define CLASS_NAME AvnPanel
#else
#define BASE_CLASS NSWindow
#define CLASS_NAME AvnWindow
#endif

#import <AppKit/AppKit.h>
#include "common.h"
#include "menu.h"
#include "WindowBaseImpl.h"
#include "WindowImpl.h"
#include "AvnView.h"
#include "WindowInterfaces.h"
#include "PopupImpl.h"

@implementation CLASS_NAME
{
    ComPtr<WindowBaseImpl> _parent;
    bool _closed;
    bool _isEnabled;
    bool _canBecomeKeyWindow;
    bool _isExtended;
    bool _isTransitioningToFullScreen;
    AvnMenu* _menu;
}

-(void) setIsExtended:(bool)value;
{
    _isExtended = value;
}

-(bool) isDialog
{
    return _parent->IsModal();
}

-(double) getExtendedTitleBarHeight
{
    if(_isExtended)
    {
        for (id subview in self.contentView.superview.subviews)
        {
            if ([subview isKindOfClass:NSClassFromString(@"NSTitlebarContainerView")])
            {
                NSView *titlebarView = [subview subviews][0];

                return (double)titlebarView.frame.size.height;
            }
        }

        return -1;
    }
    else
    {
        return 0;
    }
}

- (void)performClose:(id _Nullable )sender
{
    if([[self delegate] respondsToSelector:@selector(windowShouldClose:)])
    {
        if(![[self delegate] windowShouldClose:self]) return;
    }
    else if([self respondsToSelector:@selector(windowShouldClose:)])
    {
        if(![self windowShouldClose:self]) return;
    }

    [self close];
}

- (void)pollModalSession:(nonnull NSModalSession)session
{
    auto response = [NSApp runModalSession:session];

    if(response == NSModalResponseContinue)
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            [self pollModalSession:session];
        });
    }
    else if (!_closed)
    {
        [self orderOut:self];
        [NSApp endModalSession:session];
    }
}

-(void) showWindowMenuWithAppMenu
{
    if(_menu != nullptr)
    {
        auto appMenuItem = ::GetAppMenuItem();

        if(appMenuItem != nullptr)
        {
            auto appMenu = [appMenuItem menu];

            [appMenu removeItem:appMenuItem];

            [_menu insertItem:appMenuItem atIndex:0];

            [_menu setHasGlobalMenuItem:true];
        }

        [NSApp setMenu:_menu];
    }
    else
    {
        [self showAppMenuOnly];
    }
}

-(void) showAppMenuOnly
{
    auto appMenuItem = ::GetAppMenuItem();

    if(appMenuItem != nullptr)
    {
        auto appMenu = ::GetAppMenu();

        auto nativeAppMenu = dynamic_cast<AvnAppMenu*>(appMenu);

        [[appMenuItem menu] removeItem:appMenuItem];

        if(_menu != nullptr)
        {
            [_menu setHasGlobalMenuItem:false];
        }

        [nativeAppMenu->GetNative() addItem:appMenuItem];

        [NSApp setMenu:nativeAppMenu->GetNative()];
    }
}

-(void) applyMenu:(AvnMenu *_Nullable)menu
{
    if(menu == nullptr)
    {
        menu = [AvnMenu new];
    }

    _menu = menu;
}

-(CLASS_NAME*_Nonnull)  initWithParent: (WindowBaseImpl*_Nonnull) parent contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
{
    // https://jameshfisher.com/2020/07/10/why-is-the-contentrect-of-my-nswindow-ignored/
    // create nswindow with specific contentRect, otherwise we wont be able to resize the window
    // until several ms after the window is physically on the screen.
    self = [super initWithContentRect:contentRect styleMask: styleMask backing:NSBackingStoreBuffered defer:false];

    [self setReleasedWhenClosed:false];
    _parent = parent;
    [self setDelegate:self];
    _closed = false;
    _isEnabled = true;

    [self setOpaque:NO];

    _isExtended = false;
    _isTransitioningToFullScreen = false;

    if(self.isDialog)
    {
        [self setCollectionBehavior:NSWindowCollectionBehaviorCanJoinAllSpaces|NSWindowCollectionBehaviorFullScreenAuxiliary];
    }

    return self;
}

- (BOOL)windowShouldClose:(NSWindow *_Nonnull)sender
{
    auto window = dynamic_cast<WindowImpl*>(_parent.getRaw());

    if(window != nullptr)
    {
        return !window->WindowEvents->Closing();
    }

    return true;
}

- (void)windowDidChangeBackingProperties:(NSNotification *_Nonnull)notification
{
    [self backingScaleFactor];
}



- (void)windowWillClose:(NSNotification *_Nonnull)notification
{
    _closed = true;
    if(_parent)
    {
        ComPtr<WindowBaseImpl> parent = _parent;
        _parent = NULL;
        
        auto window = dynamic_cast<WindowImpl*>(parent.getRaw());
        
        if(window != nullptr)
        {
            window->SetParent(nullptr);
        }
        
        parent->BaseEvents->Closed();
        [parent->View onClosed];
    }
}

// From chromium:
//
// > The delegate or the window class should implement this method so that
// > -[NSWindow isZoomed] can be then determined by whether or not the current
// > window frame is equal to the zoomed frame.
//
// If we don't implement this, then isZoomed always returns true for a non-
// resizable window ¯\_(ツ)_/¯
- (NSRect)windowWillUseStandardFrame:(NSWindow*)window
                        defaultFrame:(NSRect)newFrame {
  return newFrame;
}

-(BOOL)canBecomeKeyWindow
{
    if(_canBecomeKeyWindow && !_closed)
    {
        // If the window has a child window being shown as a dialog then don't allow it to become the key window.
        auto parent = dynamic_cast<WindowImpl*>(_parent.getRaw());
        
        if(parent != nullptr)
        {
            return parent->CanBecomeKeyWindow();
        }

        return true;
    }
    
    return false;
}

#ifndef IS_NSPANEL
-(BOOL)canBecomeMainWindow
{
    return true;
}
#endif

-(void)setCanBecomeKeyWindow:(bool)value
{
    _canBecomeKeyWindow = value;
}

-(bool)shouldTryToHandleEvents
{
    return _isEnabled;
}

-(void) setEnabled:(bool)enable
{
    _isEnabled = enable;
}

-(void)becomeKeyWindow
{
    [self showWindowMenuWithAppMenu];

    if(_parent != nullptr)
    {
        _parent->BaseEvents->Activated();
    }

    [super becomeKeyWindow];
}

- (void)windowDidBecomeKey:(NSNotification *_Nonnull)notification
{
    if (_parent == nullptr)
        return;
    
    _parent->BringToFront();
    
    dispatch_async(dispatch_get_main_queue(), ^{
        @try {
            [self invalidateShadow];
            if (self->_parent != nullptr)
                self->_parent->BringToFront();
        }
        @finally{
        }
    });
}

- (void)windowDidMiniaturize:(NSNotification *_Nonnull)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());

    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}

- (void)windowDidDeminiaturize:(NSNotification *_Nonnull)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());

    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}

- (void)windowDidResize:(NSNotification *_Nonnull)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());

    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}

- (void)windowWillExitFullScreen:(NSNotification *_Nonnull)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());

    if(parent != nullptr)
    {
        parent->StartStateTransition();
    }
}

- (void)windowDidExitFullScreen:(NSNotification *_Nonnull)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());

    if(parent != nullptr)
    {
        parent->EndStateTransition();

        if(parent->Decorations() != SystemDecorationsFull && parent->WindowState() == Maximized)
        {
            NSRect screenRect = [[self screen] visibleFrame];
            [self setFrame:screenRect display:YES];
        }

        if(parent->WindowState() == Minimized)
        {
            [self miniaturize:nullptr];
        }

        parent->WindowStateChanged();
    }
}

- (void)windowWillEnterFullScreen:(NSNotification *_Nonnull)notification
{
    _isTransitioningToFullScreen = true;
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());

    if(parent != nullptr)
    {
        parent->StartStateTransition();
    }
}

- (void)windowDidEnterFullScreen:(NSNotification *_Nonnull)notification
{
    _isTransitioningToFullScreen = false;
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());

    if(parent != nullptr)
    {
        parent->EndStateTransition();
        parent->WindowStateChanged();
    }
}

- (BOOL)windowShouldZoom:(NSWindow *_Nonnull)window toFrame:(NSRect)newFrame
{
    return _parent->CanZoom();
}

-(void)windowDidResignKey:(NSNotification *)notification
{
    if(_parent)
        _parent->BaseEvents->Deactivated();

    [self showAppMenuOnly];
    
    [self invalidateShadow];
}

- (void)windowDidMove:(NSNotification *_Nonnull)notification
{
    AvnPoint position;

    if(_parent != nullptr)
    {
        auto cparent = dynamic_cast<WindowImpl*>(_parent.getRaw());

        if(cparent != nullptr)
        {
            if(!cparent->IsShown())
            {
                return;
            }

            if(cparent->WindowState() == Maximized)
            {
                cparent->SetWindowState(Normal);
            }
        }

        _parent->GetPosition(&position);
        _parent->BaseEvents->PositionChanged(position);
    }
}

- (AvnPoint) translateLocalPoint:(AvnPoint)pt
{
    pt.Y = [self frame].size.height - pt.Y;
    return pt;
}

- (void)sendEvent:(NSEvent *_Nonnull)event
{
    [super sendEvent:event];

    /// This is to detect non-client clicks. This can only be done on Windows... not popups, hence the dynamic_cast.
    if(_parent != nullptr && dynamic_cast<WindowImpl*>(_parent.getRaw()) != nullptr)
    {
        switch(event.type)
        {
            case NSEventTypeLeftMouseDown:
            {
                AvnView* view = _parent->View;
                NSPoint windowPoint = [event locationInWindow];
                NSPoint viewPoint = [view convertPoint:windowPoint fromView:nil];

                if (!NSPointInRect(viewPoint, view.bounds))
                {
                    auto avnPoint = [AvnView toAvnPoint:windowPoint];
                    auto point = [self translateLocalPoint:avnPoint];
                    AvnVector delta = { 0, 0 };

                    _parent->BaseEvents->RawMouseEvent(NonClientLeftButtonDown, static_cast<uint32>([event timestamp] * 1000), AvnInputModifiersNone, point, delta);
                }
                
                if(!_isTransitioningToFullScreen)
                {
                    _parent->BringToFront();
                }
            }
            break;

            case NSEventTypeMouseEntered:
            {
                _parent->UpdateCursor();
            }
                break;

            case NSEventTypeMouseExited:
            {
                [[NSCursor arrowCursor] set];
            }
                break;

            default:
                break;
        }
    }
}

- (void)disconnectParent {
    _parent = nullptr;
}

@end

