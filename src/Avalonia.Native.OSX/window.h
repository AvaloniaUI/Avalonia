// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#ifndef window_h
#define window_h
#import "ArcTraceWindow.h"
class WindowBaseImpl;

@interface AvnView : NSView<NSTextInputClient>
-(AvnView* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent;
-(NSEvent* _Nonnull) lastMouseDownEvent;
-(AvnPoint) translateLocalPoint:(AvnPoint)pt;
-(void) setSwRenderedFrame: (AvnFramebuffer* _Nonnull) fb dispose: (IUnknown* _Nonnull) dispose;
@end

@interface AvnWindow : ArcTraceWindow <NSWindowDelegate>
-(AvnWindow* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent;
-(void) setCanBecomeKeyAndMain;
-(void) pollModalSession: (NSModalSession _Nonnull) session;
@end

struct INSWindowHolder
{
    virtual AvnWindow* _Nonnull GetNSWindow () = 0;
};

struct IWindowStateChanged
{
    virtual void WindowStateChanged () = 0;
};

#endif /* window_h */
